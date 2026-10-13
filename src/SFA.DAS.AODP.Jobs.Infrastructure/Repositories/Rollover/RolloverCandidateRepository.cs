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
    private const string FundingMovedToNewVersionReason =
        "The source funding moved to a newer qualification version.";

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
        var invalidatedCandidateReasons = await MoveOfqualCandidatesToFundedVersionsAsync(
            ofqualCandidateFundingStreams,
            existingCandidates,
            now,
            cancellationToken);
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
                invalidatedCandidateReasons.TryAdd(
                    candidate.Id,
                    FundingNoLongerApplicableReason);
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

        if (invalidatedCandidateReasons.Count > 0)
        {
            var invalidatedCandidateIds = invalidatedCandidateReasons.Keys.ToList();
            var workflowCandidates = await context.RolloverWorkflowCandidates
                .Where(candidate =>
                    invalidatedCandidateIds.Contains(candidate.RolloverCandidatesId) &&
                    !candidate.InvalidatedAt.HasValue)
                .ToListAsync(cancellationToken);

            foreach (var workflowCandidate in workflowCandidates)
            {
                workflowCandidate.Invalidate(
                    invalidatedCandidateReasons[workflowCandidate.RolloverCandidatesId],
                    now);
            }
        }

        await context.SaveChangesAsync(cancellationToken);

        return created;
    }

    private async Task<Dictionary<Guid, string>> MoveOfqualCandidatesToFundedVersionsAsync(
        IReadOnlyCollection<RolloverCandidateFundingStream> ofqualFundingStreams,
        IReadOnlyCollection<RolloverCandidate> existingCandidates,
        DateTime updatedAt,
        CancellationToken cancellationToken)
    {
        var activeOfqualCandidates = existingCandidates
            .Where(candidate =>
                candidate.SourceType == RolloverSourceTypes.Ofqual &&
                candidate.IsActive)
            .ToList();
        if (activeOfqualCandidates.Count == 0 || ofqualFundingStreams.Count == 0)
        {
            return [];
        }

        var versionIds = activeOfqualCandidates
            .Select(candidate => candidate.SourceQualificationId)
            .Concat(ofqualFundingStreams.Select(stream => stream.SourceQualificationId))
            .Distinct()
            .ToList();
        var qualificationIdsByVersion = await context.QualificationVersions
            .AsNoTracking()
            .Where(version => versionIds.Contains(version.Id))
            .ToDictionaryAsync(
                version => version.Id,
                version => version.QualificationId,
                cancellationToken);
        var targetStreams = ofqualFundingStreams
            .Where(stream => qualificationIdsByVersion.ContainsKey(stream.SourceQualificationId))
            .ToDictionary(
                stream => (
                    qualificationIdsByVersion[stream.SourceQualificationId],
                    stream.FundingOfferId));
        var invalidatedCandidateReasons = new Dictionary<Guid, string>();

        foreach (var candidateGroup in activeOfqualCandidates
                     .Where(candidate => qualificationIdsByVersion.ContainsKey(
                         candidate.SourceQualificationId))
                     .GroupBy(candidate => (
                         qualificationIdsByVersion[candidate.SourceQualificationId],
                         candidate.FundingOfferId)))
        {
            if (!targetStreams.TryGetValue(candidateGroup.Key, out var targetStream))
            {
                continue;
            }

            var targetCandidateExists = candidateGroup.Any(candidate =>
                candidate.SourceQualificationId == targetStream.SourceQualificationId);
            if (targetCandidateExists)
            {
                foreach (var staleCandidate in candidateGroup.Where(candidate =>
                             candidate.SourceQualificationId != targetStream.SourceQualificationId))
                {
                    staleCandidate.Deactivate(updatedAt);
                    invalidatedCandidateReasons.TryAdd(
                        staleCandidate.Id,
                        FundingMovedToNewVersionReason);
                }

                continue;
            }

            var candidateToMove = candidateGroup
                .OrderByDescending(candidate => candidate.RolloverRound)
                .First();
            candidateToMove.MoveSourceQualification(
                targetStream.SourceQualificationId,
                updatedAt);
            invalidatedCandidateReasons.Add(
                candidateToMove.Id,
                FundingMovedToNewVersionReason);
        }

        return invalidatedCandidateReasons;
    }
}
