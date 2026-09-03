using Azure.Storage.Blobs;
using SFA.DAS.AODP.Common.Storage;
using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Jobs.Helpers;

namespace SFA.DAS.AODP.Jobs.Functions;

/**
 * The funded-import files (approved.csv / archived.csv) are dropped into blob storage manually,
 * outside the app entirely, so there is no app code path to reset their tracked scan status on
 * re-upload. This trigger fires on blob create/overwrite and resets the matching FileRecord back
 * to NotScanned, so FileProcessingService.GetReadyFileAsync can't treat a new file as already
 * scanned just because a previous version of it was.
 *
 * When a record already exists for the category, ScanResult is always reset to NotScanned rather
 * than read from the blob's scan-result tag: tags are not cleared on content overwrite, so any tag
 * present at this point describes the previous version of the file, not the one that just landed.
 *
 * When no record exists yet, that reasoning doesn't hold — there's no prior write we know just
 * happened, so the blob may simply be an already-scanned file our tracking has never seen before
 * (e.g. this trigger's first run against a pre-existing blob). In that case we read the current
 * tag and trust it, since forcing NotScanned would strand a genuinely clean file outside
 * DefenderScanReconciliationFunction's 7-day lookback with nothing left to ever re-scan it.
 * */
public class FundedImportBlobTriggerFunction
{
    private readonly ILogger<FundedImportBlobTriggerFunction> _logger;
    private readonly IFileRecordRepository _fileRepository;
    private readonly BlobServiceClient _blobServiceClient;

    public FundedImportBlobTriggerFunction(
        ILogger<FundedImportBlobTriggerFunction> logger,
        IFileRecordRepository fileRepository,
        BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _blobServiceClient = blobServiceClient;
    }

    [Function("FundedImportBlobTriggerFunction")]
    public async Task Run(
        [BlobTrigger(BlobStoragePaths.ContainerFundingImport + "/{name}", Connection = "Storage")] string blob,
        string name)
    {
        var category = MapFileNameToCategory(name);

        if (category == null)
        {
            _logger.LogInformation(
                "Ignoring blob {Name} in {Container} — does not match a known funded-import file name.",
                name, BlobStoragePaths.ContainerFundingImport);
            return;
        }

        _logger.LogInformation(
            "Funded import file {Name} uploaded — resetting scan status for {Category}.",
            name, category);

        var container = _blobServiceClient.GetBlobContainerClient(BlobStoragePaths.ContainerFundingImport);
        var blobClient = container.GetBlobClient(name);

        if (!await blobClient.ExistsAsync())
        {
            _logger.LogWarning(
                "Blob {Name} in {Container} no longer exists — skipping, the trigger likely fired after the blob was deleted.",
                name, BlobStoragePaths.ContainerFundingImport);
            return;
        }

        var blobProperties = await blobClient.GetPropertiesAsync();

        var uploadedAt = blobProperties.Value.LastModified.UtcDateTime;
        var contentType = blobProperties.Value.ContentType;

        // Read as late as possible, right before deciding what to write, to keep the window
        // small for another invocation (e.g. a rapid second overwrite) racing this one.
        var fileRecord = await _fileRepository.GetByCategoryAsync(category.Value);

        if (fileRecord != null && fileRecord.UploadedAt >= uploadedAt)
        {
            // A version at least as new as this one has already been recorded — this invocation
            // is processing a stale write that finished later than a newer one. Skip rather than
            // overwrite the newer, already-correct result with data about an older version.
            _logger.LogInformation(
                "Skipping {Category} — a result for a version at least as new (recorded UploadedAt {RecordedAt:o}) is already saved.",
                category, fileRecord.UploadedAt);
            return;
        }

        if (fileRecord == null)
        {
            var existingScanResult = await TryGetExistingScanResultAsync(blobClient);

            fileRecord = new FileRecord
            {
                Id = Guid.NewGuid(),
                FileCategory = category.Value,
                FileName = name,
                ContentType = contentType,
                BlobContainer = BlobStoragePaths.ContainerFundingImport,
                BlobPath = name,
                UploadedByDisplayName = "DfEStaffUser",
                UploadedAt = uploadedAt,
                ScanResult = existingScanResult ?? MalwareScanStatus.NotScanned,
                LastScanAt = existingScanResult != null ? DateTime.UtcNow : null
            };

            await _fileRepository.InsertAsync(fileRecord);
        }
        else
        {
            fileRecord.FileName = name;
            fileRecord.ContentType = contentType;
            fileRecord.BlobContainer = BlobStoragePaths.ContainerFundingImport;
            fileRecord.BlobPath = name;
            fileRecord.UploadedByDisplayName = "DfEStaffUser";
            fileRecord.UploadedAt = uploadedAt;
            fileRecord.ScanResult = MalwareScanStatus.NotScanned;
            fileRecord.LastScanAt = null;

            await _fileRepository.UpdateAsync(fileRecord);
        }
    }

    private static async Task<MalwareScanStatus?> TryGetExistingScanResultAsync(BlobClient blobClient)
    {
        var tagResponse = await blobClient.GetTagsAsync();

        return tagResponse.Value.Tags.TryGetValue(MalwareScanResultMapper.ScanResultTagKey, out var scanResult)
            ? MalwareScanResultMapper.Map(scanResult)
            : null;
    }

    private static FileCategory? MapFileNameToCategory(string blobName)
    {
        if (string.Equals(blobName, BlobStoragePaths.ApprovedFundingFileName, StringComparison.OrdinalIgnoreCase))
        {
            return FileCategory.ApprovedFunding;
        }

        if (string.Equals(blobName, BlobStoragePaths.ArchivedFundingFileName, StringComparison.OrdinalIgnoreCase))
        {
            return FileCategory.ArchivedFunding;
        }

        return null;
    }
}
