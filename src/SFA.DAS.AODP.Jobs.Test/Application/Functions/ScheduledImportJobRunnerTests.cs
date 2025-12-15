using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Jobs.Functions;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Models.Config;
using SFA.DAS.Funding.ApprenticeshipEarnings.Domain.Services;

namespace SFA.DAS.AODP.Jobs.Test.Application.Functions;

public class ScheduledImportJobRunnerTests
{
    private readonly Mock<ILogger<ScheduledImportJobRunner>> _loggerMock;
    private readonly AodpJobsConfiguration _configuration;
    private readonly Mock<ISchedulerClientService> _schedulerClientService;   
    private readonly Mock<ISystemClockService> _systemClockService;
    private readonly Mock<IJobConfigurationService> _jobConfigurationService;
    private readonly ScheduledImportJobRunner _function;
    private readonly FunctionContext _functionContext;
    private string fundedJobName = "approvedQualificationsImport";
    private string fundedJobUrl = "api/approvedQualificationsImport";
    private string regulatedJobName = "regulatedQualificationsImport";
    private string regulatedJobUrl = "gov/regulatedQualificationsImport";

    public ScheduledImportJobRunnerTests()
    {
        _loggerMock = new Mock<ILogger<ScheduledImportJobRunner>>();
        _configuration = new AodpJobsConfiguration() 
        { 
            Environment = "Dev", 
            FunctionAppBaseUrl = "https://locatohost:7001", 
            FunctionHostKey = "???"
        };
        _schedulerClientService = new Mock<ISchedulerClientService>();        
        _functionContext = new Mock<FunctionContext>().Object;
        _systemClockService = new Mock<ISystemClockService>();
        _systemClock_service_setup();
        _jobConfigurationService = new Mock<IJobConfigurationService>();

        _function = new ScheduledImportJobRunner(
            _loggerMock.Object,           
            _jobConfigurationService.Object,
            _configuration,
            _schedulerClientService.Object,
            _systemClockService.Object
        );
    }

    private void _systemClock_service_setup()
    {
        _systemClockService.SetupGet(s => s.UtcNow).Returns(DateTime.UtcNow);
    }

