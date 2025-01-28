using System.Collections.Specialized;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using RestEase;
using SFA.DAS.AODP.Data;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Functions.Functions;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Test.Mocks;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Test.Application.Functions;

public class RegulatedQualificationsDataFunctionTests
{
    private readonly Mock<ILogger<RegulatedQualificationsDataFunction>> _loggerMock;
    private readonly Mock<IApplicationDbContext> _applicationDbContextMock;
    private readonly Mock<IMapper> _mapper;
    private readonly Mock<IOfqualRegisterService> _ofqualRegisterServiceMock;
    private readonly Mock<IQualificationsService> _qualificationsServiceMock;
    private readonly RegulatedQualificationsDataFunction _function;
    private readonly FunctionContext _functionContext;

    public RegulatedQualificationsDataFunctionTests()
    {
        _loggerMock = new Mock<ILogger<RegulatedQualificationsDataFunction>>();
        _applicationDbContextMock = new Mock<IApplicationDbContext>();
        _mapper = new Mock<IMapper>();
        _qualificationsServiceMock = new Mock<IQualificationsService>();
        _ofqualRegisterServiceMock = new Mock<IOfqualRegisterService>();
        _functionContext = new Mock<FunctionContext>().Object;
        _function = new RegulatedQualificationsDataFunction(
            _loggerMock.Object,
            _applicationDbContextMock.Object,
            _qualificationsServiceMock.Object,
            _ofqualRegisterServiceMock.Object,
            _mapper.Object
            );
    }

