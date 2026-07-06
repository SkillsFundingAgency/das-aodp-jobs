using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities.Rollover;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Models;
using SFA.DAS.AODP.Infrastructure.Repositories.Rollover;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services.Rollover;

public class RolloverCandidateRepositoryTests
{
    private class FakeSystemClockService : ISystemClockService
    {
        public DateTime UtcNow => new(2026, 07, 01);
    }

    [Fact]
    public async Task CreateInitialRolloverCandidatesAsync_CreatesCandidatesForLatestEligibleVersionsWithActiveFunding()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new RolloverCandidateRepository(context, new FakeSystemClockService());
        var qualificationId = Guid.NewGuid();
        var olderVersionId = Guid.NewGuid();
        var latestVersionId = Guid.NewGuid();
        var ineligibleVersionId = Guid.NewGuid();
        var activeOfferId = Guid.NewGuid();
        var openEndedOfferId = Guid.NewGuid();
        var expiredOfferId = Guid.NewGuid();
        var academicYear = new AcademicYear("2025/26", new DateOnly(2025, 8, 1), new DateOnly(2026, 7, 31));

        context.QualificationVersions.AddRange(
            CreateQualificationVersion(olderVersionId, qualificationId, 1, true),
            CreateQualificationVersion(latestVersionId, qualificationId, 2, true),
            CreateQualificationVersion(ineligibleVersionId, Guid.NewGuid(), 1, false));

        context.QualificationFundings.AddRange(
            QualificationFunding.Create(olderVersionId, activeOfferId, null, new DateOnly(2026, 7, 31), null),
            QualificationFunding.Create(latestVersionId, activeOfferId, null, new DateOnly(2026, 7, 31), null),
            QualificationFunding.Create(latestVersionId, openEndedOfferId, null, null, null),
            QualificationFunding.Create(latestVersionId, expiredOfferId, null, new DateOnly(2025, 7, 30), null),
            QualificationFunding.Create(ineligibleVersionId, activeOfferId, null, new DateOnly(2026, 7, 31), null));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.CreateInitialRolloverCandidatesAsync(
            academicYear,
            CancellationToken.None);

        // Assert
        result.ShouldBe(2);
        var stored = await context.RolloverCandidates.ToListAsync();
        stored.Count.ShouldBe(2);
        stored.ShouldAllBe(candidate => candidate.SourceType == RolloverSourceTypes.Ofqual);
        stored.ShouldAllBe(candidate => candidate.SourceQualificationId == latestVersionId);
        stored.Select(candidate => candidate.FundingOfferId).ShouldBe([activeOfferId, openEndedOfferId], ignoreOrder: true);
        stored.ShouldAllBe(candidate => candidate.AcademicYear == "2025/26");
        stored.ShouldAllBe(candidate => candidate.RolloverRound == 1);
        stored.ShouldAllBe(candidate => candidate.IsActive);
    }

    [Fact]
    public async Task CreateInitialRolloverCandidatesAsync_DoesNotCreateDuplicateCandidates()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new RolloverCandidateRepository(context, new FakeSystemClockService());
        var qualificationVersionId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var createdAt = new DateTime(2026, 6, 28, 9, 30, 0, DateTimeKind.Utc);
        var academicYear = new AcademicYear("2025/26", new DateOnly(2025, 8, 1), new DateOnly(2026, 7, 31));

        context.QualificationVersions.Add(CreateQualificationVersion(qualificationVersionId, Guid.NewGuid(), 1, true));
        context.QualificationFundings.Add(QualificationFunding.Create(qualificationVersionId, fundingOfferId, null, null, null));
        context.RolloverCandidates.Add(RolloverCandidate.CreateInitialRound(qualificationVersionId, fundingOfferId, "2025/26", createdAt, null));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.CreateInitialRolloverCandidatesAsync(
            academicYear,
            CancellationToken.None);

        // Assert
        result.ShouldBe(0);
        context.RolloverCandidates.Count().ShouldBe(1);
    }

    [Fact]
    public async Task CreateInitialRolloverCandidatesAsync_ReturnsZeroAndDoesNotDuplicate_WhenRunAgainForSameAcademicYear()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new RolloverCandidateRepository(context, new FakeSystemClockService());
        var qualificationVersionId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var academicYear = new AcademicYear("2025/26", new DateOnly(2025, 8, 1), new DateOnly(2026, 7, 31));

        context.QualificationVersions.Add(CreateQualificationVersion(qualificationVersionId, Guid.NewGuid(), 1, true));
        context.QualificationFundings.Add(QualificationFunding.Create(qualificationVersionId, fundingOfferId, null, null, null));
        await context.SaveChangesAsync();

        await repository.CreateInitialRolloverCandidatesAsync(
            academicYear,
            CancellationToken.None);

        // Act
        var result = await repository.CreateInitialRolloverCandidatesAsync(
            academicYear,
            CancellationToken.None);

        // Assert
        result.ShouldBe(0);
        context.RolloverCandidates.Count().ShouldBe(1);
    }

    [Fact]
    public async Task CreateInitialRolloverCandidatesAsync_DoesNotCreateCandidate_WhenLatestVersionIsNotEligible()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new RolloverCandidateRepository(context, new FakeSystemClockService());
        var qualificationId = Guid.NewGuid();
        var olderEligibleVersionId = Guid.NewGuid();
        var latestIneligibleVersionId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var academicYear = new AcademicYear("2025/26", new DateOnly(2025, 8, 1), new DateOnly(2026, 7, 31));

        context.QualificationVersions.AddRange(
            CreateQualificationVersion(olderEligibleVersionId, qualificationId, 1, true),
            CreateQualificationVersion(latestIneligibleVersionId, qualificationId, 2, false));
        context.QualificationFundings.Add(QualificationFunding.Create(olderEligibleVersionId, fundingOfferId, null, null, null));
        await context.SaveChangesAsync();

        // Act
        var result = await repository.CreateInitialRolloverCandidatesAsync(
            academicYear,
            CancellationToken.None);

        // Assert
        result.ShouldBe(0);
        context.RolloverCandidates.ShouldBeEmpty();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static QualificationVersions CreateQualificationVersion(Guid id, Guid qualificationId, int version, bool eligibleForFunding)
    {
        return new QualificationVersions
        {
            Id = id,
            QualificationId = qualificationId,
            Version = version,
            EligibleForFunding = eligibleForFunding,
            VersionFieldChangesId = Guid.NewGuid(),
            ProcessStatusId = Guid.NewGuid(),
            LifecycleStageId = Guid.NewGuid(),
            AwardingOrganisationId = Guid.NewGuid(),
            Status = "Approved",
            Type = "Type",
            Ssa = "SSA",
            Level = "Level 3",
            SubLevel = string.Empty,
            EqfLevel = string.Empty,
            RegulationStartDate = DateTime.UtcNow,
            OperationalStartDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow,
            UiLastUpdatedDate = DateTime.UtcNow,
            InsertedDate = DateTime.UtcNow
        };
    }
}
