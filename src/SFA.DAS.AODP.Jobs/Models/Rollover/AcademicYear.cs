namespace SFA.DAS.AODP.Jobs.Models.Rollover;

public sealed record AcademicYear(string Name, DateOnly StartDate, DateOnly EndDate)
{
    public static AcademicYear FromDate(DateTime date)
    {
        var startYear = date.Month >= 8 ? date.Year : date.Year - 1;
        var endYear = startYear + 1;

        return new AcademicYear(
            $"{startYear}/{endYear % 100:00}",
            new DateOnly(startYear, 8, 1),
            new DateOnly(endYear, 7, 31));
    }
}
