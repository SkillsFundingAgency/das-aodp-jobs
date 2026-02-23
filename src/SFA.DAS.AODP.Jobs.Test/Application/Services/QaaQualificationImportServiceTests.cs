using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Repositories;
using SFA.DAS.AODP.Jobs.Client;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Models.QaaQualification;
using SFA.DAS.Funding.ApprenticeshipEarnings.Domain.Services;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services;

public class QaaQualificationImportServiceTests
{
    private readonly Mock<IQaaApiClient> _mockQaaApiClient;
    private readonly Mock<IQaaRepository> _mockQaaRepository;
    private readonly Mock<ISystemClockService> _mockClockService;
    private readonly QaaQualificationImportService _service;

    public QaaQualificationImportServiceTests()
    {
        _mockQaaApiClient = new Mock<IQaaApiClient>();
        _mockQaaRepository = new Mock<IQaaRepository>();
        _mockClockService = new Mock<ISystemClockService>();

        _service = new QaaQualificationImportService(
            NullLogger<QaaQualificationImportService>.Instance, 
            _mockQaaApiClient.Object,
            _mockQaaRepository.Object,
            _mockClockService.Object);
    }

    [Fact]
    public async Task ImportDataAsync_WithValidQualifications_CreatesAndLogsSuccessfully()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var qualifications = new List<QaaQualificationResponse>
        {
            new()
            {
                AimCode = "Z1234567",
                DiplomaTitle = "Access to Higher Education Diploma (Science)",
                AwardingBody = "Test Awarding Body",
                SsaTier1 = "2",
                SsaTier2 = "1",
                StartDateOfQualification = new DateOnly(2023, 09, 01),
                LastDateForRegistrations = new DateOnly(2025, 08, 31)
            }
        };

        _mockQaaApiClient.Setup(client => client.GetQualificationsAsync(cancellationToken))
            .ReturnsAsync(qualifications);

        _mockQaaRepository.Setup(repo => repo.RunPrerequisitesForImportAsync(cancellationToken))
            .ReturnsAsync(5);

