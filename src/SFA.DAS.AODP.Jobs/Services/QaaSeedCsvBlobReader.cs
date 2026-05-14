using Azure.Storage.Blobs;

namespace SFA.DAS.AODP.Jobs.Services;

public class QaaSeedCsvBlobReader(BlobServiceClient blobServiceClient) : IQaaSeedCsvBlobReader
{
    public Task<Stream> OpenReadAsync(string containerName, string blobName, CancellationToken cancellationToken)
    {
        var blobClient = blobServiceClient
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        return blobClient.OpenReadAsync(cancellationToken: cancellationToken);
    }
}
