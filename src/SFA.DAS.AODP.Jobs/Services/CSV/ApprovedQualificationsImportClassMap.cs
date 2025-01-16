using CsvHelper.Configuration;
using SFA.DAS.AODP.Data.Entities;

namespace SFA.DAS.AODP.Jobs.Services.CSV
{
    public class ApprovedQualificationsImportClassMap : ClassMap<ApprovedQualificationsImport>
    {
        public ApprovedQualificationsImportClassMap()
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
            Map(m => m.Age1416FundingAvailable).Name("Age1416_FundingAvailable");
            Map(m => m.Age1416FundingApprovalStartDate)
                .Name("Age1416_FundingApprovalStartDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.Age1416FundingApprovalEndDate)
                .Name("Age1416_FundingApprovalEndDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.Age1416Notes).Name("Age1416_Notes");
            Map(m => m.Age1619FundingAvailable).Name("Age1619_FundingAvailable");
            Map(m => m.Age1619FundingApprovalStartDate)
                .Name("Age1619_FundingApprovalStartDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.Age1619FundingApprovalEndDate)
                .Name("Age1619_FundingApprovalEndDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.Age1619Notes).Name("Age1619_Notes");
            Map(m => m.LocalFlexibilitiesFundingAvailable).Name("LocalFlexibilities_FundingAvailable");
            Map(m => m.LocalFlexibilitiesFundingApprovalStartDate)
                .Name("LocalFlexibilities_FundingApprovalStartDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.LocalFlexibilitiesFundingApprovalEndDate)
                .Name("LocalFlexibilities_FundingApprovalEndDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.LocalFlexibilitiesNotes).Name("LocalFlexibilities_Notes");
            Map(m => m.LegalEntitlementL2l3FundingAvailable).Name("LegalEntitlementL2L3_FundingAvailable");
            Map(m => m.LegalEntitlementL2l3FundingApprovalStartDate)
                .Name("LegalEntitlementL2L3_FundingApprovalStartDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.LegalEntitlementL2l3FundingApprovalEndDate)
                .Name("LegalEntitlementL2L3_FundingApprovalEndDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.LegalEntitlementL2l3Notes).Name("LegalEntitlementL2L3_Notes");
            Map(m => m.LegalEntitlementEnglishandMathsFundingAvailable).Name("LegalEntitlementEnglishandMaths_FundingAvailable");
            Map(m => m.LegalEntitlementEnglishandMathsFundingApprovalStartDate)
                .Name("LegalEntitlementEnglishandMaths_FundingApprovalStartDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.LegalEntitlementEnglishandMathsFundingApprovalEndDate)
                .Name("LegalEntitlementEnglishandMaths_FundingApprovalEndDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.LegalEntitlementEnglishandMathsNotes).Name("LegalEntitlementEnglishandMaths_Notes");
            Map(m => m.DigitalEntitlementFundingAvailable).Name("DigitalEntitlement_FundingAvailable");
            Map(m => m.DigitalEntitlementFundingApprovalStartDate)
                .Name("DigitalEntitlement_FundingApprovalStartDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.DigitalEntitlementFundingApprovalEndDate)
                .Name("DigitalEntitlement_FundingApprovalEndDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.DigitalEntitlementNotes).Name("DigitalEntitlement_Notes");
            Map(m => m.Esflevel34FundingAvailable).Name("ESFLevel34_FundingAvailable");
            Map(m => m.Esflevel34FundingApprovalStartDate)
                .Name("ESFLevel34_FundingApprovalStartDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.Esflevel34FundingApprovalEndDate)
                .Name("ESFLevel34_FundingApprovalEndDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.Esflevel34Notes).Name("ESFLevel34_Notes");
            Map(m => m.AdvancedLearnerLoansFundingAvailable).Name("AdvancedLearnerLoans_FundingAvailable");
            Map(m => m.AdvancedLearnerLoansFundingApprovalStartDate)
                .Name("AdvancedLearnerLoans_FundingApprovalStartDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.AdvancedLearnerLoansFundingApprovalEndDate)
                .Name("AdvancedLearnerLoans_FundingApprovalEndDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.AdvancedLearnerLoansNotes).Name("AdvancedLearnerLoans_Notes");
            Map(m => m.AwardingOrganisationUrl).Name("AwardingOrganisationURL");
            Map(m => m.L3freeCoursesForJobsFundingAvailable).Name("L3FreeCoursesForJobs_FundingAvailable");
            Map(m => m.L3freeCoursesForJobsFundingApprovalStartDate)
                .Name("L3FreeCoursesForJobs_FundingApprovalStartDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.L3freeCoursesForJobsFundingApprovalEndDate)
                .Name("L3FreeCoursesForJobs_FundingApprovalEndDate")
                .TypeConverterOption.Format("dd/MM/yyyy");
            Map(m => m.L3freeCoursesForJobsNotes).Name("L3FreeCoursesForJobs_Notes");
        }
    }
}
