using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker.Http;
using SFA.DAS.AODP.Common.Storage;
using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Jobs.Helpers;

namespace SFA.DAS.AODP.Jobs.Functions;

/**
 * On-demand backfill for FileRecords that predate tracking, or were otherwise never captured
 * (e.g. a blob moved/restored outside the app). Source of truth is blob storage, not the DB —
 * this only ever inserts, never updates an existing record, and never removes or flags a
 * record whose blob is missing. A Malicious record with its blob already deleted by the scan
 * pipeline is the intended end state, not an orphan to clean up.
 *
 * Categories are caller-specified (query string, comma-separated) so this can be pointed at
 * just the gap that needs filling, or re-run safely and repeatedly as new gaps turn up.
 * 
 * Defaults to all supported categories if none are specified in the request.
 * 
 * FundingOutput is deliberately unsupported — those files are generated and returned
 * immediately, never persisted for later read access, so they sit outside the scanning model
 * entirely.
 *
 * QuestionUpload/MessageAttachment legitimately have many records per category, so they're
 * matched per blob path. Pldns/DefundingList/ApprovedFunding/ArchivedFunding are single-record
 * categories — matched per category, and skipped entirely once any record exists, regardless
 * of how many blob objects a container actually holds (an old, superseded upload left behind
 * in storage must not spawn a second record for the same category).
 *
 * */
public class FileRecordSyncFunction
{
    private static readonly FileCategory[] AllSupportedCategories =
    [
        FileCategory.QuestionUpload,
        FileCategory.MessageAttachment,
        FileCategory.Pldns,
        FileCategory.DefundingList,
        FileCategory.ApprovedFunding,
        FileCategory.ArchivedFunding
    ];

    private static readonly HashSet<FileCategory> SingleRecordCategories =
    [
        FileCategory.Pldns,
        FileCategory.DefundingList,
        FileCategory.ApprovedFunding,
        FileCategory.ArchivedFunding
    ];

    private readonly ILogger<FileRecordSyncFunction> _logger;
    private readonly IFileRecordRepository _fileRepository;
    private readonly BlobServiceClient _blobServiceClient;

    public FileRecordSyncFunction(
        ILogger<FileRecordSyncFunction> logger,
        IFileRecordRepository fileRepository,
        BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _blobServiceClient = blobServiceClient;
    }

    [Function("FileRecordSyncFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "gov/file-records/sync")] HttpRequestData req)
    {
        var categories = ParseCategories(req);

        if (categories.Count == 0)
        {
            return new BadRequestObjectResult(
                $"No valid categories supplied. Supported: {string.Join(", ", AllSupportedCategories)}");
        }

        var created = 0;
        var skipped = 0;

        foreach (var category in categories)
        {
            var result = SingleRecordCategories.Contains(category)
                ? await SyncSingleRecordCategoryAsync(category)
                : await SyncMultiRecordCategoryAsync(category);

            created += result.Created;
            skipped += result.Skipped;
        }

        var message = $"[FileRecordSyncFunction] -> {created} FileRecord(s) created, {skipped} blob(s) already tracked (categories: {string.Join(", ", categories)}).";
        _logger.LogInformation("{Message}", message);
        return new OkObjectResult(message);
    }

    private static List<FileCategory> ParseCategories(HttpRequestData req)
    {
        var raw = req.Query["categories"];

        if (string.IsNullOrWhiteSpace(raw))
        {
            return AllSupportedCategories.ToList();
        }

        var categories = new List<FileCategory>();

        foreach (var part in raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<FileCategory>(part, ignoreCase: true, out var category)
                && AllSupportedCategories.Contains(category))
            {
                categories.Add(category);
            }
        }

        return categories;
    }

    private async Task<(int Created, int Skipped)> SyncSingleRecordCategoryAsync(FileCategory category)
    {
        var existing = await _fileRepository.GetByCategoryAsync(category);

        if (existing != null)
        {
            _logger.LogInformation("[FileRecordSyncFunction] -> {Category} already tracked — skipping.", category);
            return (0, 1);
        }

        string container;
        string? blobName;

        switch (category)
        {
            case FileCategory.ApprovedFunding:
                container = BlobStoragePaths.ContainerFundingImport;
                blobName = BlobStoragePaths.ApprovedFundingFileName;
                break;
            case FileCategory.ArchivedFunding:
                container = BlobStoragePaths.ContainerFundingImport;
                blobName = BlobStoragePaths.ArchivedFundingFileName;
                break;
            case FileCategory.Pldns:
                container = BlobStoragePaths.ContainerImportFiles;
                blobName = await FindLatestBlobUnderFolderAsync(container, BlobStoragePaths.FolderPldns);
                break;
            case FileCategory.DefundingList:
                container = BlobStoragePaths.ContainerImportFiles;
                blobName = await FindLatestBlobUnderFolderAsync(container, BlobStoragePaths.FolderDefundingList);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(category), category, "Not a single-record category.");
        }

        var containerClient = _blobServiceClient.GetBlobContainerClient(container);

        if (blobName == null)
        {
            _logger.LogWarning("[FileRecordSyncFunction] -> No blob found for {Category} in {Container} — nothing to sync.", category, container);
            return (0, 0);
        }

        var blobClient = containerClient.GetBlobClient(blobName);

        if (!await blobClient.ExistsAsync())
        {
            _logger.LogWarning("[FileRecordSyncFunction] -> Blob {Container}/{Path} for {Category} no longer exists — nothing to sync.", container, blobName, category);
            return (0, 0);
        }

        await CreateRecordAsync(category, container, blobName, blobClient);
        return (1, 0);
    }

