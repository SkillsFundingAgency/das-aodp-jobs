using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Models;

namespace SFA.DAS.AODP.Infrastructure.Extensions.Rollover;

public static class QualificationFundingQueryExtensions
{
    public static IQueryable<QualificationFunding> WhereActiveForAcademicYear(
        this IQueryable<QualificationFunding> query,
        AcademicYear academicYear)
    {
        return query.Where(funding => funding.EndDate == null || funding.EndDate >= academicYear.StartDate && funding.EndDate <= academicYear.EndDate);
    }
    public static IQueryable<QaaQualificationFunding> WhereActiveForAcademicYear(
        this IQueryable<QaaQualificationFunding> query,
        AcademicYear academicYear)
    {
        return query.Where(funding =>
            funding.EndDate == null ||
             funding.EndDate >= academicYear.StartDate &&
             funding.EndDate <= academicYear.EndDate);
    }
}
