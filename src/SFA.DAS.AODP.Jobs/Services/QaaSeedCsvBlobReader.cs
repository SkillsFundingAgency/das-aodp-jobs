using Azure.Storage.Blobs;

namespace SFA.DAS.AODP.Jobs.Services;

// This is temporary code only here for the purposes of the seed function for qaa to run and can be removed
// when it has been run in production.
[ExcludeFromCodeCoverage(Justification = "This is temporary code")]
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
