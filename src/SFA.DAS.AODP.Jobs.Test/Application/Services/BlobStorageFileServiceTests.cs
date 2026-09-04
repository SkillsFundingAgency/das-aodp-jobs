using Azure.Storage.Blobs;
using Moq;
using SFA.DAS.AODP.Jobs.Services;
using System.Text;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services;

public class BlobStorageFileServiceTests
{
    [Fact]
    public async Task DownloadFileAsync_ReturnsStream_FromBlobClient()
    {
        // Arrange
        var expectedStream = new MemoryStream(Encoding.UTF8.GetBytes("hello world"));

        var blobClientMock = new Mock<BlobClient>();
        blobClientMock
            .Setup(b => b.OpenReadAsync(0, default, default, default))
            .ReturnsAsync(expectedStream);

        var blobContainerClientMock = new Mock<BlobContainerClient>();
        blobContainerClientMock
            .Setup(c => c.GetBlobClient("test.xlsx"))
            .Returns(blobClientMock.Object);

        var blobServiceClientMock = new Mock<BlobServiceClient>();
        blobServiceClientMock
            .Setup(s => s.GetBlobContainerClient("test-container"))
            .Returns(blobContainerClientMock.Object);

        var service = new BlobStorageFileService(blobServiceClientMock.Object);

        // Act
        var result = await service.DownloadFileAsync("test-container", "test.xlsx");

        // Assert
        Assert.Same(expectedStream, result);
    }
}
