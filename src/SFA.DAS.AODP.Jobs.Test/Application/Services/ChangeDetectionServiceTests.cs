using System.Collections.Generic;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Models.Qualification;
using Xunit;

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
            Assert.False(result.ChangesPresent);
            Assert.Empty(result.Fields);
            Assert.False(result.KeyFieldsChanged);
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
            Assert.True(result.ChangesPresent);
            Assert.Contains("Status", result.Fields);
            Assert.False(result.KeyFieldsChanged);
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
            Assert.True(result.ChangesPresent);
            Assert.Contains("Level", result.Fields);
            Assert.True(result.KeyFieldsChanged);
        }

        public static IEnumerable<object[]> TitleWhitespaceCases =>
            new List<object[]>
            {
                new object[] { "My Qualification Title", "My  Qualification   Title" },
                new object[] { "My Qualification Title", " My Qualification Title " },
                new object[] { "My Qualification Title", "My Qualification Title\n" },
                new object[] { "My Qualification Title", "My Qualification Title\u00A0" }
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
            Assert.True(result.ChangesPresent);
            Assert.Contains("Title", result.Fields);
            Assert.False(result.KeyFieldsChanged);
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
            Assert.True(result.ChangesPresent);
            Assert.Contains("Title", result.Fields);
            Assert.True(result.KeyFieldsChanged);
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
            Assert.True(result.ChangesPresent);
            Assert.Contains("Title", result.Fields);
            Assert.Contains("Level", result.Fields);
            Assert.True(result.KeyFieldsChanged);
        }
    }
}
