using CsvHelper.Configuration;
using SFA.DAS.AODP.Jobs.Models;
using System.Globalization;

namespace SFA.DAS.AODP.Jobs.Services.CSV;

[ExcludeFromCodeCoverage(Justification = "This is temporary code")]
public class QaaSeedCsvRecordClassMap : ClassMap<QaaSeedCsvRecord>
{
    private const string FullDateFormat = "dd/MM/yyyy";

    public QaaSeedCsvRecordClassMap()
    {
        Map(m => m.AimCode).Name("AIM code");
        Map(m => m.AwardingBody).Name("Awarding Body");
        Map(m => m.DiplomaTitle).Name("Diploma Title");
        Map(m => m.SsaTier1).Name("SSA Tier 1");
        Map(m => m.SsaTier2).Name("SSA Tier 2");
        //Map(m => m.StartDateOfQualification).Name("Start date of qualification");
        Map(m => m.FullStartDateOfQualification)
            .Name("Full start date of qualification")
            .Convert(args => ParseRequiredDate(args.Row.GetField("Full start date of qualification")));
        //Map(m => m.LastDateForRegistration).Name("Last date for registrations");
        Map(m => m.FullLastDateForRegistration)
            .Name("Full Last date for registrations")
            .Convert(args => ParseRequiredDate(args.Row.GetField("Full Last date for registrations")));
        //Map(m => m.LastDateForCertification).Name("Last date for certifications");
        Map(m => m.FullLastDateForCertification)
            .Name("Full last date for certifications")
            .Convert(args => ParseRequiredDate(args.Row.GetField("Full last date for certifications")));
        Map(m => m.AwardStatus).Name("Award Status");
        Map(m => m.DiscontinuedDate)
            .Name("Discontinued date")
            .Convert(args => ParseOptionalDate(args.Row.GetField("Discontinued date")));
    }

    private static DateOnly ParseRequiredDate(string? value)
    {
        return DateOnly.ParseExact(value!, FullDateFormat, CultureInfo.InvariantCulture);
    }

    private static DateOnly? ParseOptionalDate(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : DateOnly.ParseExact(value, FullDateFormat, CultureInfo.InvariantCulture);
    }
}
