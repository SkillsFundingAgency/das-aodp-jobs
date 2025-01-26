using CsvHelper.Configuration;
using SFA.DAS.AODP.Data.Entities;

namespace SFA.DAS.AODP.Jobs.Services.CSV
{
    public class FundedQualificationsImportClassMap : ClassMap<FundedQualification>
    {
        public FundedQualificationsImportClassMap(List<string> headers)
        {
            Map(m => m.DateOfOfqualDataSnapshot)
                .Name("DateOfOfqualDataSnapshot")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.QualificationName).Name("QualificationName");
            Map(m => m.AwardingOrganisation).Name("AwardingOrganisation");
            Map(m => m.QualificationNumber).Name("QualificationNumber");
            Map(m => m.Level).Name("Level");
            Map(m => m.QualificationType).Name("QualificationType");
            Map(m => m.Subcategory).Name("Subcategory");
            Map(m => m.SectorSubjectArea).Name("SectorSubjectArea");
            Map(m => m.Status).Name("Status");
            Map(m => m.AwardingOrganisationUrl).Name("AwardingOrganisationURL");
            Map(m => m.Offers).Convert(r =>
            {
                var offers = new List<FundedQualificationOffer>();
                foreach (var item in headers)
                {
                    var offerName = item.Split("_")[0];
                    offers.Add(new FundedQualificationOffer()
                    {
                        Name = offerName,
                        FundingAvailable = r.Row.GetField($"{offerName}_FundingAvailable"),
                        Notes = r.Row.GetField($"{offerName}_Notes"),
                        FundingApprovalEndDate = DateTime.TryParse(r.Row.GetField($"{offerName}_FundingApprovalEndDate"), out DateTime end) ? end : (DateTime?)null,
                        FundingApprovalStartDate = DateTime.TryParse(r.Row.GetField($"{offerName}_FundingApprovalStartDate"), out DateTime start) ? start : (DateTime?)null
                    });

           
                };
                return offers;

            });
        }
    }
}
