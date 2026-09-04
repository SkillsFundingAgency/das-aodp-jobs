using Azure.Storage.Blobs;

namespace SFA.DAS.AODP.Jobs.Services;

public sealed class BlobStorageFileService : IBlobStorageFileService
{
    private readonly BlobServiceClient _blobServiceClient;

    //Retry settings for accessing blob storage
    //May be waiting for scan to complete, or transient issue with blob storage
    private const int MaxAttempts = 7;//Maximum of 126 seconds delay
    private static readonly TimeSpan InitialDelay = TimeSpan.FromSeconds(2);


    public BlobStorageFileService(
        BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public async Task<Stream> DownloadFileAsync(
    string containerName,
    string blobPath,
    CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name must be provided.", nameof(containerName));

        if (string.IsNullOrWhiteSpace(blobPath))
            throw new ArgumentException("Blob path must be provided.", nameof(blobPath));

        var containerClient = _blobServiceClient.GetBlobContainerClient(containerName);
        var blobClient = containerClient.GetBlobClient(blobPath);

        var delay = InitialDelay;

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                return await blobClient.OpenReadAsync(cancellationToken: cancellationToken);
            }
            catch (Azure.RequestFailedException ex)
                when (ex.ErrorCode == "BlobNotFound")
            {
                if (attempt == MaxAttempts)
                {
                    throw new InvalidOperationException(
                        $"File did not appear in storage after {MaxAttempts} attempts. Path: {containerName}/{blobPath}",
                        ex);
                }

                await Task.Delay(delay, cancellationToken);

                // Exponential backoff
                delay = TimeSpan.FromSeconds(delay.TotalSeconds * 2);
            }
        }

        throw new InvalidOperationException(
            $"Failed to download file from storage. Path: {containerName}/{blobPath}");
    }
}