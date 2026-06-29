using SFA.DAS.AODP.Infrastructure.Extensions.Rollover;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services.Rollover;

public class RolloverCandidateQueryExtensionTests
{
    private readonly DateOnly _academicYearEndDate = new(2026, 7, 31);

    [Fact]
    public void WhereEligibleForFunding_ReturnsOnlyEligibleQualificationVersions()
    {
        // Arrange
        var eligibleQualificationVersion = new QualificationVersions { Id = Guid.NewGuid(), EligibleForFunding = true };
        var notEligibleQualificationVersion = new QualificationVersions { Id = Guid.NewGuid(), EligibleForFunding = false };
        var nullEligibilityQualificationVersion = new QualificationVersions { Id = Guid.NewGuid(), EligibleForFunding = null };
        var query = new[]
        {
            eligibleQualificationVersion,
            notEligibleQualificationVersion,
            nullEligibilityQualificationVersion
        }.AsQueryable();

        // Act
        var result = query.WhereEligibleForFunding().ToList();

        // Assert
        result.ShouldBe([eligibleQualificationVersion]);
    }

    [Fact]
    public void WhereActiveForAcademicYear_ReturnsOpenEndedAndAcademicYearEndFundingStreams()
    {
        // Arrange
        var openEndedFunding = QualificationFunding.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, null);
        var academicYearEndFunding = QualificationFunding.Create(Guid.NewGuid(), Guid.NewGuid(), null, _academicYearEndDate, null);
        var expiredFunding = QualificationFunding.Create(Guid.NewGuid(), Guid.NewGuid(), null, _academicYearEndDate.AddDays(-1), null);
        var query = new[]
        {
            openEndedFunding,
            academicYearEndFunding,
            expiredFunding
        }.AsQueryable();

        // Act
        var result = query.WhereActiveForAcademicYear(_academicYearEndDate).ToList();

        // Assert
        result.ShouldBe([openEndedFunding, academicYearEndFunding]);
    }

    [Fact]
    public void WhereLatestVersionPerQualification_ReturnsLatestVersionForEachQualification()
    {
        // Arrange
        var qualificationId = Guid.NewGuid();
        var olderVersion = CreateQualificationVersion(Guid.NewGuid(), qualificationId, 1, new DateTime(2026, 1, 1), new DateTime(2026, 1, 1));
        var latestVersion = CreateQualificationVersion(Guid.NewGuid(), qualificationId, 2, new DateTime(2026, 1, 1), new DateTime(2026, 1, 1));
        var query = new[] { olderVersion, latestVersion }.AsQueryable();

        // Act
        var result = query.WhereLatestVersionPerQualification(query).ToList();

        // Assert
        result.ShouldBe([latestVersion]);
    }

    private static QualificationVersions CreateQualificationVersion(
        Guid id,
        Guid qualificationId,
        int version,
        DateTime lastUpdatedDate,
        DateTime insertedDate)
    {
        return new QualificationVersions
        {
            Id = id,
            QualificationId = qualificationId,
            Version = version,
            LastUpdatedDate = lastUpdatedDate,
            InsertedDate = insertedDate
        };
    }
}
