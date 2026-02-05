using System.Net.Http.Json;
using Azure.Core;
using Microsoft.Extensions.Options;

namespace SFA.DAS.AODP.Jobs.Client;

/// <summary>
/// Defines an API client for interacting with the QAA (Quality Assurance Agency for Higher Education) API.
/// </summary>
public interface IQaaApiClient
{
    /// <summary>
    /// Get all qualifications (or as is called on their side, all Diplomas) from the QAA API, this call returns all qualifications on their system, it is on us to perform checks on what has changed and act accordingly.
    /// </summary>
    /// <param name="cancellationToken">Propagates a notification that the operation should be cancelled.</param>
    /// <returns>Collection of <see cref="QaaQualificationResponse"/> containing details of the diplomas retrieved from the API.</returns>
    Task<IList<QaaQualificationResponse>> GetQualificationsAsync(CancellationToken cancellationToken);
}

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

/// <summary>
/// The details of a QAA education qualification (diploma) as retrieved from the API.
/// </summary>
public record QaaQualificationResponse
{
    // TODO[HS]: Need to check with QAA what they define as a duplicate diploma, is it just the AIM Code or is it a combination of other fields? As from my own understanding of other similar qualification data from other sources the AIM Code can be the same for different qualifications if they are different versions or types across different awarding bodies.

    /// <summary>
    /// National Learning Aim identifier for this Access to Higher Education Diploma.
    /// Used by funding/ILR/MIS systems as the definitive unique key for the qualification
    /// (i.e., unique even across different awarding bodies/AVAs).
    /// </summary>
    [JsonPropertyName("AIM_code")]
    public string AimCode { get; set; } = null!;

    /// <summary>
    /// The organisation that designs, quality‑assures, and awards this Diploma
    /// (often called the awarding body or AVA in the Access to Higher Education context).
    /// </summary>
    [JsonPropertyName("Awarding_Body")]
    public string AwardingBody { get; set; } = null!;

    /// <summary>
    /// The official, QAA‑recognised title of the Diploma (e.g. "Access to Higher Education Diploma (Science)").
    /// Together with the awarding body, this describes the academic focus and intended progression.
    /// </summary>
    [JsonPropertyName("Diploma_Title")]
    public string DiplomaTitle { get; set; } = null!;

    /// <summary>
    /// Sector Subject Area (SSA) Tier 1 classification, indicating the broad subject sector
    /// for funding, analytics, and curriculum mapping (e.g., "Science and Mathematics").
    /// </summary>
    [JsonPropertyName("SSA_Tier_1")]
    public string SsaTier1 { get; set; } = null!;

    /// <summary>
    /// Sector Subject Area (SSA) Tier 2 classification, providing a more granular subject category
    /// within the Tier 1 area (e.g., "Mathematics" vs "Biology") to refine reporting and search.
    /// </summary>
    [JsonPropertyName("SSA_Tier_2")]
    public string SsaTier2 { get; set; } = null!;

    /// <summary>
    /// The first date on which learners can be registered against this qualification specification.
    /// Often aligns with a new specification cycle or funding year.
    /// </summary>
    [JsonPropertyName("Start_date_of_qualification")]
    public string StartDateOfQualification { get; set; } = null!;

    /// <summary>
    /// The last date providers are permitted to register new learners on this qualification.
    /// After this date, only existing learners can continue towards certification.
    /// </summary>
    [JsonPropertyName("Last_date_for_registrations")]
    public string LastDateForRegistrations { get; set; } = null!;

    /// <summary>
    /// The final date by which certifications must be claimed for learners registered on this qualification.
    /// Past this date, no further awards can be issued under this specification.
    /// </summary>
    [JsonPropertyName("Last_date_for_certifications")]
    public string LastDateForCertifications { get; set; } = null!;

    /// <summary>
    /// Current lifecycle state of the qualification (e.g., Active, Review, Withdrawn).
    /// Helps determine whether the Diploma is available for delivery, funding, or certification.
    /// </summary>
    [JsonPropertyName("award_status")]
    public string AwardStatus { get; set; } = null!;

