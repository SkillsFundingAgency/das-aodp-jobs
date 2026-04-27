using System.Text;
using Azure;
using Azure.Storage.Blobs;
using Moq;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Models.Config;
using Xunit;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services;

public class BlobStorageFileServiceTests
{
    private static BlobStorageSettings CreateSettings() =>
        new()
        {
            ConnectionString = "UseDevelopmentStorage=true",
            SafeContainerName = "safe",
            QuarantineContainerName = "quarantine"
        };

    [Fact]
    public async Task DownloadFileAsync_ThrowsArgumentException_WhenLogicalPathIsNull()
    {
        // Arrange
        var blobServiceClient = new Mock<BlobServiceClient>();
        var settings = CreateSettings();

        var service = new BlobStorageFileService(
            blobServiceClient.Object,
            settings);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.DownloadFileAsync(null!, CancellationToken.None));

        Assert.Equal("logicalPath", ex.ParamName);
    }

    [Fact]
    public async Task DownloadFileAsync_ThrowsArgumentException_WhenLogicalPathIsWhitespace()
    {
        // Arrange
        var blobServiceClient = new Mock<BlobServiceClient>();
        var settings = CreateSettings();

        var service = new BlobStorageFileService(
            blobServiceClient.Object,
            settings);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.DownloadFileAsync("   ", CancellationToken.None));

        Assert.Equal("logicalPath", ex.ParamName);
    }

    [Fact]
    public async Task DownloadFileAsync_ReturnsStream_WhenBlobExistsInSafeContainer()
    {
        // Arrange
        var expectedBytes = Encoding.UTF8.GetBytes("hello world");
        var blobStream = new MemoryStream(expectedBytes);

        var blobClient = new Mock<BlobClient>();
        blobClient
            .Setup(b => b.OpenReadAsync())
            .ReturnsAsync(blobStream);

        var containerClient = new Mock<BlobContainerClient>();
        containerClient
            .Setup(c => c.GetBlobClient("imports/defunding-list.xlsx"))
            .Returns(blobClient.Object);

        var blobServiceClient = new Mock<BlobServiceClient>();
        blobServiceClient
            .Setup(s => s.GetBlobContainerClient("safe"))
            .Returns(containerClient.Object);

        var service = new BlobStorageFileService(
            blobServiceClient.Object,
            CreateSettings());

        // Act
        using var result = await service.DownloadFileAsync(
            "imports/defunding-list.xlsx", CancellationToken.None);

        using var ms = new MemoryStream();
        await result.CopyToAsync(ms);

        // Assert
        Assert.Equal(expectedBytes, ms.ToArray());
        blobServiceClient.Verify(
            s => s.GetBlobContainerClient("safe"),
            Times.Once);
    }

    [Fact]
    public async Task DownloadFileAsync_RetriesAndThrows_WhenBlobNeverAppears()
    {
        // Arrange
        var blobClient = new Mock<BlobClient>();
        blobClient
            .Setup(b => b.OpenReadAsync())
            .ThrowsAsync(new RequestFailedException(
                404,
                "Blob not found",
                "BlobNotFound",
                null));

        var containerClient = new Mock<BlobContainerClient>();
        containerClient
            .Setup(c => c.GetBlobClient(It.IsAny<string>()))
            .Returns(blobClient.Object);

        var blobServiceClient = new Mock<BlobServiceClient>();
        blobServiceClient
            .Setup(s => s.GetBlobContainerClient("safe"))
            .Returns(containerClient.Object);

        var service = new BlobStorageFileService(
            blobServiceClient.Object,
            CreateSettings());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.DownloadFileAsync("imports/missing.xlsx", CancellationToken.None));

        Assert.Contains("SAFE storage", ex.Message);
    }
}