using Azure.Storage.Blobs;

namespace SFA.DAS.AODP.Jobs.Services;

public sealed class BlobStorageFileService : IBlobStorageFileService
{
    private readonly BlobServiceClient _blobServiceClient;
    private readonly BlobStorageSettings _settings;

    //Retry settings for accessing blob storage
    //May be waiting for scan to complete, or transient issue with blob storage
    private const int MaxAttempts = 7;//Maximum of 126 seconds delay
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);


    public BlobStorageFileService(
        BlobServiceClient blobServiceClient,
        BlobStorageSettings settings)
    {
        _blobServiceClient = blobServiceClient;
        _settings = settings;
    }

    /// <summary>
    /// Downloads a file from SAFE storage using a logical path.
    /// </summary>
    public async Task<Stream> DownloadFileAsync(
        string logicalPath,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(logicalPath))
        {
            throw new ArgumentException(
                "Logical path must be provided.",
                nameof(logicalPath));
        }

        var container = GetSafeContainerClient();
        var blobClient = container.GetBlobClient(logicalPath);

        var delay = InitialDelay;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return await blobClient.OpenReadAsync();
            }
            catch (Azure.RequestFailedException ex)
                when (ex.ErrorCode == "BlobNotFound")
            {
                if (attempt == MaxAttempts)
                {
                    throw new InvalidOperationException(
                        $"File did not appear in SAFE storage after {MaxAttempts} attempts. Path: {logicalPath}",
                        ex);
                }

                await Task.Delay(delay, cancellationToken);

                // Exponential backoff
                delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2);
            }
        }

        throw new InvalidOperationException(
            $"Failed to download file from SAFE storage. Path: {logicalPath}");
    }

    /// <summary>
    /// Returns a client for the SAFE container.
    /// </summary>
    private BlobContainerClient GetSafeContainerClient()
    {
        return _blobServiceClient.GetBlobContainerClient(
            _settings.SafeContainerName);
    }
}