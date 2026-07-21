using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities.Rollover;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces.Rollover;
using SFA.DAS.AODP.Infrastructure.Models;
using SFA.DAS.AODP.Infrastructure.Models.Rollover;
using SFA.DAS.Funding.ApprenticeshipEarnings.Domain.Services;

namespace SFA.DAS.AODP.Infrastructure.Repositories.Rollover;

public class RolloverCandidateRepository(IApplicationDbContext context, ISystemClockService systemClockService) : IRolloverCandidateRepository
{
    public async Task<int> CreateInitialRolloverCandidatesAsync(
        AcademicYear academicYear,
        CancellationToken cancellationToken)
    {
        var candidateFundingStreams = await RolloverCandidateQueryBuilder
            .From(
                context.QualificationVersions.AsNoTracking(),
                context.QualificationFundings.AsNoTracking())
            .WhereEligibleForFunding()
            .WithActiveFundingStreamsForAcademicYear(academicYear)
            .Build()
            .ToListAsync(cancellationToken: cancellationToken);

        if (candidateFundingStreams.Count == 0)
        {
            return 0;
        }

        var qualificationVersionIds = candidateFundingStreams
            .Select(candidate => candidate.QualificationVersionId)
            .Distinct()
            .ToList();

        var fundingOfferIds = candidateFundingStreams
            .Select(candidate => candidate.FundingOfferId)
            .Distinct()
            .ToList();

        var existingCandidateKeys = await context.RolloverCandidates
            .AsNoTracking()
            .Where(candidate =>
                candidate.AcademicYear == academicYear.Name &&
                candidate.RolloverRound == 1 &&
                qualificationVersionIds.Contains(candidate.QualificationVersionId) &&
                fundingOfferIds.Contains(candidate.FundingOfferId))
            .Select(candidate => new
            {
                candidate.QualificationVersionId,
                candidate.FundingOfferId
            })
            .ToListAsync(cancellationToken);

        var newCandidates = candidateFundingStreams
            .Where(candidate => !existingCandidateKeys.Any(existing =>
                existing.QualificationVersionId == candidate.QualificationVersionId &&
                existing.FundingOfferId == candidate.FundingOfferId))
            .Select(candidate => RolloverCandidate.CreateInitialRound(
                candidate.QualificationVersionId,
                candidate.FundingOfferId,
                academicYear.Name,
                systemClockService.UtcNow,
                candidate.EndDate))
            .ToList();

        if (newCandidates.Count == 0)
        {
            return 0;
        }

        context.RolloverCandidates.AddRange(newCandidates);
        await context.SaveChangesAsync(cancellationToken);

        return newCandidates.Count;
    }
}