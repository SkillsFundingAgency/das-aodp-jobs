using Azure;
using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Data.Repositories.Jobs;
using SFA.DAS.AODP.Infrastructure.Services;
namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Functions;

public class DefenderScanResultFunctionTests
{
    private readonly Mock<ILogger<DefenderScanResultFunction>> _logger;
    private readonly Mock<IFileRecordRepository> _fileRepository;
    private readonly Mock<BlobServiceClient> _blobServiceClient;
    private readonly Mock<IDelayService> _delayService;

    private readonly DefenderScanResultFunction _function;

    public DefenderScanResultFunctionTests()
    {
        _logger = new Mock<ILogger<DefenderScanResultFunction>>();
        _fileRepository = new Mock<IFileRecordRepository>();
        _blobServiceClient = new Mock<BlobServiceClient>();
        _delayService = new Mock<IDelayService>();
        _delayService.Setup(d => d.DelayAsync(It.IsAny<TimeSpan>(), default)).Returns(Task.CompletedTask);

        _function = new DefenderScanResultFunction(
            _logger.Object,
            _fileRepository.Object,
            _blobServiceClient.Object,
            _delayService.Object);
    }

    [Fact]
    public async Task Run_ShouldReturn_WhenEventDataInvalid()
    {
        var evt = new EventGridEvent(
            "subject",
            "type",
            "1.0",
            BinaryData.FromObjectAsJson(new { }) // missing BlobUri
        );

        await _function.Run(evt);

        _fileRepository.Verify(r => r.UpdateAsync(It.IsAny<FileRecord>()), Times.Never);
        _fileRepository.Verify(r => r.InsertAsync(It.IsAny<FileRecord>()), Times.Never);
    }

    [Fact]
    public async Task Run_ShouldReturn_WhenFileNotFound()
    {
        var evt = CreateEvent("No threats found");

        // FileRecord missing
        _fileRepository
            .Setup(r => r.GetByPathAsync("container", "file.csv"))
            .ReturnsAsync((FileRecord?)null);

        // Container does NOT exist → early exit
        var containerClient = new Mock<BlobContainerClient>();
        containerClient
            .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(false, Mock.Of<Response>()));

        _blobServiceClient
            .Setup(s => s.GetBlobContainerClient("container"))
            .Returns(containerClient.Object);

        // Act
        await _function.Run(evt);

