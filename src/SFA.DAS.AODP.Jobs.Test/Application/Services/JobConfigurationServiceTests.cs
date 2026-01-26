using Moq;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Interfaces;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.Funding.ApprenticeshipEarnings.Domain.Services;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services;

public class JobConfigurationServiceTests
{
    [Fact]
    public async Task UpdateJobRun_CallsRepositoryWhenIdsProvided()
    {
        // Arrange
        var repoMock = new Mock<IJobsRepository>();
        repoMock.Setup(r => r.UpdateJobRunAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(true);
        repoMock.Setup(r => r.UpdateJobAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<string>()))
                .ReturnsAsync(true);

        var clockMock = new Mock<ISystemClockService>();
        var now = DateTime.UtcNow;
        clockMock.Setup(c => c.UtcNow).Returns(now);

        var svc = new JobConfigurationService(repoMock.Object, clockMock.Object);

        var jobId = Guid.NewGuid();
        var jobRunId = Guid.NewGuid();
        var username = "tester";
        var totalRecords = 42;
        var status = SFA.DAS.AODP.Common.Enum.JobStatus.Completed;

        // Act
        await svc.UpdateJobRun(username, jobId, jobRunId, totalRecords, status);

        // Assert
        repoMock.Verify(r => r.UpdateJobRunAsync(jobRunId, username, now, status.ToString(), totalRecords), Times.Once);
        repoMock.Verify(r => r.UpdateJobAsync(jobId, now, status.ToString()), Times.Once);
    }

