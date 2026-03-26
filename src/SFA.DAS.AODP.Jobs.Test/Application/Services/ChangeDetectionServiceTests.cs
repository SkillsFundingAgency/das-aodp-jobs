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
            Assert.False(result.ChangesPresent);
            Assert.Empty(result.Fields);
            Assert.False(result.KeyFieldsChanged);
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
            Assert.True(result.ChangesPresent);
            Assert.Contains("Status", result.Fields);
            Assert.False(result.KeyFieldsChanged);
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
            Assert.True(result.ChangesPresent);
            Assert.Contains("Level", result.Fields);
            Assert.True(result.KeyFieldsChanged);
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
            Assert.True(result.ChangesPresent);
            Assert.Contains("Level", result.Fields);
            Assert.Contains("Tqt", result.Fields);
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
            Assert.False(result.ChangesPresent);
            Assert.Empty(result.Fields);
            Assert.False(result.KeyFieldsChanged);
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
            Assert.True(result.ChangesPresent);
            Assert.Contains("Title", result.Fields);
            Assert.True(result.KeyFieldsChanged);
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
            Assert.True(result.ChangesPresent);
            Assert.DoesNotContain("Title", result.Fields);   
            Assert.Contains("Level", result.Fields);
            Assert.True(result.KeyFieldsChanged);
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
            Assert.False(result.ChangesPresent);
            Assert.Empty(result.Fields);
            Assert.False(result.KeyFieldsChanged);
        }

    }
}