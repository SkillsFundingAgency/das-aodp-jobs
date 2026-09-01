using Azure.Storage.Blobs;
using SFA.DAS.AODP.Jobs.Interfaces;

namespace SFA.DAS.AODP.Jobs.Services;

public class BlobStorageFileService : IBlobStorageFileService
{
    private readonly BlobServiceClient _blobServiceClient;

    public BlobStorageFileService(BlobServiceClient blobServiceClient)
    {
        _blobServiceClient = blobServiceClient;
    }

    public Task<Stream> DownloadFileAsync(string containerName, string blobName, CancellationToken cancellationToken = default)
    {
        var blobClient = _blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        return blobClient.OpenReadAsync(cancellationToken: cancellationToken);
    }
}
