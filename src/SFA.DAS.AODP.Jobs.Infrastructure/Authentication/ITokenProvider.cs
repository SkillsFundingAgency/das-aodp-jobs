using Azure.Core;

namespace SFA.DAS.AODP.Infrastructure.Authentication;

/// <summary>
/// Defines a way to retrieve an Azure bearer token.
/// </summary>
/// <remarks>Register as a singleton to ensure token caching.</remarks>
public interface ITokenProvider
{
    /// <summary>
    /// Retrieves am access token from the identity provider.
    /// </summary>
    /// <param name="cancellationToken">Propagates a notification that the operation should be cancelled.</param>
    /// <returns>An <see cref="AccessToken"/>.</returns>
    Task<AccessToken> GetTokenAsync(CancellationToken cancellationToken);
}