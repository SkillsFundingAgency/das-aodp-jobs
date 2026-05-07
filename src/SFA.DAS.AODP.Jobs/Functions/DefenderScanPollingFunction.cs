using Azure;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker.Http;
using SFA.DAS.AODP.Data.Entities.Files;

namespace SFA.DAS.AODP.Jobs.Functions;

/**
 * Retrieves unscanned file records from the database and checks their current scan status in blob storage.
 * */
public class DefenderScanPollingFunction
{

    private readonly ILogger<DefenderScanPollingFunction> _logger;
    private readonly IFileRecordRepository _fileRepository;
    private readonly BlobServiceClient _blobServiceClient;

    public DefenderScanPollingFunction(
        ILogger<DefenderScanPollingFunction> logger,
        IFileRecordRepository fileRepository,
        BlobServiceClient blobServiceClient)
    {
        _logger = logger;
        _fileRepository = fileRepository;
        _blobServiceClient = blobServiceClient;
    }

    [Function("DefenderScanPollingHttp")]
    public async Task<HttpResponseData> RunHttp(
    [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "scanPolling")]
    HttpRequestData req)
    {
        _logger.LogInformation("Manual scan polling triggered at {Time}", DateTime.UtcNow);

        var response = req.CreateResponse();

        try
        {
            var cutoff = DateTime.UtcNow.AddHours(-24);

            var pendingFiles = await _fileRepository.GetPendingScanAsync(cutoff);

            var processed = 0;

            foreach (var file in pendingFiles)
            {
                await ProcessFile(file);
                processed++;
            }

            _logger.LogInformation("Manual scan polling completed. Processed {Count} files", processed);

            response.StatusCode = HttpStatusCode.OK;

            await response.WriteStringAsync(
                $"Scan polling completed. Processed {processed} files.");

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Manual scan polling failed");

            response.StatusCode = HttpStatusCode.InternalServerError;

            await response.WriteStringAsync("Error occurred during scan polling.");

            return response;
        }
    }

    //[Function("DefenderScanPollingFunction")]
    public async Task Run([TimerTrigger("0 */3 * * * *")] TimerInfo timer)
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

    private async Task ProcessFile(FileRecord file)
    {
        try
        {
            var container = _blobServiceClient.GetBlobContainerClient(file.BlobContainer);
            var blob = container.GetBlobClient(file.BlobPath);

            var properties = await blob.GetPropertiesAsync();

            var metadata = properties.Value.Metadata;

            var kvp = metadata.FirstOrDefault(m =>
                m.Key.Contains("scanresult", StringComparison.OrdinalIgnoreCase));

            var scanResult = kvp.Value;

            if (string.IsNullOrEmpty(scanResult))
            {
                return;
            }

            var status = MapScanResult(scanResult);

            if (status == MalwareScanStatus.NotScanned)
            {
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

            _logger.LogInformation("Updated {Path} → {Status}", file.BlobPath, status);
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
            _ => MalwareScanStatus.NotScanned
        };
    }
}