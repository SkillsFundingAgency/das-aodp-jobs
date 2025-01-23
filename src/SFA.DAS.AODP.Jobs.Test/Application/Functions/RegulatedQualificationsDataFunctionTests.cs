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
    private readonly Mock<IRegulatedQualificationsService> _qualificationsApiServiceMock;
    private readonly FunctionContext _functionContext;
    private readonly RegulatedQualificationsDataFunction _function;
    private readonly Mock<IMapper> _mapper;

    public RegulatedQualificationsDataFunctionTests()
    {
        _loggerMock = new Mock<ILogger<RegulatedQualificationsDataFunction>>();
        _applicationDbContextMock = new Mock<IApplicationDbContext>();
        _qualificationsApiServiceMock = new Mock<IRegulatedQualificationsService>();
        _functionContext = new Mock<FunctionContext>().Object;
        _mapper = new Mock<IMapper>();
        _function = new RegulatedQualificationsDataFunction(
            _loggerMock.Object,
            _applicationDbContextMock.Object,
            _qualificationsApiServiceMock.Object,
            _mapper.Object);
    }

    [Fact]
    public async Task Run_ShouldInsertProcessedQualifications()
    {
        // Arrange
        var qualifications = new List<RegulatedQualificationsImport>();
        var httpRequestData = new MockHttpRequestData(_functionContext);

        _applicationDbContextMock.Setup(db => db.BulkInsertAsync(It.IsAny<IEnumerable<RegulatedQualificationsImport>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<RegulatedQualificationsImport>, CancellationToken>((qualificationsList, cancellationToken) =>
            {
                qualifications.AddRange(qualificationsList);
            })
            .Returns(Task.CompletedTask);

        _qualificationsApiServiceMock.Setup(api => api.SearchPrivateQualificationsAsync(
                It.IsAny<RegulatedQualificationsQueryParameters>(),
                It.IsAny<int>(),
                It.IsAny<int>())
            )
            .ReturnsAsync(new RegulatedQualificationsPaginatedResult<RegulatedQualificationDTO>
            {
                Results = new List<RegulatedQualificationDTO>
                {
                    new RegulatedQualificationDTO { QualificationNumber = "1111", Title = "Test Qualification1" },
                    new RegulatedQualificationDTO { QualificationNumber = "2222", Title = "Test Qualification2" },
                    new RegulatedQualificationDTO { QualificationNumber = "3333", Title = "Test Qualification3" },
                    new RegulatedQualificationDTO { QualificationNumber = "4444", Title = "Test Qualification4" },
                    new RegulatedQualificationDTO { QualificationNumber = "5555", Title = "Test Qualification5" }
                }
            });

        // Act
        var result = await _function.Run(httpRequestData);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Successfully processed 5 qualifications.", okResult.Value);
        _applicationDbContextMock.Verify(
            db => db.BulkInsertAsync(It.IsAny<IEnumerable<RegulatedQualificationsImport>>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Run_ShouldLogAndReturnWhenApiReturnsNoResults()
    {
        // Arrange
        var httpRequestData = new MockHttpRequestData(_functionContext);
        
        _qualificationsApiServiceMock.Setup(x => x.SearchPrivateQualificationsAsync(
                It.IsAny<RegulatedQualificationsQueryParameters>(), 
                It.IsAny<int>(), 
                It.IsAny<int>()))
            .ReturnsAsync(new RegulatedQualificationsPaginatedResult<RegulatedQualificationDTO>
            {
                Results = null
            });

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
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);

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

        _qualificationsApiServiceMock.Setup(api => api.SearchPrivateQualificationsAsync(
                It.IsAny<RegulatedQualificationsQueryParameters>(),
                It.IsAny<int>(),
                It.IsAny<int>())
            )
            .ReturnsAsync(new RegulatedQualificationsPaginatedResult<RegulatedQualificationDTO>
            {
                Results = new List<RegulatedQualificationDTO>
                {
                    new RegulatedQualificationDTO { QualificationNumber = "1111", Title = "Test Qualification1" },
                    new RegulatedQualificationDTO { QualificationNumber = "2222", Title = "Test Qualification2" }
                }
            });

        // Act
        var result = await _function.Run(httpRequestData);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
        Assert.NotNull(okResult);
        Assert.Contains("Successfully processed", okResult.Value.ToString());

        _applicationDbContextMock.Verify(
            db => db.BulkInsertAsync(
                It.Is<IEnumerable<RegulatedQualificationsImport>>(list =>
                    list.Count() == 2 &&
                    list.ElementAt(0).QualificationNumber == "1111" &&
                    list.ElementAt(0).Title == "Test Qualification1" &&
                    list.ElementAt(1).QualificationNumber == "2222" &&
                    list.ElementAt(1).Title == "Test Qualification2"
                ),
                It.IsAny<CancellationToken>()
            ),
            Times.Once
        );

        _loggerMock.Verify(x => x.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Processing page")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
    }

    [Fact]
    public async Task Run_ShouldReturnInternalServerError_OnSystemException()
    {
        // Arrange
        var httpRequestData = new MockHttpRequestData(_functionContext);

        _qualificationsApiServiceMock
            .Setup(service => service.SearchPrivateQualificationsAsync(
                It.IsAny<RegulatedQualificationsQueryParameters>(), 
                It.IsAny<int>(), 
                It.IsAny<int>()))
            .ThrowsAsync(new SystemException("Unexpected error occurred"));

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
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
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

        _qualificationsApiServiceMock
            .Setup(service => service.SearchPrivateQualificationsAsync(
                It.IsAny<RegulatedQualificationsQueryParameters>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ThrowsAsync(apiException);

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
            (Func<It.IsAnyType, Exception, string>)It.IsAny<object>()
        ), Times.Once);
    }


}