using Azure.Storage.Blobs;

namespace SFA.DAS.AODP.Jobs.Services;

// This is temporary code only here for the purposes of the seed function for qaa to run and can be removed
// when it has been run in production.
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
