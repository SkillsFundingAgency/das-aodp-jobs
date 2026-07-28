namespace SFA.DAS.AODP.Jobs.Interfaces;

public interface IQaaSeedCsvBlobReader
{
    Task<Stream> OpenReadAsync(string containerName, string blobName, CancellationToken cancellationToken);
}
