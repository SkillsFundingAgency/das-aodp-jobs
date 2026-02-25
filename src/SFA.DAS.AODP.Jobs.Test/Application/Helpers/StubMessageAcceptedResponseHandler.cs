namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Helpers;

/// <summary>
/// Defines a stub message handler to be used to test a proper delegating handler, required to stub out the inner sending of the message in a HttpClient.
/// </summary>
public sealed class StubMessageAcceptedResponseHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)
        {
            Content = new StringContent("some content")
        });
    }
}