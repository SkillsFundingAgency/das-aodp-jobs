using Azure.Storage.Blobs;

namespace SFA.DAS.AODP.Jobs.Services;

public class QaaSeedCsvBlobReader(IAzureClientFactory<BlobServiceClient> azureClientFactory) : IQaaSeedCsvBlobReader
{
    public Task<Stream> OpenReadAsync(string containerName, string blobName, CancellationToken cancellationToken)
    {
        var blobClient = azureClientFactory.CreateClient("Storage2")
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        return blobClient.OpenReadAsync(cancellationToken: cancellationToken);
    }
}
