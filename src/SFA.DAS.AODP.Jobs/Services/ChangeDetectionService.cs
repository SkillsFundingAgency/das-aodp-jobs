using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Extensions;
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
                ChangedFields = [];
            }

            public bool ChangesPresent { get; set; }
            public List<string> ChangedFields { get; set; }
            public bool KeyFieldsChanged => KeyField.HaveKeyFieldsChanged(ChangedFields);
            public string ChangedFieldsCsv => ChangedFields != null && ChangedFields.Count > 0
                ? string.Join(", ", ChangedFields.Distinct(StringComparer.OrdinalIgnoreCase))
                : string.Empty;
        }

        private readonly IReadOnlyList<string> _keyFields;

        public ChangeDetectionService()
        {
            _keyFields = KeyField.All.Select(kf => kf.ToString()).ToList();
        }

        public DetectionResults DetectChanges(QualificationDTO newRecord, QualificationVersions qualificationVersion)
        {
            var qualification = qualificationVersion.Qualification;
            var awardingOrganisation = qualificationVersion.Organisation;

            // Could use Reflection here, but records being compared have mismatched names, different field types, or information located in other structures

            var fields = new List<string>();

            fields = fields.AppendIf(Normalise(newRecord.Ssa) != Normalise(qualificationVersion.Ssa), "Ssa");
            fields = fields.AppendIf(Normalise(newRecord.Pathways) != Normalise(qualificationVersion.Pathways), "Pathways");
            fields = fields.AppendIf(Normalise(newRecord.Status) != Normalise(qualificationVersion.Status), "Status");
            fields = fields.AppendIf(newRecord.SsaId != qualificationVersion.SsaId, "SsaId");
            fields = fields.AppendIf(newRecord.AppearsOnPublicRegister != qualificationVersion.AppearsOnPublicRegister, "AppearsOnPublicRegister");
            fields = fields.AppendIf(Normalise(newRecord.ApprenticeshipStandardReferenceNumber) != Normalise(qualificationVersion.ApprenticeshipStandardReferenceNumber), "ApprenticeshipStandardReferenceNumber");
            fields = fields.AppendIf(Normalise(newRecord.ApprenticeshipStandardTitle) != Normalise(qualificationVersion.ApprenticeshipStandardTitle), "ApprenticeshipStandardTitle");
            fields = fields.AppendIf(NormaliseBoolean(newRecord.ApprovedForDelfundedProgramme, treatFalseAsNull: true) != NormaliseBoolean(qualificationVersion.ApprovedForDelFundedProgramme, treatFalseAsNull: true), "ApprovedForDelfundedProgramme");
            fields = fields.AppendIf(NormaliseDate(newRecord.CertificationEndDate) != NormaliseDate(qualificationVersion.CertificationEndDate), "CertificationEndDate");
            fields = fields.AppendIf(newRecord.EighteenPlus != qualificationVersion.EighteenPlus, "EighteenPlus");
            fields = fields.AppendIf(Normalise(newRecord.EntitlementFrameworkDesignation) != Normalise(qualificationVersion.EntitlementFrameworkDesign), "EntitlementFrameworkDesignation");
            fields = fields.AppendIf(Normalise(newRecord.EqfLevel) != Normalise(qualificationVersion.EqfLevel), "EqfLevel");
            fields = fields.AppendIf(Normalise(newRecord.GceSizeEquivalence) != Normalise(qualificationVersion.GceSizeEquivelence), "GceSizeEquivalence");
            fields = fields.AppendIf(Normalise(newRecord.GcseSizeEquivalence) != Normalise(qualificationVersion.GcseSizeEquivelence), "GcseSizeEquivelence");
            fields = fields.AppendIf(newRecord.Glh != qualificationVersion.Glh, "Glh");
            fields = fields.AppendIf(Normalise(newRecord.GradingScale) != Normalise(qualificationVersion.GradingScale), "GradingScale");
            fields = fields.AppendIf(newRecord.GradingScaleId != qualificationVersion.GradingScaleId, "GradingScaleId");
            fields = fields.AppendIf(Normalise(newRecord.GradingType) != Normalise(qualificationVersion.GradingType), "GradingType");
            fields = fields.AppendIf(Normalise(newRecord.ImportStatus) != Normalise(qualificationVersion.ImportStatus), "ImportStatus");
            fields = fields.AppendIf(NormaliseDate(newRecord.InsertedDate) != NormaliseDate(qualificationVersion.InsertedDate), "InsertedDate");
            fields = fields.AppendIf(NormaliseDate(newRecord.LastUpdatedDate) != NormaliseDate(qualificationVersion.LastUpdatedDate), "LastUpdatedDate");
            fields = fields.AppendIf(Normalise(newRecord.Level) != Normalise(qualificationVersion.Level), "Level");
            fields = fields.AppendIf(Normalise(newRecord.LinkToSpecification) != Normalise(qualificationVersion.LinkToSpecification), "LinkToSpecification");
            fields = fields.AppendIf(newRecord.MaximumGlh != qualificationVersion.MaximumGlh, "MaximumGlh");
            fields = fields.AppendIf(newRecord.MinimumGlh != qualificationVersion.MinimumGlh, "MinimumGlh");
            fields = fields.AppendIf(Normalise(newRecord.NiDiscountCode) != Normalise(qualificationVersion.NiDiscountCode), "NiDiscountCode");
            fields = fields.AppendIf(newRecord.NineteenPlus != qualificationVersion.NineteenPlus, "NineteenPlus");
            fields = fields.AppendIf(newRecord.OfferedInEngland != qualificationVersion.OfferedInEngland, "OfferedInEngland");
            fields = fields.AppendIf(newRecord.OfferedInNorthernIreland != qualificationVersion.OfferedInNi, "OfferedInNorthernIreland");
            fields = fields.AppendIf(newRecord.OfferedInternationally != qualificationVersion.OfferedInternationally, "OfferedInternationally");
            fields = fields.AppendIf(NormaliseDate(newRecord.OperationalEndDate) != NormaliseDate(qualificationVersion.OperationalEndDate), "OperationalEndDate");
            fields = fields.AppendIf(NormaliseDate(newRecord.OperationalStartDate) != NormaliseDate(qualificationVersion.OperationalStartDate), "OperationalStartDate");

            fields = fields.AppendIf(!string.Equals(Normalise(newRecord.OrganisationAcronym), Normalise(awardingOrganisation.Acronym), StringComparison.OrdinalIgnoreCase), "OrganisationAcronym");
            fields = fields.AppendIf(!string.Equals(Normalise(newRecord.OrganisationName), Normalise(awardingOrganisation.NameOfqual), StringComparison.OrdinalIgnoreCase), "OrganisationName");
            fields = fields.AppendIf(newRecord.OrganisationId != awardingOrganisation.Ukprn, "OrganisationId");
            fields = fields.AppendIf(Normalise(newRecord.OrganisationRecognitionNumber) != Normalise(awardingOrganisation.RecognitionNumber), "OrganisationRecognitionNumber");

            fields = fields.AppendIf(newRecord.PreSixteen != qualificationVersion.PreSixteen, "PreSixteen");

            fields = fields.AppendIf(Normalise(newRecord.QualificationNumberNoObliques) != Normalise(qualification.Qan), "QualificationNumberNoObliques");
            fields = fields.AppendIf(newRecord.RegulatedByNorthernIreland != qualificationVersion.RegulatedByNorthernIreland, "RegulatedByNorthernIreland");
            fields = fields.AppendIf(NormaliseDate(newRecord.RegulationStartDate) != NormaliseDate(qualificationVersion.RegulationStartDate), "RegulationStartDate");
            fields = fields.AppendIf(NormaliseDate(newRecord.ReviewDate) != NormaliseDate(qualificationVersion.ReviewDate), "ReviewDate");
            fields = fields.AppendIf(newRecord.SixteenToEighteen != qualificationVersion.SixteenToEighteen, "SixteenToEighteen");
            fields = fields.AppendIf(Normalise(newRecord.Specialism) != Normalise(qualificationVersion.Specialism), "Specialism");
            fields = fields.AppendIf(Normalise(newRecord.SubLevel) != Normalise(qualificationVersion.SubLevel), "SubLevel");
            fields = fields.AppendIf(!string.Equals(Normalise(newRecord.Title), Normalise(qualification.QualificationName), StringComparison.OrdinalIgnoreCase), "Title");
            fields = fields.AppendIf(newRecord.TotalCredits != qualificationVersion.TotalCredits, "TotalCredits");
            fields = fields.AppendIf(newRecord.Tqt != qualificationVersion.Tqt, "Tqt");
            fields = fields.AppendIf(Normalise(newRecord.Type) != Normalise(qualificationVersion.Type), "Type");
            fields = fields.AppendIf(newRecord.TypeId != qualificationVersion.TypeId, "TypeId");
            fields = fields.AppendIf(NormaliseDate(newRecord.UiLastUpdatedDate) != NormaliseDate(qualificationVersion.UiLastUpdatedDate), "UiLastUpdatedDate");
            fields = fields.AppendIf(newRecord.IntentionToSeekFundingInEngland != qualificationVersion.IntentionToSeekFundingInEngland, "IntentionToSeekFundingInEngland");

            var results = new DetectionResults
            {
                ChangedFields = fields, 
                ChangesPresent = fields.Count > 0
            };

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

            var normalisedCharacters = s
                .Select(c =>
                {
                    if (char.IsWhiteSpace(c))
                    {
                        return ' ';
                    }

                    return c switch
                    {
                        // Zero-width/invisible formatting characters
                        '\u200B' => '\0', // Zero-width space / ZWSP
                        '\u200C' => '\0', // Zero-width non-joiner
                        '\u200D' => '\0', // Zero-width joiner
                        '\u2060' => '\0', // Word joiner
                        '\uFEFF' => '\0', // Zero-width no-break space / byte order mark

                        // Apostrophe/single quote variants
                        '\u2018' => '\'', // Left single quotation mark
                        '\u2019' => '\'', // Right single quotation mark / curly apostrophe
                        '\u201A' => '\'', // Single low-9 quotation mark
                        '\u201B' => '\'', // Single high-reversed-9 quotation mark
                        '\u2032' => '\'', // Prime
                        '\uFF07' => '\'', // Fullwidth apostrophe

                        // Hyphen/dash variants
                        '\u2010' => '-', // Hyphen
                        '\u2011' => '-', // Non-breaking hyphen
                        '\u2012' => '-', // Figure dash
                        '\u2013' => '-', // En dash
                        '\u2014' => '-', // Em dash
                        '\u2015' => '-', // Horizontal bar
                        '\u2212' => '-', // Minus sign
                        '\uFE58' => '-', // Small em dash
                        '\uFE63' => '-', // Small hyphen-minus
                        '\uFF0D' => '-', // Fullwidth hyphen-minus

                        _ => c
                    };
                })
                .Where(c => c != '\0')
                .ToArray();

            return Regex.Replace(
                new string(normalisedCharacters).Trim(),
                @" +",
                " ",
                RegexOptions.None,
                TimeSpan.FromMilliseconds(50));
        }
    }
}