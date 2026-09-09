using Azure;
using Azure.Storage.Blobs;
using SFA.DAS.AODP.Infrastructure.Services;
using System.Text;
using Xunit;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services;

public class BlobStorageFileServiceTests
{
    [Fact]
    public async Task DownloadFileAsync_ThrowsArgumentException_WhenContainerNameIsNull()
    {
        var blobServiceClient = new Mock<BlobServiceClient>();
        var service = new BlobStorageFileService(blobServiceClient.Object, new Mock<IDelayService>().Object);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.DownloadFileAsync(null!, "file.xlsx", CancellationToken.None));

        Assert.Equal("container", ex.ParamName);
    }

    [Fact]
    public async Task DownloadFileAsync_ThrowsArgumentException_WhenBlobPathIsNull()
    {
        var blobServiceClient = new Mock<BlobServiceClient>();
        var service = new BlobStorageFileService(blobServiceClient.Object, new Mock<IDelayService>().Object);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.DownloadFileAsync("container", null!, CancellationToken.None));

        Assert.Equal("blobPath", ex.ParamName);
    }


    [Fact]
    public async Task DownloadFileAsync_ThrowsArgumentException_WhenLogicalPathIsWhitespace()
    {
        // Arrange
        var blobServiceClient = new Mock<BlobServiceClient>();
        var service = new BlobStorageFileService(blobServiceClient.Object, new Mock<IDelayService>().Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.DownloadFileAsync("   ", " ", CancellationToken.None));

        Assert.Equal("container", ex.ParamName);
    }

    [Fact]
    public async Task DownloadFileAsync_ReturnsStream_WhenBlobExists()
    {
        var expectedBytes = Encoding.UTF8.GetBytes("hello world");
        var blobStream = new MemoryStream(expectedBytes);

        var blobClient = new Mock<BlobClient>();

        blobClient
            .Setup(b => b.OpenReadAsync())
            .ReturnsAsync(blobStream);

        var containerClient = new Mock<BlobContainerClient>();
        containerClient
            .Setup(c => c.GetBlobClient("defunding-list.xlsx"))
            .Returns(blobClient.Object);

        var blobServiceClient = new Mock<BlobServiceClient>();
        blobServiceClient
            .Setup(s => s.GetBlobContainerClient("imports"))
            .Returns(containerClient.Object);

        var service = new BlobStorageFileService(blobServiceClient.Object, new Mock<IDelayService>().Object);

        using var result = await service.DownloadFileAsync(
            "imports",
            "defunding-list.xlsx",
            CancellationToken.None);

        using var ms = new MemoryStream();
        await result.CopyToAsync(ms);

        Assert.Equal(expectedBytes, ms.ToArray());
    }



    [Fact]
    public async Task DownloadFileAsync_RetriesAndThrows_WhenBlobNeverAppears()
    {
        var blobClient = new Mock<BlobClient>();
        blobClient
            .Setup(b => b.OpenReadAsync())
            .ThrowsAsync(new RequestFailedException(404, "Blob not found", "BlobNotFound", null));

        var containerClient = new Mock<BlobContainerClient>();
        containerClient
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClient.Object);

        var blobServiceClient = new Mock<BlobServiceClient>();
        blobServiceClient
            .Setup(s => s.GetBlobContainerClient("imports"))
            .Returns(containerClient.Object);

        var delayServiceMock = new Mock<IDelayService>();
        delayServiceMock
            .Setup(d => d.DelayAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new BlobStorageFileService(blobServiceClient.Object, delayServiceMock.Object);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DownloadFileAsync("imports", "missing.xlsx", CancellationToken.None));

        Assert.Contains("did not appear in storage", ex.Message);
    }
}