using SFA.DAS.AODP.Models.Qualification;
using Shouldly;

namespace SFA.DAS.AODP.Jobs.Test.Application.Services
{
    public class ChangeDetectionServiceTests : UnitTest
    {
        private static ChangeDetectionService CreateSut()
            => new ChangeDetectionService();

        private static (QualificationDTO dto, QualificationVersions version) CreateEmptyBaseline()
        {
            var version = new QualificationVersions
            {
                Qualification = new Qualification(),
                Organisation = new AwardingOrganisation(),
            };

            var dto = new QualificationDTO();

            return (dto, version);
        }

        [Fact]
        public void DetectChanges_NoChanges_ReturnsNoChangesAndNoKeyChanges()
        {
            // Arrange
            var sut = CreateSut();
            var (qualificationDTO, qualificationVersion) = CreateEmptyBaseline();

            // Act
            var result = sut.DetectChanges(qualificationDTO, qualificationVersion);

            // Assert
            result.ChangesPresent.ShouldBe(false);
            result.ChangedFields.ShouldBeEmpty();
            result.KeyFieldsChanged.ShouldBe(false);
        }

        [Fact]
        public void DetectChanges_WhenAllFieldsChange_ShouldReturnAllChangedFields()
        {
            // Arrange

            var qualification = new Qualification
            {
                Qan = "12345678",
                QualificationName = "Original Qualification"
            };

            var organisation = new AwardingOrganisation
            {
                Acronym = "old",
                NameOfqual = "old",
                Ukprn = 1,
                RecognitionNumber = "old"
            };

            var version = new QualificationVersions
            {
                Qualification = qualification,
                Organisation = organisation,
                Ssa = "SSA",
                Pathways = "Pathway",
                Status = "Active",
                SsaId = 1,
                AppearsOnPublicRegister = true,
                ApprenticeshipStandardReferenceNumber = "ASR1",
                ApprenticeshipStandardTitle = "Standard Title",
                ApprovedForDelFundedProgramme = "true",
                CertificationEndDate = new DateTime(2025, 1, 1),
                EighteenPlus = true,
                EntitlementFrameworkDesign = "Framework",
                EqfLevel = "EQF1",
                GceSizeEquivelence = "GCE",
                GcseSizeEquivelence = "GCSE",
                Glh = 100,
                GradingScale = "Scale",
                GradingScaleId = 1,
                GradingType = "Type",
                ImportStatus = "Imported",
                InsertedDate = new DateTime(2025, 1, 1),
                LastUpdatedDate = new DateTime(2025, 1, 2),
                Level = "Level1",
                LinkToSpecification = "https://old.com",
                MaximumGlh = 200,
                MinimumGlh = 50,
                NiDiscountCode = "DISC",
                NineteenPlus = true,
                OfferedInEngland = true,
                OfferedInNi = true,
                OfferedInternationally = true,
                OperationalEndDate = new DateTime(2030, 1, 1),
                OperationalStartDate = new DateTime(2020, 1, 1),
                PreSixteen = true,
                RegulatedByNorthernIreland = true,
                RegulationStartDate = new DateTime(2020, 2, 1),
                ReviewDate = new DateTime(2026, 1, 1),
                SixteenToEighteen = true,
                Specialism = "Specialism",
                SubLevel = "Sub",
                TotalCredits = 10,
                Tqt = 100,
                Type = "QualificationType",
                TypeId = 1,
                UiLastUpdatedDate = new DateTime(2025, 3, 1),
                IntentionToSeekFundingInEngland = true
            };

            var dto = new QualificationDTO
            {
                Ssa = "SSA_CHANGED",
                Pathways = "Pathway_CHANGED",
                Status = "Inactive",
                SsaId = 2,
                AppearsOnPublicRegister = false,
                ApprenticeshipStandardReferenceNumber = "ASR2",
                ApprenticeshipStandardTitle = "Standard Title Changed",
                ApprovedForDelfundedProgramme = "false",
                CertificationEndDate = new DateTime(2025, 2, 1),
                EighteenPlus = false,
                EntitlementFrameworkDesignation = "Framework Changed",
                EqfLevel = "EQF2",
                GceSizeEquivalence = "GCE Changed",
                GcseSizeEquivalence = "GCSE Changed",
                Glh = 101,
                GradingScale = "Scale Changed",
                GradingScaleId = 2,
                GradingType = "Type Changed",
                ImportStatus = "Pending",
                InsertedDate = new DateTime(2025, 2, 1),
                LastUpdatedDate = new DateTime(2025, 2, 2),
                Level = "Level2",
                LinkToSpecification = "https://new.com",
                MaximumGlh = 201,
                MinimumGlh = 51,
                NiDiscountCode = "DISC2",
                NineteenPlus = false,
                OfferedInEngland = false,
                OfferedInNorthernIreland = false,
                OfferedInternationally = false,
                OperationalEndDate = new DateTime(2031, 1, 1),
                OperationalStartDate = new DateTime(2021, 1, 1),

                OrganisationAcronym = "NEWORG",
                OrganisationName = "New Organisation",
                OrganisationId = 20000002,
                OrganisationRecognitionNumber = "RN002",

                PreSixteen = false,
                QualificationNumberNoObliques = "87654321",
                RegulatedByNorthernIreland = false,
                RegulationStartDate = new DateTime(2021, 2, 1),
                ReviewDate = new DateTime(2027, 1, 1),
                SixteenToEighteen = false,
                Specialism = "Specialism Changed",
                SubLevel = "Sub Changed",
                Title = "New Qualification",
                TotalCredits = 20,
                Tqt = 200,
                Type = "QualificationTypeChanged",
                TypeId = 2,
                UiLastUpdatedDate = new DateTime(2025, 4, 1),
                IntentionToSeekFundingInEngland = false
            };

            var sut = new ChangeDetectionService();

            var expectedFields = new[]
            {
                "Ssa",
                "Pathways",
                "Status",
                "SsaId",
                "AppearsOnPublicRegister",
                "ApprenticeshipStandardReferenceNumber",
                "ApprenticeshipStandardTitle",
                "ApprovedForDelfundedProgramme",
                "CertificationEndDate",
                "EighteenPlus",
                "EntitlementFrameworkDesignation",
                "EqfLevel",
                "GceSizeEquivalence",
                "GcseSizeEquivelence",
                "Glh",
                "GradingScale",
                "GradingScaleId",
                "GradingType",
                "ImportStatus",
                "InsertedDate",
                "LastUpdatedDate",
                "Level",
                "LinkToSpecification",
                "MaximumGlh",
                "MinimumGlh",
                "NiDiscountCode",
                "NineteenPlus",
                "OfferedInEngland",
                "OfferedInNorthernIreland",
                "OfferedInternationally",
                "OperationalEndDate",
                "OperationalStartDate",
                "OrganisationAcronym",
                "OrganisationName",
                "OrganisationId",
                "OrganisationRecognitionNumber",
                "PreSixteen",
                "QualificationNumberNoObliques",
                "RegulatedByNorthernIreland",
                "RegulationStartDate",
                "ReviewDate",
                "SixteenToEighteen",
                "Specialism",
                "SubLevel",
                "Title",
                "TotalCredits",
                "Tqt",
                "Type",
                "TypeId",
                "UiLastUpdatedDate",
                "IntentionToSeekFundingInEngland"
            };

            // Act
            var result = sut.DetectChanges(dto, version);

            // Assert
            result.ChangesPresent.ShouldBeTrue();
            result.ChangedFields.Count.ShouldBe(expectedFields.Length);
            result.ChangedFields.ShouldBe(expectedFields);
        }