    [Fact]
    public async Task Run_ShouldInsertProcessedQualifications()
    {
        // Arrange
        var qualificationEntities = new List<RegulatedQualificationsImport>();
        var httpRequestData = new MockHttpRequestData(_functionContext);
        var parameters = new RegulatedQualificationsQueryParameters { Page = 1, Limit = 10 };
        var qualifications = new List<QualificationDTO>
                {
                    new() { QualificationNumber = "1111", Title = "Test Qualification1" },
                    new() { QualificationNumber = "2222", Title = "Test Qualification2" },
                    new() { QualificationNumber = "3333", Title = "Test Qualification3" },
                    new() { QualificationNumber = "4444", Title = "Test Qualification4" },
                    new() { QualificationNumber = "5555", Title = "Test Qualification5" }
                };

        _applicationDbContextMock.Setup(db => db.BulkInsertAsync(It.IsAny<IEnumerable<RegulatedQualificationsImport>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<RegulatedQualificationsImport>, CancellationToken>((qualificationsList, cancellationToken) =>
            {
                qualificationEntities.AddRange(qualificationsList);
            })
            .Returns(Task.CompletedTask);

        _ofqualRegisterServiceMock.Setup(api => api.SearchPrivateQualificationsAsync(
                It.IsAny<RegulatedQualificationsQueryParameters>())
            )
            .ReturnsAsync(new RegulatedQualificationsPaginatedResult<QualificationDTO>
            {
                Results = qualifications
            });

        _ofqualRegisterServiceMock.Setup(service => service.ExtractQualificationsList(
            It.IsAny<RegulatedQualificationsPaginatedResult<QualificationDTO>>())
        )
        .Returns(qualifications);

        _qualificationsServiceMock.Setup(service => service.CompareAndUpdateQualificationsAsync(
                It.IsAny<List<QualificationDTO>>(),
                It.IsAny<List<QualificationDTO>>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        _qualificationsServiceMock.Setup(service => service.SaveRegulatedQualificationsAsync(It.IsAny<List<QualificationDTO>>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        _ofqualRegisterServiceMock.Setup(service => service.ParseQueryParameters(
                It.IsAny<NameValueCollection>()))
            .Returns(parameters);

        // Act
        var result = await _function.Run(httpRequestData);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Successfully processed 5 qualifications.", okResult.Value);

        _qualificationsServiceMock.Verify(service => service.CompareAndUpdateQualificationsAsync(
            It.IsAny<List<QualificationDTO>>(),
            It.IsAny<List<QualificationDTO>>()), Times.Once);

        _qualificationsServiceMock.Verify(service => service.SaveRegulatedQualificationsAsync(
            It.IsAny<List<QualificationDTO>>()), Times.Once);

    }

    [Fact]
    public async Task Run_ShouldLogAndReturnWhenApiReturnsNoResults()
    {
        // Arrange
        var httpRequestData = new MockHttpRequestData(_functionContext);
        var parameters = new RegulatedQualificationsQueryParameters { Page = 1, Limit = 10 };

        _ofqualRegisterServiceMock.Setup(x => x.SearchPrivateQualificationsAsync(
                It.IsAny<RegulatedQualificationsQueryParameters>()))
            .ReturnsAsync(new RegulatedQualificationsPaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>()
            });

        _ofqualRegisterServiceMock.Setup(service => service.ParseQueryParameters(
                It.IsAny<NameValueCollection>()))
            .Returns(parameters);

        // Act
        var result = await _function.Run(httpRequestData);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        Assert.Equal("Successfully processed 0 qualifications.", okResult.Value);

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString() == "No more qualifications to process."),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);

        _applicationDbContextMock.Verify(
            db => db.BulkInsertAsync(It.IsAny<IEnumerable<RegulatedQualificationsImport>>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Run_ShouldBulkInsertQualificationsWhenApiReturnsResults()
    {
        // Arrange
        var httpRequestData = new MockHttpRequestData(_functionContext);
        var qualifications = new List<QualificationDTO>
        {
            new QualificationDTO { QualificationNumber = "1111", Title = "Test Qualification1" },
            new QualificationDTO { QualificationNumber = "2222", Title = "Test Qualification2" },
            new QualificationDTO { QualificationNumber = "3333", Title = "Test Qualification3" },
            new QualificationDTO { QualificationNumber = "4444", Title = "Test Qualification4" },
            new QualificationDTO { QualificationNumber = "5555", Title = "Test Qualification5" }
        };

        var parameters = new RegulatedQualificationsQueryParameters { Page = 1, Limit = 10 };

        _ofqualRegisterServiceMock.Setup(api => api.SearchPrivateQualificationsAsync(
                It.IsAny<RegulatedQualificationsQueryParameters>())
            )
            .ReturnsAsync(new RegulatedQualificationsPaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>
                {
                    new QualificationDTO { QualificationNumber = "1111", Title = "Test Qualification1" },
                    new QualificationDTO { QualificationNumber = "2222", Title = "Test Qualification2" }
                }
            });

        _ofqualRegisterServiceMock.Setup(service => service.ExtractQualificationsList(
            It.IsAny<RegulatedQualificationsPaginatedResult<QualificationDTO>>())
        )
        .Returns(qualifications);

        _qualificationsServiceMock.Setup(service => service.SaveRegulatedQualificationsAsync(It.IsAny<List<QualificationDTO>>()))
            .Returns(Task.CompletedTask)
            .Verifiable();

        _ofqualRegisterServiceMock.Setup(service => service.ParseQueryParameters(
                It.IsAny<NameValueCollection>()))
            .Returns(parameters);

        // Act
        var result = await _function.Run(httpRequestData);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        Assert.Contains("Successfully processed", okResult.Value?.ToString());

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processing page")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Run_ShouldReturnInternalServerError_OnSystemException()
    {
        // Arrange
        var httpRequestData = new MockHttpRequestData(_functionContext);
        var parameters = new RegulatedQualificationsQueryParameters { Page = 1, Limit = 10 };

        _ofqualRegisterServiceMock
            .Setup(service => service.SearchPrivateQualificationsAsync(
                It.IsAny<RegulatedQualificationsQueryParameters>()))
            .ThrowsAsync(new SystemException("Unexpected error occurred"));

        _ofqualRegisterServiceMock.Setup(service => service.ParseQueryParameters(
                It.IsAny<NameValueCollection>()))
            .Returns(parameters);

        // Act
        var result = await _function.Run(httpRequestData);

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Unexpected error occurred")),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()
        ), Times.Once);
    }

    [Fact]
    public async Task Run_ShouldReturnStatusCode_OnApiException()
    {
        // Arrange
        var httpRequestData = new MockHttpRequestData(_functionContext);
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://test.com");
        var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request")
        };
        var apiException = new ApiException(requestMessage, responseMessage, "Bad Request");
        var parameters = new RegulatedQualificationsQueryParameters { Page = 1, Limit = 10 };

        _ofqualRegisterServiceMock
            .Setup(service => service.SearchPrivateQualificationsAsync(
                It.IsAny<RegulatedQualificationsQueryParameters>()))
            .ThrowsAsync(apiException);

        _ofqualRegisterServiceMock.Setup(service => service.ParseQueryParameters(
                It.IsAny<NameValueCollection>()))
            .Returns(parameters);

        // Act
        var result = await _function.Run(httpRequestData);

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(400, statusCodeResult.StatusCode);
        _loggerMock.Verify(logger => logger.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((o, t) => o.ToString().Contains("Unexpected api exception occurred:") &&
                                            o.ToString().Contains("GET \"https://test.com/\"") &&
                                            o.ToString().Contains("400 (Bad Request)")),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()
        ), Times.Once);
    }


}