using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Data.Entities.Rollover;
using SFA.DAS.AODP.Data.Entities.Rollover.Enums;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces.Rollover;
using SFA.DAS.AODP.Infrastructure.Repositories.Rollover;
using SFA.DAS.AODP.Infrastructure.Services;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services.Rollover;

public class FundingDomainEventDispatcherTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task DispatchAsync_WhenOfqualFundingMoves_PreservesCandidateDecision()
    {
        // Arrange
        await using var context = CreateContext();
        var oldVersionId = Guid.NewGuid();
        var newVersionId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var candidate = RolloverCandidate.CreateInitialRound(
            RolloverSourceTypes.Ofqual,
            oldVersionId,
            fundingOfferId,
            "2025/26",
            Now.AddDays(-1),
            new DateOnly(2026, 7, 31));
        SetPrivateProperty(
            candidate,
            nameof(RolloverCandidate.RolloverStatus),
            RolloverStatus.Extended);
        SetPrivateProperty(
            candidate,
            nameof(RolloverCandidate.NewFundingEndDate),
            new DateTime(2027, 7, 31));
        context.RolloverCandidates.Add(candidate);
        await context.SaveChangesAsync();
        var sut = new FundingDomainEventDispatcher(
            new FakeSystemClockService(),
            NullLogger<FundingDomainEventDispatcher>.Instance);

        // Act
        await sut.DispatchAsync(
            context,
            [new FundingChangedDomainEvent(
                RolloverSourceTypes.Ofqual,
                newVersionId,
                fundingOfferId,
                oldVersionId)],
            CancellationToken.None);

        // Assert
        candidate.SourceQualificationId.ShouldBe(newVersionId);
        candidate.RolloverStatus.ShouldBe(RolloverStatus.Extended);
        candidate.NewFundingEndDate.ShouldBe(new DateTime(2027, 7, 31));
        candidate.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenQaaFundingChanges_DispatchesAtContextBoundary()
    {
        // Arrange
        var dispatcher = new Mock<IFundingDomainEventDispatcher>();
        dispatcher
            .Setup(instance => instance.DispatchAsync(
                It.IsAny<ApplicationDbContext>(),
                It.IsAny<IReadOnlyCollection<FundingDomainEvent>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options, dispatcher.Object);
        var qualificationId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        context.QaaQualificationFundings.Add(QaaQualificationFunding.Create(
            qualificationId,
            fundingOfferId,
            null,
            new DateOnly(2026, 7, 31),
            "Approved",
            Now));

        // Act
        await context.SaveChangesAsync();

        // Assert
        dispatcher.Verify(instance => instance.DispatchAsync(
            context,
            It.Is<IReadOnlyCollection<FundingDomainEvent>>(events =>
                events.OfType<FundingChangedDomainEvent>().Any(domainEvent =>
                    domainEvent.SourceType == RolloverSourceTypes.Qaa &&
                    domainEvent.SourceQualificationId == qualificationId &&
                    domainEvent.FundingOfferId == fundingOfferId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_WhenOfqualFundingIsEligibleWithNoExistingCandidate_CreatesNewCandidate()
    {
        // Arrange
        await using var context = CreateContext();
        var qualificationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        context.QualificationVersions.Add(CreateQualificationVersion(versionId, qualificationId, 1, true));
        context.QualificationFundings.Add(QualificationFunding.Create(
            versionId, fundingOfferId, null, new DateOnly(2026, 7, 31), null));
        await context.SaveChangesAsync();
        var sut = CreateDispatcher();

        // Act
        await sut.DispatchAsync(
            context,
            [new FundingChangedDomainEvent(RolloverSourceTypes.Ofqual, versionId, fundingOfferId)],
            CancellationToken.None);

        // Assert
        await context.SaveChangesAsync();
        var candidate = context.RolloverCandidates.Single();
        candidate.SourceQualificationId.ShouldBe(versionId);
        candidate.AcademicYear.ShouldBe("2025/26");
        candidate.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WhenNoMatchingFundingRowExists_DeactivatesActiveCandidateAndInvalidatesWorkflow()
    {
        // Arrange - ineligibility here comes from there being no funding row at all for this key.
        await using var context = CreateContext();
        var versionId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var candidate = RolloverCandidate.CreateInitialRound(
            RolloverSourceTypes.Ofqual, versionId, fundingOfferId, "2025/26", Now.AddDays(-1), new DateOnly(2026, 7, 31));
        var workflow = new RolloverWorkflowCandidate();
        SetPrivateProperty(workflow, nameof(RolloverWorkflowCandidate.Id), Guid.NewGuid());
        SetPrivateProperty(workflow, nameof(RolloverWorkflowCandidate.RolloverCandidatesId), candidate.Id);
        context.RolloverCandidates.Add(candidate);
        context.RolloverWorkflowCandidates.Add(workflow);
        await context.SaveChangesAsync();
        var sut = CreateDispatcher();

        // Act
        await sut.DispatchAsync(
            context,
            [new FundingChangedDomainEvent(RolloverSourceTypes.Ofqual, versionId, fundingOfferId)],
            CancellationToken.None);

        // Assert
        candidate.IsActive.ShouldBeFalse();
        workflow.InvalidatedAt.ShouldBe(Now);
        workflow.InvalidationReason.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task DispatchAsync_WhenEligibleFundingChangesForActiveCandidate_RefreshesWithoutDuplicating()
    {
        // Arrange
        await using var context = CreateContext();
        var qualificationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        context.QualificationVersions.Add(CreateQualificationVersion(versionId, qualificationId, 1, true));
        context.QualificationFundings.Add(QualificationFunding.Create(
            versionId, fundingOfferId, null, new DateOnly(2026, 7, 31), null));
        var candidate = RolloverCandidate.CreateInitialRound(
            RolloverSourceTypes.Ofqual, versionId, fundingOfferId, "2025/26", Now.AddDays(-1), null);
        context.RolloverCandidates.Add(candidate);
        await context.SaveChangesAsync();
        var sut = CreateDispatcher();

        // Act
        await sut.DispatchAsync(
            context,
            [new FundingChangedDomainEvent(RolloverSourceTypes.Ofqual, versionId, fundingOfferId)],
            CancellationToken.None);

        // Assert
        context.RolloverCandidates.Count().ShouldBe(1);
        candidate.IsActive.ShouldBeTrue();
        candidate.PreviousFundingEndDate.ShouldBe(new DateOnly(2026, 7, 31).ToDateTime(TimeOnly.MinValue));
    }

    [Fact]
    public async Task DispatchAsync_WhenEligibleFundingMatchesInactiveCandidate_Reactivates()
    {
        // Arrange
        await using var context = CreateContext();
        var qualificationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        context.QualificationVersions.Add(CreateQualificationVersion(versionId, qualificationId, 1, true));
        context.QualificationFundings.Add(QualificationFunding.Create(
            versionId, fundingOfferId, null, new DateOnly(2026, 7, 31), null));
        var candidate = RolloverCandidate.CreateInitialRound(
            RolloverSourceTypes.Ofqual, versionId, fundingOfferId, "2025/26", Now.AddDays(-1), null);
        candidate.Deactivate(Now.AddDays(-1));
        context.RolloverCandidates.Add(candidate);
        await context.SaveChangesAsync();
        var sut = CreateDispatcher();

        // Act
        await sut.DispatchAsync(
            context,
            [new FundingChangedDomainEvent(RolloverSourceTypes.Ofqual, versionId, fundingOfferId)],
            CancellationToken.None);

        // Assert
        candidate.IsActive.ShouldBeTrue();
        candidate.RolloverStatus.ShouldBe(RolloverStatus.NeedsReview);
    }

    [Fact]
    public async Task DispatchAsync_WhenQaaFundingIsEligibleWithNoExistingCandidate_CreatesNewCandidate()
    {
        // Arrange - QAA eligibility only depends on being active for the academic year, not on
        // a QualificationVersions row (there isn't one for QAA).
        await using var context = CreateContext();
        var qaaQualificationId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        context.QaaQualificationFundings.Add(QaaQualificationFunding.Create(
            qaaQualificationId, fundingOfferId, null, new DateOnly(2026, 7, 31), "Approved", Now));
        await context.SaveChangesAsync();
        var sut = CreateDispatcher();

        // Act
        await sut.DispatchAsync(
            context,
            [new FundingChangedDomainEvent(RolloverSourceTypes.Qaa, qaaQualificationId, fundingOfferId)],
            CancellationToken.None);

        // Assert
        await context.SaveChangesAsync();
        var candidate = context.RolloverCandidates.Single();
        candidate.SourceType.ShouldBe(RolloverSourceTypes.Qaa);
        candidate.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WhenSourceTypeIsUnsupported_ThrowsNotSupportedException()
    {
        // Arrange
        await using var context = CreateContext();
        var sut = CreateDispatcher();

        // Act / Assert
        await Should.ThrowAsync<NotSupportedException>(() =>
            sut.DispatchAsync(
                context,
                [new FundingChangedDomainEvent("FutureRegulator", Guid.NewGuid(), Guid.NewGuid())],
                CancellationToken.None));
    }

    [Fact]
    public async Task DispatchAsync_WhenQaaFundingHasPreviousQualification_ThrowsBecauseOnlyOfqualCanMove()
    {
        // Arrange
        await using var context = CreateContext();
        var sut = CreateDispatcher();

        // Act / Assert
        await Should.ThrowAsync<InvalidOperationException>(() =>
            sut.DispatchAsync(
                context,
                [new FundingChangedDomainEvent(
                    RolloverSourceTypes.Qaa, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid())],
                CancellationToken.None));
    }

    [Fact]
    public async Task DispatchAsync_WhenTargetCandidateAlreadyActive_DeactivatesOldCandidateInsteadOfMoving()
    {
        // Arrange
        await using var context = CreateContext();
        var oldVersionId = Guid.NewGuid();
        var newVersionId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var oldCandidate = RolloverCandidate.CreateInitialRound(
            RolloverSourceTypes.Ofqual, oldVersionId, fundingOfferId, "2025/26", Now.AddDays(-2), null);
        var targetCandidate = RolloverCandidate.CreateInitialRound(
            RolloverSourceTypes.Ofqual, newVersionId, fundingOfferId, "2025/26", Now.AddDays(-1), null);
        context.RolloverCandidates.AddRange(oldCandidate, targetCandidate);
        await context.SaveChangesAsync();
        var sut = CreateDispatcher();

        // Act
        await sut.DispatchAsync(
            context,
            [new FundingChangedDomainEvent(
                RolloverSourceTypes.Ofqual, newVersionId, fundingOfferId, oldVersionId)],
            CancellationToken.None);

        // Assert
        oldCandidate.IsActive.ShouldBeFalse();
        oldCandidate.SourceQualificationId.ShouldBe(oldVersionId);
        targetCandidate.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task DispatchAsync_WhenEligibilityChangeEventFires_ExpandsToMatchingFundingOfferAndReconciles()
    {
        // Arrange - QualificationFundingEligibilityChangedDomainEvent only carries the qualification
        // version id; the dispatcher must expand it to the funding offers on that version before
        // it can reconcile candidates.
        await using var context = CreateContext();
        var qualificationId = Guid.NewGuid();
        var versionId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        context.QualificationVersions.Add(CreateQualificationVersion(versionId, qualificationId, 1, true));
        context.QualificationFundings.Add(QualificationFunding.Create(
            versionId, fundingOfferId, null, new DateOnly(2026, 7, 31), null));
        await context.SaveChangesAsync();
        var sut = CreateDispatcher();

        // Act
        await sut.DispatchAsync(
            context,
            [new QualificationFundingEligibilityChangedDomainEvent(RolloverSourceTypes.Ofqual, versionId)],
            CancellationToken.None);

        // Assert
        await context.SaveChangesAsync();
        var candidate = context.RolloverCandidates.Single();
        candidate.SourceQualificationId.ShouldBe(versionId);
        candidate.FundingOfferId.ShouldBe(fundingOfferId);
    }

    private static FundingDomainEventDispatcher CreateDispatcher()
    {
        return new FundingDomainEventDispatcher(
            new FakeSystemClockService(),
            NullLogger<FundingDomainEventDispatcher>.Instance);
    }

    private static QualificationVersions CreateQualificationVersion(
        Guid id, Guid qualificationId, int version, bool eligibleForFunding)
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

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void SetPrivateProperty<T>(T instance, string propertyName, object value)
    {
        typeof(T).GetProperty(propertyName)!.SetValue(instance, value);
    }

    private sealed class FakeSystemClockService : ISystemClockService
    {
        public DateTime UtcNow => Now;
        public DateOnly Today => DateOnly.FromDateTime(Now);
    }
}
