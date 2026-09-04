using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Infrastructure.Services;
using SFA.DAS.AODP.Jobs.Helpers;

/**
 * Handles Event Grid notifications to check file scan status in blob storage.
 *
 * Parses the scan event for the blob it describes, discards it if that blob has since been
 * overwritten (eTag mismatch) or no longer exists, then looks up the matching FileRecord and
 * updates its scan result — deleting the blob if the result is Malicious. 
 * 
 * Never creates a FileRecord; every category already has a deliberate creator elsewhere (the upload flow
 * for QuestionUpload/MessageAttachment/Pldns/DefundingList, and the sync/backfill function for
 * ApprovedFunding/ArchivedFunding and historical files generally).
 *
 * A missing record doesn't necessarily mean nothing ever will create it — confirmed in practice,
 * a scan can complete and its event arrive before the upload flow's own record-creation call has
 * finished, since the two aren't sequenced against each other. So a missing record gets a few
 * quick retries first, which resolves that common case — the record showing up a moment later —
 * without waiting on Event Grid's own redelivery at all. If it's still missing after that,
 * throwing lets Event Grid retry the delivery later with backoff, which covers anything slower
 * than a few seconds.
 * The retry delays stay well under Event Grid's own 30-second per-attempt response window, so
 * this and Event Grid's redelivery never end up racing each other over the same event.
 * */
public class DefenderScanResultFunction
{
    private static readonly TimeSpan[] RecordLookupRetryDelays =
    [
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2)
    ];

    private readonly ILogger<DefenderScanResultFunction> _logger;
    private readonly IFileRecordRepository _fileRepository;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly IDelayService _delayService;

    public DefenderScanResultFunction(
        ILogger<DefenderScanResultFunction> logger,
        IFileRecordRepository fileRepository,
        BlobServiceClient blobServiceClient,
        IDelayService delayService)
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _blobServiceClient = blobServiceClient;
        _delayService = delayService;
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

        var fileRecord = await GetRecordWithShortRetryAsync(containerName, blobPath);

        if (fileRecord == null)
        {
            _logger.LogWarning(
                "No FileRecord found for {Container}/{Path} after {Attempts} quick retries — the " +
                "upload flow or the sync function may not have created it yet. Throwing so Event " +
                "Grid retries this delivery rather than discarding it.",
                containerName, blobPath, RecordLookupRetryDelays.Length + 1);

            throw new InvalidOperationException(
                $"No FileRecord found for {containerName}/{blobPath} — retry expected to resolve this once the record exists.");
        }

        fileRecord.ScanResult = status;
        fileRecord.LastScanAt = DateTime.UtcNow;

        await _fileRepository.UpdateAsync(fileRecord);

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

    private async Task<FileRecord?> GetRecordWithShortRetryAsync(string containerName, string blobPath)
    {
        var fileRecord = await _fileRepository.GetByPathAsync(containerName, blobPath);

        foreach (var delay in RecordLookupRetryDelays)
        {
            if (fileRecord != null)
            {
                break;
            }

            await _delayService.DelayAsync(delay);
            fileRecord = await _fileRepository.GetByPathAsync(containerName, blobPath);
        }

        return fileRecord;
    }

    private static string NormaliseETag(string eTag) => eTag.Trim('"');

    private MalwareScanStatus MapScanResult(string? scanResult)
    {
        return MalwareScanResultMapper.Map(scanResult) ?? MalwareScanStatus.NotScanned;
    }
}
