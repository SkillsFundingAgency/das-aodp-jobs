namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services;

public class QualificationReferenceTests
{
    [Theory]
    [InlineData("End-Point Assessment")]
    [InlineData("end-point assessment")]
    [InlineData("Apprenticeship Assessment Qualification")]
    [InlineData("apprenticeship assessment qualification")]
    public void IsIneligibleType_WhenTypeIsIneligible_ReturnsTrue(string type)
    {
        // Arrange

        // Act
        var result = QualificationReference.IsIneligibleType(type);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Some Other Type")]
    public void IsIneligibleType_WhenTypeIsNotIneligible_ReturnsFalse(string? type)
    {
        // Arrange

        // Act
        var result = QualificationReference.IsIneligibleType(type);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void HasIneligibleTitle_WhenTitleIsNullOrWhiteSpace_ReturnsFalse(string? title)
    {
        // Arrange

        // Act
        var result = QualificationReference.HasIneligibleTitle(QualificationLevel.Level4.Value, title);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [InlineData(null, "ESOL International")]
    [InlineData("", "ESOL International")]
    [InlineData("Unknown Level", "ESOL International")]
    public void HasIneligibleTitle_WhenLevelIsUnknown_AndCommonRuleMatches_ReturnsTrue(string? level, string title)
    {
        // Arrange

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(null, "Ordinary Qualification")]
    [InlineData("", "Ordinary Qualification")]
    [InlineData("Unknown Level", "Ordinary Qualification")]
    public void HasIneligibleTitle_WhenLevelIsUnknown_AndCommonRuleDoesNotMatch_ReturnsFalse(string? level, string title)
    {
        // Arrange

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasIneligibleTitle_WhenKnownLevelHasNoConfiguredRules_AndNoCommonMatch_ReturnsFalse()
    {
        // Arrange
        var level = QualificationLevel.Level3;
        var title = "Ordinary Qualification";

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasIneligibleTitle_WhenKnownLevelHasNoConfiguredRules_ButCommonMatchExists_ReturnsTrue()
    {
        // Arrange
        var level = QualificationLevel.Level3;
        var title = "ESOL International Certificate";

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasIneligibleTitle_WhenTitleHasWhitespaceAndDifferentCase_ReturnsTrue()
    {
        // Arrange
        var level = QualificationLevel.Level7;
        var title = "   advanced MASTER in finance   ";

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(Level4TrueCases))]
    public void HasIneligibleTitle_Level4_WhenTitleShouldBeIneligible_ReturnsTrue(string title)
    {
        // Arrange
        var level = QualificationLevel.Level4;

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(Level4FalseCases))]
    public void HasIneligibleTitle_Level4_WhenTitleShouldNotBeIneligible_ReturnsFalse(string title)
    {
        // Arrange
        var level = QualificationLevel.Level4;

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(Level5TrueCases))]
    public void HasIneligibleTitle_Level5_WhenTitleShouldBeIneligible_ReturnsTrue(string title)
    {
        // Arrange
        var level = QualificationLevel.Level5;

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(Level5FalseCases))]
    public void HasIneligibleTitle_Level5_WhenTitleShouldNotBeIneligible_ReturnsFalse(string title)
    {
        // Arrange
        var level = QualificationLevel.Level5;

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(Level6TrueCases))]
    public void HasIneligibleTitle_Level6_WhenTitleShouldBeIneligible_ReturnsTrue(string title)
    {
        // Arrange
        var level = QualificationLevel.Level6;

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(Level6FalseCases))]
    public void HasIneligibleTitle_Level6_WhenTitleShouldNotBeIneligible_ReturnsFalse(string title)
    {
        // Arrange
        var level = QualificationLevel.Level6;

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(Level7TrueCases))]
    public void HasIneligibleTitle_Level7_WhenTitleShouldBeIneligible_ReturnsTrue(string title)
    {
        // Arrange
        var level = QualificationLevel.Level7;

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(Level7FalseCases))]
    public void HasIneligibleTitle_Level7_WhenTitleShouldNotBeIneligible_ReturnsFalse(string title)
    {
        // Arrange
        var level = QualificationLevel.Level7;

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.False(result);
    }

    [Theory]
    [MemberData(nameof(Level8TrueCases))]
    public void HasIneligibleTitle_Level8_WhenTitleShouldBeIneligible_ReturnsTrue(string title)
    {
        // Arrange
        var level = QualificationLevel.Level8;

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(Level8FalseCases))]
    public void HasIneligibleTitle_Level8_WhenTitleShouldNotBeIneligible_ReturnsFalse(string title)
    {
        // Arrange
        var level = QualificationLevel.Level8;

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasIneligibleTitle_StringOverload_WhenKnownLevelMatchesConfiguredRule_ReturnsTrue()
    {
        // Arrange
        var level = QualificationLevel.Level5.Value;
        var title = "Higher National Diploma in Business";

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasIneligibleTitle_StringOverload_WhenKnownLevelDoesNotMatchConfiguredRule_ReturnsFalse()
    {
        // Arrange
        var level = QualificationLevel.Level5.Value;
        var title = "Ordinary Diploma in Business";

        // Act
        var result = QualificationReference.HasIneligibleTitle(level, title);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasIneligibleTitle_WhenLevelAndStringOverloadsUsedWithSameInputs_ReturnSameResult()
    {
        // Arrange
        var level = QualificationLevel.Level7;
        var title = "Award in MBA";

        // Act
        var resultFromLevelOverload = QualificationReference.HasIneligibleTitle(level, title);
        var resultFromStringOverload = QualificationReference.HasIneligibleTitle(level.Value, title);

        // Assert
        Assert.Equal(resultFromLevelOverload, resultFromStringOverload);
    }

    public static IEnumerable<object[]> Level4TrueCases()
    {
        yield return ["Higher National Certificate in Engineering"];
        yield return ["Certificate of Higher Education in Science"];
        yield return ["Cert HE in Business"];
        yield return ["Award in HNC"];
        yield return ["ESOL International Level 4"];
    }

    public static IEnumerable<object[]> Level4FalseCases()
    {
        yield return ["Ordinary Certificate"];
        yield return ["Foundation Learning Award"];
        yield return ["This contains HNCC but not the whole word"];
        yield return ["This contains Cert HENow but not the actual phrase"];
    }

    public static IEnumerable<object[]> Level5TrueCases()
    {
        yield return ["Foundation Degree in Business"];
        yield return ["Higher National Diploma in Computing"];
        yield return ["Diploma of Higher Education in Health"];
        yield return ["Diploma in Teaching (Further Education and Skills)"];
        yield return ["Diploma in Teaching (FE and Skills)"];
        yield return ["Diploma in Teaching (FE)"];
        yield return ["Further Education and Skills Teaching Award"];
        yield return ["Certificate in Education"];
        yield return ["Learning and Skills Teacher qualification"];
        yield return ["Award in HND"];
        yield return ["Award in Dip HE"];
        yield return ["Award in FdA"];
        yield return ["Award in FdEng"];
        yield return ["Award in FdSc"];
        yield return ["Award in DiT"];
        yield return ["Award in DIT"];
        yield return ["Award in CertEd"];
        yield return ["Award in CertED"];
        yield return ["Award in LST"];
        yield return ["ESOL International Level 5"];
    }

    public static IEnumerable<object[]> Level5FalseCases()
    {
        yield return ["Ordinary Diploma in Business"];
        yield return ["Diploma in Teaching and Learning"];
        yield return ["This contains HNDD but not the whole word"];
        yield return ["This contains FdAcademic but not the abbreviation as a whole word"];
        yield return ["This contains LSTX but not the whole word"];
    }

    public static IEnumerable<object[]> Level6TrueCases()
    {
        yield return ["Degree in Computing"];
        yield return ["Professional Graduate Certificate in Education"];
        yield return ["Professional Graduate Diploma in Education"];
        yield return ["Award in BA"];
        yield return ["Award in BSc"];
        yield return ["Award in BEd"];
        yield return ["Award in BEng"];
        yield return ["Award in BTech"];
        yield return ["Award in PgCE"];
        yield return ["Award in PgDE"];
        yield return ["ESOL International Level 6"];
    }

    public static IEnumerable<object[]> Level6FalseCases()
    {
        yield return ["Ordinary Diploma in Business"];
        yield return ["This contains BAX but not the whole word"];
        yield return ["Advanced Learning Certificate"];
    }

    public static IEnumerable<object[]> Level7TrueCases()
    {
        yield return ["Master in Finance"];
        yield return ["Postgraduate Certificate in Education"];
        yield return ["Postgraduate Diploma in Education"];
        yield return ["Award in MPhil"];
        yield return ["Award in MSc"];
        yield return ["Award in MA"];
        yield return ["Award in MBA"];
        yield return ["Award in MDes"];
        yield return ["Award in MRes"];
        yield return ["Award in PGCE"];
        yield return ["Award in PGDE"];
        yield return ["ESOL International Level 7"];
    }

    public static IEnumerable<object[]> Level7FalseCases()
    {
        yield return ["Ordinary Diploma in Business"];
        yield return ["This contains MAButNotAWholeWord"];
        yield return ["This contains PGCEX but not the whole word"];
    }

    public static IEnumerable<object[]> Level8TrueCases()
    {
        yield return ["Doctor of Education"];
        yield return ["Award in PhD"];
        yield return ["Award in EngD"];
        yield return ["ESOL International Level 8"];
    }

    public static IEnumerable<object[]> Level8FalseCases()
    {
        yield return ["Ordinary Diploma in Business"];
        yield return ["This contains PhDX but not the whole word"];
        yield return ["Advanced Research Award"];
    }
}