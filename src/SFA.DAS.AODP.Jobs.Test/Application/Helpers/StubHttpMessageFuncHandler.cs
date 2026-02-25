namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Helpers;

/// <summary>
/// Defines a stub message handler similar to <see cref="StubMessageAcceptedResponseHandler"/> but this version instead allows for passing a delegate to control the code that runs in the 'SendAsync'
/// </summary>
/// <param name="handlerFunc"></param>
public sealed class StubHttpMessageFuncHandler(
    Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handlerFunc)
    : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handlerFunc = handlerFunc;

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return _handlerFunc(request, cancellationToken);
    }
}