    /// <summary>
    /// The date the qualification was formally discontinued/withdrawn (if applicable).
    /// </summary>
    [JsonPropertyName("Discontinued_date")]
    public DateTime? DiscontinuedDate { get; set; }
}

/// <summary>
/// Defines the service layer for importing QAA qualification (diploma) data and handling any processing.
/// </summary>
public interface IQaaQualificationImportService
{
    /// <summary>
    /// Imports the QAA data from the external data source.
    /// </summary>
    /// <param name="cancellationToken">Propagates a notification that the operation should be cancelled.</param>
    /// <returns><c>True</c> if the import was successful, <c>False</c> otherwise.</returns>
    Task<bool> ImportDataAsync(CancellationToken cancellationToken);
}

/// <summary>
/// Default implementation for <see cref="IQaaQualificationImportService"/>
/// </summary>
/// <param name="logger">The logger to log to.</param>
/// <param name="qaaApiClient">The named HttpClient client interface.</param>
public class QaaQualificationImportService(ILogger<QaaQualificationImportService> logger, IQaaApiClient qaaApiClient, IQaaRepository qaaRepository) : IQaaQualificationImportService
{
    private readonly ILogger<QaaQualificationImportService> _logger = logger;
    private readonly IQaaApiClient _qaaApiClient = qaaApiClient;
    private readonly IQaaRepository _qaaRepository = qaaRepository;

    /// <inheritdoc/>.
    public async Task<bool> ImportDataAsync(CancellationToken cancellationToken)
    {
        var proposedQualifications = await _qaaApiClient.GetQualificationsAsync(cancellationToken);

        if (proposedQualifications.Any())
        {
            // get existing quals from db
            var existing = await _qaaRepository.GetAllAsync(cancellationToken);

            // check for changes

            // insert/update as needed

            // transform ssa 1 and 2 to a single column of ssa1.ssa2 format

            // save

            // log results
        }
    }
}

/// <summary>
/// Defines configuration QAA API client.
/// </summary>
public sealed record QaaApiConfiguration
{
    /// <summary>
    /// The name of the section within configuration that all config related to this will be grouped under.
    /// </summary>
    public const string SectionName = "QaaApi";

    /// <summary>
    /// The Url of the QAA API to be called.
    /// </summary>
    public required string BaseUrl { get; set; } = null!;

    /// <summary>
    /// Defines the configuration required for client credentials authentication flow.
    /// </summary>
    public required ClientCredentialsAuthenticationConfiguration Authentication { get; set; } = null!;
}

public sealed record ClientCredentialsAuthenticationConfiguration
{
    public required string? TenantId { get; init; }

    public required string? ClientId { get; init; }

    public required string? ClientSecret { get; init; }

    public required string[] Scopes { get; init; } = [];
}

public interface ITokenProvider
{
    Task<AccessToken> GetTokenAsync(CancellationToken cancellationToken);
}

public class TokenProvider([FromKeyedServices("QaaApi")]TokenCredential tokenCredential, IOptions<QaaApiConfiguration> qaaApiConfiguration) : ITokenProvider
{
    private readonly QaaApiConfiguration _qaaApiConfiguration = qaaApiConfiguration.Value;
    private readonly TokenCredential _credential = tokenCredential;

    public async Task<AccessToken> GetTokenAsync(CancellationToken cancellationToken)
    {
        var tokenContext = new TokenRequestContext(_qaaApiConfiguration.Authentication.Scopes);
        return await _credential.GetTokenAsync(tokenContext, cancellationToken);
    }
}

public sealed class QaaApiAuthenticationHandler(ITokenProvider tokenProvider) : DelegatingHandler
{
    private readonly ITokenProvider _tokenProvider = tokenProvider;
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var accessToken = await _tokenProvider.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken.Token);
        return await base.SendAsync(request, cancellationToken);
    }
}

public interface IQaaRepository
{
    Task<IList<QaaQualification>> GetAllAsync( CancellationToken cancellationToken);
    Task<QaaQualification> CreateAsync(QaaQualificationResponse qualificationResponse, CancellationToken cancellationToken);
    Task UpdateAsync(QaaQualificationResponse qualificationResponse, CancellationToken cancellationToken);
}

public class QaaQualification
{
    public string AimCode { get; protected set; }
}