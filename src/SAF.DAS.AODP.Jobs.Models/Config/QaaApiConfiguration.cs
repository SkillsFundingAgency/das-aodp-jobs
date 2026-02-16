namespace SFA.DAS.AODP.Models.Config;

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
    public ClientCredentialsAuthenticationConfiguration Authentication { get; set; } = null!;
}