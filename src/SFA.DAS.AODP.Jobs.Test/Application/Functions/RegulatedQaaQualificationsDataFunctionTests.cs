using SFA.DAS.AODP.Jobs.Functions.Abstractions;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Functions;

public class RegulatedQaaQualificationsDataFunctionTests
{
    private readonly Mock<IQaaQualificationImportService> _qualificationImportServiceMock;
    private readonly Mock<IJobFunctionRunner> _jobFunctionRunnerMock;
    private readonly RegulatedQaaQualificationsDataFunction _function;
    private readonly Mock<FunctionContext> _functionContextMock;

    public RegulatedQaaQualificationsDataFunctionTests()
    {
        var loggerMock = new Mock<ILogger<RegulatedQaaQualificationsDataFunction>>();
        _qualificationImportServiceMock = new Mock<IQaaQualificationImportService>();
        _jobFunctionRunnerMock = new Mock<IJobFunctionRunner>();
        _functionContextMock = new Mock<FunctionContext>();
        
        _function = new RegulatedQaaQualificationsDataFunction(
            loggerMock.Object,
            _qualificationImportServiceMock.Object,
            _jobFunctionRunnerMock.Object
        );
    }

    [Fact]
    public async Task Run_Should_Call_JobFunctionRunner_With_Correct_Parameters_And_CancellationToken()
    {
        // Arrange
        var timerInfo = new TimerInfo
        {
            ScheduleStatus = new ScheduleStatus
            {
                Last = DateTime.UtcNow.AddHours(-1), 
                Next = DateTime.UtcNow.AddHours(1),
                LastUpdated = DateTime.UtcNow.AddHours(-1)
            },
            IsPastDue = false
        };

        var cancellationToken = CancellationToken.None;
        var expectedResult = new OkObjectResult("Success");

        _functionContextMock.SetupGet(x => x.CancellationToken).Returns(cancellationToken);

        _jobFunctionRunnerMock
            .Setup(x => x.RunAsync(
                "RegulatedQaaQualificationsDataFunction",
                "SYSTEM",
                JobNames.QaaQualifications,
                It.IsAny<Func<JobControl, CancellationToken, Task<int>>>(),
                cancellationToken))
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _function.Run(timerInfo, _functionContextMock.Object);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        Assert.Equal(expectedResult, result);
        _jobFunctionRunnerMock.Verify(
            x => x.RunAsync(
                "RegulatedQaaQualificationsDataFunction",
                "SYSTEM",
                JobNames.QaaQualifications,
                It.IsAny<Func<JobControl, CancellationToken, Task<int>>>(),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Run_Should_Import_When_RunApiImport_Is_True()
    {
        // Arrange
        var timerInfo = new TimerInfo
        {
            ScheduleStatus = new ScheduleStatus
            {
                Last = DateTime.UtcNow.AddHours(-1),
                Next = DateTime.UtcNow.AddHours(1),
                LastUpdated = DateTime.UtcNow.AddHours(-1)
            },
            IsPastDue = false
        };

        var jobControl = new QaaQualificationJobControl { RunApiImport = true };
        var cancellationToken = CancellationToken.None;
        var expectedRecordsCount = 150;
        var expectedResult = new OkObjectResult("Success");

        _functionContextMock.SetupGet(x => x.CancellationToken).Returns(cancellationToken);

        _qualificationImportServiceMock
            .Setup(x => x.ImportDataAsync(cancellationToken))
            .ReturnsAsync(expectedRecordsCount);

        _jobFunctionRunnerMock
            .Setup(x => x.RunAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<JobNames>(),
                It.IsAny<Func<JobControl, CancellationToken, Task<int>>>(),
                cancellationToken))
            .Callback<string, string, JobNames, Func<JobControl, CancellationToken, Task<int>>, CancellationToken>(
                async (_, _, _, importDelegate, ct) => 
                {
                    // Invoke the delegate to execute the import logic
                    await importDelegate(jobControl, ct);
                })
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _function.Run(timerInfo, _functionContextMock.Object);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _qualificationImportServiceMock.Verify(
            x => x.ImportDataAsync(cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task Run_Should_Not_Import_When_RunApiImport_Is_False()
    {
        // Arrange
        var timerInfo = new TimerInfo
        {
            ScheduleStatus = new ScheduleStatus
            {
                Last = DateTime.UtcNow.AddHours(-1),
                Next = DateTime.UtcNow.AddHours(1),
                LastUpdated = DateTime.UtcNow.AddHours(-1)
            },
            IsPastDue = false
        };

        var jobControl = new QaaQualificationJobControl { RunApiImport = false };
        var cancellationToken = CancellationToken.None;
        var expectedResult = new OkObjectResult("Success");

        _functionContextMock.SetupGet(x => x.CancellationToken).Returns(cancellationToken);

        _jobFunctionRunnerMock
            .Setup(x => x.RunAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<JobNames>(),
                It.IsAny<Func<JobControl, CancellationToken, Task<int>>>(),
                cancellationToken))
            .Callback<string, string, JobNames, Func<JobControl, CancellationToken, Task<int>>, CancellationToken>(
                async (_, _, _, importDelegate, ct) => 
                {
                    // Invoke the delegate with RunApiImport = false
                    await importDelegate(jobControl, ct);
                })
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _function.Run(timerInfo, _functionContextMock.Object);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _qualificationImportServiceMock.Verify(
            x => x.ImportDataAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(5000)]
    public async Task Run_Delegate_Should_Return_Correct_Records_Count(int recordsCount)
    {
        // Arrange
        var timerInfo = new TimerInfo
        {
            ScheduleStatus = new ScheduleStatus
            {
                Last = DateTime.UtcNow.AddHours(-1),
                Next = DateTime.UtcNow.AddHours(1),
                LastUpdated = DateTime.UtcNow.AddHours(-1)
            },
            IsPastDue = false
        };

        var jobControl = new QaaQualificationJobControl { RunApiImport = true };
        var cancellationToken = CancellationToken.None;
        var delegateResult = 0;
        var expectedResult = new OkObjectResult("Success");

        _functionContextMock.SetupGet(x => x.CancellationToken).Returns(cancellationToken);

        _qualificationImportServiceMock
            .Setup(x => x.ImportDataAsync(cancellationToken))
            .ReturnsAsync(recordsCount);

        _jobFunctionRunnerMock
            .Setup(x => x.RunAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<JobNames>(),
                It.IsAny<Func<JobControl, CancellationToken, Task<int>>>(),
                cancellationToken))
            .Callback<string, string, JobNames, Func<JobControl, CancellationToken, Task<int>>, CancellationToken>(
                async (_, _, _, importDelegate, ct) => 
                {
                    delegateResult = await importDelegate(jobControl, ct);
                })
            .ReturnsAsync(expectedResult);

        // Act
        var result = await _function.Run(timerInfo, _functionContextMock.Object);

        // Assert
        Assert.Equal(recordsCount, delegateResult);
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task Run_Should_Propagate_ImportService_Exception()
    {
        // Arrange
        var timerInfo = new TimerInfo
        {
            ScheduleStatus = new ScheduleStatus
            {
                Last = DateTime.UtcNow.AddHours(-1),
                Next = DateTime.UtcNow.AddHours(1),
                LastUpdated = DateTime.UtcNow.AddHours(-1)
            },
            IsPastDue = false
        };

        var cancellationToken = CancellationToken.None;

        _functionContextMock.SetupGet(x => x.CancellationToken).Returns(cancellationToken);

        _jobFunctionRunnerMock
            .Setup(x => x.RunAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<JobNames>(),
                It.IsAny<Func<JobControl, CancellationToken, Task<int>>>(),
                cancellationToken))
            .Throws<Exception>();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _function.Run(timerInfo, _functionContextMock.Object));
    }
}