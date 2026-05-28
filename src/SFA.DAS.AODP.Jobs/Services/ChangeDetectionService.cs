using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Extensions;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Qualification;
using System.Text.RegularExpressions;

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
                KeyFieldsChanged = false;
            }

            public bool ChangesPresent { get; set; }
            public List<string> Fields { get; set; }
            public bool KeyFieldsChanged { get; set; }
        }

        private readonly List<string> _keyFields;

        public ChangeDetectionService()
        {
            _keyFields = new List<string>()
                {
                    "OrganisationName",
                    "Title",
                    "Level",
                    "Type",
                    "TotalCredits",
                    "Ssa",
                    "GradingType",
                    "OfferedInEngland",
                    "PreSixteen",
                    "SixteenToEighteen",
                    "EighteenPlus",
                    "NineteenPlus",
                    "IntentionToSeekFundingInEngland", 
                    "GLH",
                    "MinimumGLH",
                    "Tqt",
                    "OperationalEndDate",
                    "OfferedInternationally"
                };
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
            fields = fields.AppendIf(NormaliseBoolean(newRecord.ApprovedForDelfundedProgramme, treatFalseAsNull: true) != NormaliseBoolean(qualificationVersion.ApprovedForDelFundedProgramme, treatFalseAsNull: true), "ApprovedForDelfundedProgramme");
            fields = fields.AppendIf(NormaliseDate(newRecord.CertificationEndDate) != NormaliseDate(qualificationVersion.CertificationEndDate), "CertificationEndDate");
            fields = fields.AppendIf(newRecord.EighteenPlus != qualificationVersion.EighteenPlus, "EighteenPlus");
            fields = fields.AppendIf(newRecord.EntitlementFrameworkDesignation != qualificationVersion.EntitlementFrameworkDesign, "EntitlementFrameworkDesignation");
            fields = fields.AppendIf(newRecord.EqfLevel != qualificationVersion.EqfLevel, "EqfLevel");
            fields = fields.AppendIf(newRecord.GceSizeEquivalence != qualificationVersion.GceSizeEquivelence, "GceSizeEquivalence");
            fields = fields.AppendIf(newRecord.GcseSizeEquivalence != qualificationVersion.GcseSizeEquivelence, "GcseSizeEquivelence");
            fields = fields.AppendIf(newRecord.Glh != qualificationVersion.Glh, "Glh");
            fields = fields.AppendIf(newRecord.GradingScale != qualificationVersion.GradingScale, "GradingScale");
            fields = fields.AppendIf(newRecord.GradingType != qualificationVersion.GradingType, "GradingType");
            fields = fields.AppendIf(newRecord.ImportStatus != qualificationVersion.ImportStatus, "ImportStatus");
            fields = fields.AppendIf(NormaliseDate(newRecord.InsertedDate) != NormaliseDate(qualificationVersion.InsertedDate), "InsertedDate");
            fields = fields.AppendIf(NormaliseDate(newRecord.LastUpdatedDate) != NormaliseDate(qualificationVersion.LastUpdatedDate), "LastUpdatedDate");
            fields = fields.AppendIf(newRecord.Level != qualificationVersion.Level, "Level");
            fields = fields.AppendIf(newRecord.LinkToSpecification != qualificationVersion.LinkToSpecification, "LinkToSpecification");
            fields = fields.AppendIf(newRecord.MaximumGlh != qualificationVersion.MaximumGlh, "MaximumGlh");
            fields = fields.AppendIf(newRecord.MinimumGlh != qualificationVersion.MinimumGlh, "MinimumGlh");
            fields = fields.AppendIf(newRecord.NiDiscountCode != qualificationVersion.NiDiscountCode, "NiDiscountCode");
            fields = fields.AppendIf(newRecord.NineteenPlus != qualificationVersion.NineteenPlus, "NineteenPlus");
            fields = fields.AppendIf(newRecord.OfferedInEngland != qualificationVersion.OfferedInEngland, "OfferedInEngland");
            fields = fields.AppendIf(newRecord.OfferedInNorthernIreland != qualificationVersion.OfferedInNi, "OfferedInNorthernIreland");
            fields = fields.AppendIf(newRecord.OfferedInternationally != qualificationVersion.OfferedInternationally, "OfferedInternationally");
            fields = fields.AppendIf(NormaliseDate(newRecord.OperationalEndDate) != NormaliseDate(qualificationVersion.OperationalEndDate), "OperationalEndDate");
            fields = fields.AppendIf(NormaliseDate(newRecord.OperationalStartDate) != NormaliseDate(qualificationVersion.OperationalStartDate), "OperationalStartDate");

            fields = fields.AppendIf(newRecord.OrganisationAcronym != awardingOrganisation.Acronym, "OrganisationAcronym");
            fields = fields.AppendIf(Normalise(newRecord.OrganisationName) != Normalise(awardingOrganisation.NameOfqual), "OrganisationName");
            fields = fields.AppendIf(newRecord.OrganisationId != awardingOrganisation.Ukprn, "OrganisationId");
            fields = fields.AppendIf(newRecord.OrganisationRecognitionNumber != awardingOrganisation.RecognitionNumber, "OrganisationRecognitionNumber");

            fields = fields.AppendIf(newRecord.Pathways != qualificationVersion.Pathways, "Pathways");
            fields = fields.AppendIf(newRecord.PreSixteen != qualificationVersion.PreSixteen, "PreSixteen");

            fields = fields.AppendIf(newRecord.QualificationNumberNoObliques != qualification.Qan, "QualificationNumberNoObliques");
            fields = fields.AppendIf(newRecord.RegulatedByNorthernIreland != qualificationVersion.RegulatedByNorthernIreland, "RegulatedByNorthernIreland");
            fields = fields.AppendIf(NormaliseDate(newRecord.RegulationStartDate) != NormaliseDate(qualificationVersion.RegulationStartDate), "RegulationStartDate");
            fields = fields.AppendIf(NormaliseDate(newRecord.ReviewDate) != NormaliseDate(qualificationVersion.ReviewDate), "ReviewDate");
            fields = fields.AppendIf(newRecord.SixteenToEighteen != qualificationVersion.SixteenToEighteen, "SixteenToEighteen");
            fields = fields.AppendIf(newRecord.Specialism != qualificationVersion.Specialism, "Specialism");            
            fields = fields.AppendIf(newRecord.SubLevel != qualificationVersion.SubLevel, "SubLevel");
            fields = fields.AppendIf(newRecord.Title != qualification.QualificationName, "Title");
            fields = fields.AppendIf(newRecord.TotalCredits != qualificationVersion.TotalCredits, "TotalCredits");
            fields = fields.AppendIf(newRecord.Tqt != qualificationVersion.Tqt, "Tqt");
            fields = fields.AppendIf(newRecord.Type != qualificationVersion.Type, "Type");
            fields = fields.AppendIf(newRecord.TypeId != qualificationVersion.TypeId, "Type");
            fields = fields.AppendIf(NormaliseDate(newRecord.UiLastUpdatedDate) != NormaliseDate(qualificationVersion.UiLastUpdatedDate), "UiLastUpdatedDate");
            fields = fields.AppendIf(newRecord.IntentionToSeekFundingInEngland != qualificationVersion.IntentionToSeekFundingInEngland, "IntentionToSeekFundingInEngland");

            var results = new DetectionResults() { Fields = fields, ChangesPresent = fields.Any() };

            if (results.ChangesPresent)
            {
                var keyFieldsChanged = results.Fields.Intersect(_keyFields).ToList();

                if (keyFieldsChanged.Contains("Title") && IsWhitespaceChange(newRecord.Title, qualification.QualificationName))
                {
                    keyFieldsChanged.RemoveAll(f => string.Equals(f, "Title", StringComparison.InvariantCultureIgnoreCase));
                    results.Fields.RemoveAll(f => string.Equals(f, "Title", StringComparison.InvariantCultureIgnoreCase));
                }

                results.KeyFieldsChanged = keyFieldsChanged.Any();

                // Recalculate ChangesPresent because we may have removed fields (e.g. Title when only whitespace/case changed)
                results.ChangesPresent = results.Fields.Any();
            }

            return results;
        }

        private static bool? NormaliseBoolean(string? value, bool treatFalseAsNull = false)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var normalisedValue = value.Trim();

            bool? parsedValue = normalisedValue.ToLowerInvariant() switch
            {
                "true" => true,
                "false" => false,

                "1" => true,
                "0" => false,

                "yes" => true,
                "no" => false,

                _ => null
            };

            if (treatFalseAsNull && parsedValue == false)
            {
                return null;
            }

            return parsedValue;
        }

        private static DateTime? NormaliseDate(DateTime? date)
        {
            return date?.Date;
        }

        private static string Normalise(string? s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                return string.Empty;
            }

            s = s
                // Standard/control whitespace
                .Replace('\u0009', ' ') // Tab
                .Replace('\u000A', ' ') // Line feed
                .Replace('\u000B', ' ') // Vertical tab
                .Replace('\u000C', ' ') // Form feed
                .Replace('\u000D', ' ') // Carriage return

                // Unicode space separators
                .Replace('\u00A0', ' ') // No-break space
                .Replace('\u1680', ' ') // Ogham space mark
                .Replace('\u2000', ' ') // En quad
                .Replace('\u2001', ' ') // Em quad
                .Replace('\u2002', ' ') // En space
                .Replace('\u2003', ' ') // Em space
                .Replace('\u2004', ' ') // Three-per-em space
                .Replace('\u2005', ' ') // Four-per-em space
                .Replace('\u2006', ' ') // Six-per-em space
                .Replace('\u2007', ' ') // Figure space
                .Replace('\u2008', ' ') // Punctuation space
                .Replace('\u2009', ' ') // Thin space
                .Replace('\u200A', ' ') // Hair space
                .Replace('\u2028', ' ') // Line separator
                .Replace('\u2029', ' ') // Paragraph separator
                .Replace('\u202F', ' ') // Narrow no-break space
                .Replace('\u205F', ' ') // Medium mathematical space
                .Replace('\u3000', ' ') // Ideographic space

                // Zero-width/invisible formatting characters
                .Replace("\u200B", "") // Zero-width space / ZWSP
                .Replace("\u200C", "") // Zero-width non-joiner
                .Replace("\u200D", "") // Zero-width joiner
                .Replace("\u2060", "") // Word joiner
                .Replace("\uFEFF", "") // Zero-width no-break space / byte order mark

                // Apostrophe/single quote variants
                .Replace('\u2018', '\'') // Left single quotation mark
                .Replace('\u2019', '\'') // Right single quotation mark / curly apostrophe
                .Replace('\u201A', '\'') // Single low-9 quotation mark
                .Replace('\u201B', '\'') // Single high-reversed-9 quotation mark
                .Replace('\u2032', '\'') // Prime
                .Replace('\uFF07', '\''); // Fullwidth apostrophe

            return Regex.Replace(
                s.Trim(),
                @"\s+",
                " ",
                RegexOptions.None,
                TimeSpan.FromMilliseconds(50));
        }

        private static bool IsWhitespaceChange(string? newValue, string? oldValue)
        {
            return string.Equals(Normalise(newValue), Normalise(oldValue), StringComparison.OrdinalIgnoreCase)
                   && !string.Equals(newValue ?? "", oldValue ?? "", StringComparison.Ordinal);
        }
    }
}
