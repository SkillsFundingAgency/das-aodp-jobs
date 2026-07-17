using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;

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

        var file = await _fileRepository.GetByPathAsync(containerName, blobPath);

        if (file == null)
        {
            _logger.LogWarning("No FileRecord found for blob");
            return;
        }

        var status = MapScanResult(data.ScanResultType);

        file.ScanResult = status;
        file.LastScanAt = DateTime.UtcNow;

        if (status == MalwareScanStatus.Malicious)
        {
            _logger.LogWarning("Malware detected — deleting blob");

            var container = _blobServiceClient.GetBlobContainerClient(containerName);
            var blob = container.GetBlobClient(blobPath);

            await blob.DeleteIfExistsAsync();
        }

        await _fileRepository.UpdateAsync(file);

        _logger.LogInformation("Updated file status to {Status}", status);
    }

    private MalwareScanStatus MapScanResult(string? scanResult)
    {
        return scanResult switch
        {
            "No threats found" => MalwareScanStatus.Clean,
            "Malicious" => MalwareScanStatus.Malicious,
            _ => MalwareScanStatus.NotScanned
        };
    }
}