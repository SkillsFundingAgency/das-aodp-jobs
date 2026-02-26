namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Mocks;

public sealed class MockHttpRequestData(FunctionContext context) : HttpRequestData(context)
{
    private readonly FunctionContext _context = context;

    public override HttpResponseData CreateResponse()
    {
        return new MockHttpResponseData(_context);
    }

    public override Stream Body { get; }
    public override HttpHeadersCollection Headers { get; }
    public override IReadOnlyCollection<IHttpCookie> Cookies { get; }
    public override Uri Url { get; }
    public override IEnumerable<ClaimsIdentity> Identities { get; }
    public override string Method { get; }
    public override NameValueCollection Query { get; }
}