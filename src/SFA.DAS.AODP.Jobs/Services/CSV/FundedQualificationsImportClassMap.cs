using CsvHelper.Configuration;
using SFA.DAS.AODP.Models.Qualification;
using static SFA.DAS.AODP.Infrastructure.Repositories.QualificationVersionRepository;

namespace SFA.DAS.AODP.Jobs.Services.CSV
{
    public class FundedQualificationsImportClassMap : ClassMap<FundedQualificationDTO>
    {
        private readonly Dictionary<string, (Guid? qualificationId, Guid? organisationId)> _qualificationLookupCache;
        private readonly ILogger _logger;
        private readonly DateTime _minDate = new DateTime(1753, 1, 1);

        public FundedQualificationsImportClassMap(
            List<string> headers,
            List<QualificationLookupItem> qualificationLookupItems,
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

                // Qualifications not yet known to the Ofqual register (e.g. QAA/Access to Higher
                // Education qualifications, which are never part of the Ofqual register import) won't
                // be in this cache. Their funding offers are still needed downstream for QAA funding
                // matching, so QualificationId falls back to Guid.Empty rather than dropping the offers.
                _qualificationLookupCache.TryGetValue(qan, out var lookup);
                var qualificationId = lookup.qualificationId ?? Guid.Empty;

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
                        QualificationId = qualificationId,
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
}