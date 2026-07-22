using CsvHelper.Configuration;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services.CSV;

[ExcludeFromCodeCoverage(Justification = "This is temporary code")]
public class QaaFundedQualificationsImportClassMap : ClassMap<FundedQualificationDTO>
{
    private readonly Dictionary<string, (Guid? qualificationId, Guid? organisationId)> _qualificationLookupCache;
    private readonly ILogger _logger;
    private readonly DateTime _minDate = new DateTime(1753, 1, 1);

    public QaaFundedQualificationsImportClassMap(
        List<string> headers,
        List<QualificationVersionRepository.QualificationLookupItem> qualificationLookupItems,
        ILogger logger)
    {
        _logger = logger;
        _qualificationLookupCache = qualificationLookupItems.ToDictionary(q => q.Qan, q => (q.QualificationId, q.AwardingOrganisationId));

        Map(m => m.Id).Convert(row => {
            return Guid.NewGuid();
        });

        Map(m => m.DateOfOfqualDataSnapshot)
            .Name("DateOfOfqualDataSnapshot")
            .TypeConverterOption.Format("dd/MM/yyyy");

        Map(m => m.Qan).Name("QualificationNumber");
        Map(m => m.QualificationName).Name("QualificationName");
        Map(m => m.QualificationId).Convert(row =>
        {
            var qan = row.Row.GetField<string>("QualificationNumber");

            if (string.IsNullOrWhiteSpace(qan))
            {
                _logger.LogWarning("Empty qualification number found in CSV data");
                return default;
            }

            return _qualificationLookupCache.TryGetValue(qan, out var value)
                ? value.qualificationId
                : null;
        });

        Map(m => m.AwardingOrganisationId).Convert(row =>
        {
            var qan = row.Row.GetField<string>("QualificationNumber");

            if (string.IsNullOrWhiteSpace(qan))
            {
                _logger.LogWarning("Empty qualification number found in CSV data");
                return default;
            }

            return _qualificationLookupCache.TryGetValue(qan, out var value)
                ? value.organisationId
                : null;
        });

        Map(m => m.Level).Name("Level");
        Map(m => m.QualificationType).Name("QualificationType");
        Map(m => m.Subcategory).Name("Subcategory");
        Map(m => m.SectorSubjectArea).Name("SectorSubjectArea");
        Map(m => m.Status).Name("Status");
        Map(m => m.AwardingOrganisationName).Name("AwardingOrganisation");
        Map(m => m.AwardingOrganisationURL).Name("AwardingOrganisationURL");

        Map(m => m.Offers).Convert(row =>
        {
            var offers = new List<FundedQualificationOfferDTO>();

            var qan = row.Row.GetField<string>("QualificationNumber");

            if (string.IsNullOrWhiteSpace(qan))
            {
                _logger.LogWarning("Empty qualification number found in CSV data for offers");
                return offers;
            }
            foreach (var item in headers)
            {
                var offerName = item.Split('_')[0];

                var endKey = $"{offerName}_FundingApprovalEndDate";
                var startKey = $"{offerName}_FundingApprovalStartDate";
                var notesKey = $"{offerName}_Notes";
                var fundingKey = $"{offerName}_FundingAvailable";

                var endDateRaw = row.Row.GetField(endKey);
                var startDateRaw = row.Row.GetField(startKey);

                DateTime? endDate =
                    DateTime.TryParse(endDateRaw, out var e) && e >= _minDate
                        ? e
                        : null;

                DateTime? startDate =
                    DateTime.TryParse(startDateRaw, out var s) && s >= _minDate
                        ? s
                        : null;

                offers.Add(new FundedQualificationOfferDTO
                {
                    Id = Guid.NewGuid(),
                    Name = offerName,
                    Notes = row.Row.GetField(notesKey),
                    FundingAvailable = row.Row.GetField(fundingKey),
                    FundingApprovalEndDate = endDate,
                    FundingApprovalStartDate = startDate,
                });
            }

            return offers;
        });

    }
}