namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Mocks;

public sealed class MockHttpResponseData(FunctionContext context) : HttpResponseData(context)
{
    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; } = new();
    public override Stream Body { get; set; } = new MemoryStream();
    public override HttpCookies Cookies => null; 
}