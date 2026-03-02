using SFA.DAS.AODP.Jobs.Functions.Abstractions;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Functions;

public class JobFunctionRunnerTests
{
    private readonly Mock<IJobConfigurationService> _jobConfigurationServiceMock;
    private readonly JobFunctionRunner _runner;

    public JobFunctionRunnerTests()
    {
        var loggerMock = new Mock<ILogger<JobFunctionRunner>>();
        _jobConfigurationServiceMock = new Mock<IJobConfigurationService>();
        _runner = new JobFunctionRunner(loggerMock.Object, _jobConfigurationServiceMock.Object);
    }

    [Fact]
    public async Task RunAsync_Should_Insert_New_JobRun_And_Complete_Successfully_With_Correct_Record_Count()
    {
        // Arrange
        const string functionName = "TestFunction";
        const string username = "user";
        const int expectedRecordCount = 250;
        var jobName = JobNames.QaaQualifications;
        var jobId = Guid.NewGuid();
        var jobRunId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        var jobControl = new JobControl
        {
            JobId = jobId,
            JobEnabled = true,
            Status = nameof(JobStatus.Requested)
        };

        var lastJobRun = new JobRunControl { Id = Guid.Empty };

        _jobConfigurationServiceMock
            .Setup(x => x.ReadJobConfiguration(jobName))
            .ReturnsAsync(jobControl);

        _jobConfigurationServiceMock
            .Setup(x => x.GetLastJobRunAsync(jobName.ToString()))
            .ReturnsAsync(lastJobRun);

        _jobConfigurationServiceMock
            .Setup(x => x.InsertJobRunAsync(jobId, username, JobStatus.Running))
            .ReturnsAsync(jobRunId);

        // Act
        var result = await _runner.RunAsync(
            functionName,
            username,
            jobName,
            async (_, _) => expectedRecordCount,
            cancellationToken);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        Assert.Equal($"[{functionName}] -> Completed.", okResult.Value);

        _jobConfigurationServiceMock.Verify(
            x => x.InsertJobRunAsync(jobId, username, JobStatus.Running),
            Times.Once);

        _jobConfigurationServiceMock.Verify(
            x => x.UpdateJobRun(username, jobId, jobRunId, expectedRecordCount, JobStatus.Completed),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_Should_Update_Existing_JobRun_When_LastRun_Exists_And_Not_Running()
    {
        // Arrange
        const string functionName = "TestFunction";
        const string username = "user";
        var jobName = JobNames.QaaQualifications;
        var jobId = Guid.NewGuid();
        var lastJobRunId = Guid.NewGuid();
        var cancellationToken = CancellationToken.None;

        var jobControl = new JobControl
        {
            JobId = jobId,
            JobEnabled = true,
            Status = nameof(JobStatus.Requested)
        };

        var lastJobRun = new JobRunControl
        {
            Id = lastJobRunId,
            Status = nameof(JobStatus.Completed)
        };

        _jobConfigurationServiceMock
            .Setup(x => x.ReadJobConfiguration(jobName))
            .ReturnsAsync(jobControl);

        _jobConfigurationServiceMock
            .Setup(x => x.GetLastJobRunAsync(jobName.ToString()))
            .ReturnsAsync(lastJobRun);

        // Act
        var result = await _runner.RunAsync(
            functionName,
            username,
            jobName,
            async (_, _) => 100,
            cancellationToken);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _jobConfigurationServiceMock.Verify(
            x => x.InsertJobRunAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<JobStatus>()),
            Times.Never);

        _jobConfigurationServiceMock.Verify(
            x => x.UpdateJobRun(username, jobId, lastJobRunId, 100, JobStatus.Completed),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_Should_Return_JobDisabled_When_JobEnabled_Is_False()
    {
        // Arrange
        const string functionName = "TestFunction";
        const string username = "user";
        var jobName = JobNames.QaaQualifications;

        var jobControl = new JobControl
        {
            JobId = Guid.NewGuid(),
            JobEnabled = false,
            Status = nameof(JobStatus.Requested)
        };

        _jobConfigurationServiceMock
            .Setup(x => x.ReadJobConfiguration(jobName))
            .ReturnsAsync(jobControl);

        // Act
        var result = await _runner.RunAsync(
            functionName,
            username,
            jobName,
            async (_, _) => throw new Exception("Should not be called"),
            CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        Assert.Equal($"[{functionName}] -> Job disabled", okResult.Value);

        _jobConfigurationServiceMock.Verify(
            x => x.GetLastJobRunAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_Should_Return_JobRunning_When_Status_Is_Running()
    {
        // Arrange
        const string functionName = "TestFunction";
        const string username = "user";
        var jobName = JobNames.QaaQualifications;

        var jobControl = new JobControl
        {
            JobId = Guid.NewGuid(),
            JobEnabled = true,
            Status = nameof(JobStatus.Running)
        };

        _jobConfigurationServiceMock
            .Setup(x => x.ReadJobConfiguration(jobName))
            .ReturnsAsync(jobControl);

        // Act
        var result = await _runner.RunAsync(
            functionName,
            username,
            jobName,
            async (_, _) => throw new Exception("Should not be called"),
            CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var okResult = (OkObjectResult)result;
        Assert.Equal($"[{functionName}] -> Job currently running", okResult.Value);

        _jobConfigurationServiceMock.Verify(
            x => x.GetLastJobRunAsync(It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task RunAsync_Should_Handle_ApiException_And_Return_StatusCode()
    {
        // Arrange
        const string functionName = "TestFunction";
        const string username = "user";
        var jobName = JobNames.QaaQualifications;
        var jobId = Guid.NewGuid();
        var jobRunId = Guid.NewGuid();
        var apiErrorMessage = "API call failed";

        var jobControl = new JobControl
        {
            JobId = jobId,
            JobEnabled = true,
            Status = nameof(JobStatus.Requested)
        };

        var lastJobRun = new JobRunControl { Id = Guid.Empty };

        _jobConfigurationServiceMock
            .Setup(x => x.ReadJobConfiguration(jobName))
            .ReturnsAsync(jobControl);

        _jobConfigurationServiceMock
            .Setup(x => x.GetLastJobRunAsync(jobName.ToString()))
            .ReturnsAsync(lastJobRun);

        _jobConfigurationServiceMock
            .Setup(x => x.InsertJobRunAsync(jobId, username, JobStatus.Running))
            .ReturnsAsync(jobRunId);

        var apiException = new ApiException(new HttpRequestMessage(new HttpMethod("GET"), "local"),
            new HttpResponseMessage(HttpStatusCode.BadRequest), apiErrorMessage);

        // Act
        var result = await _runner.RunAsync(
            functionName,
            username,
            jobName,
            async (_, _)  => throw apiException,
            CancellationToken.None);

        // Assert
        Assert.IsType<StatusCodeResult>(result);
        var statusResult = (StatusCodeResult)result;
        Assert.Equal((int)HttpStatusCode.BadRequest, statusResult.StatusCode);

        _jobConfigurationServiceMock.Verify(
            x => x.UpdateJobRun(username, jobId, jobRunId, 0, JobStatus.Error),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_Should_Handle_HttpRequestException_And_Return_StatusCode()
    {
        // Arrange
        const string functionName = "TestFunction";
        const string username = "user";
        var jobName = JobNames.QaaQualifications;
        var jobId = Guid.NewGuid();
        var jobRunId = Guid.NewGuid();
        var httpErrorMessage = "HTTP request failed";

        var jobControl = new JobControl
        {
            JobId = jobId,
            JobEnabled = true,
            Status = nameof(JobStatus.Requested)
        };

        var lastJobRun = new JobRunControl { Id = Guid.Empty };

        _jobConfigurationServiceMock
            .Setup(x => x.ReadJobConfiguration(jobName))
            .ReturnsAsync(jobControl);

        _jobConfigurationServiceMock
            .Setup(x => x.GetLastJobRunAsync(jobName.ToString()))
            .ReturnsAsync(lastJobRun);

        _jobConfigurationServiceMock
            .Setup(x => x.InsertJobRunAsync(jobId, username, JobStatus.Running))
            .ReturnsAsync(jobRunId);

        var httpException = new HttpRequestException(httpErrorMessage, null, HttpStatusCode.ServiceUnavailable);

        // Act
        var result = await _runner.RunAsync(
            functionName,
            username,
            jobName,
            async (_, _) => throw httpException,
            CancellationToken.None);

        // Assert
        Assert.IsType<StatusCodeResult>(result);
        var statusResult = (StatusCodeResult)result;
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, statusResult.StatusCode);

        _jobConfigurationServiceMock.Verify(
            x => x.UpdateJobRun(username, jobId, jobRunId, 0, JobStatus.Error),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_Should_Handle_General_Exception_And_Return_InternalServerError()
    {
        // Arrange
        const string functionName = "TestFunction";
        const string username = "user";
        var jobName = JobNames.QaaQualifications;
        var jobId = Guid.NewGuid();
        var jobRunId = Guid.NewGuid();

        var jobControl = new JobControl
        {
            JobId = jobId,
            JobEnabled = true,
            Status = nameof(JobStatus.Requested)
        };

        var lastJobRun = new JobRunControl { Id = Guid.Empty };

        _jobConfigurationServiceMock
            .Setup(x => x.ReadJobConfiguration(jobName))
            .ReturnsAsync(jobControl);

        _jobConfigurationServiceMock
            .Setup(x => x.GetLastJobRunAsync(jobName.ToString()))
            .ReturnsAsync(lastJobRun);

        _jobConfigurationServiceMock
            .Setup(x => x.InsertJobRunAsync(jobId, username, JobStatus.Running))
            .ReturnsAsync(jobRunId);

        var generalException = new InvalidOperationException("Something went wrong");

        // Act
        var result = await _runner.RunAsync(
            functionName,
            username,
            jobName,
            async (_, _) => throw generalException,
            CancellationToken.None);

        // Assert
        Assert.IsType<StatusCodeResult>(result);
        var statusResult = (StatusCodeResult)result;
        Assert.Equal((int)HttpStatusCode.InternalServerError, statusResult.StatusCode);

        _jobConfigurationServiceMock.Verify(
            x => x.UpdateJobRun(username, jobId, jobRunId, 0, JobStatus.Error),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_Should_Pass_CancellationToken_To_Import_Delegate()
    {
        // Arrange
        const string functionName = "TestFunction";
        const string username = "user";
        var jobName = JobNames.QaaQualifications;
        var jobId = Guid.NewGuid();
        var jobRunId = Guid.NewGuid();
        var cancellationToken = new CancellationToken();
        var receivedToken = default(CancellationToken?);

        var jobControl = new JobControl
        {
            JobId = jobId,
            JobEnabled = true,
            Status = nameof(JobStatus.Requested)
        };

        var lastJobRun = new JobRunControl { Id = Guid.Empty };

        _jobConfigurationServiceMock
            .Setup(x => x.ReadJobConfiguration(jobName))
            .ReturnsAsync(jobControl);

        _jobConfigurationServiceMock
            .Setup(x => x.GetLastJobRunAsync(jobName.ToString()))
            .ReturnsAsync(lastJobRun);

        _jobConfigurationServiceMock
            .Setup(x => x.InsertJobRunAsync(jobId, username, JobStatus.Running))
            .ReturnsAsync(jobRunId);

        // Act
        var result = await _runner.RunAsync(
            functionName,
            username,
            jobName,
            async (_, token) =>
            {
                receivedToken = token;
                return 50;
            },
            cancellationToken);

        // Assert
        Assert.Equal(cancellationToken, receivedToken);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task RunAsync_Should_Pass_Updated_JobControl_With_JobRunId_To_Delegate()
    {
        // Arrange
        const string functionName = "TestFunction";
        const string username = "user";
        var jobName = JobNames.QaaQualifications;
        var jobId = Guid.NewGuid();
        var jobRunId = Guid.NewGuid();
        var receivedJobControl = default(JobControl);

        var jobControl = new JobControl
        {
            JobId = jobId,
            JobEnabled = true,
            Status = nameof(JobStatus.Requested)
        };

        var lastJobRun = new JobRunControl { Id = Guid.Empty };

        _jobConfigurationServiceMock
            .Setup(x => x.ReadJobConfiguration(jobName))
            .ReturnsAsync(jobControl);

        _jobConfigurationServiceMock
            .Setup(x => x.GetLastJobRunAsync(jobName.ToString()))
            .ReturnsAsync(lastJobRun);

        _jobConfigurationServiceMock
            .Setup(x => x.InsertJobRunAsync(jobId, username, JobStatus.Running))
            .ReturnsAsync(jobRunId);

        // Act
        var result = await _runner.RunAsync(
            functionName,
            username,
            jobName,
            async (control, _) =>
            {
                receivedJobControl = control;
                return 100;
            },
            CancellationToken.None);

        // Assert
        Assert.NotNull(receivedJobControl);
        Assert.Equal(jobRunId, receivedJobControl.JobRunId);
        Assert.IsType<OkObjectResult>(result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(10000)]
    public async Task RunAsync_Should_Handle_Various_Record_Counts(int recordCount)
    {
        // Arrange
        const string functionName = "TestFunction";
        const string username = "user";
        var jobName = JobNames.QaaQualifications;
        var jobId = Guid.NewGuid();
        var jobRunId = Guid.NewGuid();

        var jobControl = new JobControl
        {
            JobId = jobId,
            JobEnabled = true,
            Status = nameof(JobStatus.Requested)
        };

        var lastJobRun = new JobRunControl { Id = Guid.Empty };

        _jobConfigurationServiceMock
            .Setup(x => x.ReadJobConfiguration(jobName))
            .ReturnsAsync(jobControl);

        _jobConfigurationServiceMock
            .Setup(x => x.GetLastJobRunAsync(jobName.ToString()))
            .ReturnsAsync(lastJobRun);

        _jobConfigurationServiceMock
            .Setup(x => x.InsertJobRunAsync(jobId, username, JobStatus.Running))
            .ReturnsAsync(jobRunId);

        // Act
        var result = await _runner.RunAsync(
            functionName,
            username,
            jobName,
            async (_, _) => recordCount,
            CancellationToken.None);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _jobConfigurationServiceMock.Verify(
            x => x.UpdateJobRun(username, jobId, jobRunId, recordCount, JobStatus.Completed),
            Times.Once);
    }

    [Fact]
    public async Task RunAsync_Should_Handle_LastJobRun_With_Running_Status()
    {
        // Arrange
        const string functionName = "TestFunction";
        const string username = "user";
        var jobName = JobNames.QaaQualifications;
        var jobId = Guid.NewGuid();

        var jobControl = new JobControl
        {
            JobId = jobId,
            JobEnabled = true,
            Status = nameof(JobStatus.Requested)
        };

        var lastJobRun = new JobRunControl
        {
            Id = Guid.NewGuid(),
            Status = nameof(JobStatus.Running)
        };

        _jobConfigurationServiceMock
            .Setup(x => x.ReadJobConfiguration(jobName))
            .ReturnsAsync(jobControl);

        _jobConfigurationServiceMock
            .Setup(x => x.GetLastJobRunAsync(jobName.ToString()))
            .ReturnsAsync(lastJobRun);

        // Act
        var result = await _runner.RunAsync(
            functionName,
            username,
            jobName,
            async (_, _) => throw new Exception("Should not be called"),
            CancellationToken.None);

        // Assert
        Assert.IsType<StatusCodeResult>(result);
        _jobConfigurationServiceMock.Verify(
            x => x.InsertJobRunAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<JobStatus>()),
            Times.Once);
    }
}