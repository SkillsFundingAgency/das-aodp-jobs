using SFA.DAS.AODP.Data.Entities;

namespace SFA.DAS.AODP.Infrastructure.Extensions.Rollover;

public static class QualificationFundingQueryExtensions
{
    public static IQueryable<QualificationFunding> WhereActiveForAcademicYear(
        this IQueryable<QualificationFunding> query,
        DateOnly academicYearEndDate)
    {
        return query.Where(funding => funding.EndDate == null || funding.EndDate == academicYearEndDate);
    }
}
