using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Extensions.Rollover;
using SFA.DAS.AODP.Data.Entities.Rollover;

namespace SFA.DAS.AODP.Infrastructure.Models.Rollover;

public class RolloverCandidateQueryBuilder
{
    private readonly IQueryable<QualificationVersions> _allQualificationVersions;
    private IQueryable<QualificationVersions> _qualificationVersions;
    private IQueryable<QualificationFunding> _fundingStreams;

    private RolloverCandidateQueryBuilder(
        IQueryable<QualificationVersions> qualificationVersions,
        IQueryable<QualificationFunding> fundingStreams)
    {
        _allQualificationVersions = qualificationVersions;
        _qualificationVersions = qualificationVersions;
        _fundingStreams = fundingStreams;
    }

    public static RolloverCandidateQueryBuilder From(
        IQueryable<QualificationVersions> qualificationVersions,
        IQueryable<QualificationFunding> fundingStreams)
    {
        return new RolloverCandidateQueryBuilder(qualificationVersions, fundingStreams);
    }

    public RolloverCandidateQueryBuilder WithLatestQualificationVersions()
    {
        _qualificationVersions = _qualificationVersions.WhereLatestVersionPerQualification(_allQualificationVersions);
        return this;
    }

    public RolloverCandidateQueryBuilder WhereEligibleForFunding()
    {
        _qualificationVersions = _qualificationVersions.WhereEligibleForFunding();
        return this;
    }

    public RolloverCandidateQueryBuilder WithActiveFundingStreamsForAcademicYear(AcademicYear academicYear)
    {
        _fundingStreams = _fundingStreams.WhereActiveForAcademicYear(academicYear);
        return this;
    }

    public IQueryable<RolloverCandidateFundingStream> Build()
    {
        var eligibleQualificationVersionIds = _qualificationVersions
            .Select(version => version.Id);

        return _fundingStreams
            .Where(funding => eligibleQualificationVersionIds.Contains(
                funding.QualificationVersionId))
            .Select(o => new RolloverCandidateFundingStream
            {
                EndDate = o.EndDate,
                FundingOfferId = o.FundingOfferId,
                SourceType = RolloverSourceTypes.Ofqual,
                SourceQualificationId = o.QualificationVersionId
            })
            .Distinct();
    }
}
