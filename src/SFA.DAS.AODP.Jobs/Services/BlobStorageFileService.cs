namespace SFA.DAS.AODP.Jobs.Services;

public class BlobStorageFileService(IHttpClientFactory httpClientFactory) : IBlobStorageFileService
{
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
        var _httpClient = httpClientFactory.CreateClient("xlsx");
        var response = await _httpClient.GetAsync(approvedUrlFilePath);
        response.EnsureSuccessStatusCode();
        return response;
    }
}
