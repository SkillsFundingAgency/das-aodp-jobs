using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities.Rollover;
using SFA.DAS.AODP.Data.Entities.Rollover.Enums;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Models;
using SFA.DAS.AODP.Infrastructure.Repositories.Rollover;
using SFA.DAS.AODP.Infrastructure.Services;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services.Rollover;

public class RolloverCandidateRepositoryTests
{
    private class FakeSystemClockService : ISystemClockService
    {
        public DateTime UtcNow => new(2026, 07, 01);
        public DateOnly Today => new(2026, 07, 01);
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
        stored.ShouldAllBe(candidate =>
            candidate.SourceType == RolloverSourceTypes.Ofqual &&
            candidate.SourceQualificationId == latestVersionId);
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

    [Fact]
    public async Task CreateInitialRolloverCandidatesAsync_CreatesOneQaaCandidatePerFundingOffer()
    {
        await using var context = CreateContext();
        var repository = new RolloverCandidateRepository(
            context,
            new FakeSystemClockService());
        var qualificationId = Guid.NewGuid();
        var fundingOfferIds = new[] { Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid() };
        var academicYear = new AcademicYear(
            "2025/26",
            new DateOnly(2025, 8, 1),
            new DateOnly(2026, 7, 31));
        context.QaaQualificationFundings.AddRange(fundingOfferIds.Select(
            fundingOfferId => QaaQualificationFunding.Create(
                qualificationId,
                fundingOfferId,
                academicYear.StartDate,
                academicYear.EndDate,
                "Not funded",
                new DateTime(2026, 7, 1))));
        await context.SaveChangesAsync();

        var result = await repository.CreateInitialRolloverCandidatesAsync(
            academicYear,
            CancellationToken.None);

        result.ShouldBe(3);
        var stored = await context.RolloverCandidates.ToListAsync();
        stored.Count.ShouldBe(3);
        stored.ShouldAllBe(candidate =>
            candidate.SourceType == RolloverSourceTypes.Qaa &&
            candidate.SourceQualificationId == qualificationId &&
            candidate.AcademicYear == academicYear.Name);
        stored.Select(candidate => candidate.FundingOfferId)
            .ShouldBe(fundingOfferIds, ignoreOrder: true);
    }

    [Fact]
    public async Task CreateInitialRolloverCandidatesAsync_QaaRequiresEndDateInRequestedAcademicYear()
    {
        await using var context = CreateContext();
        var repository = new RolloverCandidateRepository(
            context,
            new FakeSystemClockService());
        var academicYear = new AcademicYear(
            "2025/26",
            new DateOnly(2025, 8, 1),
            new DateOnly(2026, 7, 31));
        context.QaaQualificationFundings.Add(QaaQualificationFunding.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            new DateOnly(2026, 8, 1),
            new DateOnly(2027, 7, 31),
            "Approved",
            new DateTime(2026, 7, 1)));
        await context.SaveChangesAsync();

        var result = await repository.CreateInitialRolloverCandidatesAsync(
            academicYear,
            CancellationToken.None);

        result.ShouldBe(0);
        context.RolloverCandidates.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateInitialRolloverCandidatesAsync_DoesNotDuplicateApiCreatedQaaCandidate()
    {
        await using var context = CreateContext();
        var repository = new RolloverCandidateRepository(
            context,
            new FakeSystemClockService());
        var qualificationId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var academicYear = new AcademicYear(
            "2025/26",
            new DateOnly(2025, 8, 1),
            new DateOnly(2026, 7, 31));
        context.QaaQualificationFundings.Add(QaaQualificationFunding.Create(
            qualificationId,
            fundingOfferId,
            academicYear.StartDate,
            null,
            null,
            new DateTime(2026, 7, 1)));
        context.RolloverCandidates.Add(RolloverCandidate.CreateInitialRound(
            RolloverSourceTypes.Qaa,
            qualificationId,
            fundingOfferId,
            academicYear.Name,
            new DateTime(2026, 6, 30),
            null));
        await context.SaveChangesAsync();

        var result = await repository.CreateInitialRolloverCandidatesAsync(
            academicYear,
            CancellationToken.None);

        result.ShouldBe(0);
        context.RolloverCandidates.Count().ShouldBe(1);
    }

