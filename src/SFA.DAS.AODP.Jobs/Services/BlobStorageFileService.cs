using SFA.DAS.AODP.Jobs.Interfaces;

namespace SFA.DAS.AODP.Jobs.Services;

public class BlobStorageFileService : IBlobStorageFileService
{
    private readonly IHttpClientFactory _httpClientFactory;

    public BlobStorageFileService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Stream> DownloadFileAsync(string filename, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filename))
            throw new ArgumentException("Filename must be provided.", nameof(filename));

        var response = await GetDataFromUrl(filename);
        var approvedResponseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        return approvedResponseStream;
    }

    private async Task<HttpResponseMessage> GetDataFromUrl(string approvedUrlFilePath)
    {
        var _httpClient = _httpClientFactory.CreateClient("xlsx");
        var response = await _httpClient.GetAsync(approvedUrlFilePath);
        response.EnsureSuccessStatusCode();
        return response;
    }
}