        [Fact]
        public void DetectChanges_NonKeyFieldChanged_ChangesPresentButNotKey()
        {
            // Arrange
            var sut = CreateSut();
            var (qualificationDTO, qualificationVersion) = CreateEmptyBaseline();

            qualificationDTO.Status = "Active";
            qualificationVersion.Status = "Inactive";

            // Act
            var result = sut.DetectChanges(qualificationDTO, qualificationVersion);

            // Assert
            result.ChangesPresent.ShouldBe(true);
            result.ChangedFields.ShouldContain("Status");
            result.KeyFieldsChanged.ShouldBe(false);
        }

        [Fact]
        public void DetectChanges_KeyFieldChanged_Level_IsKeyChange()
        {
            // Arrange
            var sut = CreateSut();
            var (qualificationDTO, qualificationVersion) = CreateEmptyBaseline();

            qualificationDTO.Level = "3";
            qualificationVersion.Level = "2";

            // Act
            var result = sut.DetectChanges(qualificationDTO,qualificationVersion);

            // Assert
            result.ChangesPresent.ShouldBe(true);
            result.ChangedFields.ShouldContain("Level");
            result.KeyFieldsChanged.ShouldBe(true);
        }

        [Fact]
        public void DetectChanges_MultipleKeyFieldsChanged_IsKeyChange()
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version) = CreateEmptyBaseline();

