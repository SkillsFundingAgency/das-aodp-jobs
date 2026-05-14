using CsvHelper;
using SFA.DAS.AODP.Jobs.Models;
using SFA.DAS.AODP.Jobs.Services.CSV;
using System.Globalization;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services;

public class QaaSeedCsvRecordClassMapTests
{
    [Fact]
    public void CsvMap_ReadsQaaSeedRecord_AndParsesFullDates()
    {
        var csvContent = string.Join(
            Environment.NewLine,
            "AIM code,Awarding body,Diploma Title,SSA Tier 1,SSA Tier 2,Start date of qualification,Full start date of qualification,Last date for registration,Full Last date for registration,Last date for certification,Full Last date for certification,Award status,Discontinued date",
            "40001234,Test AVA,Access to HE Diploma,Science,Medicine,09/01,01/09/2024,08/31,31/08/2026,08/31,31/08/2027,Active,15/05/2026");

        using var stringReader = new StringReader(csvContent);
        using var csvReader = new CsvReader(stringReader, CultureInfo.InvariantCulture);
        csvReader.Context.RegisterClassMap<QaaSeedCsvRecordClassMap>();

        var result = csvReader.GetRecords<QaaSeedCsvRecord>().Single();

        Assert.Equal("40001234", result.AimCode);
        Assert.Equal("Test AVA", result.AwardingBody);
        Assert.Equal("Access to HE Diploma", result.DiplomaTitle);
        Assert.Equal("Science", result.SsaTier1);
        Assert.Equal("Medicine", result.SsaTier2);
        Assert.Equal("09/01", result.StartDateOfQualification);
        Assert.Equal(new DateOnly(2024, 9, 1), result.FullStartDateOfQualification);
        Assert.Equal("08/31", result.LastDateForRegistration);
        Assert.Equal(new DateOnly(2026, 8, 31), result.FullLastDateForRegistration);
        Assert.Equal("08/31", result.LastDateForCertification);
        Assert.Equal(new DateOnly(2027, 8, 31), result.FullLastDateForCertification);
        Assert.Equal("Active", result.AwardStatus);
        Assert.Equal(new DateOnly(2026, 5, 15), result.DiscontinuedDate);
    }

    [Fact]
    public void CsvMap_AllowsBlankDiscontinuedDate()
    {
        var csvContent = string.Join(
            Environment.NewLine,
            "AIM code,Awarding body,Diploma Title,SSA Tier 1,SSA Tier 2,Start date of qualification,Full start date of qualification,Last date for registration,Full Last date for registration,Last date for certification,Full Last date for certification,Award status,Discontinued date",
            "40001234,Test AVA,Access to HE Diploma,Science,Medicine,09/01,01/09/2024,08/31,31/08/2026,08/31,31/08/2027,Active,");

        using var stringReader = new StringReader(csvContent);
        using var csvReader = new CsvReader(stringReader, CultureInfo.InvariantCulture);
        csvReader.Context.RegisterClassMap<QaaSeedCsvRecordClassMap>();

        var result = csvReader.GetRecords<QaaSeedCsvRecord>().Single();

        Assert.Null(result.DiscontinuedDate);
    }
}