    [Fact]
    public async Task Run_Should_Do_Nothing_When_No_Jobs_requested()
    {
        // Arrange            
        var regulatedJobControl = new RegulatedJobControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), JobEnabled = true, ProcessStagingData = true, RunApiImport = true };
        var fundedJobControl = new FundedJobControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), JobEnabled = true, ImportFundedCsv = true, ImportArchivedCsv = true };
        var pldnsControl = new PldnsImportControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), ImportPldns = true, JobEnabled = true };
        var defundingListImportControl = new DefundingListImportControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), ImportDefundingList = true, JobEnabled = true };
        var lastRegulatedJobRun = new JobRunControl() { Id = Guid.NewGuid(), JobId = regulatedJobControl.JobId, Status = JobStatus.Completed.ToString() };
        var lastFundedJobRun = new JobRunControl() { Id = Guid.NewGuid(), JobId = fundedJobControl.JobId, Status = JobStatus.Completed.ToString() };

        _jobConfigurationService.Setup(s => s.ReadRegulatedJobConfiguration()).ReturnsAsync(regulatedJobControl).Verifiable();
        _jobConfigurationService.Setup(s => s.ReadFundedJobConfiguration()).ReturnsAsync(fundedJobControl).Verifiable();
        _jobConfigurationService.Setup(s => s.ReadPldnsImportConfiguration()).ReturnsAsync(pldnsControl).Verifiable();
        _jobConfigurationService.Setup(s => s.GetLastJobRunAsync(JobNames.RegulatedQualifications.ToString())).ReturnsAsync(lastRegulatedJobRun).Verifiable();
        _jobConfigurationService.Setup(s => s.GetLastJobRunAsync(JobNames.FundedQualifications.ToString())).ReturnsAsync(lastFundedJobRun).Verifiable();
        _jobConfigurationService.Setup(s => s.ReadDefundingListImportConfiguration()).ReturnsAsync(defundingListImportControl).Verifiable();

        // Act
        var result = await _function.Run(new TimerInfo());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _jobConfigurationService.VerifyAll();        
    }

    [Fact]
    public async Task Run_Should_Execute_RegulatedJob()
    {
        // Arrange
        var userName = "TestUser";
        var regulatedJobControl = new RegulatedJobControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), JobEnabled = true, ProcessStagingData = true, RunApiImport = true };
        var fundedJobControl = new FundedJobControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), JobEnabled = true, ImportFundedCsv = true, ImportArchivedCsv = true };
        var pldnsControl = new PldnsImportControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), ImportPldns = true, JobEnabled = true };
        var defundingListImportControl = new DefundingListImportControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), ImportDefundingList = true, JobEnabled = true };
        var lastRegulatedJobRun = new JobRunControl() { Id = Guid.NewGuid(), JobId = regulatedJobControl.JobId, Status = JobStatus.Requested.ToString(), User = userName };
        var lastFundedJobRun = new JobRunControl() { Id = Guid.NewGuid(), JobId = fundedJobControl.JobId, Status = JobStatus.Completed.ToString(), User = userName };

        _jobConfigurationService.Setup(s => s.ReadRegulatedJobConfiguration()).ReturnsAsync(regulatedJobControl).Verifiable();
        _jobConfigurationService.Setup(s => s.ReadFundedJobConfiguration()).ReturnsAsync(fundedJobControl).Verifiable();
        _jobConfigurationService.Setup(s => s.ReadPldnsImportConfiguration()).ReturnsAsync(pldnsControl).Verifiable();
        _jobConfigurationService.Setup(s => s.GetLastJobRunAsync(JobNames.RegulatedQualifications.ToString())).ReturnsAsync(lastRegulatedJobRun).Verifiable();
        _jobConfigurationService.Setup(s => s.GetLastJobRunAsync(JobNames.FundedQualifications.ToString())).ReturnsAsync(lastFundedJobRun).Verifiable();        
        _jobConfigurationService.Setup(s => s.UpdateJobRun(userName, regulatedJobControl.JobId, lastRegulatedJobRun.Id, 0, Common.Enum.JobStatus.RequestSent)).Verifiable();
        _schedulerClientService.Setup(s => s.ExecuteFunction(It.Is<JobRunControl>(j => j.Id == lastRegulatedJobRun.Id), regulatedJobName, regulatedJobUrl)).ReturnsAsync(true).Verifiable();
        _jobConfigurationService.Setup(s => s.ReadDefundingListImportConfiguration()).ReturnsAsync(defundingListImportControl).Verifiable();

        // Act
        var result = await _function.Run(new TimerInfo());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _jobConfigurationService.VerifyAll();
        _schedulerClientService.VerifyAll();
    }

    [Fact]
    public async Task Run_Should_Execute_FundedJob()
    {
        // Arrange
        var userName = "TestUser";
        var regulatedJobControl = new RegulatedJobControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), JobEnabled = true, ProcessStagingData = true, RunApiImport = true };
        var fundedJobControl = new FundedJobControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), JobEnabled = true, ImportFundedCsv = true, ImportArchivedCsv = true };
        var pldnsControl = new PldnsImportControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), ImportPldns = true, JobEnabled = true };
        var defundingListImportControl = new DefundingListImportControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), ImportDefundingList = true, JobEnabled = true };
        var lastRegulatedJobRun = new JobRunControl() { Id = Guid.NewGuid(), JobId = regulatedJobControl.JobId, Status = JobStatus.Completed.ToString(), User = userName };
        var lastFundedJobRun = new JobRunControl() { Id = Guid.NewGuid(), JobId = fundedJobControl.JobId, Status = JobStatus.Requested.ToString(), User = userName };

        _jobConfigurationService.Setup(s => s.ReadRegulatedJobConfiguration()).ReturnsAsync(regulatedJobControl).Verifiable();
        _jobConfigurationService.Setup(s => s.ReadFundedJobConfiguration()).ReturnsAsync(fundedJobControl).Verifiable();
        _jobConfigurationService.Setup(s => s.ReadPldnsImportConfiguration()).ReturnsAsync(pldnsControl).Verifiable();
        _jobConfigurationService.Setup(s => s.GetLastJobRunAsync(JobNames.RegulatedQualifications.ToString())).ReturnsAsync(lastRegulatedJobRun).Verifiable();
        _jobConfigurationService.Setup(s => s.GetLastJobRunAsync(JobNames.FundedQualifications.ToString())).ReturnsAsync(lastFundedJobRun).Verifiable();
        _jobConfigurationService.Setup(s => s.UpdateJobRun(userName, fundedJobControl.JobId, lastFundedJobRun.Id, 0, Common.Enum.JobStatus.RequestSent)).Verifiable();
        _jobConfigurationService.Setup(s => s.ReadDefundingListImportConfiguration()).ReturnsAsync(defundingListImportControl).Verifiable();
        _schedulerClientService.Setup(s => s.ExecuteFunction(It.Is<JobRunControl>(j => j.Id == lastFundedJobRun.Id), fundedJobName, fundedJobUrl)).ReturnsAsync(true).Verifiable();

        // Act
        var result = await _function.Run(new TimerInfo());

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        _jobConfigurationService.VerifyAll();
        _schedulerClientService.VerifyAll();
    }

    [Fact]
    public async Task Run_Should_Return_BadRequest_When_Function_Call_Fails()
    {
        // Arrange
        var userName = "TestUser";
        var regulatedJobControl = new RegulatedJobControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), JobEnabled = true, ProcessStagingData = true, RunApiImport = true };
        var fundedJobControl = new FundedJobControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), JobEnabled = true, ImportFundedCsv = true, ImportArchivedCsv = true };
        var pldnsControl = new PldnsImportControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), ImportPldns = true, JobEnabled = true };
        var defundingListImportControl = new DefundingListImportControl() { JobId = Guid.NewGuid(), JobRunId = Guid.NewGuid(), ImportDefundingList = true, JobEnabled = true };
        var lastRegulatedJobRun = new JobRunControl() { Id = Guid.NewGuid(), JobId = regulatedJobControl.JobId, Status = JobStatus.Completed.ToString(), User = userName };
        var lastFundedJobRun = new JobRunControl() { Id = Guid.NewGuid(), JobId = fundedJobControl.JobId, Status = JobStatus.Requested.ToString(), User = userName };

        _jobConfigurationService.Setup(s => s.ReadRegulatedJobConfiguration()).ReturnsAsync(regulatedJobControl).Verifiable();
        _jobConfigurationService.Setup(s => s.ReadFundedJobConfiguration()).ReturnsAsync(fundedJobControl).Verifiable();
        _jobConfigurationService.Setup(s => s.ReadPldnsImportConfiguration()).ReturnsAsync(pldnsControl).Verifiable();
        _jobConfigurationService.Setup(s => s.GetLastJobRunAsync(JobNames.RegulatedQualifications.ToString())).ReturnsAsync(lastRegulatedJobRun).Verifiable();
        _jobConfigurationService.Setup(s => s.GetLastJobRunAsync(JobNames.FundedQualifications.ToString())).ReturnsAsync(lastFundedJobRun).Verifiable();
        _jobConfigurationService.Setup(s => s.UpdateJobRun(userName, fundedJobControl.JobId, lastFundedJobRun.Id, 0, Common.Enum.JobStatus.RequestSent)).Verifiable();
        _jobConfigurationService.Setup(s => s.ReadDefundingListImportConfiguration()).ReturnsAsync(defundingListImportControl).Verifiable();
        _schedulerClientService.Setup(s => s.ExecuteFunction(It.Is<JobRunControl>(j => j.Id == lastFundedJobRun.Id), fundedJobName, fundedJobUrl)).ReturnsAsync(false).Verifiable();

        // Act
        var result = await _function.Run(new TimerInfo());

        // Assert
        var okResult = Assert.IsType<BadRequestObjectResult>(result);
        _jobConfigurationService.VerifyAll();
        _schedulerClientService.VerifyAll();
    }
}
