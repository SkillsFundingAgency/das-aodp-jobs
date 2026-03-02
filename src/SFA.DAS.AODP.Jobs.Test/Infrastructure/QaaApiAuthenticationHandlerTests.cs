using Azure.Core;
using SFA.DAS.AODP.Infrastructure.Authentication;
using SFA.DAS.AODP.Jobs.UnitTests.Application.Helpers;

namespace SFA.DAS.AODP.Jobs.UnitTests.Infrastructure;

public class QaaApiAuthenticationHandlerTests
{
    [Fact]
    public async Task QaaApiAuthenticationHandler_EnsureTokenAndHeaderSetCorrectly()
    {
        // Arrange
        var mockTokenProvider = new Mock<ITokenProvider>();
        var handler = new QaaApiAuthenticationHandler(mockTokenProvider.Object)
        {
            InnerHandler = new StubMessageAcceptedResponseHandler()
        };
        using var client = new HttpClient(handler);
        var request = new HttpRequestMessage(new HttpMethod("GET"), "http://example.com");

        // Expectations
        mockTokenProvider.Setup(o => o.GetTokenAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AccessToken("token", new DateTimeOffset()));

        // Act
        var result = await client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.Accepted, result.StatusCode);
        Assert.Equal("Bearer token", request.Headers.Authorization!.ToString());
    }
}