    [Fact]
    public async Task CreateInitialRolloverCandidatesAsync_IsolatesMatchingIdsBySourceType()
    {
        await using var context = CreateContext();
        var repository = new RolloverCandidateRepository(
            context,
            new FakeSystemClockService());
        var sourceQualificationId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var academicYear = new AcademicYear(
            "2025/26",
            new DateOnly(2025, 8, 1),
            new DateOnly(2026, 7, 31));
        context.QualificationVersions.Add(CreateQualificationVersion(
            sourceQualificationId,
            Guid.NewGuid(),
            1,
            true));
        context.QualificationFundings.Add(QualificationFunding.Create(
            sourceQualificationId,
            fundingOfferId,
            null,
            academicYear.EndDate,
            null));
        context.QaaQualificationFundings.Add(QaaQualificationFunding.Create(
            sourceQualificationId,
            fundingOfferId,
            academicYear.StartDate,
            academicYear.EndDate,
            null,
            new DateTime(2026, 7, 1)));
        await context.SaveChangesAsync();

        var result = await repository.CreateInitialRolloverCandidatesAsync(
            academicYear,
            CancellationToken.None);

        result.ShouldBe(2);
        var stored = await context.RolloverCandidates.ToListAsync();
        stored.Count.ShouldBe(2);
        stored.Select(candidate => candidate.SourceType)
            .ShouldBe(
                [RolloverSourceTypes.Ofqual, RolloverSourceTypes.Qaa],
                ignoreOrder: true);
    }

