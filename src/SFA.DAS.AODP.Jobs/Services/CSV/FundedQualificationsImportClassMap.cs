using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services.CSV
{
    public class FundedQualificationsImportClassMap : ClassMap<FundedQualificationDTO>
    {
        private readonly Dictionary<string, Guid> _qualificationNumberToIdCache;
        private readonly Dictionary<string, Guid> _organsationNameToIdCache;
        private readonly ILogger<FundedQualificationsImportClassMap> _logger;
        private Guid _currentQualificationId;

        public FundedQualificationsImportClassMap(
            List<string> headers,
            List<Qualification> qualifications,
            List<AwardingOrganisation> organisations,
            ILogger<FundedQualificationsImportClassMap> logger)         
        {
            _logger = logger;
            _qualificationNumberToIdCache = qualifications.ToDictionary(q => q.Qan, q => q.Id);
            _organsationNameToIdCache = organisations.ToDictionary(q => q.NameOfqual, q => q.Id);

            Map(m => m.Id).Convert(row => {
                return Guid.NewGuid();
            });

            Map(m => m.DateOfOfqualDataSnapshot)
                .Name("DateOfOfqualDataSnapshot")
                .TypeConverterOption.Format("dd/MM/yyyy");

            // Map QualificationId by looking up qualification number in the cache
            Map(m => m.QualificationId).Convert(row => {
                var qualificationNumber = row.Row.GetField<string>("QualificationNumber");

                if (string.IsNullOrEmpty(qualificationNumber))
                {
                    _logger.LogWarning("Empty qualification number found in CSV data");
                    _currentQualificationId = default;
                    return default;
                }

                if (_qualificationNumberToIdCache.TryGetValue(qualificationNumber, out Guid qualificationId))
                {
                    _currentQualificationId = qualificationId;
                    return _currentQualificationId;
                }
                else
                {
                    _logger.LogWarning($"No matching qualification found for qan: '{qualificationNumber}'");
                    _currentQualificationId = default;
                    return default;
                }
            });

            // Map AwardingOrganisationId by looking up the org name in the cache
            Map(m => m.AwardingOrganisationId).Convert(row => {
                var awardingOrganisationName = row.Row.GetField<string>("AwardingOrganisation");

                if (string.IsNullOrEmpty(awardingOrganisationName))
                {
                    _logger.LogWarning("Empty awarding organistion name found in CSV data");
                    return default;
                }

                if (_organsationNameToIdCache.TryGetValue(awardingOrganisationName, out Guid organisationId))
                {
                    return organisationId;
                }
                else
                {
                    _logger.LogWarning($"No matching awarding organistion found for name: '{awardingOrganisationName}'");
                    return default;
                }
            });

            Map(m => m.Level).Name("Level");
            Map(m => m.QualificationType).Name("QualificationType");
            Map(m => m.Subcategory).Name("Subcategory");
            Map(m => m.SectorSubjectArea).Name("SectorSubjectArea");
            Map(m => m.Status).Name("Status");
            Map(m => m.AwardingOrganisationURL).Name("AwardingOrganisationURL");
            Map(m => m.Offers).Convert(r =>
            {
                var offers = new List<FundedQualificationOfferDTO>();

                foreach (var item in headers)
                {
                    var offerName = item.Split("_")[0];

                    DateTime? endDate = null;
                    if (DateTime.TryParse(r.Row.GetField($"{offerName}_FundingApprovalEndDate"), out DateTime parsedEnd) && parsedEnd >= new DateTime(1753, 1, 1))
                    {
                        endDate = parsedEnd;
                    }

                    DateTime? startDate = null;
                    if (DateTime.TryParse(r.Row.GetField($"{offerName}_FundingApprovalStartDate"), out DateTime parsedStart) && parsedStart >= new DateTime(1753, 1, 1))
                    {
                        startDate = parsedStart;
                    }

                    offers.Add(new FundedQualificationOfferDTO()
                    {
                        Id = Guid.NewGuid(),
                        QualificationId = _currentQualificationId,
                        Name = offerName,
                        Notes = r.Row.GetField($"{offerName}_Notes"),
                        FundingAvailable = r.Row.GetField($"{offerName}_FundingAvailable"),
                        FundingApprovalEndDate = endDate,
                        FundingApprovalStartDate = startDate,
                    });
                };
                return offers;
            });

        }
    }
}