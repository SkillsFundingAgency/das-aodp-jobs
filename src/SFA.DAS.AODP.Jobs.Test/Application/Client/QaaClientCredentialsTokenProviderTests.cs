using Azure.Core;
using Microsoft.Extensions.Options;
using SFA.DAS.AODP.Infrastructure.Authentication;
using SFA.DAS.AODP.Models.Config;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Client;

public class QaaClientCredentialsTokenProviderTests
{
    [Fact]
    public async Task GetTokenAsync_CallsTokenCredentialWithConfiguredScopes_AndReturnsAccessToken()
    {
        // Arrange
        var expectedToken = new AccessToken("access-token-123", DateTimeOffset.UtcNow.AddMinutes(30));

        var fakeCredential = new FakeTokenCredential
        {
            TokenToReturn = expectedToken
        };

        var options = Options.Create(new QaaApiConfiguration
        {
            BaseUrl = "some url",
            Authentication = new ClientCredentialsAuthenticationConfiguration
            {
                ClientId = "some client id",
                ClientSecret = "some secret",
                Scopes = ["api://some-api-id/.default"],
                TenantId = "some tenant"
            }
        });

        var sut = new QaaClientCredentialsTokenProvider(fakeCredential, options);

        // Act
        var result = await sut.GetTokenAsync(CancellationToken.None);

        // Assert
        Assert.Equal(expectedToken.Token, result.Token);
        Assert.Equal(expectedToken.ExpiresOn, result.ExpiresOn);

        Assert.NotNull(fakeCredential.CapturedTokenRequestContext);
        Assert.NotNull(fakeCredential.CapturedTokenRequestContext!.Value.Scopes);
        Assert.Single(fakeCredential.CapturedTokenRequestContext.Value.Scopes);
        Assert.Equal("api://some-api-id/.default", fakeCredential.CapturedTokenRequestContext.Value.Scopes[0]);
    }

    [Fact]
    public async Task GetTokenAsync_WhenCredentialThrows_PropagatesException()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Token acquisition failed");

        var fakeCredential = new FakeTokenCredential
        {
            ExceptionToThrow = expectedException
        };

        var options = Options.Create(new QaaApiConfiguration
        {
            BaseUrl = "some url",
            Authentication = new ClientCredentialsAuthenticationConfiguration
            {
                ClientId = "some client id",
                ClientSecret = "some secret",
                Scopes = ["api://some-api-id/.default"],
                TenantId = "some tenant"
            }
        });

        var sut = new QaaClientCredentialsTokenProvider(fakeCredential, options);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sut.GetTokenAsync(CancellationToken.None));

        // Assert
        Assert.Same(expectedException, ex);
    }

    private sealed class FakeTokenCredential : TokenCredential
    {
        public AccessToken TokenToReturn { get; init; } = new("default-token", DateTimeOffset.UtcNow.AddMinutes(10));

        public Exception? ExceptionToThrow { get; init; }

        public TokenRequestContext? CapturedTokenRequestContext { get; private set; }

        public CancellationToken CapturedCancellationToken { get; private set; }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            CapturedTokenRequestContext = requestContext;
            CapturedCancellationToken = cancellationToken;

            return ExceptionToThrow is not null ? throw ExceptionToThrow : TokenToReturn;
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            CapturedTokenRequestContext = requestContext;
            CapturedCancellationToken = cancellationToken;

            return ExceptionToThrow is not null ? ValueTask.FromException<AccessToken>(ExceptionToThrow) : ValueTask.FromResult(TokenToReturn);
        }
    }
}