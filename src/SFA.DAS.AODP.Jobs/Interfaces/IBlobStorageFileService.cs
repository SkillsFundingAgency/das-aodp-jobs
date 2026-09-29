namespace SFA.DAS.AODP.Jobs.Interfaces;

public interface IBlobStorageFileService
{
    Task<Stream> DownloadFileAsync(string containerName, string blobName, CancellationToken cancellationToken = default);
}
