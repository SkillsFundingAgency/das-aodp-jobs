namespace SFA.DAS.AODP.Jobs.Interfaces;

public interface IBlobStorageFileService
{
    Task<Stream> DownloadFileAsync(string filename, CancellationToken cancellationToken = default);
}
