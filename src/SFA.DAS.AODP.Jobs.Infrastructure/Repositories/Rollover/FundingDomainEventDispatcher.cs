using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Data.Entities.Rollover;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Extensions.Rollover;
using SFA.DAS.AODP.Infrastructure.Interfaces.Rollover;
using SFA.DAS.AODP.Infrastructure.Models;
using SFA.DAS.AODP.Infrastructure.Services;

namespace SFA.DAS.AODP.Infrastructure.Repositories.Rollover;

public sealed class FundingDomainEventDispatcher(
    ISystemClockService clock,
    ILogger<FundingDomainEventDispatcher> logger) : IFundingDomainEventDispatcher
{
    private const string FundingNoLongerApplicableReason =
        "The source funding is no longer applicable for rollover.";
    private const string SourceMovedReason =
        "The Ofqual funding moved to a newer qualification version.";

    public async Task DispatchAsync(
        ApplicationDbContext context,
        IReadOnlyCollection<FundingDomainEvent> events,
        CancellationToken cancellationToken)
    {
        var fundingChanges = events.OfType<FundingChangedDomainEvent>().Distinct().ToList();
        var eligibilityChanges = events
            .OfType<QualificationFundingEligibilityChangedDomainEvent>()
            .Distinct()
            .ToList();
        var keys = new List<FundingKey>();

        foreach (var fundingChange in fundingChanges)
        {
            var moveHandled = fundingChange.PreviousSourceQualificationId.HasValue &&
                              await MoveActiveOfqualCandidatesAsync(
                                  context,
                                  fundingChange,
                                  cancellationToken);
            if (!moveHandled)
            {
                keys.Add(new FundingKey(
                    fundingChange.SourceType,
                    fundingChange.SourceQualificationId,
                    fundingChange.FundingOfferId));
            }
        }

        keys.AddRange(await ExpandEligibilityChangesAsync(
            context,
            eligibilityChanges,
            cancellationToken));

        var academicYear = GetCurrentAcademicYear();
        var distinctKeys = keys.Distinct().ToList();
        if (distinctKeys.Count > 0)
        {
            await ReconcileAsync(context, distinctKeys, academicYear, cancellationToken);
        }

        if (keys.Count > 0)
        {
            logger.LogInformation(
                "Reconciled {FundingChangeCount} rollover funding changes for academic year {AcademicYear}.",
                keys.Count,
                academicYear.Name);
        }
    }

    private async Task ReconcileAsync(
        ApplicationDbContext context,
        IReadOnlyCollection<FundingKey> keys,
        AcademicYear academicYear,
        CancellationToken cancellationToken)
    {
        var unsupportedSource = keys.FirstOrDefault(key =>
            key.SourceType != RolloverSourceTypes.Ofqual &&
            key.SourceType != RolloverSourceTypes.Qaa);
        if (unsupportedSource is not null)
        {
            throw new NotSupportedException(
                $"Rollover funding source type '{unsupportedSource.SourceType}' is not supported.");
        }

        var eligibilityByKey = await GetEligibilityAsync(
            context,
            keys,
            academicYear,
            cancellationToken);
        var sourceQualificationIds = keys
            .Select(key => key.SourceQualificationId)
            .Distinct()
            .ToList();
        var fundingOfferIds = keys
            .Select(key => key.FundingOfferId)
            .Distinct()
            .ToList();

        var candidates = await context.RolloverCandidates
            .Where(candidate =>
                sourceQualificationIds.Contains(candidate.SourceQualificationId) &&
                fundingOfferIds.Contains(candidate.FundingOfferId) &&
                candidate.AcademicYear == academicYear.Name)
            .ToListAsync(cancellationToken);
        var candidatesByKey = candidates.ToLookup(candidate => new FundingKey(
            candidate.SourceType,
            candidate.SourceQualificationId,
            candidate.FundingOfferId));
        var now = clock.UtcNow;
        var deactivatedCandidateIds = new List<Guid>();

        foreach (var key in keys)
        {
            var eligibility = eligibilityByKey.GetValueOrDefault(key) ?? new FundingEligibility(false, null);
            var matchingCandidates = candidatesByKey[key]
                .OrderByDescending(candidate => candidate.RolloverRound)
                .ToList();

            if (!eligibility.IsEligible)
            {
                foreach (var candidate in matchingCandidates.Where(candidate => candidate.IsActive))
                {
                    candidate.Deactivate(now);
                    deactivatedCandidateIds.Add(candidate.Id);
                }

                continue;
            }

            var activeCandidate = matchingCandidates.FirstOrDefault(candidate => candidate.IsActive);
            if (activeCandidate is not null)
            {
                activeCandidate.RefreshFunding(eligibility.EndDate, now);
                continue;
            }

            var inactiveCandidate = matchingCandidates.FirstOrDefault();
            if (inactiveCandidate is not null)
            {
                inactiveCandidate.Reactivate(eligibility.EndDate, now);
                continue;
            }

            context.RolloverCandidates.Add(RolloverCandidate.CreateInitialRound(
                key.SourceType,
                key.SourceQualificationId,
                key.FundingOfferId,
                academicYear.Name,
                now,
                eligibility.EndDate));
        }

        await InvalidateWorkflowsAsync(
            context,
            deactivatedCandidateIds,
            FundingNoLongerApplicableReason,
            now,
            cancellationToken);
    }

    private static async Task<Dictionary<FundingKey, FundingEligibility>> GetEligibilityAsync(
        ApplicationDbContext context,
        IReadOnlyCollection<FundingKey> keys,
        AcademicYear academicYear,
        CancellationToken cancellationToken)
    {
        var sourceQualificationIds = keys
            .Select(key => key.SourceQualificationId)
            .Distinct()
            .ToList();
        var fundingOfferIds = keys
            .Select(key => key.FundingOfferId)
            .Distinct()
            .ToList();

        var latestEligibleVersionIds = await context.QualificationVersions
            .Where(version => sourceQualificationIds.Contains(version.Id))
            .WhereLatestVersionPerQualification(context.QualificationVersions)
            .WhereEligibleForFunding()
            .Select(version => version.Id)
            .ToHashSetAsync(cancellationToken);

        var ofqualFundings = await context.QualificationFundings
            .AsNoTracking()
            .Where(item =>
                sourceQualificationIds.Contains(item.QualificationVersionId) &&
                fundingOfferIds.Contains(item.FundingOfferId))
            .Select(item => new
            {
                item.QualificationVersionId,
                item.FundingOfferId,
                item.EndDate
            })
            .ToListAsync(cancellationToken);
        var qaaFundings = await context.QaaQualificationFundings
            .AsNoTracking()
            .Where(item =>
                sourceQualificationIds.Contains(item.QaaQualificationId) &&
                fundingOfferIds.Contains(item.FundingOfferId))
            .Select(item => new
            {
                item.QaaQualificationId,
                item.FundingOfferId,
                item.EndDate
            })
            .ToListAsync(cancellationToken);

        var result = ofqualFundings.ToDictionary(
            funding => new FundingKey(
                RolloverSourceTypes.Ofqual,
                funding.QualificationVersionId,
                funding.FundingOfferId),
            funding => new FundingEligibility(
                latestEligibleVersionIds.Contains(funding.QualificationVersionId) &&
                    IsActiveForAcademicYear(funding.EndDate, academicYear),
                funding.EndDate));
        foreach (var funding in qaaFundings)
        {
            result[new FundingKey(
                RolloverSourceTypes.Qaa,
                funding.QaaQualificationId,
                funding.FundingOfferId)] = new FundingEligibility(
                IsActiveForAcademicYear(funding.EndDate, academicYear),
                funding.EndDate);
        }

        return result;
    }

    private async Task<bool> MoveActiveOfqualCandidatesAsync(
        ApplicationDbContext context,
        FundingChangedDomainEvent fundingChange,
        CancellationToken cancellationToken)
    {
        if (fundingChange.SourceType != RolloverSourceTypes.Ofqual)
        {
            throw new InvalidOperationException(
                "Only Ofqual funding can move between qualification versions.");
        }

        var previousVersionId = fundingChange.PreviousSourceQualificationId!.Value;
        var oldCandidates = await context.RolloverCandidates
            .Where(candidate =>
                candidate.SourceType == RolloverSourceTypes.Ofqual &&
                candidate.SourceQualificationId == previousVersionId &&
                candidate.FundingOfferId == fundingChange.FundingOfferId &&
                candidate.IsActive)
            .ToListAsync(cancellationToken);
        if (oldCandidates.Count == 0)
        {
            return false;
        }

        var academicYears = oldCandidates.Select(candidate => candidate.AcademicYear).Distinct().ToList();
        var targetCandidates = await context.RolloverCandidates
            .Where(candidate =>
                candidate.SourceType == RolloverSourceTypes.Ofqual &&
                candidate.SourceQualificationId == fundingChange.SourceQualificationId &&
                candidate.FundingOfferId == fundingChange.FundingOfferId &&
                academicYears.Contains(candidate.AcademicYear) &&
                candidate.IsActive)
            .ToListAsync(cancellationToken);
        var now = clock.UtcNow;

        foreach (var oldCandidate in oldCandidates)
        {
            if (targetCandidates.Any(target => target.AcademicYear == oldCandidate.AcademicYear))
            {
                oldCandidate.Deactivate(now);
            }
            else
            {
                oldCandidate.MoveSourceQualification(fundingChange.SourceQualificationId, now);
            }
        }

        await InvalidateWorkflowsAsync(
            context,
            oldCandidates.Select(candidate => candidate.Id).ToList(),
            SourceMovedReason,
            now,
            cancellationToken);
        return true;
    }

    private static async Task InvalidateWorkflowsAsync(
        ApplicationDbContext context,
        IReadOnlyCollection<Guid> candidateIds,
        string reason,
        DateTime invalidatedAt,
        CancellationToken cancellationToken)
    {
        if (candidateIds.Count == 0)
        {
            return;
        }

        var workflows = await context.RolloverWorkflowCandidates
            .Where(candidate =>
                candidateIds.Contains(candidate.RolloverCandidatesId) &&
                !candidate.InvalidatedAt.HasValue)
            .ToListAsync(cancellationToken);
        foreach (var workflow in workflows)
        {
            workflow.Invalidate(reason, invalidatedAt);
        }
    }

    private static async Task<IReadOnlyCollection<FundingKey>> ExpandEligibilityChangesAsync(
        ApplicationDbContext context,
        IReadOnlyCollection<QualificationFundingEligibilityChangedDomainEvent> events,
        CancellationToken cancellationToken)
    {
        var versionIds = events
            .Where(domainEvent => domainEvent.SourceType == RolloverSourceTypes.Ofqual)
            .Select(domainEvent => domainEvent.SourceQualificationId)
            .Distinct()
            .ToList();
        return await context.QualificationFundings
            .AsNoTracking()
            .Where(funding => versionIds.Contains(funding.QualificationVersionId))
            .Select(funding => new FundingKey(
                RolloverSourceTypes.Ofqual,
                funding.QualificationVersionId,
                funding.FundingOfferId))
            .ToListAsync(cancellationToken);
    }

    private AcademicYear GetCurrentAcademicYear()
    {
        var today = clock.Today;
        var startYear = today.Month >= 8 ? today.Year : today.Year - 1;
        return new AcademicYear(
            $"{startYear}/{(startYear + 1) % 100:00}",
            new DateOnly(startYear, 8, 1),
            new DateOnly(startYear + 1, 7, 31));
    }

    private static bool IsActiveForAcademicYear(DateOnly? endDate, AcademicYear academicYear) =>
        endDate is null ||
        endDate >= academicYear.StartDate && endDate <= academicYear.EndDate;

    private sealed record FundingKey(
        string SourceType,
        Guid SourceQualificationId,
        Guid FundingOfferId);

    private sealed record FundingEligibility(bool IsEligible, DateOnly? EndDate);
}
