using Azure.Storage.Blobs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Config;

namespace SFA.DAS.AODP.Jobs.Services;

public class BlobStorageFileService : IBlobStorageFileService
{
    private readonly BlobStorageSettings _blobStorageSettings;
    private readonly BlobServiceClient _blobServiceClient;
    private BlobContainerClient? _blobContainerClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<BlobStorageFileService> _logger;

    public BlobStorageFileService(BlobServiceClient blobServiceClient, 
        IOptions<BlobStorageSettings> settings,
        ILogger<BlobStorageFileService> logger, 
        IHttpClientFactory httpClientFactory)
    {
        _blobServiceClient = blobServiceClient;
        _blobStorageSettings = settings.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger;
        _httpClientFactory = httpClientFactory;
    }

    public async Task<Stream> DownloadFileAsync(string filename, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filename))
            throw new ArgumentException("Filename must be provided.", nameof(filename));

        var response = await GetDataFromUrl(filename);
        var approvedResponseStream = await response.Content.ReadAsStreamAsync();
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
