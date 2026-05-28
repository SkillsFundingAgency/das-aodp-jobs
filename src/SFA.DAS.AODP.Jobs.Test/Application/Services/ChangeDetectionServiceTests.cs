using System.Collections.Generic;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Models.Qualification;
using Xunit;
using Shouldly;

namespace SFA.DAS.AODP.Jobs.Test.Application.Services
{
    public class ChangeDetectionServiceTests
    {
        private static ChangeDetectionService CreateSut()
            => new ChangeDetectionService();

        private static (QualificationDTO dto,
                        QualificationVersions version,
                        AwardingOrganisation org,
                        Qualification qual) CreateEmptyBaseline()
        {
            return (new QualificationDTO(),
                    new QualificationVersions(),
                    new AwardingOrganisation(),
                    new Qualification());
        }

        [Fact]
        public void DetectChanges_NoChanges_ReturnsNoChangesAndNoKeyChanges()
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version, org, qual) = CreateEmptyBaseline();

            // Act
            var result = sut.DetectChanges(dto, version, org, qual);

            // Assert
            result.ChangesPresent.ShouldBeFalse();
            result.Fields.ShouldBeEmpty();
            result.KeyFieldsChanged.ShouldBeFalse();
        }

        [Fact]
        public void DetectChanges_NonKeyFieldChanged_ChangesPresentButNotKey()
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version, org, qual) = CreateEmptyBaseline();

            dto.Status = "Active";
            version.Status = "Inactive";

            // Act
            var result = sut.DetectChanges(dto, version, org, qual);

            // Assert
            result.ChangesPresent.ShouldBeTrue();
            result.Fields.ShouldContain("Status");
            result.KeyFieldsChanged.ShouldBeFalse();
        }

        [Fact]
        public void DetectChanges_KeyFieldChanged_Level_IsKeyChange()
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version, org, qual) = CreateEmptyBaseline();

            dto.Level = "3";
            version.Level = "2";

            // Act
            var result = sut.DetectChanges(dto, version, org, qual);

            // Assert
            result.ChangesPresent.ShouldBeTrue();
            result.Fields.ShouldContain("Level");
            result.KeyFieldsChanged.ShouldBeTrue();
        }

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

        [Theory]
        [MemberData(nameof(TitleWhitespaceCases))]
        public void DetectChanges_TitleWhitespaceOnly_IsChangeButNotKey(
            string oldTitle,
            string newTitle)
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version, org, qual) = CreateEmptyBaseline();

            qual.QualificationName = oldTitle;
            dto.Title = newTitle;

            // Act
            var result = sut.DetectChanges(dto, version, org, qual);

            // Assert
            // Whitespace / invisible / case-only changes are normalised away and should not be treated as changes
            result.ChangesPresent.ShouldBeFalse();
            result.Fields.ShouldBeEmpty();
            result.KeyFieldsChanged.ShouldBeFalse();
        }

        [Fact]
        public void DetectChanges_TitleMeaningfulChange_IsKeyChange()
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version, org, qual) = CreateEmptyBaseline();

            qual.QualificationName = "My Qualification Title";
            dto.Title = "My Qualification Title v2";

            // Act
            var result = sut.DetectChanges(dto, version, org, qual);

            // Assert
            result.ChangesPresent.ShouldBeTrue();
            result.Fields.ShouldContain("Title");
            result.KeyFieldsChanged.ShouldBeTrue();
        }

        [Fact]
        public void DetectChanges_TitleWhitespacePlusOtherKeyField_RemainsKeyChange()
        {
            // Arrange
            var sut = CreateSut();
            var (dto, version, org, qual) = CreateEmptyBaseline();

            qual.QualificationName = "My Qualification Title";
            dto.Title = "My  Qualification   Title"; // whitespace only

            dto.Level = "3";
            version.Level = "2"; // real key-field change

            // Act
            var result = sut.DetectChanges(dto, version, org, qual);

            // Assert
            // Title whitespace change should be normalised away; Level remains a key change
            result.ChangesPresent.ShouldBeTrue();
            result.Fields.ShouldNotContain("Title");
            result.Fields.ShouldContain("Level");
            result.KeyFieldsChanged.ShouldBeTrue();
        }
    }
}
