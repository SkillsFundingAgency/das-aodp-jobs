using Azure.Storage.Blobs;

namespace SFA.DAS.AODP.Jobs.Services;

[ExcludeFromCodeCoverage(Justification = "This is temporary code")]
public class QaaSeedCsvBlobReader(IAzureClientFactory<BlobServiceClient> azureClientFactory, IHostEnvironment environment) : IQaaSeedCsvBlobReader
{
    public Task<Stream> OpenReadAsync(string containerName, string blobName, CancellationToken cancellationToken)
    {
        var clientName = string.Empty;
        if (environment.IsDevelopment())
        {
            clientName = "Local";
        }
        else
        {
            clientName = "Storage2";
        }
        var blobClient = azureClientFactory.CreateClient(clientName)
            .GetBlobContainerClient(containerName)
            .GetBlobClient(blobName);

        return blobClient.OpenReadAsync(cancellationToken: cancellationToken);
    }
}
