using System.ComponentModel.DataAnnotations;
using CsvHelper.Configuration;

namespace SFA.DAS.AODP.Data.Entities
{
    public partial class ApprovedQualificationsImport
    {
        public int Id { get; set; }
        public DateTime? DateOfOfqualDataSnapshot { get; set; }
        public string? QualificationName { get; set; }
        public string? AwardingOrganisation { get; set; }
        public string? QualificationNumber { get; set; }
        public string? Level { get; set; }
        public string? QualificationType { get; set; }
        public string? Subcategory { get; set; }
        public string? SectorSubjectArea { get; set; }
        public string? Status { get; set; }
        public bool Age1416FundingAvailable { get; set; }
        public DateTime? Age1416FundingApprovalStartDate { get; set; }
        public DateTime? Age1416FundingApprovalEndDate { get; set; }
        public string? Age1416Notes { get; set; }
        public bool Age1619FundingAvailable { get; set; }
        public DateTime? Age1619FundingApprovalStartDate { get; set; }
        public DateTime? Age1619FundingApprovalEndDate { get; set; }
        public string? Age1619Notes { get; set; }
        public bool LocalFlexibilitiesFundingAvailable { get; set; }
        public DateTime? LocalFlexibilitiesFundingApprovalStartDate { get; set; }
        public DateTime? LocalFlexibilitiesFundingApprovalEndDate { get; set; }
        public string? LocalFlexibilitiesNotes { get; set; }
        public bool LegalEntitlementL2l3FundingAvailable { get; set; }
        public DateTime? LegalEntitlementL2l3FundingApprovalStartDate { get; set; }
        public DateTime? LegalEntitlementL2l3FundingApprovalEndDate { get; set; }
        public string? LegalEntitlementL2l3Notes { get; set; }
        public bool LegalEntitlementEnglishandMathsFundingAvailable { get; set; }
        public DateTime? LegalEntitlementEnglishandMathsFundingApprovalStartDate { get; set; }
        public DateTime? LegalEntitlementEnglishandMathsFundingApprovalEndDate { get; set; }
        public string? LegalEntitlementEnglishandMathsNotes { get; set; }
        public bool DigitalEntitlementFundingAvailable { get; set; }
        public DateTime? DigitalEntitlementFundingApprovalStartDate { get; set; }
        public DateTime? DigitalEntitlementFundingApprovalEndDate { get; set; }
        public string? DigitalEntitlementNotes { get; set; }
        public bool Esflevel34FundingAvailable { get; set; }
        public DateTime? Esflevel34FundingApprovalStartDate { get; set; }
        public DateTime? Esflevel34FundingApprovalEndDate { get; set; }
        public string? Esflevel34Notes { get; set; }
        public bool AdvancedLearnerLoansFundingAvailable { get; set; }
        public DateTime? AdvancedLearnerLoansFundingApprovalStartDate { get; set; }
        public DateTime? AdvancedLearnerLoansFundingApprovalEndDate { get; set; }
        public string? AdvancedLearnerLoansNotes { get; set; }
        public string? AwardingOrganisationUrl { get; set; }
        public bool L3freeCoursesForJobsFundingAvailable { get; set; }
        public DateTime? L3freeCoursesForJobsFundingApprovalStartDate { get; set; }
        public DateTime? L3freeCoursesForJobsFundingApprovalEndDate { get; set; }
        public string? L3freeCoursesForJobsNotes { get; set; }
        public string? QualificationNumberVarchar { get; set; }
    }
}