    private async Task<string?> FindLatestBlobUnderFolderAsync(string container, string folder)
    {
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);
        var prefix = folder + "/";

        string? latestName = null;
        var latestModified = DateTimeOffset.MinValue;

        await foreach (var blob in containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, prefix, CancellationToken.None))
        {
            if (blob.Properties.LastModified is { } modified && modified > latestModified)
            {
                latestModified = modified;
                latestName = blob.Name;
            }
        }

        return latestName;
    }

    private async Task<(int Created, int Skipped)> SyncMultiRecordCategoryAsync(FileCategory category)
    {
        var container = BlobStoragePaths.ContainerFiles;
        var containerClient = _blobServiceClient.GetBlobContainerClient(container);

        var created = 0;
        var skipped = 0;

        await foreach (var blob in containerClient.GetBlobsAsync(BlobTraits.None, BlobStates.None, null, CancellationToken.None))
        {
            var (parsedCategory, _, _, _) = BlobPathParser.ParseBlobPath(container, blob.Name);

            if (parsedCategory != category)
            {
                continue;
            }

            var existing = await _fileRepository.GetByPathAsync(container, blob.Name);

            if (existing != null)
            {
                skipped++;
                continue;
            }

            await CreateRecordAsync(category, container, blob.Name, containerClient.GetBlobClient(blob.Name));
            created++;
        }

        return (created, skipped);
    }

    private async Task CreateRecordAsync(FileCategory category, string container, string blobPath, BlobClient blobClient)
    {
        var properties = await blobClient.GetPropertiesAsync();
        var existingScanResult = await TryGetExistingScanResultAsync(blobClient);

        var record = new FileRecord
        {
            Id = Guid.NewGuid(),
            FileCategory = category,
            FileName = blobPath.Split('/').Last(),
            ContentType = properties.Value.ContentType,
            BlobContainer = container,
            BlobPath = blobPath,
            UploadedByDisplayName = "FileRecordSync",
            UploadedAt = DateTime.UtcNow,
            ScanResult = existingScanResult ?? MalwareScanStatus.NotScanned,
            LastScanAt = existingScanResult != null ? DateTime.UtcNow : null
        };

        await _fileRepository.InsertAsync(record);

        if (existingScanResult == null)
        {
            _logger.LogWarning(
                "[FileRecordSyncFunction] -> Created record for {Container}/{Path} with no existing scan tag — remains NotScanned until a scan is triggered.",
                container, blobPath);
        }
        else
        {
            _logger.LogInformation(
                "[FileRecordSyncFunction] -> Created record for {Container}/{Path} from existing tag: {ScanResult}.",
                container, blobPath, existingScanResult);
        }
    }

    private static async Task<MalwareScanStatus?> TryGetExistingScanResultAsync(BlobClient blobClient)
    {
        var tagResponse = await blobClient.GetTagsAsync();

        return tagResponse.Value.Tags.TryGetValue(MalwareScanResultMapper.ScanResultTagKey, out var scanResult)
            ? MalwareScanResultMapper.Map(scanResult)
            : null;
    }
}
