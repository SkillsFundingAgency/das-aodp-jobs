using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.AODP.Functions.Functions;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using RestEase;
using Xunit;
using Microsoft.Azure.Functions.Worker;

namespace SFA.DAS.AODP.Jobs.Test.Application.Functions;

public class RegulatedQualificationsDataFunctionTests
{
    private readonly Mock<ILogger<RegulatedQualificationsDataFunction>> _loggerMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IQualificationsService> _qualificationsServiceMock;
    private readonly Mock<IOfqualImportService> _ofqualImportServiceMock;
    private readonly RegulatedQualificationsDataFunction _function;
    private readonly FunctionContext _functionContext;

    public RegulatedQualificationsDataFunctionTests()
    {
        _loggerMock = new Mock<ILogger<RegulatedQualificationsDataFunction>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _qualificationsServiceMock = new Mock<IQualificationsService>();
        _ofqualImportServiceMock = new Mock<IOfqualImportService>();
        _functionContext = new Mock<FunctionContext>().Object;

        _function = new RegulatedQualificationsDataFunction(
            _loggerMock.Object,
            _dbContextMock.Object,
            _qualificationsServiceMock.Object,
            _ofqualImportServiceMock.Object
        );
    }

    [Fact]
    public async Task Run_Should_Return_Ok_When_Processing_Succeeds()
    {
        // Arrange
        var httpRequestMock = new Mock<HttpRequestData>(_functionContext);

        _ofqualImportServiceMock.Setup(s => s.StageQualificationsDataAsync(It.IsAny<HttpRequestData>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _function.Run(httpRequestMock.Object);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Successfully Imported Ofqual Data.", okResult.Value);
    }

    [Fact]
    public async Task Run_Should_Return_StatusCodeResult_When_ApiException_Occurs()
    {
        // Arrange
        var httpRequestMock = new Mock<HttpRequestData>(_functionContext);
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://test.com");
        var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request")
        };
        var apiException = new ApiException(requestMessage, responseMessage, "Bad Request");

        _ofqualImportServiceMock.Setup(s => s.StageQualificationsDataAsync(It.IsAny<HttpRequestData>()))
            .ThrowsAsync(apiException);

        // Act
        var result = await _function.Run(httpRequestMock.Object);

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal((int)HttpStatusCode.BadRequest, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Run_Should_Return_InternalServerError_When_SystemException_Occurs()
    {
        // Arrange
        var httpRequestMock = new Mock<HttpRequestData>(_functionContext);

        _ofqualImportServiceMock.Setup(s => s.StageQualificationsDataAsync(It.IsAny<HttpRequestData>()))
            .ThrowsAsync(new SystemException("System error"));

        // Act
        var result = await _function.Run(httpRequestMock.Object);

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
}