        _mockQaaRepository.Setup(repo => repo.RunImportAsync(It.IsAny<IList<RegulatedQaaQualification>>(), cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ImportDataAsync(cancellationToken);

        // Assert
        Assert.Equal(1, result);
        _mockQaaApiClient.Verify(client => client.GetQualificationsAsync(cancellationToken), Times.Once);
        _mockQaaRepository.Verify(repo => repo.RunPrerequisitesForImportAsync(cancellationToken), Times.Once);
        _mockQaaRepository.Verify(repo => repo.RunImportAsync(It.IsAny<IList<RegulatedQaaQualification>>(), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ImportDataAsync_WithMultipleQualifications_CreatesAllSuccessfully()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var qualifications = new List<QaaQualificationResponse>
        {
            new()
            {
                AimCode = "Z1234567",
                DiplomaTitle = "Access to Higher Education Diploma (Science)",
                AwardingBody = "Awarding Body 1",
                SsaTier1 = "2",
                SsaTier2 = "1",
                StartDateOfQualification = new DateOnly(2023, 09, 01),
                LastDateForRegistrations = new DateOnly(2025, 08, 31)
            },
            new()
            {
                AimCode = "Z1234568",
                DiplomaTitle = "Access to Higher Education Diploma (Engineering)",
                AwardingBody = "Awarding Body 2",
                SsaTier1 = "4",
                SsaTier2 = "1",
                StartDateOfQualification = new DateOnly(2023, 09, 01),
                LastDateForRegistrations = new DateOnly(2025, 08, 31)
            },
            new()
            {
                AimCode = "Z1234569",
                DiplomaTitle = "Access to Higher Education Diploma (Business)",
                AwardingBody = "Awarding Body 3",
                SsaTier1 = "15",
                SsaTier2 = "3",
                StartDateOfQualification = new DateOnly(2023, 09, 01),
                LastDateForRegistrations = new DateOnly(2025, 08, 31)
            }
        };

        _mockQaaApiClient.Setup(client => client.GetQualificationsAsync(cancellationToken))
            .ReturnsAsync(qualifications);

        _mockQaaRepository.Setup(repo => repo.RunPrerequisitesForImportAsync(cancellationToken))
            .ReturnsAsync(15);

        _mockQaaRepository.Setup(repo => repo.RunImportAsync(It.IsAny<IList<RegulatedQaaQualification>>(), cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ImportDataAsync(cancellationToken);

        // Assert
        Assert.Equal(3, result);
        _mockQaaApiClient.Verify(client => client.GetQualificationsAsync(cancellationToken), Times.Once);
        _mockQaaRepository.Verify(repo => repo.RunPrerequisitesForImportAsync(cancellationToken), Times.Once);
        _mockQaaRepository.Verify(repo => repo.RunImportAsync(It.IsAny<IList<RegulatedQaaQualification>>(), cancellationToken), Times.Once);
    }

    [Fact]
    public async Task ImportDataAsync_WithEmptyQualificationList_ReturnsZeroAndDoesNotCallImport()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var qualifications = new List<QaaQualificationResponse>();

        _mockQaaApiClient.Setup(client => client.GetQualificationsAsync(cancellationToken))
            .ReturnsAsync(qualifications);

        // Act
        var result = await _service.ImportDataAsync(cancellationToken);

        // Assert
        Assert.Equal(0, result);
        _mockQaaApiClient.Verify(client => client.GetQualificationsAsync(cancellationToken), Times.Once);
        _mockQaaRepository.Verify(repo => repo.RunPrerequisitesForImportAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockQaaRepository.Verify(repo => repo.RunImportAsync(It.IsAny<IList<RegulatedQaaQualification>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportDataAsync_WithHttpRequestException_LogsErrorAndThrows()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var httpException = new HttpRequestException("API connection failed", null, System.Net.HttpStatusCode.ServiceUnavailable);

        _mockQaaApiClient.Setup(client => client.GetQualificationsAsync(cancellationToken))
            .ThrowsAsync(httpException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _service.ImportDataAsync(cancellationToken));
        Assert.Equal("API connection failed", exception.Message);
        _mockQaaApiClient.Verify(client => client.GetQualificationsAsync(cancellationToken), Times.Once);
        _mockQaaRepository.Verify(repo => repo.RunPrerequisitesForImportAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockQaaRepository.Verify(repo => repo.RunImportAsync(It.IsAny<IList<RegulatedQaaQualification>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ImportDataAsync_PassesCorrectDataToRunImportAndPreservesToken()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var aimCode = "Z1234567";
        var diplomaTitle = "Access to Higher Education Diploma (Science)";
        var awardingBody = "Test Awarding Body";
        var startDate = new DateOnly(2023, 09, 01);
        var lastRegDate = new DateOnly(2025, 08, 31);

        var qualifications = new List<QaaQualificationResponse>
        {
            new()
            {
                AimCode = aimCode,
                DiplomaTitle = diplomaTitle,
                AwardingBody = awardingBody,
                SsaTier1 = "2",
                SsaTier2 = "1",
                StartDateOfQualification = startDate,
                LastDateForRegistrations = lastRegDate
            }
        };

        _mockQaaApiClient.Setup(client => client.GetQualificationsAsync(cancellationToken))
            .ReturnsAsync(qualifications);

        _mockQaaRepository.Setup(repo => repo.RunPrerequisitesForImportAsync(cancellationToken))
            .ReturnsAsync(5);

        _mockQaaRepository.Setup(repo => repo.RunImportAsync(It.IsAny<IList<RegulatedQaaQualification>>(), cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ImportDataAsync(cancellationToken);

        // Assert
        _mockQaaApiClient.Verify(client => client.GetQualificationsAsync(cancellationToken), Times.Once);
        _mockQaaRepository.Verify(repo => repo.RunPrerequisitesForImportAsync(cancellationToken), Times.Once);
        _mockQaaRepository.Verify(
            repo => repo.RunImportAsync(
                It.Is<IList<RegulatedQaaQualification>>(list =>
                    list.Count() == 1 &&
                    list.First().AimCode == aimCode &&
                    list.First().QualificationTitle == diplomaTitle &&
                    list.First().AwardingBody == awardingBody &&
                    list.First().StartDate == startDate &&
                    list.First().LastDateForRegistration == lastRegDate),
                cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ImportDataAsync_WithValidQualifications_ReturnsTotalCountOfRecordsProcessed()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var expectedCount = 5;
        var qualifications = new List<QaaQualificationResponse>();

        for (int i = 0; i < expectedCount; i++)
        {
            qualifications.Add(new QaaQualificationResponse
            {
                AimCode = $"Z123456{i}",
                DiplomaTitle = $"Access to Higher Education Diploma ({i})",
                AwardingBody = $"Awarding Body {i}",
                SsaTier1 = "2",
                SsaTier2 = "1",
                StartDateOfQualification = new DateOnly(2023, 09, 01),
                LastDateForRegistrations = new DateOnly(2025, 08, 31)
            });
        }

        _mockQaaApiClient.Setup(client => client.GetQualificationsAsync(cancellationToken))
            .ReturnsAsync(qualifications);

        _mockQaaRepository.Setup(repo => repo.RunPrerequisitesForImportAsync(cancellationToken))
            .ReturnsAsync(10);

        _mockQaaRepository.Setup(repo => repo.RunImportAsync(It.IsAny<IList<RegulatedQaaQualification>>(), cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.ImportDataAsync(cancellationToken);

        // Assert
        Assert.Equal(expectedCount, result);
    }

    [Fact]
    public async Task ImportDataAsync_MapsSectorSubjectAreasCorrectly()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var qualifications = new List<QaaQualificationResponse>
        {
            new()
            {
                AimCode = "Z1234567",
                DiplomaTitle = "Science Diploma",
                AwardingBody = "Body 1",
                SsaTier1 = "2",
                SsaTier2 = "1",
                StartDateOfQualification = new DateOnly(2023, 09, 01),
                LastDateForRegistrations = new DateOnly(2025, 08, 31)
            },
            new()
            {
                AimCode = "Z1234568",
                DiplomaTitle = "Engineering Diploma",
                AwardingBody = "Body 2",
                SsaTier1 = "4",
                SsaTier2 = "1",
                StartDateOfQualification = new DateOnly(2023, 09, 01),
                LastDateForRegistrations = new DateOnly(2025, 08, 31)
            },
            new()
            {
                AimCode = "Z1234569",
                DiplomaTitle = "Unknown Sector Diploma",
                AwardingBody = "Body 3",
                SsaTier1 = "99",
                SsaTier2 = "9",
                StartDateOfQualification = new DateOnly(2023, 09, 01),
                LastDateForRegistrations = new DateOnly(2025, 08, 31)
            }
        };

        _mockQaaApiClient.Setup(client => client.GetQualificationsAsync(cancellationToken))
            .ReturnsAsync(qualifications);

        _mockQaaRepository.Setup(repo => repo.RunPrerequisitesForImportAsync(cancellationToken))
            .ReturnsAsync(5);

        _mockQaaRepository.Setup(repo => repo.RunImportAsync(It.IsAny<IList<RegulatedQaaQualification>>(), cancellationToken))
            .Returns(Task.CompletedTask);

        // Act
        await _service.ImportDataAsync(cancellationToken);

        // Assert
        _mockQaaRepository.Verify(
            repo => repo.RunImportAsync(
                It.Is<IList<RegulatedQaaQualification>>(list =>
                    list.Count == 3 &&
                    list.ElementAt(0).SectorSubjectArea.Code == "2.1" &&
                    list.ElementAt(1).SectorSubjectArea.Code == "4.1" &&
                    list.ElementAt(2).SectorSubjectArea.Code == "99.9"),
                cancellationToken),
            Times.Once);
    }
}