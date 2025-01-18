using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Functions;
using SFA.DAS.AODP.Jobs.Services.CSV;
using Microsoft.Azure.Functions.Worker;
using SFA.DAS.AODP.Jobs.Test.Mocks;
using Xunit;

namespace SFA.DAS.AODP.Jobs.Test.Application.Functions
{
    public class ApprovedQualificationsDataFunctionTests
    {
        private readonly Mock<ILogger<ApprovedQualificationsDataFunction>> _loggerMock;
        private readonly Mock<IApplicationDbContext> _applicationDbContextMock;
        private readonly Mock<ICsvReaderService> _csvReaderServiceMock;
        private readonly FunctionContext _functionContext;
        private readonly ApprovedQualificationsDataFunction _function;

        public ApprovedQualificationsDataFunctionTests()
        {
            _loggerMock = new Mock<ILogger<ApprovedQualificationsDataFunction>>();
            _applicationDbContextMock = new Mock<IApplicationDbContext>();
            _csvReaderServiceMock = new Mock<ICsvReaderService>();
            _functionContext = new Mock<FunctionContext>().Object;
            _function = new ApprovedQualificationsDataFunction(
                _loggerMock.Object,
                _applicationDbContextMock.Object,
                _csvReaderServiceMock.Object);
        }

        [Fact]
        public async Task Run_ShouldReturnOk_WhenCsvFileIsProcessedSuccessfully()
        {
            // Arrange
            var approvedQualifications = new List<ApprovedQualificationsImport>
            {
                new ApprovedQualificationsImport { Id = 1, QualificationName = "Test Qualification" }
            };
            _csvReaderServiceMock
                .Setup(service => service.ReadCsvFileFromUrlAsync<ApprovedQualificationsImport, ApprovedQualificationsImportClassMap>(It.IsAny<string>()))
                .ReturnsAsync(approvedQualifications);

            var httpRequestData = new MockHttpRequestData(_functionContext);
            Environment.SetEnvironmentVariable("ApprovedQualificationsImportUrl", "https://example.com/approved.csv");

            // Act
            var response = await _function.Run(httpRequestData);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
            _applicationDbContextMock.Verify(db => db.BulkInsertAsync(approvedQualifications, default), Times.Once);
        }

        [Fact]
        public async Task Run_ShouldReturnNotFound_WhenCsvFileIsNotFound()
        {
            // Arrange
            _csvReaderServiceMock
                .Setup(service => service.ReadCsvFileFromUrlAsync<ApprovedQualificationsImport, ApprovedQualificationsImportClassMap>(It.IsAny<string>()))
                .ReturnsAsync(new List<ApprovedQualificationsImport>());

            var httpRequestData = new MockHttpRequestData(_functionContext);
            Environment.SetEnvironmentVariable("ApprovedQualificationsImportUrl", "https://example.com/approved.csv");

            // Act
            var response = await _function.Run(httpRequestData);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            _applicationDbContextMock.Verify(db => db.BulkInsertAsync(It.IsAny<IEnumerable<ApprovedQualificationsImport>>(), default), Times.Never);
        }

        [Fact]
        public async Task Run_ShouldReturnNotFound_WhenEnvironmentVariableIsNotSet()
        {
            // Arrange
            Environment.SetEnvironmentVariable("ApprovedQualificationsImportUrl", null);
            var httpRequestData = new MockHttpRequestData(_functionContext);

            // Act
            var response = await _function.Run(httpRequestData);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
            _csvReaderServiceMock.Verify(service => service.ReadCsvFileFromUrlAsync<ApprovedQualificationsImport, ApprovedQualificationsImportClassMap>(It.IsAny<string>()), Times.Never);
            _applicationDbContextMock.Verify(db => db.BulkInsertAsync(It.IsAny<IEnumerable<ApprovedQualificationsImport>>(), default), Times.Never);
        }
    }
}

