using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Moq;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Models.Config;
using System.Net;
using System.Text;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services;

public class BlobStorageFileServiceTests
{
    [Fact]
    public async Task DownloadFileAsync_ThrowsArgumentException_WhenFilenameIsNull()
    {
        // Arrange
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var service = new BlobStorageFileService(
            Mock.Of<BlobServiceClient>(),
            Options.Create(new BlobStorageSettings { ConnectionString = "x", FileUploadContainerName = "c" }),
            httpFactoryMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadFileAsync(null!));
        Assert.Equal("filename", ex.ParamName);
        Assert.Contains("Filename must be provided.", ex.Message);
    }

    [Fact]
    public async Task DownloadFileAsync_ThrowsArgumentException_WhenFilenameIsWhitespace()
    {
        // Arrange
        var httpFactoryMock = new Mock<IHttpClientFactory>();
        var service = new BlobStorageFileService(
            Mock.Of<BlobServiceClient>(),
            Options.Create(new BlobStorageSettings { ConnectionString = "x", FileUploadContainerName = "c" }),
            httpFactoryMock.Object);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.DownloadFileAsync("   "));
        Assert.Equal("filename", ex.ParamName);
        Assert.Contains("Filename must be provided.", ex.Message);
    }

    [Fact]
    public async Task DownloadFileAsync_ReturnsStream_WhenResponseIsSuccessful()
    {
        // Arrange
        var expectedContent = Encoding.UTF8.GetBytes("hello world");
        string? capturedRequestUri = null;

        var handler = new FakeHttpMessageHandler((req, ct) =>
        {
            capturedRequestUri = req.RequestUri?.ToString();
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StreamContent(new MemoryStream(expectedContent))
            };
            return response;
        });

        var httpClient = new HttpClient(handler);

        var httpFactoryMock = new Mock<IHttpClientFactory>();
        httpFactoryMock.Setup(f => f.CreateClient("xlsx")).Returns(httpClient);

        var service = new BlobStorageFileService(
            Mock.Of<BlobServiceClient>(),
            Options.Create(new BlobStorageSettings { ConnectionString = "x", FileUploadContainerName = "c" }),
            httpFactoryMock.Object);

        var fileUrl = "https://example.test/files/test.xlsx";

        // Act
        using var resultStream = await service.DownloadFileAsync(fileUrl);
        using var ms = new MemoryStream();
        await resultStream.CopyToAsync(ms);
        var actual = ms.ToArray();

        // Assert
        Assert.Equal(expectedContent, actual);
        Assert.Equal(fileUrl, capturedRequestUri);
    }

    [Fact]
    public async Task DownloadFileAsync_ThrowsHttpRequestException_WhenResponseIsNotSuccessful()
    {
        // Arrange
        var handler = new FakeHttpMessageHandler((req, ct) =>
        {
            return new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                Content = new StringContent("failure")
            };
        });

        var httpClient = new HttpClient(handler);

        var httpFactoryMock = new Mock<IHttpClientFactory>();
        httpFactoryMock.Setup(f => f.CreateClient("xlsx")).Returns(httpClient);

        var service = new BlobStorageFileService(
            Mock.Of<BlobServiceClient>(),
            Options.Create(new BlobStorageSettings { ConnectionString = "x", FileUploadContainerName = "c" }),
            httpFactoryMock.Object);

        var fileUrl = "https://example.test/files/test.xlsx";

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => service.DownloadFileAsync(fileUrl));
    }

    private class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> _responder;

        public FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> responder)
        {
            _responder = responder ?? throw new ArgumentNullException(nameof(responder));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_responder(request, cancellationToken));
    }
}
