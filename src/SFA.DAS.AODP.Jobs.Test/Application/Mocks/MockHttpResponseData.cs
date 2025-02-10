using Microsoft.Azure.Functions.Worker;
using System.Net;
using Microsoft.Azure.Functions.Worker.Http;

namespace SFA.DAS.AODP.Jobs.Test.Application.Mocks
{
    public sealed class MockHttpResponseData : HttpResponseData
    {
        public MockHttpResponseData(FunctionContext context) : base(context)
        {
            Headers = new HttpHeadersCollection();
            Body = new MemoryStream();
        }

        public override HttpStatusCode StatusCode { get; set; }
        public override HttpHeadersCollection Headers { get; set; }
        public override Stream Body { get; set; }
        public override HttpCookies Cookies => null; 
    }
}