using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities.Rollover;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Extensions.Rollover;
using SFA.DAS.AODP.Infrastructure.Interfaces.Rollover;
using SFA.DAS.AODP.Infrastructure.Models;
using SFA.DAS.AODP.Infrastructure.Models.Rollover;
using SFA.DAS.AODP.Infrastructure.Services;

namespace SFA.DAS.AODP.Infrastructure.Repositories.Rollover;

public class RolloverCandidateRepository(IApplicationDbContext context, ISystemClockService systemClockService) : IRolloverCandidateRepository
{
    private const string FundingNoLongerApplicableReason =
        "The source funding is no longer applicable for rollover.";

    public async Task<int> CreateInitialRolloverCandidatesAsync(
        AcademicYear academicYear,
        CancellationToken cancellationToken)
    {
        var ofqualCandidateFundingStreams = await RolloverCandidateQueryBuilder
            .From(
                context.QualificationVersions.AsNoTracking(),
                context.QualificationFundings.AsNoTracking())
            .WithLatestQualificationVersions()
            .WhereEligibleForFunding()
            .WithActiveFundingStreamsForAcademicYear(academicYear)
            .Build()
            .ToListAsync(cancellationToken: cancellationToken);

        var qaaCandidateFundingStreams = await context.QaaQualificationFundings
            .AsNoTracking()
            .WhereActiveForAcademicYear(academicYear)
            .Select(funding => new RolloverCandidateFundingStream
            {
                SourceType = RolloverSourceTypes.Qaa,
                SourceQualificationId = funding.QaaQualificationId,
                FundingOfferId = funding.FundingOfferId,
                EndDate = funding.EndDate
            })
            .ToListAsync(cancellationToken);

        var candidateFundingStreams = ofqualCandidateFundingStreams
            .Concat(qaaCandidateFundingStreams)
            .DistinctBy(candidate => new
            {
                candidate.SourceType,
                candidate.SourceQualificationId,
                candidate.FundingOfferId
            })
            .ToList();

        var existingCandidates = await context.RolloverCandidates
            .Where(candidate =>
                candidate.AcademicYear == academicYear.Name &&
                (candidate.SourceType == RolloverSourceTypes.Ofqual ||
                 candidate.SourceType == RolloverSourceTypes.Qaa))
            .ToListAsync(cancellationToken);

        var now = systemClockService.UtcNow;
        var deactivatedCandidateIds = new List<Guid>();
        var eligibleKeys = candidateFundingStreams
            .Select(funding => (
                funding.SourceType,
                funding.SourceQualificationId,
                funding.FundingOfferId))
            .ToHashSet();
        var candidatesByFundingKey = existingCandidates.ToLookup(candidate => (
            candidate.SourceType,
            candidate.SourceQualificationId,
            candidate.FundingOfferId));

        foreach (var candidate in existingCandidates.Where(candidate => candidate.IsActive))
        {
            var remainsEligible = eligibleKeys.Contains((
                candidate.SourceType,
                candidate.SourceQualificationId,
                candidate.FundingOfferId));

            if (!remainsEligible)
            {
                candidate.Deactivate(now);
                deactivatedCandidateIds.Add(candidate.Id);
            }
        }

        var created = 0;
        foreach (var funding in candidateFundingStreams)
        {
            var matchingCandidates = candidatesByFundingKey[(
                    funding.SourceType,
                    funding.SourceQualificationId,
                    funding.FundingOfferId)]
                .OrderByDescending(candidate => candidate.RolloverRound)
                .ToList();

            var activeCandidate = matchingCandidates.FirstOrDefault(candidate => candidate.IsActive);
            if (activeCandidate is not null)
            {
                activeCandidate.RefreshFunding(funding.EndDate, now);
                continue;
            }

            var inactiveCandidate = matchingCandidates.FirstOrDefault();
            if (inactiveCandidate is not null)
            {
                inactiveCandidate.Reactivate(funding.EndDate, now);
                continue;
            }

            var newCandidate = RolloverCandidate.CreateInitialRound(
                funding.SourceType,
                funding.SourceQualificationId,
                funding.FundingOfferId,
                academicYear.Name,
                now,
                funding.EndDate);
            context.RolloverCandidates.Add(newCandidate);
            created++;
        }

        if (deactivatedCandidateIds.Count > 0)
        {
            var workflowCandidates = await context.RolloverWorkflowCandidates
                .Where(candidate =>
                    deactivatedCandidateIds.Contains(candidate.RolloverCandidatesId) &&
                    !candidate.InvalidatedAt.HasValue)
                .ToListAsync(cancellationToken);

            foreach (var workflowCandidate in workflowCandidates)
            {
                workflowCandidate.Invalidate(FundingNoLongerApplicableReason, now);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return created;
    }
}
