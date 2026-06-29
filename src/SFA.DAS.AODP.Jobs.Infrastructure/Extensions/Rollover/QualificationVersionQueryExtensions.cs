using SFA.DAS.AODP.Data.Entities;

namespace SFA.DAS.AODP.Infrastructure.Extensions.Rollover;

public static class QualificationVersionQueryExtensions
{
    public static IQueryable<QualificationVersions> WhereEligibleForFunding(this IQueryable<QualificationVersions> query)
    {
        return query.Where(qualificationVersion => qualificationVersion.EligibleForFunding == true);
    }

    public static IQueryable<QualificationVersions> WhereLatestVersionPerQualification(
        this IQueryable<QualificationVersions> query,
        IQueryable<QualificationVersions> allQualificationVersions)
    {
        return query.Where(qualificationVersion => !allQualificationVersions.Any(otherVersion =>
            otherVersion.QualificationId == qualificationVersion.QualificationId &&
            (
                (otherVersion.Version ?? 0) > (qualificationVersion.Version ?? 0) ||
                (
                    (otherVersion.Version ?? 0) == (qualificationVersion.Version ?? 0) &&
                    otherVersion.LastUpdatedDate > qualificationVersion.LastUpdatedDate
                ) ||
                (
                    (otherVersion.Version ?? 0) == (qualificationVersion.Version ?? 0) &&
                    otherVersion.LastUpdatedDate == qualificationVersion.LastUpdatedDate &&
                    otherVersion.InsertedDate > qualificationVersion.InsertedDate
                )
            )));
    }
}
