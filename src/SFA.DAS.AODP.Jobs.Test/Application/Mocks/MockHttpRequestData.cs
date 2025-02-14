using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using SFA.DAS.AODP.Jobs.Test.Application.Mocks;
using System.Collections.Specialized;
using System.Security.Claims;

namespace SFA.DAS.AODP.Jobs.Test.Mocks
{
    public sealed class MockHttpRequestData : HttpRequestData
    {
        private readonly FunctionContext _context;

        public MockHttpRequestData(FunctionContext context) : base(context)
        {
            _context = context;
        }

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
}


