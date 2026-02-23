namespace SFA.DAS.AODP.Infrastructure.Authentication;

/// <summary>
/// Defines a handler to add in the required authentication header with a bearer token to requests.
/// </summary>
/// <param name="tokenProvider"></param>
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