    [Fact]
    public async Task CreateInitialRolloverCandidatesAsync_DeactivatesIneligibleCandidateAndInvalidatesWorkflow()
    {
        await using var context = CreateContext();
        var repository = new RolloverCandidateRepository(
            context,
            new FakeSystemClockService());
        var academicYear = new AcademicYear(
            "2025/26",
            new DateOnly(2025, 8, 1),
            new DateOnly(2026, 7, 31));
        var candidate = RolloverCandidate.CreateInitialRound(
            RolloverSourceTypes.Qaa,
            Guid.NewGuid(),
            Guid.NewGuid(),
            academicYear.Name,
            new DateTime(2026, 6, 1),
            academicYear.EndDate);
        var workflowCandidate = new RolloverWorkflowCandidate();
        SetPrivateProperty(workflowCandidate, nameof(RolloverWorkflowCandidate.Id), Guid.NewGuid());
        SetPrivateProperty(
            workflowCandidate,
            nameof(RolloverWorkflowCandidate.RolloverCandidatesId),
            candidate.Id);
        context.RolloverCandidates.Add(candidate);
        context.RolloverWorkflowCandidates.Add(workflowCandidate);
        await context.SaveChangesAsync();

        var result = await repository.CreateInitialRolloverCandidatesAsync(
            academicYear,
            CancellationToken.None);

        result.ShouldBe(0);
        candidate.IsActive.ShouldBeFalse();
        workflowCandidate.InvalidatedAt.ShouldBe(new FakeSystemClockService().UtcNow);
        workflowCandidate.InvalidationReason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task CreateInitialRolloverCandidatesAsync_ReactivatesHighestExistingRound()
    {
        await using var context = CreateContext();
        var repository = new RolloverCandidateRepository(
            context,
            new FakeSystemClockService());
        var qualificationId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var academicYear = new AcademicYear(
            "2025/26",
            new DateOnly(2025, 8, 1),
            new DateOnly(2026, 7, 31));
        var candidate = RolloverCandidate.CreateInitialRound(
            RolloverSourceTypes.Qaa,
            qualificationId,
            fundingOfferId,
            academicYear.Name,
            new DateTime(2026, 6, 1),
            academicYear.EndDate);
        candidate.Deactivate(new DateTime(2026, 6, 2));
        context.RolloverCandidates.Add(candidate);
        context.QaaQualificationFundings.Add(QaaQualificationFunding.Create(
            qualificationId,
            fundingOfferId,
            academicYear.StartDate,
            academicYear.EndDate,
            "Approved",
            new DateTime(2026, 6, 3)));
        await context.SaveChangesAsync();

        var result = await repository.CreateInitialRolloverCandidatesAsync(
            academicYear,
            CancellationToken.None);

        result.ShouldBe(0);
        candidate.IsActive.ShouldBeTrue();
        candidate.RolloverRound.ShouldBe(1);
        candidate.PreviousFundingEndDate.ShouldBe(
            academicYear.EndDate.ToDateTime(TimeOnly.MinValue));
    }

    [Fact]
    public async Task CreateInitialRolloverCandidatesAsync_WhenFundingMovedToLatestVersion_MovesActiveCandidateWithoutResettingDecision()
    {
        // Arrange
        await using var context = CreateContext();
        var repository = new RolloverCandidateRepository(
            context,
            new FakeSystemClockService());
        var qualificationId = Guid.NewGuid();
        var oldVersionId = Guid.NewGuid();
        var newVersionId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var academicYear = new AcademicYear(
            "2025/26",
            new DateOnly(2025, 8, 1),
            new DateOnly(2026, 7, 31));
        context.QualificationVersions.AddRange(
            CreateQualificationVersion(oldVersionId, qualificationId, 1, true),
            CreateQualificationVersion(newVersionId, qualificationId, 2, true));
        context.QualificationFundings.Add(QualificationFunding.Create(
            newVersionId,
            fundingOfferId,
            null,
            academicYear.EndDate,
            null));
        var candidate = RolloverCandidate.CreateInitialRound(
            RolloverSourceTypes.Ofqual,
            oldVersionId,
            fundingOfferId,
            academicYear.Name,
            new DateTime(2026, 6, 1),
            academicYear.EndDate);
        SetPrivateProperty(
            candidate,
            nameof(RolloverCandidate.RolloverStatus),
            RolloverStatus.Extended);
        SetPrivateProperty(
            candidate,
            nameof(RolloverCandidate.NewFundingEndDate),
            new DateTime(2027, 7, 31));
        var workflow = new RolloverWorkflowCandidate();
        SetPrivateProperty(workflow, nameof(RolloverWorkflowCandidate.Id), Guid.NewGuid());
        SetPrivateProperty(
            workflow,
            nameof(RolloverWorkflowCandidate.RolloverCandidatesId),
            candidate.Id);
        context.RolloverCandidates.Add(candidate);
        context.RolloverWorkflowCandidates.Add(workflow);
        await context.SaveChangesAsync();

        // Act
        var result = await repository.CreateInitialRolloverCandidatesAsync(
            academicYear,
            CancellationToken.None);

        // Assert
        result.ShouldBe(0);
        context.RolloverCandidates.Count().ShouldBe(1);
        candidate.SourceQualificationId.ShouldBe(newVersionId);
        candidate.RolloverStatus.ShouldBe(RolloverStatus.Extended);
        candidate.NewFundingEndDate.ShouldBe(new DateTime(2027, 7, 31));
        candidate.IsActive.ShouldBeTrue();
        workflow.InvalidatedAt.ShouldBe(new FakeSystemClockService().UtcNow);
        workflow.InvalidationReason.ShouldNotBeNull().ShouldContain("newer qualification version");
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    [Fact]
    public async Task CreateInitialRolloverCandidatesAsync_WhenTargetVersionAlreadyHasACandidate_DeactivatesStaleCandidateInsteadOfMoving()
    {
        // Arrange - two active Ofqual candidates for the same qualification end up in the same
        // group once mapped to qualification id: one already sits on the funded (target) version,
        // the other is stale on an older version. The stale one should be deactivated rather than
        // moved, since the target slot is already taken.
        await using var context = CreateContext();
        var repository = new RolloverCandidateRepository(context, new FakeSystemClockService());
        var qualificationId = Guid.NewGuid();
        var staleVersionId = Guid.NewGuid();
        var targetVersionId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var academicYear = new AcademicYear("2025/26", new DateOnly(2025, 8, 1), new DateOnly(2026, 7, 31));

        context.QualificationVersions.AddRange(
            CreateQualificationVersion(staleVersionId, qualificationId, 1, true),
            CreateQualificationVersion(targetVersionId, qualificationId, 2, true));
        context.QualificationFundings.Add(QualificationFunding.Create(
            targetVersionId, fundingOfferId, null, new DateOnly(2026, 7, 31), null));

        var staleCandidate = RolloverCandidate.CreateInitialRound(
            RolloverSourceTypes.Ofqual, staleVersionId, fundingOfferId, academicYear.Name,
            new DateTime(2026, 6, 1), academicYear.EndDate);
        var targetCandidate = RolloverCandidate.CreateInitialRound(
            RolloverSourceTypes.Ofqual, targetVersionId, fundingOfferId, academicYear.Name,
            new DateTime(2026, 6, 1), academicYear.EndDate);
        context.RolloverCandidates.AddRange(staleCandidate, targetCandidate);
        await context.SaveChangesAsync();

        // Act
        await repository.CreateInitialRolloverCandidatesAsync(academicYear, CancellationToken.None);

        // Assert
        staleCandidate.IsActive.ShouldBeFalse();
        staleCandidate.SourceQualificationId.ShouldBe(staleVersionId);
        targetCandidate.IsActive.ShouldBeTrue();
        targetCandidate.SourceQualificationId.ShouldBe(targetVersionId);
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

    private static void SetPrivateProperty<T>(
        T instance,
        string propertyName,
        object value)
    {
        typeof(T).GetProperty(propertyName)!.SetValue(instance, value);
    }
}
