using Shouldly;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services;

public class KeyFieldTests : UnitTest
{
    public static IEnumerable<object[]> ExpectedKeyFields()
    {
        yield return [KeyField.OrganisationName, "OrganisationName"];
        yield return [KeyField.Title, "Title"];
        yield return [KeyField.Level, "Level"];
        yield return [KeyField.Type, "Type"];
        yield return [KeyField.TotalCredits, "TotalCredits"];
        yield return [KeyField.Ssa, "Ssa"];
        yield return [KeyField.GradingType, "GradingType"];
        yield return [KeyField.OfferedInEngland, "OfferedInEngland"];
        yield return [KeyField.PreSixteen, "PreSixteen"];
        yield return [KeyField.SixteenToEighteen, "SixteenToEighteen"];
        yield return [KeyField.EighteenPlus, "EighteenPlus"];
        yield return [KeyField.NineteenPlus, "NineteenPlus"];
        yield return [KeyField.IntentionToSeekFundingInEngland, "IntentionToSeekFundingInEngland"];
        yield return [KeyField.Glh, "GLH"];
        yield return [KeyField.MinimumGlh, "MinimumGLH"];
        yield return [KeyField.Tqt, "TQT"];
        yield return [KeyField.OperationalEndDate, "OperationalEndDate"];
        yield return [KeyField.OfferedInternationally, "OfferedInternationally"];
        yield return [KeyField.EligibleForFunding, "EligibleForFunding"];
    }

    public static IEnumerable<object[]> CaseInsensitiveChangedFields()
    {
        yield return ["Title"];
        yield return ["title"];
        yield return ["TITLE"];
        yield return ["tItLe"];

        yield return ["OrganisationName"];
        yield return ["organisationname"];
        yield return ["ORGANISATIONNAME"];
        yield return ["oRgAnIsAtIoNnAmE"];

        yield return ["GLH"];
        yield return ["glh"];
        yield return ["GlH"];

        yield return ["MinimumGLH"];
        yield return ["minimumglh"];
        yield return ["MINIMUMGLH"];
        yield return ["MinimumGlh"];

        yield return ["TQT"];
        yield return ["tqt"];
        yield return ["TqT"];

        yield return ["OperationalEndDate"];
        yield return ["operationalenddate"];
        yield return ["OPERATIONALENDDATE"];
        yield return ["Operationalenddate"];
    }

    [Theory]
    [MemberData(nameof(ExpectedKeyFields))]
    public void StaticKeyField_ShouldHaveExpectedKey(KeyField keyField, string expectedKey)
    {
        // Arrange / Act
        var actualKey = keyField.Key;

        // Assert
        actualKey.ShouldBe(expectedKey);
    }

    [Theory]
    [MemberData(nameof(ExpectedKeyFields))]
    public void ToString_ShouldReturnKey(KeyField keyField, string expectedKey)
    {
        // Arrange / Act
        var result = keyField.ToString();

        // Assert
        result.ShouldBe(expectedKey);
    }

    [Fact]
    public void All_ShouldContainEveryKeyField()
    {
        // Arrange
        var expectedKeyFields = ExpectedKeyFields()
            .Select(x => (KeyField)x[0])
            .ToList();

        // Act
        var result = KeyField.All;

        // Assert
        result.ShouldBe(expectedKeyFields);
    }

    [Fact]
    public void All_ShouldContainEveryKeyFieldInExpectedOrder()
    {
        // Arrange
        var expectedKeys = ExpectedKeyFields()
            .Select(x => (string)x[1])
            .ToList();

        // Act
        var result = KeyField.All
            .Select(x => x.Key)
            .ToList();

        // Assert
        result.ShouldBe(expectedKeys);
    }

    [Fact]
    public void HaveKeyFieldsChanged_ShouldReturnTrue_WhenChangedFieldMatchesKeyField()
    {
        // Arrange
        var changedFields = new List<string>
        {
            "Title"
        };

        // Act
        var result = KeyField.HaveKeyFieldsChanged(changedFields);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void HaveKeyFieldsChanged_ShouldReturnTrue_WhenAnyChangedFieldMatchesKeyField()
    {
        // Arrange
        var changedFields = new List<string>
        {
            "SomeNonKeyField",
            "AnotherNonKeyField",
            "OperationalEndDate"
        };

        // Act
        var result = KeyField.HaveKeyFieldsChanged(changedFields);

        // Assert
        result.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(CaseInsensitiveChangedFields))]
    public void HaveKeyFieldsChanged_ShouldReturnTrue_WhenChangedFieldMatchesKeyFieldRegardlessOfCase(string changedField)
    {
        // Arrange
        var changedFields = new List<string>
        {
            changedField
        };

        // Act
        var result = KeyField.HaveKeyFieldsChanged(changedFields);

        // Assert
        result.ShouldBeTrue();
    }

    [Fact]
    public void HaveKeyFieldsChanged_ShouldReturnFalse_WhenChangedFieldOnlyPartiallyMatchesKeyField()
    {
        // Arrange
        var changedFields = new List<string>
        {
            "TitleChanged",
            "QualificationTitle",
            "GL",
            "Minimum",
            "Operational"
        };

        // Act
        var result = KeyField.HaveKeyFieldsChanged(changedFields);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void HaveKeyFieldsChanged_ShouldReturnFalse_WhenChangedFieldHasExtraWhitespace()
    {
        // Arrange
        var changedFields = new List<string>
        {
            " Title ",
            " GLH ",
            "OperationalEndDate "
        };

        // Act
        var result = KeyField.HaveKeyFieldsChanged(changedFields);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void HaveKeyFieldsChanged_ShouldReturnFalse_WhenNoChangedFieldsMatchKeyFields()
    {
        // Arrange
        var changedFields = new List<string>
        {
            "SomeNonKeyField",
            "AnotherNonKeyField"
        };

        // Act
        var result = KeyField.HaveKeyFieldsChanged(changedFields);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void HaveKeyFieldsChanged_ShouldReturnFalse_WhenChangedFieldsIsEmpty()
    {
        // Arrange
        var changedFields = new List<string>();

        // Act
        var result = KeyField.HaveKeyFieldsChanged(changedFields);

        // Assert
        result.ShouldBeFalse();
    }

    [Fact]
    public void KeyField_ShouldUseRecordValueEquality()
    {
        // Arrange
        var first = new KeyField("Title");
        var second = new KeyField("Title");

        // Act / Assert
        first.ShouldBe(second);
    }

    [Fact]
    public void KeyField_ShouldBeCaseSensitive_WhenUsingRecordValueEquality()
    {
        // Arrange
        var first = new KeyField("Title");
        var second = new KeyField("title");

        // Act / Assert
        first.ShouldNotBe(second);
    }

    [Fact]
    public void KeyField_ShouldNotBeEqual_WhenKeysAreDifferent()
    {
        // Arrange
        var first = new KeyField("Title");
        var second = new KeyField("Level");

        // Act / Assert
        first.ShouldNotBe(second);
    }

    [Fact]
    public void HaveKeyFieldsChanged_ShouldThrowArgumentNullException_WhenChangedFieldsIsNull()
    {
        // Arrange
        IList<string> changedFields = null!;

        // Act
        var exception = Should.Throw<ArgumentNullException>(() =>
            KeyField.HaveKeyFieldsChanged(changedFields));

        // Assert
        exception.ShouldNotBeNull();
    }
}