using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Extensions.Rollover;

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

    public RolloverCandidateQueryBuilder WithActiveFundingStreamsForAcademicYear(DateOnly academicYearEndDate)
    {
        _fundingStreams = _fundingStreams.WhereActiveForAcademicYear(academicYearEndDate);
        return this;
    }

    public IQueryable<RolloverCandidateFundingStream> Build()
    {
        return _fundingStreams
            .Join(
                _qualificationVersions,
                funding => funding.QualificationVersionId,
                qualificationVersion => qualificationVersion.Id,
                (funding, qualificationVersion) => new RolloverCandidateFundingStream
                {
                    QualificationVersionId = qualificationVersion.Id,
                    FundingOfferId = funding.FundingOfferId,
                    EndDate = funding.EndDate
                })
            .Distinct();
    }
}