    [Fact]
    public async Task UpdateJobRun_DoesNotCallRepositoryWhenIdsEmpty()
    {
        // Arrange
        var repoMock = new Mock<IJobsRepository>();
        var clockMock = new Mock<ISystemClockService>();
        clockMock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        var svc = new JobConfigurationService(repoMock.Object, clockMock.Object);

        // Act
        await svc.UpdateJobRun("u", Guid.Empty, Guid.Empty, 0, SFA.DAS.AODP.Common.Enum.JobStatus.Error);

        // Assert
        repoMock.Verify(r => r.UpdateJobRunAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        repoMock.Verify(r => r.UpdateJobAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ReadRegulatedJobConfiguration_NoJob_ReturnsDefaults()
    {
        // Arrange
        var repoMock = new Mock<IJobsRepository>();
        repoMock.Setup(r => r.GetJobByNameAsync(It.IsAny<string>())).ReturnsAsync((Job?)null);

        var svc = new JobConfigurationService(repoMock.Object, Mock.Of<ISystemClockService>());

        // Act
        var result = await svc.ReadRegulatedJobConfiguration();

        // Assert
        Assert.False(result.JobEnabled);
        Assert.Equal(Guid.Empty, result.JobId);
        Assert.False(result.RunApiImport);
        Assert.False(result.ProcessStagingData);
        Assert.Equal(string.Empty, result.Status);
    }

    [Fact]
    public async Task ReadRegulatedJobConfiguration_ParsesConfigurations_WhenPresent()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new Job { Id = jobId, Enabled = true, Status = "Active" };

        var configs = new List<JobConfiguration>
        {
            new JobConfiguration { Name = "ApiImport", Value = "true" },
            new JobConfiguration { Name = "ProcessStagingData", Value = "true" }
        };

        var repoMock = new Mock<IJobsRepository>();
        repoMock.Setup(r => r.GetJobByNameAsync(It.IsAny<string>())).ReturnsAsync(job);
        repoMock.Setup(r => r.GetJobConfigurationsByIdAsync(jobId)).ReturnsAsync(configs);

        var svc = new JobConfigurationService(repoMock.Object, Mock.Of<ISystemClockService>());

        // Act
        var result = await svc.ReadRegulatedJobConfiguration();

        // Assert
        Assert.True(result.JobEnabled);
        Assert.Equal(jobId, result.JobId);
        Assert.True(result.RunApiImport);
        Assert.True(result.ProcessStagingData);
        Assert.Equal("Active", result.Status);
    }

    [Fact]
    public async Task ReadFundedJobConfiguration_NoJob_ReturnsDefaults()
    {
        // Arrange
        var repoMock = new Mock<IJobsRepository>();
        repoMock.Setup(r => r.GetJobByNameAsync(It.IsAny<string>())).ReturnsAsync((Job?)null);

        var svc = new JobConfigurationService(repoMock.Object, Mock.Of<ISystemClockService>());

        // Act
        var result = await svc.ReadFundedJobConfiguration();

        // Assert
        Assert.False(result.JobEnabled);
        Assert.Equal(Guid.Empty, result.JobId);
        Assert.False(result.ImportFundedCsv);
        Assert.False(result.ImportArchivedCsv);
        Assert.Equal(string.Empty, result.Status);
    }

    [Fact]
    public async Task ReadFundedJobConfiguration_ParsesConfigurations_WhenPresent()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new Job { Id = jobId, Enabled = true, Status = "OK" };

        var configs = new List<JobConfiguration>
        {
            new JobConfiguration { Name = "ImportFundedCsv", Value = "true" },
            new JobConfiguration { Name = "ImportArchivedCsv", Value = "true" }
        };

        var repoMock = new Mock<IJobsRepository>();
        repoMock.Setup(r => r.GetJobByNameAsync(It.IsAny<string>())).ReturnsAsync(job);
        repoMock.Setup(r => r.GetJobConfigurationsByIdAsync(jobId)).ReturnsAsync(configs);

        var svc = new JobConfigurationService(repoMock.Object, Mock.Of<ISystemClockService>());

        // Act
        var result = await svc.ReadFundedJobConfiguration();

        // Assert
        Assert.True(result.JobEnabled);
        Assert.Equal(jobId, result.JobId);
        Assert.True(result.ImportFundedCsv);
        Assert.True(result.ImportArchivedCsv);
        Assert.Equal("OK", result.Status);
    }

    [Fact]
    public async Task InsertJobRunAsync_CallsRepositoryAndReturnsGuid()
    {
        // Arrange
        var repoMock = new Mock<IJobsRepository>();
        var expected = Guid.NewGuid();
        repoMock.Setup(r => r.InsertJobRunAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<string>()))
                .ReturnsAsync(expected);

        var now = DateTime.UtcNow;
        var clockMock = new Mock<ISystemClockService>();
        clockMock.Setup(c => c.UtcNow).Returns(now);

        var svc = new JobConfigurationService(repoMock.Object, clockMock.Object);

        // Act
        var result = await svc.InsertJobRunAsync(Guid.NewGuid(), "user", SFA.DAS.AODP.Common.Enum.JobStatus.Running);

        // Assert
        Assert.Equal(expected, result);
        repoMock.Verify(r => r.InsertJobRunAsync(It.IsAny<Guid>(), "user", now, SFA.DAS.AODP.Common.Enum.JobStatus.Running.ToString()), Times.Once);
    }

    [Fact]
    public async Task GetLastJobRunAsync_MapsRecordToControl()
    {
        // Arrange
        var jobRunId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var jr = new JobRun
        {
            Id = jobRunId,
            JobId = jobId,
            Status = "Completed",
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow,
            User = "bob",
            RecordsProcessed = 7
        };

        var repoMock = new Mock<IJobsRepository>();
        repoMock.Setup(r => r.GetLastJobRunsAsync(It.IsAny<string>())).ReturnsAsync(jr);

        var svc = new JobConfigurationService(repoMock.Object, Mock.Of<ISystemClockService>());

        // Act
        var result = await svc.GetLastJobRunAsync("any");

        // Assert
        Assert.Equal(jobRunId, result.Id);
        Assert.Equal(jobId, result.JobId);
        Assert.Equal("Completed", result.Status);
        Assert.Equal(jr.StartTime, result.StartTime);
        Assert.Equal(jr.EndTime, result.EndTime);
        Assert.Equal("bob", result.User);
        Assert.Equal(7, result.RecordsProcessed);
    }

