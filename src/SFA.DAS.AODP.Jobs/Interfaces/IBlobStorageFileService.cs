namespace SFA.DAS.AODP.Jobs.Interfaces;

public interface IBlobStorageFileService
{
    Task<Stream> DownloadFileAsync(string container, string blobPath,  CancellationToken cancellationToken = default);
}
