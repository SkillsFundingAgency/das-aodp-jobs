using System.Net;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SFA.DAS.AODP.Jobs.Services.CSV;

namespace SFA.DAS.AODP.Jobs.Test.Application.Services
{
    public class CsvReaderServiceTests
    {
        private readonly Mock<ILogger<CsvReaderService>> _loggerMock;
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly CsvReaderService _csvReaderService;

        public CsvReaderServiceTests()
        {
            _loggerMock = new Mock<ILogger<CsvReaderService>>();
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _csvReaderService = new CsvReaderService(_loggerMock.Object, _httpClientFactoryMock.Object);
        }

        [Fact]
        public void ReadCSVFromFilePath_ShouldReturnRecords_WhenCsvFileIsValid()
        {
            // Arrange
            var csvContent = "Id,Name\n1,Test\n2,Test2";
            var filePath = "test.csv";
            File.WriteAllText(filePath, csvContent);

            // Act
            var result = _csvReaderService.ReadCSVFromFilePath<TestRecord, TestRecordMap>(filePath);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Id);
            Assert.Equal("Test", result[0].Name);
            Assert.Equal(2, result[1].Id);
            Assert.Equal("Test2", result[1].Name);

            // Clean up
            File.Delete(filePath);
        }

        [Fact]
        public async Task ReadCsvFileFromUrlAsync_ShouldReturnRecords_WhenCsvFileIsValid()
        {
            // Arrange
            var csvContent = "Id,Name\n1,Test\n2,Test2";
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent(csvContent)
                });

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            var result = await _csvReaderService.ReadCsvFileFromUrlAsync<TestRecord, TestRecordMap>("http://test.com/test.csv");

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Id);
            Assert.Equal("Test", result[0].Name);
            Assert.Equal(2, result[1].Id);
            Assert.Equal("Test2", result[1].Name);
        }

        [Fact]
        public async Task ReadCsvFileFromUrlAsync_ShouldLogError_WhenHttpRequestFails()
        {
            // Arrange
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("Request failed"));

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            var result = await _csvReaderService.ReadCsvFileFromUrlAsync<TestRecord, TestRecordMap>("http://test.com/test.csv");

            // Assert
            Assert.Empty(result);
            _loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("HTTP request error downloading CSV file from url")),
                    It.IsAny<HttpRequestException>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task ReadCsvFileFromUrlAsync_ShouldLogError_WhenExceptionOccurs()
        {
            // Arrange
            var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
            httpMessageHandlerMock
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new Exception("Unexpected error"));

            var httpClient = new HttpClient(httpMessageHandlerMock.Object);
            _httpClientFactoryMock.Setup(_ => _.CreateClient(It.IsAny<string>())).Returns(httpClient);

            // Act
            var result = await _csvReaderService.ReadCsvFileFromUrlAsync<TestRecord, TestRecordMap>("http://test.com/test.csv");

            // Assert
            Assert.Empty(result);
            _loggerMock.Verify(
                x => x.Log(
                    It.Is<LogLevel>(l => l == LogLevel.Error),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error downloading CSV file from url")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        private class TestRecord
        {
            public int? Id { get; set; } = null;
            public string? Name { get; set; } = null;
        }

        private sealed class TestRecordMap : ClassMap<TestRecord>
        {
            public TestRecordMap()
            {
                Map(m => m.Id).Name("Id");
                Map(m => m.Name).Name("Name");
            }
        }
    }
}




