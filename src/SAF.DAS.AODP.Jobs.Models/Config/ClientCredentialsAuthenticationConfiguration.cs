namespace SFA.DAS.AODP.Models.Config;

public sealed record ClientCredentialsAuthenticationConfiguration
{
    public required string? TenantId { get; init; }

    public required string? ClientId { get; init; }

    public required string? ClientSecret { get; init; }

    public required string[] Scopes { get; init; } = [];
}