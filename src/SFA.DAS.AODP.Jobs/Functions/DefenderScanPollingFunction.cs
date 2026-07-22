using Azure;
using Azure.Storage.Blobs;
using SFA.DAS.AODP.Data.Entities.Files;
using FeatureManagementOptions = SFA.DAS.AODP.Jobs.FeatureManagement.FeatureManagementOptions;

namespace SFA.DAS.AODP.Jobs.Functions;

/**
 * Retrieves unscanned file records from the database , checks their current scan status in blob storage
 * and updates status in the database. If a file is found to be malicious, it is deleted from blob storage
 * but the file remains in the database.
 * */
public class DefenderScanPollingFunction
{

    private readonly ILogger<DefenderScanPollingFunction> _logger;
    private readonly IFileRecordRepository _fileRepository;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly FeatureManagementOptions _features;

    public DefenderScanPollingFunction(
        ILogger<DefenderScanPollingFunction> logger,
        IFileRecordRepository fileRepository,
        BlobServiceClient blobServiceClient,
        IOptionsSnapshot<FeatureManagementOptions> features)
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _blobServiceClient = blobServiceClient;
        _features = features.Value;
    }

    [Function("DefenderScanPollingFunction")]
    public async Task Run([TimerTrigger("0 */3 * * * *")] TimerInfo timer)
    {
        if(_features.DefenderPollingEnabled)
        {
            _logger.LogInformation("Scan polling started at {Time}", DateTime.UtcNow);

            var cutoff = DateTime.UtcNow.AddHours(-24);

            //Get files from db where scan result is still pending and
            //uploaded within the last 24 hours 
            var pendingFiles = await _fileRepository.GetPendingScanAsync(cutoff);

            foreach (var file in pendingFiles)
            {
                await ProcessFile(file);
            }

            _logger.LogInformation("Scan polling completed at {Time}", DateTime.UtcNow);
        }
    }

    private async Task ProcessFile(FileRecord file)
    {
        try
        {
            var container = _blobServiceClient.GetBlobContainerClient(file.BlobContainer);
            var blob = container.GetBlobClient(file.BlobPath);

            var tagResponse = await blob.GetTagsAsync();
            var tags = tagResponse.Value.Tags;

            if (!tags.TryGetValue("Malware Scanning scan result", out var scanResult))
            {
                _logger.LogInformation("Blob {Path} has no ms-scan-result tag yet", file.BlobPath);
                return;
            }

            if (string.IsNullOrWhiteSpace(scanResult))
            {
                _logger.LogInformation("File {Path} still pending scan (no metadata)", file.BlobPath);
                return;
            }

            _logger.LogInformation("Scan Result {scanResult}", scanResult);
            var status = MapScanResult(scanResult);

            if (status == MalwareScanStatus.NotScanned)
            {
                _logger.LogInformation("File {Path} still pending scan (status = NotScanned)", file.BlobPath);
                return;
            }

            file.ScanResult = status;
            file.LastScanAt = DateTime.UtcNow;

            if (status == MalwareScanStatus.Malicious)
            {
                _logger.LogWarning("Malware detected: {Path}", file.BlobPath);
                await blob.DeleteIfExistsAsync();
            }

            await _fileRepository.UpdateAsync(file);

            _logger.LogInformation("Updated {Path} → {Status} (raw metadata = {Raw})",
                file.BlobPath, status, scanResult);
        }
        catch (RequestFailedException ex) when (ex.ErrorCode == "BlobNotFound")
        {
            _logger.LogWarning("Blob missing: {Path}", file.BlobPath);

            file.ScanResult = MalwareScanStatus.Error;
            file.LastScanAt = DateTime.UtcNow;

            await _fileRepository.UpdateAsync(file);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing {Path}", file.BlobPath);
        }
    }

    private MalwareScanStatus MapScanResult(string scanResult)
    {
        return scanResult?.ToLowerInvariant() switch
        {
            "no threats found" => MalwareScanStatus.Clean,
            "malicious" => MalwareScanStatus.Malicious,
            "error" => MalwareScanStatus.Error,
            "unsupported" => MalwareScanStatus.Error,
            "scan timed out" => MalwareScanStatus.Error,
            _ => MalwareScanStatus.NotScanned
        };
    }
}