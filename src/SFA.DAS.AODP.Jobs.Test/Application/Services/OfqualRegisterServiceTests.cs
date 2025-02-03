using Moq;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Data;
using SFA.DAS.AODP.Infrastructure.Context;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using SFA.DAS.AODP.Jobs.Client;
using AutoFixture;

namespace SFA.DAS.AODP.Jobs.Test.Application.Services
{
    public class QualificationsApiServiceTests
    {
        private readonly Mock<ILogger<QualificationsService>> _mockLogger;
        private readonly Mock<IOfqualRegisterApi> _mockApiClient;
        private readonly Mock<IApplicationDbContext> _mockDbContext;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly OfqualRegisterService _service;
        private Fixture _fixture;

        public QualificationsApiServiceTests()
        {
            _mockApiClient = new Mock<IOfqualRegisterApi>();
            _mockLogger = new Mock<ILogger<QualificationsService>>();
            _mockDbContext = new Mock<IApplicationDbContext>();
            _mockConfiguration = new Mock<IConfiguration>();
            _fixture = new Fixture();
            _service = new OfqualRegisterService(_mockLogger.Object, _mockApiClient.Object, _mockConfiguration.Object);
        }

        [Fact]
        public async Task SearchPrivateQualificationsAsync_CallsApiClient_WithCorrectParameters()
        {
            // Arrange
            var parameters = _fixture.Create<QualificationsQueryParameters>();
            var expectedResult = new PaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>(),
                Count = 1
            };

            _mockApiClient.Setup(client => client.SearchPrivateQualificationsAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>()
                ))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.SearchPrivateQualificationsAsync(parameters);

            // Assert
            _mockApiClient.Verify(client => client.SearchPrivateQualificationsAsync(
                parameters.Title,
                parameters.Page,
                parameters.Limit,
                parameters.AssessmentMethods,
                parameters.GradingTypes,
                parameters.AwardingOrganisations,
                parameters.Availability,
                parameters.QualificationTypes,
                parameters.QualificationLevels,
                parameters.NationalAvailability,
                parameters.SectorSubjectAreas,
                parameters.MinTotalQualificationTime,
                parameters.MaxTotalQualificationTime,
                parameters.MinGuidedLearningHours,
                parameters.MaxGuidedLearningHours
            ), Times.Once);

            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task SearchPrivateQualificationsAsync_ReturnsCorrectResult()
        {
            // Arrange
            var parameters = _fixture.Create<QualificationsQueryParameters>();
            var expectedResult = new PaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO> { _fixture.Create<QualificationDTO>() },
                Count = 1
            };

            _mockApiClient.Setup(client => client.SearchPrivateQualificationsAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>()
                ))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.SearchPrivateQualificationsAsync(parameters);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task SearchPrivateQualificationsAsync_ReturnsEmptyResults_WhenApiReturnsNoData()
        {
            // Arrange
            var parameters = _fixture.Create<QualificationsQueryParameters>();
            var expectedResult = new PaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>(),
                Count = 0
            };

            _mockApiClient.Setup(client => client.SearchPrivateQualificationsAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>()
                ))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.SearchPrivateQualificationsAsync(parameters);

            // Assert
            Assert.Empty(result.Results);
            Assert.Equal(0, result.Count);
        }

        [Fact]
        public async Task SearchPrivateQualificationsAsync_ThrowsException_WhenApiClientFails()
        {
            // Arrange
            var parameters = _fixture.Create<QualificationsQueryParameters>();

            _mockApiClient.Setup(client => client.SearchPrivateQualificationsAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>()
                ))
                .ThrowsAsync(new Exception("API failure"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _service.SearchPrivateQualificationsAsync(parameters));
            Assert.Equal("API failure", exception.Message);
        }

        [Fact]
        public async Task SearchPrivateQualificationsAsync_ReturnsLargeResultSet_WhenApiReturnsManyResults()
        {
            // Arrange
            var parameters = _fixture.Create<QualificationsQueryParameters>();

            var largeResults = new List<QualificationDTO>();
            for (int i = 0; i < 1000; i++)
            {
                largeResults.Add(_fixture.Create<QualificationDTO>());
            }

            var expectedResult = new PaginatedResult<QualificationDTO>
            {
                Results = largeResults,
                Count = 1000
            };

            _mockApiClient.Setup(client => client.SearchPrivateQualificationsAsync(
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>(),
                    It.IsAny<int?>()
                ))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.SearchPrivateQualificationsAsync(parameters);

            // Assert
            Assert.Equal(1000, result.Count);
            Assert.Equal(1000, result.Results.Count);
        }

    }
}