    [Fact]
    public async Task ReadPldnsImportConfiguration_NoJob_ReturnsDefaults()
    {
        // Arrange
        var repoMock = new Mock<IJobsRepository>();
        repoMock.Setup(r => r.GetJobByNameAsync(It.IsAny<string>())).ReturnsAsync((Job?)null);

        var svc = new JobConfigurationService(repoMock.Object, Mock.Of<ISystemClockService>());

        // Act
        var result = await svc.ReadPldnsImportConfiguration();

        // Assert
        Assert.False(result.JobEnabled);
        Assert.Equal(Guid.Empty, result.JobId);
        Assert.Equal(string.Empty, result.Status);
        Assert.Equal(Guid.Empty, result.JobRunId);
        Assert.False(result.ImportPldns);
    }

    [Fact]
    public async Task ReadPldnsImportConfiguration_ParsesConfiguration_WhenPresent()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new Job { Id = jobId, Enabled = true, Status = "S" };

        var configs = new List<JobConfiguration>
        {
            new JobConfiguration { Name = "ImportPldns", Value = "true" }
        };

        var repoMock = new Mock<IJobsRepository>();
        repoMock.Setup(r => r.GetJobByNameAsync(It.IsAny<string>())).ReturnsAsync(job);
        repoMock.Setup(r => r.GetJobConfigurationsByIdAsync(jobId)).ReturnsAsync(configs);

        var svc = new JobConfigurationService(repoMock.Object, Mock.Of<ISystemClockService>());

        // Act
        var result = await svc.ReadPldnsImportConfiguration();

        // Assert
        Assert.True(result.JobEnabled);
        Assert.Equal(jobId, result.JobId);
        Assert.Equal("S", result.Status);
        Assert.Equal(jobId, result.JobRunId);
        Assert.True(result.ImportPldns);
    }

    [Fact]
    public async Task ReadDefundingListImportConfiguration_NoJob_ReturnsDefaults()
    {
        // Arrange
        var repoMock = new Mock<IJobsRepository>();
        repoMock.Setup(r => r.GetJobByNameAsync(It.IsAny<string>())).ReturnsAsync((Job?)null);

        var svc = new JobConfigurationService(repoMock.Object, Mock.Of<ISystemClockService>());

        // Act
        var result = await svc.ReadDefundingListImportConfiguration();

        // Assert
        Assert.False(result.JobEnabled);
        Assert.Equal(Guid.Empty, result.JobId);
        Assert.Equal(string.Empty, result.Status);
        Assert.Equal(Guid.Empty, result.JobRunId);
        Assert.False(result.ImportDefundingList);
    }

    [Fact]
    public async Task ReadDefundingListImportConfiguration_ParsesConfiguration_WhenPresent()
    {
        // Arrange
        var jobId = Guid.NewGuid();
        var job = new Job { Id = jobId, Enabled = true, Status = "S2" };

        var configs = new List<JobConfiguration>
        {
            new JobConfiguration { Name = "ImportDefundingList", Value = "true" }
        };

        var repoMock = new Mock<IJobsRepository>();
        repoMock.Setup(r => r.GetJobByNameAsync(It.IsAny<string>())).ReturnsAsync(job);
        repoMock.Setup(r => r.GetJobConfigurationsByIdAsync(jobId)).ReturnsAsync(configs);

        var svc = new JobConfigurationService(repoMock.Object, Mock.Of<ISystemClockService>());

        // Act
        var result = await svc.ReadDefundingListImportConfiguration();

        // Assert
        Assert.True(result.JobEnabled);
        Assert.Equal(jobId, result.JobId);
        Assert.Equal("S2", result.Status);
        Assert.Equal(jobId, result.JobRunId);
        Assert.True(result.ImportDefundingList);
    }
}