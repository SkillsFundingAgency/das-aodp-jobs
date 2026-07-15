using Azure;
using Azure.Messaging.EventGrid;
using Azure.Storage.Blobs;
using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Data.Repositories.Jobs;
using SFA.DAS.AODP.Jobs.Models.Jobs;

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
        var eventGridEvent = new EventGridEvent(
            "subject",
            "type",
            "1.0",
            BinaryData.FromObjectAsJson(new { })
        );

        await _function.Run(eventGridEvent);

        _fileRepository.Verify(r => r.UpdateAsync(It.IsAny<FileRecord>()), Times.Never);
    }

    [Fact]
    public async Task Run_ShouldReturn_WhenFileNotFound()
    {
        var eventGridEvent = CreateEvent("No threats found");

        _fileRepository
            .Setup(r => r.GetByPathAsync("container", "file.csv"))
            .ReturnsAsync((FileRecord?)null);

        await _function.Run(eventGridEvent);

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

        var eventGridEvent = CreateEvent("No threats found");

        _fileRepository
            .Setup(r => r.GetByPathAsync("container", "file.csv"))
            .ReturnsAsync(file);

        await _function.Run(eventGridEvent);

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

        var blobClient = new Mock<BlobClient>();

        blobClient
            .Setup(b => b.DeleteIfExistsAsync())
            .ReturnsAsync(Response.FromValue(true, Mock.Of<Response>()));

        var containerClient = new Mock<BlobContainerClient>();
        containerClient
            .Setup(c => c.GetBlobClient("file.csv"))
            .Returns(blobClient.Object);

        _blobServiceClient
            .Setup(s => s.GetBlobContainerClient("container"))
            .Returns(containerClient.Object);

        var eventGridEvent = CreateEvent("Malicious");

        _fileRepository
            .Setup(r => r.GetByPathAsync("container", "file.csv"))
            .ReturnsAsync(file);

        await _function.Run(eventGridEvent);

        Assert.Equal(MalwareScanStatus.Malicious, file.ScanResult);

        blobClient.Verify(
            b => b.DeleteIfExistsAsync(),
            Times.Once);

        _fileRepository.Verify(
            r => r.UpdateAsync(file),
            Times.Once);
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