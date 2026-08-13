using Moq.Protected;
using SFA.DAS.AODP.Models.Config;
using System.Net;

namespace SFA.DAS.AODP.Jobs.Test.Application.Services;

public class SchedulerClientServiceTests
{
    private readonly Mock<HttpMessageHandler> _handlerMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<ILogger<SchedulerClientService>> _loggerMock;
    private readonly AodpJobsConfiguration _config;
    private readonly SchedulerClientService _service;

    public SchedulerClientServiceTests()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _loggerMock = new Mock<ILogger<SchedulerClientService>>();
        _config = new AodpJobsConfiguration
        {
            FunctionAppBaseUrl = "https://functions.local",
            FunctionHostKey = null
        };

        _httpClientFactoryMock
            .Setup(f => f.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(_handlerMock.Object));

        _service = new SchedulerClientService(_loggerMock.Object, _config, _httpClientFactoryMock.Object);
    }

    private static JobRunControl CreateJobRun(string user = "tester") => new() { User = user };

    private void SetupResponse(HttpStatusCode statusCode, string body = "", Action<HttpRequestMessage>? capture = null)
    {
        _handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capture?.Invoke(req))
            .ReturnsAsync(new HttpResponseMessage(statusCode) { Content = new StringContent(body) });
    }

    [Fact]
    public async Task ExecuteFunction_ReturnsTrue_OnSuccess()
    {
        SetupResponse(HttpStatusCode.OK, "ok body");

        var result = await _service.ExecuteFunction(CreateJobRun(), "ImportPldnsDataFunction", "api/importPldns");

        result.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteFunction_ReturnsFalse_OnNotFound()
    {
        SetupResponse(HttpStatusCode.NotFound, "not found body");

        var result = await _service.ExecuteFunction(CreateJobRun(), "ImportPldnsDataFunction", "api/importPldns");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteFunction_ReturnsFalse_OnOtherErrorStatus()
    {
        SetupResponse(HttpStatusCode.InternalServerError, "server error");

        var result = await _service.ExecuteFunction(CreateJobRun(), "ImportPldnsDataFunction", "api/importPldns");

        result.ShouldBeFalse();
    }

    [Fact]
    public async Task ExecuteFunction_UsesRequestedUser_InUrl()
    {
        HttpRequestMessage? captured = null;
        SetupResponse(HttpStatusCode.OK, capture: req => captured = req);

        await _service.ExecuteFunction(CreateJobRun("jane.doe"), "ImportPldnsDataFunction", "api/importPldns");

        captured.ShouldNotBeNull();
        captured!.RequestUri!.ToString().ShouldBe("https://functions.local/api/importPldns/jane.doe");
    }

    [Fact]
    public async Task ExecuteFunction_DefaultsUser_WhenUserIsWhitespace()
    {
        HttpRequestMessage? captured = null;
        SetupResponse(HttpStatusCode.OK, capture: req => captured = req);

        await _service.ExecuteFunction(CreateJobRun("   "), "ImportPldnsDataFunction", "api/importPldns");

        captured!.RequestUri!.ToString().ShouldBe("https://functions.local/api/importPldns/ScheduledJob");
    }

    [Fact]
    public async Task ExecuteFunction_AppendsHostKey_WhenConfigured()
    {
        _config.FunctionHostKey = "secret-key";
        HttpRequestMessage? captured = null;
        SetupResponse(HttpStatusCode.OK, capture: req => captured = req);

        await _service.ExecuteFunction(CreateJobRun(), "ImportPldnsDataFunction", "api/importPldns");

        captured!.RequestUri!.Query.ShouldBe("?code=secret-key");
    }

    [Fact]
    public async Task ExecuteFunction_UsesDefaultBaseUrl_WhenNotConfigured()
    {
        _config.FunctionAppBaseUrl = null;
        HttpRequestMessage? captured = null;
        SetupResponse(HttpStatusCode.OK, capture: req => captured = req);

        await _service.ExecuteFunction(CreateJobRun(), "ImportPldnsDataFunction", "api/importPldns");

        captured!.RequestUri!.Host.ShouldBe("localhost");
        captured!.RequestUri!.Port.ShouldBe(7000);
    }
}
