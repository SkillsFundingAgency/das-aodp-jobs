namespace SFA.DAS.AODP.Infrastructure.Models;

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

    public static AcademicYear NextAcademicYear(AcademicYear academicYear)
    {
        var startDate = academicYear.StartDate.AddYears(1);
        var endDate = academicYear.EndDate.AddYears(1);
        var name = $"{startDate.Year}/{endDate.Year % 100:00}";

        return new AcademicYear(name, startDate, endDate);
    }
}
