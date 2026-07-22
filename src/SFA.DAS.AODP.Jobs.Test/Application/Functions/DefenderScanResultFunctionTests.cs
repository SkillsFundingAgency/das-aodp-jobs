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

        // Delete call
        blobClient
            .Setup(b => b.DeleteIfExistsAsync())
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        containerClient
            .Setup(c => c.GetBlobClient("file.csv"))
            .Returns(blobClient.Object);

        _blobServiceClient
            .Setup(s => s.GetBlobContainerClient("container"))
            .Returns(containerClient.Object);

        // Act
        await _function.Run(evt);

        // Assert
        blobClient.Verify(b => b.DeleteIfExistsAsync(), Times.Once);
        _fileRepository.Verify(r => r.UpdateAsync(file), Times.Once);
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

        // Assert — insert + update both happen
        _fileRepository.Verify(r => r.InsertAsync(It.IsAny<FileRecord>()), Times.Once);
        _fileRepository.Verify(r => r.UpdateAsync(It.IsAny<FileRecord>()), Times.Once);
    }


    private static EventGridEvent CreateEvent(string scanResult)
    {
        return new EventGridEvent(
            "subject",
            "type",
            "1.0",
            BinaryData.FromObjectAsJson(new DefenderScanEvent
            {
                BlobUri = "https://storage/container/file.csv",
                ScanResultType = scanResult
            })
        );
    }
}
