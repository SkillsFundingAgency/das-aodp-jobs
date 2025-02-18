using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.AODP.Functions.Functions;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using RestEase;
using Microsoft.Azure.Functions.Worker;
using SFA.DAS.Funding.ApprenticeshipEarnings.Domain.Services;
using SFA.DAS.AODP.Data.Repositories.Jobs;
using SFA.DAS.AODP.Jobs.Services;

namespace SFA.DAS.AODP.Jobs.Test.Application.Functions;

public class RegulatedQualificationsDataFunctionTests
{
    private readonly Mock<ILogger<RegulatedQualificationsDataFunction>> _loggerMock;
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IQualificationsService> _qualificationsServiceMock;
    private readonly Mock<IOfqualImportService> _ofqualImportServiceMock;
    private readonly Mock<ISystemClockService> _systemClockService;
    private readonly Mock<IJobConfigurationService> _jobConfigurationService;
    private readonly RegulatedQualificationsDataFunction _function;
    private readonly FunctionContext _functionContext;

    public RegulatedQualificationsDataFunctionTests()
    {
        _loggerMock = new Mock<ILogger<RegulatedQualificationsDataFunction>>();
        _dbContextMock = new Mock<IApplicationDbContext>();
        _qualificationsServiceMock = new Mock<IQualificationsService>();
        _ofqualImportServiceMock = new Mock<IOfqualImportService>();
        _functionContext = new Mock<FunctionContext>().Object;
        _systemClockService = new Mock<ISystemClockService>();
        _systemClockService.SetupGet(s => s.UtcNow).Returns(DateTime.UtcNow);
        _jobConfigurationService = new Mock<IJobConfigurationService>();

        _function = new RegulatedQualificationsDataFunction(
            _loggerMock.Object,
            _dbContextMock.Object,
            _qualificationsServiceMock.Object,
            _ofqualImportServiceMock.Object,
            _jobConfigurationService.Object            
        );
    }

