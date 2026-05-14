using Microsoft.Extensions.Logging.Abstractions;
using SFA.DAS.AODP.Models.Config;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services;

public class QaaQualificationSeedServiceTests
{
    private readonly Mock<IQaaSeedCsvBlobReader> _blobReaderMock = new();
    private readonly Mock<IQaaRepository> _qaaRepositoryMock = new();
    private readonly Mock<ISystemClockService> _clockServiceMock = new();

    [Fact]
    public async Task SeedAsync_ReadsConfiguredBlob_AndImportsMappedQaaQualifications()
    {
        var cancellationToken = CancellationToken.None;
        var snapshotDate = new DateTime(2026, 05, 14, 10, 30, 0);
        var configuration = CreateConfiguration();
        using var csvStream = CreateCsvStream(
            "40001234,Test AVA,Access to HE Diploma,Science,Medicine,09/01,01/09/2024,08/31,31/08/2026,08/31,31/08/2027,Active,15/05/2026");

        _blobReaderMock
            .Setup(reader => reader.OpenReadAsync("qaa-seed-data", "qaa-qualifications.csv", cancellationToken))
            .ReturnsAsync(csvStream);
        _clockServiceMock.Setup(service => service.UtcNow).Returns(snapshotDate);
        _qaaRepositoryMock
            .Setup(repository => repository.ImportQaaQualificationsAsync(
                It.IsAny<IReadOnlyCollection<QaaQualificationResponse>>(),
                snapshotDate,
                cancellationToken))
            .ReturnsAsync(1);

        var service = CreateService(configuration);

        var result = await service.SeedAsync(cancellationToken);

        Assert.Equal(1, result);
        _qaaRepositoryMock.Verify(repository => repository.ImportQaaQualificationsAsync(
            It.Is<IReadOnlyCollection<QaaQualificationResponse>>(records =>
                records.Count == 1 &&
                records.First().AimCode == "40001234" &&
                records.First().AwardingBody == "Test AVA" &&
                records.First().DiplomaTitle == "Access to HE Diploma" &&
                records.First().SsaTier1 == "Science" &&
                records.First().SsaTier2 == "Medicine" &&
                records.First().StartDateOfQualification == new DateOnly(2024, 9, 1) &&
                records.First().LastDateForRegistrations == new DateOnly(2026, 8, 31) &&
                records.First().LastDateForCertifications == new DateOnly(2027, 8, 31) &&
                records.First().AwardStatus == "Active" &&
                records.First().DiscontinuedDate == new DateOnly(2026, 5, 15)),
            snapshotDate,
            cancellationToken),
            Times.Once);
    }

    [Fact]
    public async Task SeedAsync_WhenCsvIsEmpty_ReturnsZero_AndDoesNotImport()
    {
        var cancellationToken = CancellationToken.None;
        var configuration = CreateConfiguration();
        using var csvStream = CreateCsvStream();

        _blobReaderMock
            .Setup(reader => reader.OpenReadAsync("qaa-seed-data", "qaa-qualifications.csv", cancellationToken))
            .ReturnsAsync(csvStream);

        var service = CreateService(configuration);

        var result = await service.SeedAsync(cancellationToken);

        Assert.Equal(0, result);
        _qaaRepositoryMock.Verify(repository => repository.ImportQaaQualificationsAsync(
            It.IsAny<IReadOnlyCollection<QaaQualificationResponse>>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null, "qaa-qualifications.csv", "QaaSeedData:ContainerName must be configured.")]
    [InlineData("qaa-seed-data", null, "QaaSeedData:BlobName must be configured.")]
    public async Task SeedAsync_WhenConfigurationIsMissing_ThrowsClearError(
        string? containerName,
        string? blobName,
        string expectedMessage)
    {
        var configuration = new QaaSeedDataConfiguration
        {
            ContainerName = containerName,
            BlobName = blobName
        };
        var service = CreateService(configuration);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SeedAsync(CancellationToken.None));

        Assert.Equal(expectedMessage, exception.Message);
        _blobReaderMock.Verify(reader => reader.OpenReadAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task SeedAsync_WhenBlobCannotBeRead_PropagatesException()
    {
        var cancellationToken = CancellationToken.None;
        var configuration = CreateConfiguration();
        var expectedException = new InvalidOperationException("Blob read failed.");

        _blobReaderMock
            .Setup(reader => reader.OpenReadAsync("qaa-seed-data", "qaa-qualifications.csv", cancellationToken))
            .ThrowsAsync(expectedException);

        var service = CreateService(configuration);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SeedAsync(cancellationToken));

        Assert.Equal(expectedException, exception);
    }

    private QaaQualificationSeedService CreateService(QaaSeedDataConfiguration configuration)
    {
        return new QaaQualificationSeedService(
            NullLogger<QaaQualificationSeedService>.Instance,
            configuration,
            _blobReaderMock.Object,
            _qaaRepositoryMock.Object,
            _clockServiceMock.Object);
    }

    private static QaaSeedDataConfiguration CreateConfiguration()
    {
        return new QaaSeedDataConfiguration
        {
            Enabled = true,
            ContainerName = "qaa-seed-data",
            BlobName = "qaa-qualifications.csv"
        };
    }

    private static MemoryStream CreateCsvStream(params string[] rows)
    {
        var csvContent = string.Join(
            Environment.NewLine,
            new[]
            {
                "AIM code,Awarding body,Diploma Title,SSA Tier 1,SSA Tier 2,Start date of qualification,Full start date of qualification,Last date for registration,Full Last date for registration,Last date for certification,Full Last date for certification,Award status,Discontinued date"
            }.Concat(rows));

        return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(csvContent));
    }
}
