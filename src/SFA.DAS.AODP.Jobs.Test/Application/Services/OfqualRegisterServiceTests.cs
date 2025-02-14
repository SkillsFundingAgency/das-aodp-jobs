using Moq;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Data;
using SFA.DAS.AODP.Infrastructure.Context;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Jobs.Client;
using AutoFixture;
using System.Collections.Specialized;
using Microsoft.Extensions.Options;
using SFA.DAS.AODP.Models.Config;

namespace SFA.DAS.AODP.Jobs.Test.Application.Services
{
    public class OfqualRegisterServiceTests
    {
        private readonly Mock<ILogger<QualificationsService>> _mockLogger;
        private readonly Mock<IOfqualRegisterApi> _mockApiClient;
        private readonly Mock<IApplicationDbContext> _mockDbContext;
        private readonly Mock<IOptions<AodpJobsConfiguration>> _mockConfiguration;
        private readonly OfqualRegisterService _service;
        private Fixture _fixture;
        private AodpJobsConfiguration mockConfig;

        public OfqualRegisterServiceTests()
        {
            _mockApiClient = new Mock<IOfqualRegisterApi>();
            _mockLogger = new Mock<ILogger<QualificationsService>>();
            _mockDbContext = new Mock<IApplicationDbContext>();
            _mockConfiguration = new Mock<IOptions<AodpJobsConfiguration>>();
            _fixture = new Fixture();
            _service = new OfqualRegisterService(_mockLogger.Object, _mockApiClient.Object, _mockConfiguration.Object);

            mockConfig = new AodpJobsConfiguration
            {
                DefaultImportPage = 1,
                DefaultImportLimit = 100
            };
        }

        [Fact]
        public void Constructor_ShouldInitializeDependencies()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<QualificationsService>>();
            var mockApiClient = new Mock<IOfqualRegisterApi>();
            var mockConfiguration = new Mock<IOptions<AodpJobsConfiguration>>();

            // Act
            var service = new OfqualRegisterService(mockLogger.Object, mockApiClient.Object, mockConfiguration.Object);

            // Assert
            Assert.NotNull(service);
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

        [Fact]
        public void ParseQueryParameters_NullQuery_ReturnsDefaults()
        {
            // Arange
            _mockConfiguration.Setup(opt => opt.Value).Returns(mockConfig);

            // Act
            var result = _service.ParseQueryParameters(null);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Page);
            Assert.Equal(100, result.Limit);
        }

        [Fact]
        public void ParseQueryParameters_EmptyQuery_ReturnsDefaults()
        {
            // Arrange
            var query = new NameValueCollection();

            _mockConfiguration.Setup(opt => opt.Value).Returns(mockConfig);

            // Act
            var result = _service.ParseQueryParameters(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Page);
            Assert.Equal(100, result.Limit);
        }

        [Fact]
        public void ParseQueryParameters_ValidQuery_ReturnsCorrectValues()
        {
            // Arrange
            var query = new NameValueCollection
            {
                { "page", "5" },
                { "limit", "50" },
                { "title", "Test Title" },
                { "availability", "Available" }
            };

            _mockConfiguration.Setup(opt => opt.Value).Returns(mockConfig);

            // Act
            var result = _service.ParseQueryParameters(query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(5, result.Page);
            Assert.Equal(50, result.Limit);
            Assert.Equal("Test Title", result.Title);
            Assert.Equal("Available", result.Availability);
        }

        [Fact]
        public void ParseQueryParameters_MissingOptionalValues_ReturnsNullForStrings()
        {
            // Arrange
            var query = new NameValueCollection();

            _mockConfiguration.Setup(opt => opt.Value).Returns(mockConfig);

            // Act
            var result = _service.ParseQueryParameters(query);

            // Assert
            Assert.Null(result.Title);
            Assert.Null(result.AssessmentMethods);
            Assert.Null(result.Availability);
        }

        [Fact]
        public void ExtractQualificationsList_ShouldMapCorrectly()
        {
            // Arrange
            var paginatedResult = new PaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>
                {
                    _fixture.Create<QualificationDTO>()
                }
            };

            // Act
            var result = _service.ExtractQualificationsList(paginatedResult);

            // Assert
            Assert.NotNull(result);
            Assert.Single(result);
        }

        [Fact]
        public void ExtractQualificationsList_EmptyResults_ReturnsEmptyList()
        {
            // Arrange
            var paginatedResult = new PaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>()
            };

            // Act
            var result = _service.ExtractQualificationsList(paginatedResult);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public void ExtractQualificationsList_MultipleResults_ReturnsCorrectCount()
        {
            // Arrange
            var inputDtos = new List<QualificationDTO>
            {
                new QualificationDTO { QualificationNumber = "QN123" },
                new QualificationDTO { QualificationNumber = "QN456" },
                new QualificationDTO { QualificationNumber = "QN789" }
            };

            var paginatedResult = new PaginatedResult<QualificationDTO>
            {
                Results = inputDtos
            };

            // Act
            var result = _service.ExtractQualificationsList(paginatedResult);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(inputDtos.Select(x => x.QualificationNumber), result.Select(x => x.QualificationNumber));
        }
    }
}
