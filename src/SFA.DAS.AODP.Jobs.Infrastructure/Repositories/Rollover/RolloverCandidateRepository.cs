using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities.Rollover;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Extensions.Rollover;
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
        var ofqualCandidateFundingStreams = await RolloverCandidateQueryBuilder
            .From(
                context.QualificationVersions.AsNoTracking(),
                context.QualificationFundings.AsNoTracking())
            .WithLatestQualificationVersions()
            .WhereEligibleForFunding()
            .WithActiveFundingStreamsForAcademicYear(academicYear)
            .Build()
            .ToListAsync(cancellationToken);

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

        if (candidateFundingStreams.Count == 0)
        {
            return 0;
        }

        var sourceQualificationIds = candidateFundingStreams
            .Select(candidate => candidate.SourceQualificationId)
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
                sourceQualificationIds.Contains(candidate.SourceQualificationId) &&
                fundingOfferIds.Contains(candidate.FundingOfferId))
            .Select(candidate => new
            {
                candidate.SourceType,
                candidate.SourceQualificationId,
                candidate.FundingOfferId
            })
            .ToListAsync(cancellationToken);

        var newCandidates = candidateFundingStreams
            .Where(candidate => !existingCandidateKeys.Any(existing =>
                existing.SourceType == candidate.SourceType &&
                existing.SourceQualificationId == candidate.SourceQualificationId &&
                existing.FundingOfferId == candidate.FundingOfferId))
            .Select(candidate => RolloverCandidate.CreateInitialRound(
                candidate.SourceType,
                candidate.SourceQualificationId,
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
