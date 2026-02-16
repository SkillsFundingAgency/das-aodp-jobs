using Azure.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SFA.DAS.AODP.Models.Config;

namespace SFA.DAS.AODP.Infrastructure.Authentication;

/// <summary>
/// Default implementation for <see cref="ITokenProvider"/>.
/// </summary>
/// <param name="tokenCredential">A credential used to obtain a token.</param>
/// <param name="qaaApiConfiguration">Strongly-typed configuration for the Qaa API.</param>
public class TokenProvider([FromKeyedServices("QaaApi")]TokenCredential tokenCredential, IOptions<QaaApiConfiguration> qaaApiConfiguration) : ITokenProvider
{
    private readonly QaaApiConfiguration _qaaApiConfiguration = qaaApiConfiguration.Value;
    private readonly TokenCredential _credential = tokenCredential;

    /// <inheritdoc/>>.
    public async Task<AccessToken> GetTokenAsync(CancellationToken cancellationToken)
    {
        var tokenContext = new TokenRequestContext(_qaaApiConfiguration.Authentication.Scopes);
        return await _credential.GetTokenAsync(tokenContext, cancellationToken);
    }
}