using Moq;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Functions.Interfaces;
using SFA.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Data;
using Xunit;


namespace SFA.DAS.AODP.Jobs.Test.Application.Services
{
    public class QualificationsApiServiceTests
    {
        private readonly Mock<IOfqualRegisterApi> _mockApiClient;
        private readonly QualificationsService _service;

        public QualificationsApiServiceTests()
        {
            _mockApiClient = new Mock<IOfqualRegisterApi>();
            _service = new RegulatedQualificationsService(_mockApiClient.Object);
        }

        [Fact]
        public async Task SearchPrivateQualificationsAsync_CallsApiClient_WithCorrectParameters()
        {
            // Arrange
            var parameters = new RegulatedQualificationsQueryParameters
            {
                Title = "Test Title",
                AssessmentMethods = "Test Method",
                GradingTypes = "Test Grade",
                AwardingOrganisations = "Test Organisation",
                Availability = "Test Availability",
                QualificationTypes = "Test Type",
                QualificationLevels = "Test Level",
                NationalAvailability = "Test National",
                SectorSubjectAreas = "Test Area",
                MinTotalQualificationTime = 10,
                MaxTotalQualificationTime = 100,
                MinGuidedLearningHours = 5,
                MaxGuidedLearningHours = 50
            };
            int page = 1;
            int limit = 10;

            var expectedResult = new RegulatedQualificationsPaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>(),
                Count = 0
            };

            _mockApiClient.Setup(client => client.SearchPrivateQualificationsAsync(
                    parameters.Title,
                    page,
                    limit,
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
                ))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.SearchPrivateQualificationsAsync(parameters, page, limit);

            // Assert
            _mockApiClient.Verify(client => client.SearchPrivateQualificationsAsync(
                parameters.Title,
                page,
                limit,
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
            var parameters = new RegulatedQualificationsQueryParameters { Title = "Test Title" };
            var expectedResult = new RegulatedQualificationsPaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO> { new QualificationDTO { Title = "Test Qualification" } },
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
            var result = await _service.SearchPrivateQualificationsAsync(parameters, 1, 10);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        [Fact]
        public async Task SearchPrivateQualificationsAsync_ThrowsArgumentNullException_WhenParametersAreNull()
        {
            // Arrange
            RegulatedQualificationsQueryParameters parameters = null;
            int page = 1;
            int limit = 10;

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _service.SearchPrivateQualificationsAsync(parameters, page, limit));

            // Assert
            Assert.Equal("parameters", exception.ParamName);
        }

        [Fact]
        public async Task SearchPrivateQualificationsAsync_ReturnsEmptyResults_WhenApiReturnsNoData()
        {
            // Arrange
            var parameters = new RegulatedQualificationsQueryParameters { Title = "Test Title" };
            int page = 1;
            int limit = 10;

            var expectedResult = new RegulatedQualificationsPaginatedResult<QualificationDTO>
            {
                Results = new List<QualificationDTO>(),
                Count = 0
            };

            _mockApiClient.Setup(client => client.SearchPrivateQualificationsAsync(
                    parameters.Title,
                    page,
                    limit,
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
                ))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.SearchPrivateQualificationsAsync(parameters, page, limit);

            // Assert
            Assert.Empty(result.Results);
            Assert.Equal(0, result.Count);
        }

        [Fact]
        public async Task SearchPrivateQualificationsAsync_ThrowsException_WhenApiClientFails()
        {
            // Arrange
            var parameters = new RegulatedQualificationsQueryParameters { Title = "Test Title" };
            int page = 1;
            int limit = 10;

            _mockApiClient.Setup(client => client.SearchPrivateQualificationsAsync(
                    parameters.Title,
                    page,
                    limit,
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
                ))
                .ThrowsAsync(new Exception("API failure"));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _service.SearchPrivateQualificationsAsync(parameters, page, limit));
            Assert.Equal("API failure", exception.Message);
        }

        [Fact]
        public async Task SearchPrivateQualificationsAsync_ReturnsLargeResultSet_WhenApiReturnsManyResults()
        {
            // Arrange
            var parameters = new RegulatedQualificationsQueryParameters { Title = "Test Title" };
            int page = 1;
            int limit = 100;

            var largeResults = new List<QualificationDTO>();
            for (int i = 0; i < 1000; i++)
            {
                largeResults.Add(new QualificationDTO { Title = $"Qualification {i}" });
            }

            var expectedResult = new RegulatedQualificationsPaginatedResult<QualificationDTO>
            {
                Results = largeResults,
                Count = 1000
            };

            _mockApiClient.Setup(client => client.SearchPrivateQualificationsAsync(
                    parameters.Title,
                    page,
                    limit,
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
                ))
                .ReturnsAsync(expectedResult);

            // Act
            var result = await _service.SearchPrivateQualificationsAsync(parameters, page, limit);

            // Assert
            Assert.Equal(1000, result.Count);
            Assert.Equal(1000, result.Results.Count);
        }


    }
}