    [Fact]
    public async Task Run_Should_Return_Ok_When_Processing_Succeeds()
    {
        // Arrange
        var httpRequestMock = new Mock<HttpRequestData>(_functionContext);
        string userName = "test";
        var totalRecords = 1000;
        var jobControl = new JobControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), JobEnabled = true, ProcessStagingData = true, RunApiImport = true };

        _jobConfigurationService.Setup(s => s.ReadJobConfiguration()).ReturnsAsync(jobControl).Verifiable();
        _jobConfigurationService.Setup(s => s.InsertJobRunAsync(jobControl.JobId, userName, Enum.JobStatus.Started)).ReturnsAsync(jobControl.JobRunId).Verifiable();
        _jobConfigurationService.Setup(s => s.UpdateJobRun(userName, jobControl.JobId, jobControl.JobRunId, totalRecords, Enum.JobStatus.Completed)).Verifiable();

        _ofqualImportServiceMock.Setup(s => s.StageQualificationsDataAsync(It.IsAny<HttpRequestData>()))
            .Returns(Task.FromResult(totalRecords));

        // Act
        var result = await _function.Run(httpRequestMock.Object, userName);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _jobConfigurationService.VerifyAll();
        Assert.Equal("[RegulatedQualificationsDataFunction] -> Successfully Imported Ofqual Data.", okResult.Value);
    }

    [Fact]
    public async Task Run_Should_Return_Ok_When_Disabled()
    {
        // Arrange
        var httpRequestMock = new Mock<HttpRequestData>(_functionContext);
        string userName = "test";
        var totalRecords = 1000;
        var jobControl = new JobControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), JobEnabled = false, ProcessStagingData = true, RunApiImport = true };

        _jobConfigurationService.Setup(s => s.ReadJobConfiguration()).ReturnsAsync(jobControl).Verifiable();       

        _ofqualImportServiceMock.Setup(s => s.StageQualificationsDataAsync(It.IsAny<HttpRequestData>()))
            .Returns(Task.FromResult(totalRecords));

        // Act
        var result = await _function.Run(httpRequestMock.Object, userName);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _jobConfigurationService.VerifyAll();
        Assert.Equal("[RegulatedQualificationsDataFunction] -> Job disabled", okResult.Value);
    }

    [Fact]
    public async Task Run_Should_Return_Ok_When_ApiImport_Disabled()
    {
        // Arrange
        var httpRequestMock = new Mock<HttpRequestData>(_functionContext);
        string userName = "test";
        var totalRecords = 0;
        var jobControl = new JobControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), JobEnabled = true, ProcessStagingData = true, RunApiImport = false };

        _jobConfigurationService.Setup(s => s.ReadJobConfiguration()).ReturnsAsync(jobControl).Verifiable();
        _jobConfigurationService.Setup(s => s.InsertJobRunAsync(jobControl.JobId, userName, Enum.JobStatus.Started)).ReturnsAsync(jobControl.JobRunId).Verifiable();
        _jobConfigurationService.Setup(s => s.UpdateJobRun(userName, jobControl.JobId, jobControl.JobRunId, totalRecords, Enum.JobStatus.Completed)).Verifiable();        

        // Act
        var result = await _function.Run(httpRequestMock.Object, userName);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _jobConfigurationService.VerifyAll();
        
        _ofqualImportServiceMock.Verify(s => s.StageQualificationsDataAsync(It.IsAny<HttpRequestData>()), Times.Never());
        Assert.Equal("[RegulatedQualificationsDataFunction] -> Successfully Imported Ofqual Data.", okResult.Value);
    }

    [Fact]
    public async Task Run_Should_Return_Ok_When_Processing_Disabled()
    {
        // Arrange
        var httpRequestMock = new Mock<HttpRequestData>(_functionContext);
        string userName = "test";
        var totalRecords = 1000;
        var jobControl = new JobControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), JobEnabled = true, ProcessStagingData = false, RunApiImport = true };

        _ofqualImportServiceMock.Setup(s => s.StageQualificationsDataAsync(It.IsAny<HttpRequestData>()))
            .Returns(Task.FromResult(totalRecords));
        _jobConfigurationService.Setup(s => s.ReadJobConfiguration()).ReturnsAsync(jobControl).Verifiable();
        _jobConfigurationService.Setup(s => s.InsertJobRunAsync(jobControl.JobId, userName, Enum.JobStatus.Started)).ReturnsAsync(jobControl.JobRunId).Verifiable();
        _jobConfigurationService.Setup(s => s.UpdateJobRun(userName, jobControl.JobId, jobControl.JobRunId, totalRecords, Enum.JobStatus.Completed)).Verifiable();       

        // Act
        var result = await _function.Run(httpRequestMock.Object, userName);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _jobConfigurationService.VerifyAll();
        _ofqualImportServiceMock.Verify(s => s.ProcessQualificationsDataAsync(), Times.Never());
        Assert.Equal("[RegulatedQualificationsDataFunction] -> Successfully Imported Ofqual Data.", okResult.Value);
    }

    [Fact]
    public async Task Run_Should_Return_StatusCodeResult_When_ApiException_Occurs()
    {
        // Arrange
        var httpRequestMock = new Mock<HttpRequestData>(_functionContext);
        string userName = "test";
        var totalRecords = 1000;
        var jobControl = new JobControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), JobEnabled = true, ProcessStagingData = true, RunApiImport = true };

        _jobConfigurationService.Setup(s => s.ReadJobConfiguration()).ReturnsAsync(jobControl).Verifiable();
        _jobConfigurationService.Setup(s => s.InsertJobRunAsync(jobControl.JobId, userName, Enum.JobStatus.Started)).ReturnsAsync(jobControl.JobRunId).Verifiable();
        _jobConfigurationService.Setup(s => s.UpdateJobRun(userName, jobControl.JobId, jobControl.JobRunId, totalRecords, Enum.JobStatus.Completed)).Verifiable();
        var requestMessage = new HttpRequestMessage(HttpMethod.Get, "https://test.com");
        var responseMessage = new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("Bad Request")
        };
        var apiException = new ApiException(requestMessage, responseMessage, "Bad Request");

        _ofqualImportServiceMock.Setup(s => s.StageQualificationsDataAsync(It.IsAny<HttpRequestData>()))
            .ThrowsAsync(apiException);

        // Act
        var result = await _function.Run(httpRequestMock.Object, userName);

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal((int)HttpStatusCode.BadRequest, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task Run_Should_Return_InternalServerError_When_SystemException_Occurs()
    {
        // Arrange
        var httpRequestMock = new Mock<HttpRequestData>(_functionContext);
        string userName = "test";
        var totalRecords = 1000;
        var jobControl = new JobControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), JobEnabled = true, ProcessStagingData = true, RunApiImport = true };

        _jobConfigurationService.Setup(s => s.ReadJobConfiguration()).ReturnsAsync(jobControl).Verifiable();
        _jobConfigurationService.Setup(s => s.InsertJobRunAsync(jobControl.JobId, userName, Enum.JobStatus.Started)).ReturnsAsync(jobControl.JobRunId).Verifiable();
        _jobConfigurationService.Setup(s => s.UpdateJobRun(userName, jobControl.JobId, jobControl.JobRunId, totalRecords, Enum.JobStatus.Completed)).Verifiable();
        _ofqualImportServiceMock.Setup(s => s.StageQualificationsDataAsync(It.IsAny<HttpRequestData>()))
            .ThrowsAsync(new SystemException("System error"));

        // Act
        var result = await _function.Run(httpRequestMock.Object, userName);

        // Assert
        var statusCodeResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusCodeResult.StatusCode);
    }
}
