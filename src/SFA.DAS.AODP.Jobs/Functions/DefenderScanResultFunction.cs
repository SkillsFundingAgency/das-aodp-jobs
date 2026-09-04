using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Jobs.Helpers;

/**
 * Handles Event Grid notifications to check file scan status in blob storage.
 *
 * Never creates a FileRecord — every category already has a deliberate creator elsewhere
 * (the upload flow for QuestionUpload/MessageAttachment and Pldns/DefundingList,
 * FundedImportBlobTriggerFunction's first-run capture for ApprovedFunding/ArchivedFunding,
 * the sync/backfill function for historical files). A scan event arriving with no tracked
 * record means something upstream hasn't created it yet, not necessarily that it never will —
 * confirmed in practice: a scan can complete and its event arrive before the upload flow's own
 * record-creation call has finished, since the two aren't sequenced against each other.
 *
 * Throwing here (rather than logging and returning) is deliberate: it makes Event Grid treat
 * the delivery as failed and retry it later with backoff, which naturally resolves the race
 * once the record catches up, without this function needing any hand-rolled wait or retry
 * logic of its own.
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
            _logger.LogWarning(
                "No FileRecord found for {Container}/{Path} — the upload flow, the funded-import " +
                "trigger, or the sync function may not have created it yet. Throwing so Event Grid " +
                "retries this delivery rather than discarding it.",
                containerName, blobPath);

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

    private static string NormaliseETag(string eTag) => eTag.Trim('"');

    private MalwareScanStatus MapScanResult(string? scanResult)
    {
        return MalwareScanResultMapper.Map(scanResult) ?? MalwareScanStatus.NotScanned;
    }
}
