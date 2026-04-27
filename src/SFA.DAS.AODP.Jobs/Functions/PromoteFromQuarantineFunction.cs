using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Azure.Functions.Worker.Http;
using System.Text.Json;

public class PromoteFromQuarantineFunction
{
    private readonly ILogger<PromoteFromQuarantineFunction> _logger;
    private readonly BlobServiceClient _blobService;
    private readonly BlobStorageSettings _settings;

    public PromoteFromQuarantineFunction(
        ILogger<PromoteFromQuarantineFunction> logger,
        BlobServiceClient blobService,
        BlobStorageSettings settings)
    {
        _logger = logger;
        _blobService = blobService;
        _settings = settings;
    }

    [Function("PromoteFromQuarantine")]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post")]
        HttpRequestData req)
    {
        var payload = await JsonDocument.ParseAsync(req.Body);

        if (!payload.RootElement.TryGetProperty("path", out var pathElement))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("Missing 'path' in request body.");
            return badRequest;
        }

        var logicalPath = pathElement.GetString();
        if (string.IsNullOrWhiteSpace(logicalPath))
        {
            var badRequest = req.CreateResponse(HttpStatusCode.BadRequest);
            await badRequest.WriteStringAsync("'path' must not be empty.");
            return badRequest;
        }

        var quarantineContainer =
            _blobService.GetBlobContainerClient(_settings.QuarantineContainerName);
        var safeContainer =
            _blobService.GetBlobContainerClient(_settings.SafeContainerName);

        var sourceBlob = quarantineContainer.GetBlobClient(logicalPath);
        var destinationBlob = safeContainer.GetBlobClient(logicalPath);

        if (!await sourceBlob.ExistsAsync())
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync($"Blob not found in quarantine: {logicalPath}");
            return notFound;
        }

        _logger.LogInformation(
            "Promoting blob from quarantine to safe. Path: {path}",
            logicalPath);

        _logger.LogInformation(
            "COPY DEBUG — source={source}  destination={dest}",
            sourceBlob.Uri,
            destinationBlob.Uri);

        var copyOperation = await destinationBlob.StartCopyFromUriAsync(sourceBlob.Uri);

        BlobProperties properties;
        do
        {
            await Task.Delay(500);
            properties = await destinationBlob.GetPropertiesAsync();
        }
        while (properties.CopyStatus == CopyStatus.Pending);

        if (properties.CopyStatus != CopyStatus.Success)
        {
            _logger.LogError(
                "Copy failed for blob {path}. Status: {status}",
                logicalPath, properties.CopyStatus);

            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync("Failed to copy blob to safe container.");
            return error;
        }

        await sourceBlob.DeleteIfExistsAsync();

        _logger.LogInformation(
            "Successfully promoted blob to safe and deleted from quarantine.");

        var ok = req.CreateResponse(HttpStatusCode.OK);
        await ok.WriteStringAsync("Blob promoted to safe.");
        return ok;
    }
}