﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using Moq;
using RestEase;
using SFA.DAS.AODP.Data;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Functions.Functions;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Test.Application.Functions;

public class RegulatedQualificationsDataFunctionTests
{
    private readonly Mock<ILogger<RegulatedQualificationsDataFunction>> _loggerMock;
    private readonly Mock<IApplicationDbContext> _applicationDbContextMock;
    private readonly Mock<IQualificationsApiService> _qualificationsApiServiceMock;
    private readonly FunctionContext _functionContext;
    private readonly RegulatedQualificationsDataFunction _function;

    public RegulatedQualificationsDataFunctionTests()
    {
        _loggerMock = new Mock<ILogger<RegulatedQualificationsDataFunction>>();
        _applicationDbContextMock = new Mock<IApplicationDbContext>();
        _qualificationsApiServiceMock = new Mock<IQualificationsApiService>();
        _functionContext = new Mock<FunctionContext>().Object;
        _function = new RegulatedQualificationsDataFunction(
            _loggerMock.Object,
            _applicationDbContextMock.Object,
            _qualificationsApiServiceMock.Object);
    }

    [Fact]
    public async Task Run_ShouldInsertProcessedQualifications()
    {
        // Arrange
        var qualifications = new List<RegulatedQualificationsImport>();
        _applicationDbContextMock.Setup(db => db.BulkInsertAsync(It.IsAny<IEnumerable<RegulatedQualificationsImport>>(), It.IsAny<CancellationToken>()))
            .Callback<IEnumerable<RegulatedQualificationsImport>, CancellationToken>((qualificationsList, cancellationToken) =>
            {
                qualifications.AddRange(qualificationsList);
            })
            .Returns(Task.CompletedTask);

        _qualificationsApiServiceMock.Setup(api => api.SearchPrivateQualificationsAsync(
                It.IsAny<RegulatedQualificationQueryParameters>(),
                It.IsAny<int>(),
                It.IsAny<int>())
            )
            .ReturnsAsync(new PaginatedResult<RegulatedQualification>
            {
                Results = new List<RegulatedQualification>
                {
                    new RegulatedQualification { QualificationNumber = "1111", Title = "Test Qualification1" },
                    new RegulatedQualification { QualificationNumber = "2222", Title = "Test Qualification2" },
                    new RegulatedQualification { QualificationNumber = "3333", Title = "Test Qualification3" },
                    new RegulatedQualification { QualificationNumber = "4444", Title = "Test Qualification4" },
                    new RegulatedQualification { QualificationNumber = "5555", Title = "Test Qualification5" }
                }
            });

        var httpRequestMock = new Mock<Microsoft.AspNetCore.Http.HttpRequest>();
        httpRequestMock.Setup(req => req.Query)
            .Returns(new QueryCollection());

        // Act
        var result = await _function.Run(httpRequestMock.Object);

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
        _qualificationsApiServiceMock.Setup(x => x.SearchPrivateQualificationsAsync(
                It.IsAny<RegulatedQualificationQueryParameters>(), 
                It.IsAny<int>(), 
                It.IsAny<int>()))
            .ReturnsAsync(new PaginatedResult<RegulatedQualification>
            {
                Results = null
            });

        var httpRequestMock = new Mock<Microsoft.AspNetCore.Http.HttpRequest>();
        httpRequestMock.Setup(req => req.Query)
            .Returns(new QueryCollection());

        // Act
        var result = await _function.Run(httpRequestMock.Object);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
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
        _qualificationsApiServiceMock.Setup(api => api.SearchPrivateQualificationsAsync(
                It.IsAny<RegulatedQualificationQueryParameters>(),
                It.IsAny<int>(),
                It.IsAny<int>())
            )
            .ReturnsAsync(new PaginatedResult<RegulatedQualification>
            {
                Results = new List<RegulatedQualification>
                {
                    new RegulatedQualification { QualificationNumber = "1111", Title = "Test Qualification1" },
                    new RegulatedQualification { QualificationNumber = "2222", Title = "Test Qualification2" }
                }
            });

        var httpRequestMock = new Mock<Microsoft.AspNetCore.Http.HttpRequest>();
        httpRequestMock.Setup(req => req.Query)
            .Returns(new QueryCollection());

        // Act
        var result = await _function.Run(httpRequestMock.Object);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = result as OkObjectResult;
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
        var httpRequestMock = new Mock<HttpRequest>();
        httpRequestMock.Setup(req => req.Query).Returns(new QueryCollection());

        _qualificationsApiServiceMock
            .Setup(service => service.SearchPrivateQualificationsAsync(
                It.IsAny<RegulatedQualificationQueryParameters>(), 
                It.IsAny<int>(), 
                It.IsAny<int>()))
            .ThrowsAsync(new SystemException("Unexpected error occurred"));

        // Act
        var result = await _function.Run(httpRequestMock.Object);

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
        var httpRequestMock = new Mock<HttpRequest>();
        httpRequestMock.Setup(req => req.Query).Returns(new QueryCollection());

        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://test.com");
        var responseMessage = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request")
        };
        var apiException = new ApiException(requestMessage, responseMessage, "Bad Request");

        _qualificationsApiServiceMock
            .Setup(service => service.SearchPrivateQualificationsAsync(
                It.IsAny<RegulatedQualificationQueryParameters>(),
                It.IsAny<int>(),
                It.IsAny<int>()))
            .ThrowsAsync(apiException);

        // Act
        var result = await _function.Run(httpRequestMock.Object);

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