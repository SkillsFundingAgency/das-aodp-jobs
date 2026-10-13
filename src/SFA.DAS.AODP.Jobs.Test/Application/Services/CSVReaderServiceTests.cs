using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Services.CSV;

namespace SFA.DAS.AODP.Jobs.Test.Application.Services
{
    public class CsvReaderServiceTests
    {
        private readonly Mock<ILogger<CsvReaderService>> _loggerMock;
        private readonly CsvReaderService _csvReaderService;

        public CsvReaderServiceTests()
        {
            _loggerMock = new Mock<ILogger<CsvReaderService>>();
            _csvReaderService = new CsvReaderService(_loggerMock.Object);
        }

        [Fact]
        public void ReadCSVFromFilePath_ShouldReturnRecords_WhenCsvFileIsValid()
        {
            // Arrange
            var csvContent = "Id,Name,Test_FundingAvailable\n1,Test,100\n2,Test2,200";
            var filePath = "test.csv";
            File.WriteAllText(filePath, csvContent);

            var organisations = new List<AwardingOrganisation>();
            var qualifications = new List<Qualification>();

            var loggerMock = new Mock<ILogger<CsvReaderService>>();
            var csvReaderService = new CsvReaderService(loggerMock.Object);

            // Act
            var result = csvReaderService.ReadCSVFromFilePath<TestRecord, TestRecordMap>(
                filePath,
                organisations,
                qualifications
            );

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(1, result[0].Id);
            Assert.Equal("Test", result[0].Name);
            Assert.Equal(2, result[1].Id);
            Assert.Equal("Test2", result[1].Name);

            // Clean up
            File.Delete(filePath);
        }

        private class TestRecord
        {
            public int? Id { get; set; } = null;
            public string? Name { get; set; } = null;
        }

        private class TestRecordMap : ClassMap<TestRecord>
        {
            public TestRecordMap(List<string> customHeaders, List<AwardingOrganisation> organisations, List<Qualification> qualifications)
            {
                Map(m => m.Id).Name("Id");
                Map(m => m.Name).Name("Name");
            }
        }
    }
}




