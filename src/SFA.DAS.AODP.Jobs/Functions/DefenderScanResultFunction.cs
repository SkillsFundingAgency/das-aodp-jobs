using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using SFA.DAS.AODP.Common.Storage;
using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Jobs.Helpers;

/**
 * Handles Event Grid notifications to check file scan status in blob storage. Untested.
 * */
public class DefenderScanResultFunction
{
    private readonly ILogger<DefenderScanResultFunction> _logger;
    private readonly IFileRecordRepository _fileRepository;
    private readonly BlobServiceClient _blobServiceClient;

    public DefenderScanResultFunction(
        ILogger<DefenderScanResultFunction> logger,
        IFileRecordRepository fileRepository,
        BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _blobServiceClient = blobServiceClient;
    }

    [Function("DefenderScanResultFunction")]
    public async Task Run([EventGridTrigger] EventGridEvent eventGridEvent)
    {
        _logger.LogInformation("Received Defender scan event");

        var data = eventGridEvent.Data.ToObjectFromJson<DefenderScanEvent>();

        if (data?.BlobUri == null)
        {
            _logger.LogWarning("Invalid event payload");
            return;
        }

        var uri = new Uri(data.BlobUri);

        var containerName = uri.Segments[1].TrimEnd('/');
        var blobPath = string.Join("", uri.Segments.Skip(2));

        _logger.LogInformation("Processing blob: {Container}/{Path}", containerName, blobPath);

        var container = _blobServiceClient.GetBlobContainerClient(containerName);

        if (!await container.ExistsAsync())
        {
            _logger.LogWarning(
                "Blob container '{Container}' does not exist — cannot process Defender event.",
                containerName);

            return;
        }

        var blob = container.GetBlobClient(blobPath);

        if (!await blob.ExistsAsync())
        {
            _logger.LogWarning(
                "Blob does not exist — cannot process Defender event. " +
                "Event may have arrived after blob deletion."
            );
            return;
        }

        var blobProperties = await blob.GetPropertiesAsync();
        var currentETag = blobProperties.Value.ETag.ToString();

        // Categories such as Pldns/DefundingList overwrite the same blob path on every import,
        // so a scan event can arrive after the blob it describes has already been superseded by
        // a newer upload. Comparing the event's eTag against the blob's current eTag lets us
        // discard that stale result rather than misapplying it to the newer file.
        if (!string.IsNullOrEmpty(data.ETag) &&
            !string.Equals(NormaliseETag(currentETag), NormaliseETag(data.ETag), StringComparison.Ordinal))
        {
            _logger.LogWarning(
                "Scan result eTag {EventETag} does not match current blob eTag {CurrentETag} for {Container}/{Path} — " +
                "blob has been overwritten since this scan started; discarding stale result.",
                data.ETag, currentETag, containerName, blobPath);
            return;
        }

        var status = MapScanResult(data.ScanResultType);

        var fileRecord = await _fileRepository.GetByPathAsync(containerName, blobPath);

        if (fileRecord == null)
        {
            _logger.LogWarning("FileRecord missing — creating from blob metadata");

            var parsedBlobPath = ParseBlobPath(containerName, blobPath);

            fileRecord = new FileRecord
            {
                Id = Guid.NewGuid(),
                FileName = Path.GetFileName(blobPath),
                ContentType = blobProperties.Value.ContentType,
                BlobPath = blobPath,
                BlobContainer = containerName,
                FileCategory = parsedBlobPath.Category,
                ApplicationId = parsedBlobPath.ApplicationId,
                MessageId = parsedBlobPath.MessageId,
                QuestionId = parsedBlobPath.QuestionId,
                UploadedAt = blobProperties.Value.CreatedOn.UtcDateTime,
                UploadedByDisplayName = "DfEStaffUser",
                ScanResult = status,
                LastScanAt = DateTime.UtcNow
            };

            await _fileRepository.InsertAsync(fileRecord);
        }
        else
        {
            fileRecord.ScanResult = status;
            fileRecord.LastScanAt = DateTime.UtcNow;

            await _fileRepository.UpdateAsync(fileRecord);
        }

        if (status == MalwareScanStatus.Malicious)
        {
            _logger.LogWarning("Malware detected — deleting blob");

            await blob.DeleteIfExistsAsync();
        }

        _logger.LogInformation(
            "Updated file status to {Status} (raw metadata = {Raw})",
            status,
            data.ScanResultType
        );
    }

    private static string NormaliseETag(string eTag) => eTag.Trim('"');

    private MalwareScanStatus MapScanResult(string? scanResult)
    {
        return MalwareScanResultMapper.Map(scanResult) ?? MalwareScanStatus.NotScanned;
    }

    public static (FileCategory Category, Guid? ApplicationId, Guid? MessageId, Guid? QuestionId) ParseBlobPath(string containerName, string blobPath)
    {
        var segments = blobPath.Split('/', StringSplitOptions.RemoveEmptyEntries);

        FileCategory category = FileCategory.Unknown;
        Guid? applicationId = null;
        Guid? messageId = null;
        Guid? questionId = null;

        // importfilescontainer/DefundingList/{fileId}
        if (containerName.Equals(BlobStoragePaths.ContainerImportFiles, StringComparison.OrdinalIgnoreCase))
        {
            if (segments[0].Equals(BlobStoragePaths.FolderDefundingList, StringComparison.OrdinalIgnoreCase))
            {
                category = FileCategory.DefundingList;
            }
            else if (segments[0].Equals(BlobStoragePaths.FolderPldns, StringComparison.OrdinalIgnoreCase))
            {
                category = FileCategory.Pldns;
            }
        }
        // files/messages/{appId}/{messageId}/{fileId}
        else if (containerName.Equals(BlobStoragePaths.ContainerFiles, StringComparison.OrdinalIgnoreCase))
        {
            if (segments[0].Equals(BlobStoragePaths.FolderMessages, StringComparison.OrdinalIgnoreCase))
            {
                category = FileCategory.MessageAttachment;
                applicationId = Guid.Parse(segments[1]);
                messageId = Guid.Parse(segments[2]);
            }
            else
            {
                // files/{applicationId}/{questionId}/{fileId}
                category = FileCategory.QuestionUpload;
                applicationId = Guid.Parse(segments[0]);
                questionId = Guid.Parse(segments[1]);
            }
        }
        // funded-qualifications-import/approved.csv or archived.csv
        else if (containerName.Equals(BlobStoragePaths.ContainerFundingImport, StringComparison.OrdinalIgnoreCase))
        {
            if (segments[0].Equals(BlobStoragePaths.ApprovedFundingFileName, StringComparison.OrdinalIgnoreCase))
            {
                category = FileCategory.ApprovedFunding;
            }
            else if (segments[0].Equals(BlobStoragePaths.ArchivedFundingFileName, StringComparison.OrdinalIgnoreCase))
            {
                category = FileCategory.ArchivedFunding;
            }
        }
        // funded-qualifications-output/{date}-AOdPApprovedOutputFile.csv
        else if (containerName.Equals(BlobStoragePaths.ContainerFundingOutput, StringComparison.OrdinalIgnoreCase))
        {
            category = FileCategory.FundingOutput;
        }

        return (category, applicationId, messageId, questionId);
    }


}
