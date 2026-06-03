using Microsoft.Extensions.Logging.Abstractions;
using SFA.DAS.AODP.Infrastructure.Services;

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
    public async Task ImportDataAsync_WithValidQualifications_CallsRepositoryAndReturnsProcessedCount()
    {
        var cancellationToken = CancellationToken.None;
        var dateOfSnapshot = new DateTime(2024, 02, 15);
        var qualifications = new List<QaaQualificationResponse>
        {
            CreateQualification("Z1234567")
        };

        _mockClockService.Setup(service => service.UtcNow).Returns(dateOfSnapshot);
        _mockQaaApiClient.Setup(client => client.GetQualificationsAsync(cancellationToken))
            .ReturnsAsync(qualifications);
        _mockQaaRepository
            .Setup(repo => repo.ImportQaaQualificationsAsync(qualifications, dateOfSnapshot, cancellationToken))
            .ReturnsAsync(1);

        var result = await _service.ImportDataAsync(cancellationToken);

        Assert.Equal(1, result);
        _mockQaaApiClient.Verify(client => client.GetQualificationsAsync(cancellationToken), Times.Once);
        _mockQaaRepository.Verify(
            repo => repo.ImportQaaQualificationsAsync(qualifications, dateOfSnapshot, cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task ImportDataAsync_WithMultipleQualifications_ReturnsRepositoryProcessedCount()
    {
        var cancellationToken = CancellationToken.None;
        var dateOfSnapshot = new DateTime(2024, 02, 15);
        var qualifications = new List<QaaQualificationResponse>
        {
            CreateQualification("Z1234567"),
            CreateQualification("Z1234568"),
            CreateQualification("Z1234569")
        };

        _mockClockService.Setup(service => service.UtcNow).Returns(dateOfSnapshot);
        _mockQaaApiClient.Setup(client => client.GetQualificationsAsync(cancellationToken))
            .ReturnsAsync(qualifications);
        _mockQaaRepository
            .Setup(repo => repo.ImportQaaQualificationsAsync(qualifications, dateOfSnapshot, cancellationToken))
            .ReturnsAsync(3);

        var result = await _service.ImportDataAsync(cancellationToken);

        Assert.Equal(3, result);
    }

    [Fact]
    public async Task ImportDataAsync_WithEmptyQualificationList_ReturnsZeroAndDoesNotCallImport()
    {
        var cancellationToken = CancellationToken.None;
        var qualifications = new List<QaaQualificationResponse>();

        _mockQaaApiClient.Setup(client => client.GetQualificationsAsync(cancellationToken))
            .ReturnsAsync(qualifications);

        var result = await _service.ImportDataAsync(cancellationToken);

        Assert.Equal(0, result);
        _mockQaaApiClient.Verify(client => client.GetQualificationsAsync(cancellationToken), Times.Once);
        _mockQaaRepository.Verify(
            repo => repo.ImportQaaQualificationsAsync(
                It.IsAny<IReadOnlyCollection<QaaQualificationResponse>>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportDataAsync_WithHttpRequestException_LogsErrorAndThrows()
    {
        var cancellationToken = CancellationToken.None;
        var httpException = new HttpRequestException("API connection failed", null, System.Net.HttpStatusCode.ServiceUnavailable);

        _mockQaaApiClient.Setup(client => client.GetQualificationsAsync(cancellationToken))
            .ThrowsAsync(httpException);

        var exception = await Assert.ThrowsAsync<HttpRequestException>(() => _service.ImportDataAsync(cancellationToken));

        Assert.Equal("API connection failed", exception.Message);
        _mockQaaApiClient.Verify(client => client.GetQualificationsAsync(cancellationToken), Times.Once);
        _mockQaaRepository.Verify(
            repo => repo.ImportQaaQualificationsAsync(
                It.IsAny<IReadOnlyCollection<QaaQualificationResponse>>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ImportDataAsync_PassesApiDataAndSnapshotDateToRepository()
    {
        var cancellationToken = CancellationToken.None;
        var dateOfSnapshot = new DateTime(2024, 02, 15);
        var qualifications = new List<QaaQualificationResponse>
        {
            CreateQualification("Z1234567", discontinuedDate: new DateOnly(2024, 01, 31))
        };

        _mockClockService.Setup(service => service.UtcNow).Returns(dateOfSnapshot);
        _mockQaaApiClient.Setup(client => client.GetQualificationsAsync(cancellationToken))
            .ReturnsAsync(qualifications);
        _mockQaaRepository
            .Setup(repo => repo.ImportQaaQualificationsAsync(
                It.IsAny<IReadOnlyCollection<QaaQualificationResponse>>(),
                dateOfSnapshot,
                cancellationToken))
            .ReturnsAsync(1);

        await _service.ImportDataAsync(cancellationToken);

        _mockQaaRepository.Verify(
            repo => repo.ImportQaaQualificationsAsync(
                It.Is<IReadOnlyCollection<QaaQualificationResponse>>(list =>
                    list.Count == 1 &&
                    list.First().AimCode == "Z1234567" &&
                    list.First().DiscontinuedDate == new DateOnly(2024, 01, 31)),
                dateOfSnapshot,
                cancellationToken),
            Times.Once);
    }

    private static QaaQualificationResponse CreateQualification(string aimCode, DateOnly? discontinuedDate = null)
    {
        return new QaaQualificationResponse
        {
            AimCode = aimCode,
            DiplomaTitle = "Access to Higher Education Diploma (Science)",
            AwardingBody = "Test Awarding Body",
            SsaTier1 = "2",
            SsaTier2 = "1",
            StartDateOfQualification = new DateOnly(2023, 09, 01),
            LastDateForRegistrations = new DateOnly(2025, 08, 31),
            DiscontinuedDate = discontinuedDate
        };
    }
}
