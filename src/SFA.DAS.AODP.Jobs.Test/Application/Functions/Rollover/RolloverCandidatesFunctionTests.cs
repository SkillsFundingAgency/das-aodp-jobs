using SFA.DAS.AODP.Jobs.Functions.Rollover;
using SFA.DAS.AODP.Jobs.Interfaces.Rollover;
using SFA.DAS.AODP.Jobs.Test.Mocks;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Functions.Rollover;

public class RolloverCandidatesFunctionTests
{
    [Fact]
    public async Task Run_GeneratesRolloverCandidatesAndUpdatesJobRun()
    {
        // Arrange
        var logger = new Mock<ILogger<RolloverCandidatesFunction>>();
        var service = new Mock<IRolloverCandidateService>();
        var jobConfigurationService = new Mock<IJobConfigurationService>();
        var jobId = Guid.NewGuid();
        var jobRunId = Guid.NewGuid();

        jobConfigurationService
            .Setup(x => x.ReadJobConfiguration(JobNames.RolloverCandidates))
            .ReturnsAsync(new JobControl
            {
                JobEnabled = true,
                JobId = jobId,
                Status = JobStatus.Completed.ToString()
            });
        jobConfigurationService
            .Setup(x => x.InsertJobRunAsync(jobId, "SYSTEM", JobStatus.Running))
            .ReturnsAsync(jobRunId);
        service.Setup(x => x.GenerateRolloverCandidatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var function = new RolloverCandidatesFunction(logger.Object, service.Object, jobConfigurationService.Object);

        var timerInfo = new TimerInfo
        {
            ScheduleStatus = new ScheduleStatus
            {
                Next = DateTime.UtcNow.AddYears(1)
            }
        };
        var functionContext = new Mock<FunctionContext>().Object;

        // Act
        await function.Run(timerInfo, functionContext);

        // Assert
        service.Verify(x => x.GenerateRolloverCandidatesAsync(It.IsAny<CancellationToken>()), Times.Once);
        jobConfigurationService.Verify(x => x.UpdateJobRun("SYSTEM", jobId, jobRunId, 5, JobStatus.Completed), Times.Once);
    }

    [Fact]
    public async Task RunManual_GeneratesRolloverCandidates()
    {
        // Arrange
        var logger = new Mock<ILogger<RolloverCandidatesFunction>>();
        var service = new Mock<IRolloverCandidateService>();
        var jobConfigurationService = new Mock<IJobConfigurationService>();
        var jobId = Guid.NewGuid();
        var jobRunId = Guid.NewGuid();
        var functionContext = new Mock<FunctionContext>().Object;

        jobConfigurationService
            .Setup(x => x.ReadJobConfiguration(JobNames.RolloverCandidates))
            .ReturnsAsync(new JobControl
            {
                JobEnabled = true,
                JobId = jobId,
                Status = JobStatus.Completed.ToString()
            });
        jobConfigurationService
            .Setup(x => x.InsertJobRunAsync(jobId, "manual-user", JobStatus.Running))
            .ReturnsAsync(jobRunId);
        service.Setup(x => x.GenerateRolloverCandidatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var function = new RolloverCandidatesFunction(logger.Object, service.Object, jobConfigurationService.Object);
        var request = new MockHttpRequestData(functionContext);

        // Act
        var result = await function.RunManual(request, "manual-user");

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe("[RolloverCandidatesFunction] -> 3 rollover candidates created.");
        jobConfigurationService.Verify(x => x.UpdateJobRun("manual-user", jobId, jobRunId, 3, JobStatus.Completed), Times.Once);
    }

    [Fact]
    public async Task RunManual_ReturnsSuccessfulNoOp_WhenNoCandidatesAreCreated()
    {
        // Arrange
        var logger = new Mock<ILogger<RolloverCandidatesFunction>>();
        var service = new Mock<IRolloverCandidateService>();
        var jobConfigurationService = new Mock<IJobConfigurationService>();
        var jobId = Guid.NewGuid();
        var jobRunId = Guid.NewGuid();
        var functionContext = new Mock<FunctionContext>().Object;

        jobConfigurationService
            .Setup(x => x.ReadJobConfiguration(JobNames.RolloverCandidates))
            .ReturnsAsync(new JobControl
            {
                JobEnabled = true,
                JobId = jobId,
                Status = JobStatus.Completed.ToString()
            });
        jobConfigurationService
            .Setup(x => x.InsertJobRunAsync(jobId, "manual-user", JobStatus.Running))
            .ReturnsAsync(jobRunId);
        service.Setup(x => x.GenerateRolloverCandidatesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var function = new RolloverCandidatesFunction(logger.Object, service.Object, jobConfigurationService.Object);
        var request = new MockHttpRequestData(functionContext);

        // Act
        var result = await function.RunManual(request, "manual-user");

        // Assert
        var okResult = result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe("[RolloverCandidatesFunction] -> No qualification versions were added as rollover candidates.");
        jobConfigurationService.Verify(x => x.UpdateJobRun("manual-user", jobId, jobRunId, 0, JobStatus.Completed), Times.Once);
    }

    [Fact]
    public async Task Run_RethrowsExceptionAndMarksJobRunAsError_WhenGenerationFails()
    {
        // Arrange
        var logger = new Mock<ILogger<RolloverCandidatesFunction>>();
        var service = new Mock<IRolloverCandidateService>();
        var jobConfigurationService = new Mock<IJobConfigurationService>();
        var jobId = Guid.NewGuid();
        var jobRunId = Guid.NewGuid();
        var expectedException = new InvalidOperationException("Generation failed");

        jobConfigurationService
            .Setup(x => x.ReadJobConfiguration(JobNames.RolloverCandidates))
            .ReturnsAsync(new JobControl
            {
                JobEnabled = true,
                JobId = jobId,
                Status = JobStatus.Completed.ToString()
            });
        jobConfigurationService
            .Setup(x => x.InsertJobRunAsync(jobId, "SYSTEM", JobStatus.Running))
            .ReturnsAsync(jobRunId);
        service.Setup(x => x.GenerateRolloverCandidatesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var function = new RolloverCandidatesFunction(logger.Object, service.Object, jobConfigurationService.Object);

        var timerInfo = new TimerInfo();
        var functionContext = new Mock<FunctionContext>().Object;

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => function.Run(timerInfo, functionContext));

        // Assert
        exception.ShouldBe(expectedException);
        jobConfigurationService.Verify(x => x.UpdateJobRun("SYSTEM", jobId, jobRunId, 0, JobStatus.Error), Times.Once);
    }
}
