using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.AODP.Models.Config;

/// <summary>
/// Defines a strongly typed configuration object for client credentials for authenticating with a resource protected with
/// OAuth2.0.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed record ClientCredentialsAuthenticationConfiguration
{
    /// <summary>
    /// Gets the tenant ID for the OAuth2.0 client credentials flow.
    /// </summary>
    public required string? TenantId { get; init; }
    
    /// <summary>
    /// Gets the application (client) ID registered in the identity provider.
    /// </summary>
    public required string? ClientId { get; init; }

    /// <summary>
    /// Gets the client secret used to authenticate the application with the OAuth2.0 resource.
    /// </summary>
    public required string? ClientSecret { get; init; }

    /// <summary>
    /// Gets the OAuth2.0 scope required for accessing the protected resource.
    /// </summary>
    public required string[] Scopes { get; init; } = [];
}