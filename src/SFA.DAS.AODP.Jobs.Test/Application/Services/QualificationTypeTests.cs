namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services;

public class QualificationTypeTests
{
    [Theory]
    [InlineData("End-Point Assessment")]
    [InlineData("end-point assessment")]
    [InlineData("END-POINT ASSESSMENT")]
    [InlineData("Apprenticeship Assessment Qualification")]
    [InlineData("apprenticeship assessment qualification")]
    [InlineData("APPRENTICESHIP ASSESSMENT QUALIFICATION")]
    public void IsIneligible_WhenTypeMatchesIneligibleType_ReturnsTrue(string type)
    {
        // Arrange

        // Act
        var result = QualificationType.IsIneligible(type);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Some Other Type")]
    [InlineData("End Point Assessment")]
    [InlineData("Apprenticeship Assessment")]
    [InlineData("End-Point Assessments")]
    [InlineData("Apprenticeship Assessment Qualification ")]
    [InlineData(" End-Point Assessment")]
    public void IsIneligible_WhenTypeDoesNotExactlyMatchIneligibleType_ReturnsFalse(string? type)
    {
        // Arrange

        // Act
        var result = QualificationType.IsIneligible(type);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EndPointAssessment_WhenAccessed_HasExpectedValue()
    {
        // Arrange

        // Act
        var result = QualificationType.EndPointAssessment.Value;

        // Assert
        Assert.Equal("End-Point Assessment", result);
    }

    [Fact]
    public void ApprenticeshipAssessmentQualification_WhenAccessed_HasExpectedValue()
    {
        // Arrange

        // Act
        var result = QualificationType.ApprenticeshipAssessmentQualification.Value;

        // Assert
        Assert.Equal("Apprenticeship Assessment Qualification", result);
    }
}