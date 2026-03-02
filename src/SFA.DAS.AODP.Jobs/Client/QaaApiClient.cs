using System.Net.Http.Json;

namespace SFA.DAS.AODP.Jobs.Client;

/// <summary>
/// Default implementation for <see cref="IQaaApiClient"/>.
/// </summary>
/// <param name="logger">The logger instance to log to.</param>
/// <param name="httpClient">The underlying <see cref="HttpClient"/> wrapper for the interface.</param>
public class QaaApiClient(ILogger<QaaApiClient> logger, HttpClient httpClient) : IQaaApiClient
{
    private readonly ILogger<QaaApiClient> _logger = logger;
    private readonly HttpClient _httpClient = httpClient;

    /// <inheritdoc/>.
    public async Task<IList<QaaQualificationResponse>> GetQualificationsAsync(CancellationToken cancellationToken)
    {
        const string endpoint = "diplomas/all";

        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            response.EnsureSuccessStatusCode();

            return (await response.Content.ReadFromJsonAsync<IList<QaaQualificationResponse>>(cancellationToken))!;
            
        }
        catch (HttpRequestException e)
        {
            _logger.LogError(e, "Failed to call {Endpoint}", endpoint);
            throw;
        }
    }
}