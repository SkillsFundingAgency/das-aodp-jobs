using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Data.Entities.Rollover;

namespace SFA.DAS.AODP.Infrastructure.Context;

internal static class FundingDomainEventCollector
{
    public static IReadOnlyCollection<FundingDomainEvent> Collect(ChangeTracker changeTracker)
    {
        changeTracker.DetectChanges();
        var events = changeTracker.Entries()
            .Where(entry => entry.Entity is IFundingDomainEventSource)
            .SelectMany(entry => ((IFundingDomainEventSource)entry.Entity).FundingDomainEvents)
            .ToList();

        foreach (var entry in changeTracker.Entries<QualificationFunding>())
        {
            AddFundingChange(
                events,
                entry,
                RolloverSourceTypes.Ofqual,
                nameof(QualificationFunding.QualificationVersionId),
                nameof(QualificationFunding.FundingOfferId),
                nameof(QualificationFunding.StartDate),
                nameof(QualificationFunding.EndDate),
                nameof(QualificationFunding.Comments));
        }

        foreach (var entry in changeTracker.Entries<QaaQualificationFunding>())
        {
            AddFundingChange(
                events,
                entry,
                RolloverSourceTypes.Qaa,
                nameof(QaaQualificationFunding.QaaQualificationId),
                nameof(QaaQualificationFunding.FundingOfferId),
                nameof(QaaQualificationFunding.StartDate),
                nameof(QaaQualificationFunding.EndDate),
                nameof(QaaQualificationFunding.FundingStatus),
                nameof(QaaQualificationFunding.Comments));
        }

        foreach (var entry in changeTracker.Entries<QualificationVersions>()
                     .Where(entry =>
                         entry.State == EntityState.Modified &&
                         entry.Property(nameof(QualificationVersions.EligibleForFunding)).IsModified))
        {
            events.Add(new QualificationFundingEligibilityChangedDomainEvent(
                RolloverSourceTypes.Ofqual,
                entry.Entity.Id));
        }

        return events.Distinct().ToList();
    }

    public static void Clear(ChangeTracker changeTracker)
    {
        foreach (var source in changeTracker.Entries()
                     .Select(entry => entry.Entity)
                     .OfType<IFundingDomainEventSource>())
        {
            source.ClearFundingDomainEvents();
        }
    }

    private static void AddFundingChange<TEntity>(
        ICollection<FundingDomainEvent> events,
        EntityEntry<TEntity> entry,
        string sourceType,
        string sourceIdProperty,
        string fundingOfferIdProperty,
        params string[] trackedProperties)
        where TEntity : class
    {
        if (entry.State == EntityState.Deleted)
        {
            throw new InvalidOperationException(
                $"{typeof(TEntity).Name} records must be archived, not deleted.");
        }

        var changed = entry.State == EntityState.Added ||
                      entry.State == EntityState.Modified &&
                      trackedProperties.Any(property => entry.Property(property).IsModified);
        var sourceMoved = entry.State == EntityState.Modified &&
                          entry.Property(sourceIdProperty).IsModified;
        if (!changed && !sourceMoved)
        {
            return;
        }

        events.Add(new FundingChangedDomainEvent(
            sourceType,
            (Guid)entry.Property(sourceIdProperty).CurrentValue!,
            (Guid)entry.Property(fundingOfferIdProperty).CurrentValue!,
            sourceMoved ? (Guid)entry.Property(sourceIdProperty).OriginalValue! : null));
    }
}