        // Assert — nothing should be inserted or updated
        _fileRepository.Verify(r => r.InsertAsync(It.IsAny<FileRecord>()), Times.Never);
        _fileRepository.Verify(r => r.UpdateAsync(It.IsAny<FileRecord>()), Times.Never);
    }


    [Fact]
    public async Task Run_ShouldUpdateFile_WhenClean()
    {
        var file = new FileRecord
        {
            BlobContainer = "container",
            BlobPath = "file.csv"
        };

        var evt = CreateEvent("No threats found");

        _fileRepository
            .Setup(r => r.GetByPathAsync("container", "file.csv"))
            .ReturnsAsync(file);

        SetupBlobExists("container", "file.csv", out var blobClient, out _);
        SetupBlobProperties(blobClient, eTag: null);

        await _function.Run(evt);

        Assert.Equal(MalwareScanStatus.Clean, file.ScanResult);

        _fileRepository.Verify(r => r.UpdateAsync(file), Times.Once);
    }

    [Fact]
    public async Task Run_ShouldDeleteBlobAndUpdate_WhenMalicious()
    {
        var file = new FileRecord
        {
            BlobContainer = "container",
            BlobPath = "file.csv"
        };

        var evt = CreateEvent("Malicious");

        // FileRecord exists
        _fileRepository
            .Setup(r => r.GetByPathAsync("container", "file.csv"))
            .ReturnsAsync(file);

        SetupBlobExists("container", "file.csv", out var blobClient, out _);
        SetupBlobProperties(blobClient, eTag: null);

        // Delete call
        blobClient
            .Setup(b => b.DeleteIfExistsAsync())
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        // Act
        await _function.Run(evt);

        // Assert
        blobClient.Verify(b => b.DeleteIfExistsAsync(), Times.Once);
        _fileRepository.Verify(r => r.UpdateAsync(file), Times.Once);
    }

    [Fact]
    public async Task Run_ShouldApplyScanResult_WhenEventETagMatchesCurrentBlobETag()
    {
        var file = new FileRecord
        {
            BlobContainer = "container",
            BlobPath = "file.csv"
        };

        var evt = CreateEvent("No threats found", eTag: "0xCURRENT");

        _fileRepository
            .Setup(r => r.GetByPathAsync("container", "file.csv"))
            .ReturnsAsync(file);

        SetupBlobExists("container", "file.csv", out var blobClient, out _);
        SetupBlobProperties(blobClient, eTag: "0xCURRENT");

        await _function.Run(evt);

        Assert.Equal(MalwareScanStatus.Clean, file.ScanResult);
        _fileRepository.Verify(r => r.UpdateAsync(file), Times.Once);
    }

    [Fact]
    public async Task Run_ShouldDiscardScanResult_WhenEventETagDoesNotMatchCurrentBlobETag()
    {
        var file = new FileRecord
        {
            BlobContainer = "container",
            BlobPath = "file.csv",
            ScanResult = MalwareScanStatus.NotScanned
        };

        // Event describes a scan of an earlier version of the blob; the blob has since
        // been overwritten by a newer upload with a different eTag.
        var evt = CreateEvent("No threats found", eTag: "0xSTALE");

        _fileRepository
            .Setup(r => r.GetByPathAsync("container", "file.csv"))
            .ReturnsAsync(file);

        SetupBlobExists("container", "file.csv", out var blobClient, out _);
        SetupBlobProperties(blobClient, eTag: "0xCURRENT");

        await _function.Run(evt);

        // The stale result must not be applied — the record is left exactly as it was.
        Assert.Equal(MalwareScanStatus.NotScanned, file.ScanResult);
        _fileRepository.Verify(r => r.UpdateAsync(It.IsAny<FileRecord>()), Times.Never);
        _fileRepository.Verify(r => r.InsertAsync(It.IsAny<FileRecord>()), Times.Never);
        _fileRepository.Verify(r => r.GetByPathAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }


    [Fact]
    public async Task Run_ShouldThrow_WhenNoFileRecordTracksTheBlob()
    {
        var evt = CreateEvent("No threats found");

        // FileRecord missing — could mean nothing upstream (upload flow, funded-import trigger,
        // sync function) has created a record for this blob yet, or it could just not have
        // caught up yet (a scan can complete before the upload flow's own record-creation call
        // finishes). Throwing lets Event Grid retry the delivery later rather than discarding
        // a result that might resolve itself given a moment longer.
        _fileRepository
            .Setup(r => r.GetByPathAsync("container", "file.csv"))
            .ReturnsAsync((FileRecord?)null);

        SetupBlobExists("container", "file.csv", out var blobClient, out _);
        SetupBlobProperties(blobClient, eTag: null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _function.Run(evt));

        // Initial lookup plus one retry per configured delay.
        _fileRepository.Verify(r => r.GetByPathAsync("container", "file.csv"), Times.Exactly(4));
        _delayService.Verify(d => d.DelayAsync(It.IsAny<TimeSpan>(), default), Times.Exactly(3));
        _fileRepository.Verify(r => r.InsertAsync(It.IsAny<FileRecord>()), Times.Never);
        _fileRepository.Verify(r => r.UpdateAsync(It.IsAny<FileRecord>()), Times.Never);
    }

    [Fact]
    public async Task Run_ShouldRecoverAndUpdate_WhenFileRecordAppearsPartwayThroughShortRetries()
    {
        var evt = CreateEvent("No threats found");

        var file = new FileRecord
        {
            BlobContainer = "container",
            BlobPath = "file.csv"
        };

        // Missing on the first two lookups, present by the third — proves the retry loop
        // actually recovers a record that shows up a moment late, not just that it exists.
        _fileRepository
            .SetupSequence(r => r.GetByPathAsync("container", "file.csv"))
            .ReturnsAsync((FileRecord?)null)
            .ReturnsAsync((FileRecord?)null)
            .ReturnsAsync(file);

        SetupBlobExists("container", "file.csv", out var blobClient, out _);
        SetupBlobProperties(blobClient, eTag: null);

        await _function.Run(evt);

        Assert.Equal(MalwareScanStatus.Clean, file.ScanResult);
        _fileRepository.Verify(r => r.GetByPathAsync("container", "file.csv"), Times.Exactly(3));
        _delayService.Verify(d => d.DelayAsync(It.IsAny<TimeSpan>(), default), Times.Exactly(2));
        _fileRepository.Verify(r => r.UpdateAsync(file), Times.Once);
    }


    private static EventGridEvent CreateEvent(string scanResult, string? eTag = null)
    {
        return new EventGridEvent(
            "subject",
            "type",
            "1.0",
            BinaryData.FromObjectAsJson(new DefenderScanEvent
            {
                BlobUri = "https://storage/container/file.csv",
                ETag = eTag,
                ScanResultType = scanResult
            })
        );
    }

    private void SetupBlobExists(
        string containerName,
        string blobPath,
        out Mock<BlobClient> blobClient,
        out Mock<BlobContainerClient> containerClient)
    {
        containerClient = new Mock<BlobContainerClient>();
        containerClient
            .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        blobClient = new Mock<BlobClient>();
        blobClient
            .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        containerClient
            .Setup(c => c.GetBlobClient(blobPath))
            .Returns(blobClient.Object);

        _blobServiceClient
            .Setup(s => s.GetBlobContainerClient(containerName))
            .Returns(containerClient.Object);
    }

    private static void SetupBlobProperties(Mock<BlobClient> blobClient, string? eTag)
    {
        var blobProps = BlobsModelFactory.BlobProperties(
            contentType: "text/csv",
            createdOn: DateTimeOffset.UtcNow,
            eTag: eTag == null ? default : new ETag(eTag));

        blobClient
            .Setup(b => b.GetPropertiesAsync())
            .ReturnsAsync(Response.FromValue(blobProps, Mock.Of<Response>()));
    }
}
