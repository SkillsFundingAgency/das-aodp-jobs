using SFA.DAS.AODP.Jobs.Models.Rollover;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services.Rollover;

public class AcademicYearTests
{
    [Theory]
    [InlineData(2026, 6, 28, "2025/26", 2025, 8, 1, 2026, 7, 31)]
    [InlineData(2026, 8, 1, "2026/27", 2026, 8, 1, 2027, 7, 31)]
    public void FromDate_ReturnsAcademicYearForDate(
        int year,
        int month,
        int day,
        string expectedName,
        int expectedStartYear,
        int expectedStartMonth,
        int expectedStartDay,
        int expectedEndYear,
        int expectedEndMonth,
        int expectedEndDay)
    {
        // Arrange
        var date = new DateTime(year, month, day);

        // Act
        var result = AcademicYear.FromDate(date);

        // Assert
        result.Name.ShouldBe(expectedName);
        result.StartDate.ShouldBe(new DateOnly(expectedStartYear, expectedStartMonth, expectedStartDay));
        result.EndDate.ShouldBe(new DateOnly(expectedEndYear, expectedEndMonth, expectedEndDay));
    }
}
