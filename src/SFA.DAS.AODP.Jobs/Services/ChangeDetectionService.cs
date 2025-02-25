using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Extensions;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class ChangeDetectionService : IChangeDetectionService
    {
        public struct DetectionResults
        {
            public DetectionResults()
            {
                ChangesPresent = false;
                Fields = new List<string>();
            }

            public bool ChangesPresent { get; set; }
            public List<string> Fields { get; set; }
        }

        public DetectionResults DetectChanges(QualificationDTO newRecord, QualificationVersions qualificationVersion, AwardingOrganisation awardingOrganisation, Qualification qualification)
        {
            // Could use Reflection here, but records being compared have mismatched names, different field types, or information located in other structures

            var fields = new List<string>();
            
            fields = fields.AppendIf(newRecord.Ssa != qualificationVersion.Ssa, "Ssa");
            fields = fields.AppendIf(newRecord.Pathways != qualificationVersion.Pathways, "Pathways");
            fields = fields.AppendIf(newRecord.Status != qualificationVersion.Status, "Status");
            fields = fields.AppendIf(newRecord.SsaId != qualificationVersion.SsaId, "SsaId");
            fields = fields.AppendIf(newRecord.AppearsOnPublicRegister != qualificationVersion.AppearsOnPublicRegister, "AppearsOnPublicRegister");
            fields = fields.AppendIf(newRecord.ApprenticeshipStandardReferenceNumber != qualificationVersion.ApprenticeshipStandardReferenceNumber, "ApprenticeshipStandardReferenceNumber");
            fields = fields.AppendIf(newRecord.ApprenticeshipStandardTitle != qualificationVersion.ApprenticeshipStandardTitle, "ApprenticeshipStandardTitle");
            fields = fields.AppendIf(newRecord.ApprovedForDelfundedProgramme != qualificationVersion.ApprovedForDelFundedProgramme, "ApprovedForDelfundedProgramme");
            fields = fields.AppendIf(newRecord.CertificationEndDate != qualificationVersion.CertificationEndDate, "CertificationEndDate");
            fields = fields.AppendIf(newRecord.EighteenPlus != qualificationVersion.EighteenPlus, "EighteenPlus");
            fields = fields.AppendIf(newRecord.EntitlementFrameworkDesignation != qualificationVersion.EntitlementFrameworkDesign, "EntitlementFrameworkDesignation");
            fields = fields.AppendIf(newRecord.EqfLevel != qualificationVersion.EqfLevel, "EqfLevel");
            fields = fields.AppendIf(newRecord.GceSizeEquivalence != qualificationVersion.GceSizeEquivelence, "GceSizeEquivalence");
            fields = fields.AppendIf(newRecord.GcseSizeEquivalence != qualificationVersion.GcseSizeEquivelence, "GcseSizeEquivelence");
            fields = fields.AppendIf(newRecord.Glh != qualificationVersion.Glh, "Glh");
            fields = fields.AppendIf(newRecord.GradingScale != qualificationVersion.GradingScale, "GradingScale");
            fields = fields.AppendIf(newRecord.GradingType != qualificationVersion.GradingType, "GradingType");
            fields = fields.AppendIf(newRecord.ImportStatus != qualificationVersion.ImportStatus, "ImportStatus");
            fields = fields.AppendIf(newRecord.InsertedDate != qualificationVersion.InsertedDate, "InsertedDate");
            fields = fields.AppendIf(newRecord.LastUpdatedDate != qualificationVersion.LastUpdatedDate, "LastUpdatedDate");
            fields = fields.AppendIf(newRecord.Level != qualificationVersion.Level, "Level");
            fields = fields.AppendIf(newRecord.LinkToSpecification != qualificationVersion.LinkToSpecification, "LinkToSpecification");
            fields = fields.AppendIf(newRecord.MaximumGlh != qualificationVersion.MaximumGlh, "MaximumGlh");
            fields = fields.AppendIf(newRecord.MinimumGlh != qualificationVersion.MinimumGlh, "MinimumGlh");
            fields = fields.AppendIf(newRecord.NiDiscountCode != qualificationVersion.NiDiscountCode, "NiDiscountCode");
            fields = fields.AppendIf(newRecord.NineteenPlus != qualificationVersion.NineteenPlus, "NineteenPlus");
            fields = fields.AppendIf(newRecord.OfferedInEngland != qualificationVersion.OfferedInEngland, "OfferedInEngland");
            fields = fields.AppendIf(newRecord.OfferedInNorthernIreland != qualificationVersion.OfferedInNi, "OfferedInNorthernIreland");
            fields = fields.AppendIf(newRecord.OfferedInternationally != qualificationVersion.OfferedInternationally, "OfferedInternationally");
            fields = fields.AppendIf(newRecord.OperationalEndDate != qualificationVersion.OperationalEndDate, "OperationalEndDate");
            fields = fields.AppendIf(newRecord.OperationalStartDate != qualificationVersion.OperationalStartDate, "OperationalStartDate");

            fields = fields.AppendIf(newRecord.OrganisationAcronym != awardingOrganisation.Acronym, "OrganisationAcronym");
            fields = fields.AppendIf(newRecord.OrganisationName != awardingOrganisation.NameOfqual, "OrganisationName");
            fields = fields.AppendIf(newRecord.OrganisationId != awardingOrganisation.Ukprn, "OrganisationId");
            fields = fields.AppendIf(newRecord.OrganisationRecognitionNumber != awardingOrganisation.RecognitionNumber, "OrganisationRecognitionNumber");

            fields = fields.AppendIf(newRecord.Pathways != qualificationVersion.Pathways, "Pathways");
            fields = fields.AppendIf(newRecord.PreSixteen != qualificationVersion.PreSixteen, "PreSixteen");

            fields = fields.AppendIf(newRecord.QualificationNumberNoObliques != qualification.Qan, "QualificationNumberNoObliques");
            fields = fields.AppendIf(newRecord.RegulatedByNorthernIreland != qualificationVersion.RegulatedByNorthernIreland, "RegulatedByNorthernIreland");
            fields = fields.AppendIf(newRecord.RegulationStartDate != qualificationVersion.RegulationStartDate, "RegulationStartDate");
            fields = fields.AppendIf(newRecord.ReviewDate != qualificationVersion.ReviewDate, "ReviewDate");
            fields = fields.AppendIf(newRecord.SixteenToEighteen != qualificationVersion.SixteenToEighteen, "SixteenToEighteen");
            fields = fields.AppendIf(newRecord.Specialism != qualificationVersion.Specialism, "Specialism");
            fields = fields.AppendIf(newRecord.Ssa != qualificationVersion.Ssa, "Ssa");
            fields = fields.AppendIf(newRecord.Status != qualificationVersion.Status, "Status");
            fields = fields.AppendIf(newRecord.SubLevel != qualificationVersion.SubLevel, "SubLevel");
            fields = fields.AppendIf(newRecord.Title != qualification.QualificationName, "Title");
            fields = fields.AppendIf(newRecord.TotalCredits != qualificationVersion.TotalCredits, "TotalCredits");
            fields = fields.AppendIf(newRecord.Tqt != qualificationVersion.Tqt, "Tqt");
            fields = fields.AppendIf(newRecord.Type != qualificationVersion.Type, "Type");
            fields = fields.AppendIf(newRecord.TypeId != qualificationVersion.TypeId, "Type");
            fields = fields.AppendIf(newRecord.UiLastUpdatedDate != qualificationVersion.UiLastUpdatedDate, "UiLastUpdatedDate");            

            return new DetectionResults() { Fields = fields, ChangesPresent = fields.Any() };
        }
    }
}
