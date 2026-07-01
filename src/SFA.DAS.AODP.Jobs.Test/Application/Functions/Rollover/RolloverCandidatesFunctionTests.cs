using SFA.DAS.AODP.Jobs.Functions.Abstractions;
using SFA.DAS.AODP.Jobs.Functions.Rollover;
using SFA.DAS.AODP.Jobs.Interfaces.Rollover;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Functions.Rollover;

public class RolloverCandidatesFunctionTests
{
    [Fact]
    public async Task Run_GeneratesRolloverCandidatesAndUpdatesJobRun()
    {
        // Arrange
        var logger = new Mock<ILogger<RolloverCandidatesFunction>>();
        var service = new Mock<IRolloverCandidateService>();
        var jobFunctionRunnerMock = new Mock<IJobFunctionRunner>();
        
        jobFunctionRunnerMock.Setup(x => x.RunAsync("RolloverCandidatesFunction", "SYSTEM", JobNames.RolloverCandidates, It.IsAny<Func<JobControl, CancellationToken, Task<int>>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OkObjectResult(10));

        var function = new RolloverCandidatesFunction(logger.Object, service.Object, jobFunctionRunnerMock.Object);

        var timerInfo = new TimerInfo
        {
            ScheduleStatus = new ScheduleStatus
            {
                Next = DateTime.UtcNow.AddYears(1)
            }
        };
        var functionContext = new Mock<FunctionContext>().Object;

        // Act
        var result = await function.Run(timerInfo, functionContext);

        // Assert
        result.ShouldBeOfType<OkObjectResult>();
        jobFunctionRunnerMock.Verify(
            x => x.RunAsync(
                "RolloverCandidatesFunction",
                "SYSTEM",
                JobNames.RolloverCandidates,
                It.IsAny<Func<JobControl, CancellationToken, Task<int>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    
    [Fact]
    public async Task Run_RethrowsExceptionAndMarksJobRunAsError_WhenGenerationFails()
    {
        // Arrange
        var logger = new Mock<ILogger<RolloverCandidatesFunction>>();
        var service = new Mock<IRolloverCandidateService>();
        var expectedException = new InvalidOperationException("Generation failed");

        var jobFunctionRunnerMock = new Mock<IJobFunctionRunner>();

        jobFunctionRunnerMock.Setup(x => x.RunAsync("RolloverCandidatesFunction", "SYSTEM", JobNames.RolloverCandidates,
                It.IsAny<Func<JobControl, CancellationToken, Task<int>>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(expectedException);

        var function = new RolloverCandidatesFunction(logger.Object, service.Object, jobFunctionRunnerMock.Object);

        var timerInfo = new TimerInfo();
        var functionContext = new Mock<FunctionContext>().Object;

        // Act
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => function.Run(timerInfo, functionContext));

        // Assert
        exception.ShouldBe(expectedException);
        jobFunctionRunnerMock.Verify(
            x => x.RunAsync(
                "RolloverCandidatesFunction",
                "SYSTEM",
                JobNames.RolloverCandidates,
                It.IsAny<Func<JobControl, CancellationToken, Task<int>>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
