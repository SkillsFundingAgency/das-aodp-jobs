using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Data.Repositories.Jobs;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services;

public class FileProcessingServiceTests
{
    private readonly Mock<IFileRecordRepository> _fileRepo;
    private readonly Mock<IBlobStorageFileService> _blobService;
    private readonly Mock<IJobConfigurationService> _jobService;
    private readonly Mock<ISystemClockService> _clock;

    private readonly FileProcessingService _service;

    public FileProcessingServiceTests()
    {
        _fileRepo = new Mock<IFileRecordRepository>();
        _blobService = new Mock<IBlobStorageFileService>();
        _jobService = new Mock<IJobConfigurationService>();
        _clock = new Mock<ISystemClockService>();

        _service = new FileProcessingService(
            _fileRepo.Object,
            _blobService.Object,
            _jobService.Object,
            _clock.Object);
    }

    [Fact]
    public async Task GetReadyFileAsync_ReturnsNotReady_WhenFileMissing()
    {
        _fileRepo
            .Setup(r => r.GetByCategoryAsync(It.IsAny<FileCategory>()))
            .ReturnsAsync((FileRecord?)null);

        _clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        var result = await _service.GetReadyFileAsync(
            FileCategory.DefundingList,
            "user",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            CancellationToken.None);

        Assert.False(result.IsReady);
        Assert.False(result.IsTimedOut);
        Assert.Null(result.Stream);
    }

    [Fact]
    public async Task GetReadyFileAsync_ReturnsNotReady_WhenFileNotClean()
    {
        _fileRepo
            .Setup(r => r.GetByCategoryAsync(It.IsAny<FileCategory>()))
            .ReturnsAsync(new FileRecord
            {
                ScanResult = MalwareScanStatus.NotScanned
            });

        _clock.Setup(c => c.UtcNow).Returns(DateTime.UtcNow);

        var result = await _service.GetReadyFileAsync(
            FileCategory.DefundingList,
            "user",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            CancellationToken.None);

        Assert.False(result.IsReady);
        Assert.False(result.IsTimedOut);
        Assert.Null(result.Stream);
    }

    [Fact]
    public async Task GetReadyFileAsync_ReturnsTimedOut_WhenNotReadyAndOlderThanOneHour()
    {
        _fileRepo
            .Setup(r => r.GetByCategoryAsync(It.IsAny<FileCategory>()))
            .ReturnsAsync((FileRecord?)null);

        var now = DateTime.UtcNow;

        _clock.Setup(c => c.UtcNow).Returns(now);

        var startTime = now.AddHours(-2); // older than 1 hour

        var jobId = Guid.NewGuid();
        var jobRunId = Guid.NewGuid();

        var result = await _service.GetReadyFileAsync(
            FileCategory.DefundingList,
            "user",
            jobId,
            jobRunId,
            startTime,
            CancellationToken.None);

        Assert.False(result.IsReady);
        Assert.True(result.IsTimedOut);
        Assert.Null(result.Stream);

        _jobService.Verify(s =>
            s.UpdateJobRun("user", jobId, jobRunId, 0, JobStatus.Error),
            Times.Once);
    }

    [Fact]
    public async Task GetReadyFileAsync_ReturnsStream_WhenFileIsClean()
    {
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        _fileRepo
            .Setup(r => r.GetByCategoryAsync(It.IsAny<FileCategory>()))
            .ReturnsAsync(new FileRecord
            {
                ScanResult = MalwareScanStatus.Clean,
                BlobContainer = "container",
                BlobPath = "file.xlsx"
            });

        _blobService
            .Setup(b => b.DownloadFileAsync("container", "file.xlsx", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stream);

        var result = await _service.GetReadyFileAsync(
            FileCategory.DefundingList,
            "user",
            Guid.NewGuid(),
            Guid.NewGuid(),
            DateTime.UtcNow,
            CancellationToken.None);

        Assert.True(result.IsReady);
        Assert.False(result.IsTimedOut);
        Assert.NotNull(result.Stream);
    }
}