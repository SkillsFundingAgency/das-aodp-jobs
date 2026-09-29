using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Data.Entities.Rollover;
using SFA.DAS.AODP.Infrastructure.Context;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Context;

public class FundingDomainEventCollectorTests
{
    [Fact]
    public async Task Collect_WhenTrackedPropertyChangedDirectlyWithoutDomainMethod_RaisesFundingChangedEvent()
    {
        // Arrange - directly mutating EndDate bypasses UpdateFunding()/RecordChanged(), so the
        // only thing that can catch this is AddFundingChange's own change-tracker inspection.
        await using var context = CreateContext();
        var funding = QualificationFunding.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, null);
        context.QualificationFundings.Add(funding);
        await context.SaveChangesAsync();
        FundingDomainEventCollector.Clear(context.ChangeTracker);

        var untouched = QualificationFunding.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, null);
        context.QualificationFundings.Add(untouched);
        await context.SaveChangesAsync();
        FundingDomainEventCollector.Clear(context.ChangeTracker);

        funding.EndDate = new DateOnly(2027, 7, 31);

        // Act
        var events = FundingDomainEventCollector.Collect(context.ChangeTracker);

        // Assert - only the entity that actually changed raises an event
        var changeEvent = events.OfType<FundingChangedDomainEvent>().Single();
        changeEvent.SourceType.ShouldBe(RolloverSourceTypes.Ofqual);
        changeEvent.SourceQualificationId.ShouldBe(funding.QualificationVersionId);
        changeEvent.FundingOfferId.ShouldBe(funding.FundingOfferId);
        changeEvent.PreviousSourceQualificationId.ShouldBeNull();
    }

    [Fact]
    public async Task Collect_WhenSourceQualificationIdChangedDirectly_RaisesEventWithPreviousId()
    {
        // Arrange
        await using var context = CreateContext();
        var originalVersionId = Guid.NewGuid();
        var newVersionId = Guid.NewGuid();
        var funding = QualificationFunding.Create(originalVersionId, Guid.NewGuid(), null, null, null);
        context.QualificationFundings.Add(funding);
        await context.SaveChangesAsync();
        FundingDomainEventCollector.Clear(context.ChangeTracker);

        funding.QualificationVersionId = newVersionId;

        // Act
        var events = FundingDomainEventCollector.Collect(context.ChangeTracker);

        // Assert
        var changeEvent = events.OfType<FundingChangedDomainEvent>().Single();
        changeEvent.SourceQualificationId.ShouldBe(newVersionId);
        changeEvent.PreviousSourceQualificationId.ShouldBe(originalVersionId);
    }

    [Fact]
    public async Task Collect_WhenTrackedEntityIsDeleted_ThrowsBecauseRecordsMustBeArchivedNotDeleted()
    {
        // Arrange
        await using var context = CreateContext();
        var funding = QualificationFunding.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, null);
        context.QualificationFundings.Add(funding);
        await context.SaveChangesAsync();
        context.QualificationFundings.Remove(funding);

        // Act / Assert
        var exception = Should.Throw<InvalidOperationException>(() =>
            FundingDomainEventCollector.Collect(context.ChangeTracker));
        exception.Message.ShouldContain(nameof(QualificationFunding));
    }

    [Fact]
    public async Task Collect_WhenEligibleForFundingChanges_RaisesEligibilityChangedEvent()
    {
        // Arrange
        await using var context = CreateContext();
        var version = new QualificationVersions
        {
            Id = Guid.NewGuid(),
            QualificationId = Guid.NewGuid(),
            Version = 1,
            EligibleForFunding = true,
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
        context.QualificationVersions.Add(version);
        await context.SaveChangesAsync();

        version.EligibleForFunding = false;

        // Act
        var events = FundingDomainEventCollector.Collect(context.ChangeTracker);

        // Assert
        var eligibilityEvent = events.OfType<QualificationFundingEligibilityChangedDomainEvent>().Single();
        eligibilityEvent.SourceType.ShouldBe(RolloverSourceTypes.Ofqual);
        eligibilityEvent.SourceQualificationId.ShouldBe(version.Id);
    }

    [Fact]
    public async Task Clear_RemovesRecordedEventsFromTrackedEntities()
    {
        // Arrange
        await using var context = CreateContext();
        var funding = QualificationFunding.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, null);
        context.QualificationFundings.Add(funding);
        funding.FundingDomainEvents.ShouldNotBeEmpty();

        // Act
        FundingDomainEventCollector.Clear(context.ChangeTracker);

        // Assert
        funding.FundingDomainEvents.ShouldBeEmpty();
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase($"FundingDomainEventCollector_{Guid.NewGuid()}")
            .Options;
        return new ApplicationDbContext(options);
    }
}
