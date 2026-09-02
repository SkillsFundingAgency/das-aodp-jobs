using Azure;
using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Data.Repositories.Jobs;
namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Functions;

public class DefenderScanResultFunctionTests
{
    private readonly Mock<ILogger<DefenderScanResultFunction>> _logger;
    private readonly Mock<IFileRecordRepository> _fileRepository;
    private readonly Mock<BlobServiceClient> _blobServiceClient;

    private readonly DefenderScanResultFunction _function;

    public DefenderScanResultFunctionTests()
    {
        _logger = new Mock<ILogger<DefenderScanResultFunction>>();
        _fileRepository = new Mock<IFileRecordRepository>();
        _blobServiceClient = new Mock<BlobServiceClient>();

        _function = new DefenderScanResultFunction(
            _logger.Object,
            _fileRepository.Object,
            _blobServiceClient.Object);
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
    public async Task Run_ShouldInsertFileRecord_WhenMissing_And_MetadataAvailable()
    {
        var evt = CreateEvent("No threats found");

        // FileRecord missing
        _fileRepository
            .Setup(r => r.GetByPathAsync("container", "file.csv"))
            .ReturnsAsync((FileRecord?)null);

        // Container exists
        var containerClient = new Mock<BlobContainerClient>();
        containerClient
            .Setup(c => c.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        // Blob exists
        var blobClient = new Mock<BlobClient>();
        blobClient
            .Setup(b => b.ExistsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        // Blob metadata
        var blobProps = BlobsModelFactory.BlobProperties(
            contentType: "application/pdf",
            createdOn: DateTimeOffset.UtcNow);

        blobClient
            .Setup(b => b.GetPropertiesAsync())
            .ReturnsAsync(Response.FromValue(blobProps, Mock.Of<Response>()));

        containerClient
            .Setup(c => c.GetBlobClient("file.csv"))
            .Returns(blobClient.Object);

        _blobServiceClient
            .Setup(s => s.GetBlobContainerClient("container"))
            .Returns(containerClient.Object);

        // Act
        await _function.Run(evt);

        // Assert — the new record is inserted once, already carrying the scan result from this
        // event; there is no separate update call for a record that didn't exist a moment ago.
        _fileRepository.Verify(r =>
            r.InsertAsync(It.Is<FileRecord>(f => f.ScanResult == MalwareScanStatus.Clean)),
            Times.Once);
        _fileRepository.Verify(r => r.UpdateAsync(It.IsAny<FileRecord>()), Times.Never);
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