            version.Level = "2";
            dto.Level = "3";

            version.Tqt = 10;
            dto.Tqt = 20;

            // Act
            var result = sut.DetectChanges(dto, version);

            // Assert
            result.ChangesPresent.ShouldBe(true);
            result.ChangedFields.ShouldContain("Level");
            result.ChangedFields.ShouldContain("Tqt");
            result.KeyFieldsChanged.ShouldBe(true);
        }

        [Fact]
        public void DetectChanges_GlhKeyFieldsChanged_CaseInsensitivityCheck()
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version) = CreateEmptyBaseline();
         
            version.Glh = 10;
            dto.Glh = 20;

            version.MinimumGlh = 10;
            dto.MinimumGlh = 20;

            // Act
            var result = sut.DetectChanges(dto, version);

            // Assert
            result.ChangesPresent.ShouldBe(true);
            result.ChangedFields.ShouldBe(["Glh", "MinimumGlh"], Case.Insensitive);

            result.KeyFieldsChanged.ShouldBe(true);
        }

        [Theory]
        [MemberData(nameof(TitleWhitespaceCases))]
        public void DetectChanges_TitleWhitespaceOnly_NoChange(
            string oldTitle,
            string newTitle)
        {
            // Arrange
            var sut = CreateSut();
            var (qualificationDTO, qualificationVersion) = CreateEmptyBaseline();

            qualificationVersion.Qualification.QualificationName = oldTitle;
            qualificationDTO.Title = newTitle;

            // Act
            var result = sut.DetectChanges(qualificationDTO, qualificationVersion);

            // Assert
            result.ChangesPresent.ShouldBe(false);
            result.ChangedFields.ShouldBeEmpty();
            result.KeyFieldsChanged.ShouldBe(false);
        }

        [Fact]
        public void DetectChanges_TitleMeaningfulChange_IsKeyChange()
        {
            // Arrange
            var sut = CreateSut();
            var (qualificationDTO, qualificationVersion) = CreateEmptyBaseline();

            qualificationVersion.Qualification.QualificationName = "My Qualification Title";
            qualificationDTO.Title = "My Qualification Title v2";

            // Act
            var result = sut.DetectChanges(qualificationDTO, qualificationVersion);

            // Assert
            result.ChangesPresent.ShouldBe(true);
            result.ChangedFields.ShouldContain("Title");
            result.KeyFieldsChanged.ShouldBe(true);
        }

        [Fact]
        public void DetectChanges_TitleWhitespacePlusOtherKeyField_RemainsKeyChange()
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version) = CreateEmptyBaseline();

            version.Qualification.QualificationName = "My Qualification Title";
            dto.Title = "My  Qualification   Title"; 

            dto.Level = "3";
            version.Level = "2";

            // Act
            var result = sut.DetectChanges(dto, version);

            // Assert
            result.ChangesPresent.ShouldBe(true);
            result.ChangedFields.ShouldNotContain("Title");
            result.ChangedFields.ShouldContain("Level");
            result.KeyFieldsChanged.ShouldBe(true);
        }

        [Fact]
        public void DetectChanges_TitleNullAndWhitespace_NoChange()
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version) = CreateEmptyBaseline();

            version.Qualification.QualificationName = null;
            dto.Title = "   ";

            // Act
            var result = sut.DetectChanges(dto, version);

            // Assert
            result.ChangesPresent.ShouldBe(false);
            result.ChangedFields.ShouldBeEmpty();
            result.KeyFieldsChanged.ShouldBe(false);
        }

        [Fact]
        public void DetectChanges_ApprovedForDelFundedProgramme_FalseTreatedAsNull_NoChange()
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version) = CreateEmptyBaseline();

            dto.ApprovedForDelfundedProgramme = "false"; // treated as null
            version.ApprovedForDelFundedProgramme = null;

            // Act
            var result = sut.DetectChanges(dto, version);

            // Assert
            result.ChangesPresent.ShouldBeFalse();
            result.ChangedFields.ShouldBeEmpty();
            result.KeyFieldsChanged.ShouldBeFalse();
        }

        [Fact]
        public void DetectChanges_ApprovedForDelFundedProgramme_True_IsChange()
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version) = CreateEmptyBaseline();

            dto.ApprovedForDelfundedProgramme = "true";
            version.ApprovedForDelFundedProgramme = null;

            // Act
            var result = sut.DetectChanges(dto, version);

            // Assert
            result.ChangesPresent.ShouldBeTrue();
            result.ChangedFields.ShouldContain("ApprovedForDelfundedProgramme");
            result.KeyFieldsChanged.ShouldBeFalse();
        }

        [Fact]
        public void DetectChanges_LastUpdatedDate_TimeOfDayDifferences_AreIgnored()
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version) = CreateEmptyBaseline();

            dto.LastUpdatedDate = new DateTime(2024, 1, 1, 13, 30, 0);
            version.LastUpdatedDate = new DateTime(2024, 1, 1, 0, 0, 0);

            // Act
            var result = sut.DetectChanges(dto, version);

            // Assert
            result.ChangesPresent.ShouldBeFalse();
            result.ChangedFields.ShouldNotContain("LastUpdatedDate");
        }

        [Fact]
        public void DetectChanges_OrganisationName_WithUnicodeAndZeroWidth_AreNormalised_NoChange()
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version) = CreateEmptyBaseline();

            dto.OrganisationName = "O'Connor";
            version.Organisation.NameOfqual = "O\u2019Connor\u200B"; // curly apostrophe + zero-width space

            // Act
            var result = sut.DetectChanges(dto, version);

            // Assert
            result.ChangesPresent.ShouldBeFalse();
            result.ChangedFields.ShouldBeEmpty();
            result.KeyFieldsChanged.ShouldBeFalse();
        }

        [Theory]
        [MemberData(nameof(HyphenVariantsCases))]
        public void DetectChanges_HyphenVariants_AreNormalised_NoChange(
            string oldTitle,
            string newTitle)
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version) = CreateEmptyBaseline();

            version.Qualification.QualificationName = oldTitle;
            dto.Title = newTitle;

            // Act
            var result = sut.DetectChanges(dto, version);

            // Assert
            // Various unicode hyphen/dash characters should normalise to a simple hyphen and not be treated as changes
            result.ChangesPresent.ShouldBeFalse();
            result.ChangedFields.ShouldBeEmpty();
            result.KeyFieldsChanged.ShouldBeFalse();
        }

        [Fact]
        public void DetectChanges_HyphenRemoved_IsKeyChange()
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version) = CreateEmptyBaseline();

            version.Qualification.QualificationName = "A-B Qualification";
            dto.Title = "A B Qualification"; // hyphen removed -> treated as meaningful change

            // Act
            var result = sut.DetectChanges(dto, version);

            // Assert
            result.ChangesPresent.ShouldBeTrue();
            result.ChangedFields.ShouldContain("Title");
            result.KeyFieldsChanged.ShouldBeTrue();
        }

        #region Theory Data

        public static IEnumerable<object[]> TitleWhitespaceCases =>
             new List<object[]>
             {
                // multiple spaces
                new object[] { "My Qualification Title", "My  Qualification   Title" },
                // leading/trailing spaces
                new object[] { "My Qualification Title", " My Qualification Title " },
                // newline
                new object[] { "My Qualification Title", "My Qualification Title\n" },
                // no-break space
                new object[] { "My Qualification Title", "My Qualification Title\u00A0" },
                // tab
                new object[] { "My Qualification Title", "My\tQualification Title" },
                // vertical tab and form feed
                new object[] { "My Qualification Title", "My Qualification\u000B Title" },
                new object[] { "My Qualification Title", "My Qualification\u000C Title" },
                // carriage return
                new object[] { "My Qualification Title", "My Qualification Title\r" },
                // various unicode space separators
                new object[] { "My Qualification Title", "My Qualification\u1680 Title" },
                new object[] { "My Qualification Title", "My Qualification\u2003 Title" },
                new object[] { "My Qualification Title", "My Qualification\u2009 Title" },
                new object[] { "My Qualification Title", "My Qualification\u200A Title" },
                new object[] { "My Qualification Title", "My Qualification\u2028 Title" },
                new object[] { "My Qualification Title", "My Qualification\u2029 Title" },
                new object[] { "My Qualification Title", "My Qualification\u202F Title" },
                new object[] { "My Qualification Title", "My Qualification\u205F Title" },
                new object[] { "My Qualification Title", "My Qualification\u3000 Title" },
                // zero-width / invisible characters
                new object[] { "My Qualification Title", "My\u200B Qualification Title" },
                new object[] { "My Qualification Title", "My Qualification\u200C Title" },
                new object[] { "My Qualification Title", "My\u200D Qualification Title" },
                new object[] { "My Qualification Title", "My Qualification\u2060 Title" },
                new object[] { "My Qualification Title", "\uFEFFMy Qualification Title" },
                // apostrophe/quote variants should normalise to straight apostrophe
                new object[] { "O'Connor", "O\u2019Connor" },
                new object[] { "O'Connor", "O\u2018Connor" },
                new object[] { "O'Connor", "O\u201AConnor" },
                new object[] { "O'Connor", "O\u201BConnor" },
                new object[] { "O'Connor", "O\u2032Connor" },
                new object[] { "O'Connor", "O\uFF07Connor" },
                // case-only change should be treated as non-key (normalisation compares ignoring case)
                new object[] { "My Qualification Title", "my qualification title" },
                // mixed invisible and spacing characters
                new object[] { "My Qualification Title", " My\u200B Qualification\u00A0 Title\u200D " }
             };

        public static IEnumerable<object[]> HyphenVariantsCases =>
            new List<object[]>
            {
                new object[] { "A-B Qualification", "A\u2010B Qualification" }, // hyphen
                new object[] { "A-B Qualification", "A\u2011B Qualification" }, // non-breaking hyphen
                new object[] { "A-B Qualification", "A\u2012B Qualification" }, // figure dash
                new object[] { "A-B Qualification", "A\u2013B Qualification" }, // en dash
                new object[] { "A-B Qualification", "A\u2014B Qualification" }, // em dash
                new object[] { "A-B Qualification", "A\u2015B Qualification" }, // horizontal bar
                new object[] { "A-B Qualification", "A\u2212B Qualification" }, // minus sign
                new object[] { "A-B Qualification", "A\uFE58B Qualification" }, // small em dash
                new object[] { "A-B Qualification", "A\uFE63B Qualification" }, // small hyphen-minus
                new object[] { "A-B Qualification", "A\uFF0DB Qualification" }  // fullwidth hyphen-minus
            };

        #endregion
    }
}