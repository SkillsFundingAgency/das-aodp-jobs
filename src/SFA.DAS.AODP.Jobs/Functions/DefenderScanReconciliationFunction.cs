using Azure;
using Azure.Storage.Blobs;
using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Jobs.Helpers;
using FeatureManagementOptions = SFA.DAS.AODP.Jobs.FeatureManagement.FeatureManagementOptions;

namespace SFA.DAS.AODP.Jobs.Functions;

/**
 * Reconciliation safety net for scan results, not the primary update path — DefenderScanResultFunction
 * (Event Grid) is. This exists because Event Grid delivery isn't guaranteed: a dropped or dead-lettered
 * event currently has no other recovery path, and would otherwise leave a record stuck at NotScanned
 * permanently, with nothing left to ever re-check it.
 *
 * Runs nightly rather than near-real-time, checking blob tags directly (a channel independent of
 * Event Grid) for anything still pending. The lookback window is deliberately wider than the run
 * interval so a late or occasionally-skipped run doesn't create a permanent gap — the cutoff only
 * ever looks backward from "now," not forward from the last successful run.
 * */
public class DefenderScanReconciliationFunction
{

    private readonly ILogger<DefenderScanReconciliationFunction> _logger;
    private readonly IFileRecordRepository _fileRepository;
    private readonly BlobServiceClient _blobServiceClient;
    private readonly FeatureManagementOptions _features;

    public DefenderScanReconciliationFunction(
        ILogger<DefenderScanReconciliationFunction> logger,
        IFileRecordRepository fileRepository,
        BlobServiceClient blobServiceClient,
        IOptionsSnapshot<FeatureManagementOptions> features)
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _blobServiceClient = blobServiceClient;
        _features = features.Value;
    }

    [Function("DefenderScanReconciliationFunction")]
    public async Task Run([TimerTrigger("0 0 2 * * *")] TimerInfo timer)
    {
        if(_features.DefenderPollingEnabled)
        {
            _logger.LogInformation("Scan reconciliation started at {Time}", DateTime.UtcNow);

            var cutoff = DateTime.UtcNow.AddDays(-7);

            //Get files from db where scan result is still pending and
            //uploaded within the last 7 days
            var pendingFiles = await _fileRepository.GetPendingScanAsync(cutoff);

            foreach (var file in pendingFiles)
            {
                await ProcessFile(file);
            }

            _logger.LogInformation("Scan reconciliation completed at {Time}", DateTime.UtcNow);
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

            if (!tags.TryGetValue(MalwareScanResultMapper.ScanResultTagKey, out var scanResult))
            {
                _logger.LogInformation("Blob {Path} has no {ScanResultTag} tag yet", file.BlobPath, MalwareScanResultMapper.ScanResultTagKey);
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
        return MalwareScanResultMapper.Map(scanResult) ?? MalwareScanStatus.NotScanned;
    }
}
