using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CsvHelper.Configuration;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using SFA.DAS.AODP.Jobs.Services.CSV;
using Xunit;

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
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(1);
            result[0].Name.Should().Be("Test");
            result[1].Id.Should().Be(2);
            result[1].Name.Should().Be("Test2");

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
            result.Should().HaveCount(2);
            result[0].Id.Should().Be(1);
            result[0].Name.Should().Be("Test");
            result[1].Id.Should().Be(2);
            result[1].Name.Should().Be("Test2");
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
            result.Should().BeEmpty();
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
            result.Should().BeEmpty();
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
            public int Id { get; set; }
            public string Name { get; set; }
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


