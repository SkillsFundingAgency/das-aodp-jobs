using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities.Rollover;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces.Rollover;
using SFA.DAS.AODP.Infrastructure.Models.Rollover;

namespace SFA.DAS.AODP.Infrastructure.Repositories.Rollover;

public class RolloverCandidateRepository : IRolloverCandidateRepository
{
    private readonly IApplicationDbContext _context;

    public RolloverCandidateRepository(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> CreateInitialRolloverCandidatesAsync(
        string academicYear,
        DateOnly academicYearEndDate,
        DateTime createdAt,
        CancellationToken cancellationToken = default)
    {
        var candidateFundingStreams = await RolloverCandidateQueryBuilder
            .From(
                _context.QualificationVersions.AsNoTracking(),
                _context.QualificationFundings.AsNoTracking())
            .WithLatestQualificationVersions()
            .WhereEligibleForFunding()
            .WithActiveFundingStreamsForAcademicYear(academicYearEndDate)
            .Build()
            .ToListAsync(cancellationToken);

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

        var existingCandidateKeys = await _context.RolloverCandidates
            .AsNoTracking()
            .Where(candidate =>
                candidate.AcademicYear == academicYear &&
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
                academicYear,
                createdAt,
                candidate.EndDate))
            .ToList();

        if (newCandidates.Count == 0)
        {
            return 0;
        }

        _context.RolloverCandidates.AddRange(newCandidates);
        await _context.SaveChangesAsync(cancellationToken);

        return newCandidates.Count;
    }
}
