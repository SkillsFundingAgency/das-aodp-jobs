namespace SFA.DAS.AODP.Jobs.UnitTests.Entities;

public class QualificationFundingTests : UnitTest
{
    [Fact]
    public void QualificationFunding_Create_EnsureCreatedCorrectly()
    {
        // Arrange
        var qualificationVersion = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var startDate = new DateOnly(2025, 01, 20);
        var endDate = new DateOnly(2027, 01, 20);

        // Act
        var qualificationFunding = QualificationFunding.Create(qualificationVersion, fundingOfferId, startDate, endDate, "comments");

        // Assert
        Assert.Equal(qualificationVersion, qualificationFunding.QualificationVersionId);
        Assert.Equal(fundingOfferId, qualificationFunding.FundingOfferId);
        Assert.Equal(startDate, qualificationFunding.StartDate);
        Assert.Equal(endDate, qualificationFunding.EndDate);
        Assert.Equal("comments", qualificationFunding.Comments);
    }

    [Fact]
    public void QualificationFunding_Update_EnsureCreatedCorrectly()
    {
        // Arrange
        var qualificationVersion = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var startDate = new DateOnly(2025, 01, 20);
        var endDate = new DateOnly(2027, 01, 20);
        var qualificationFunding = QualificationFunding.Create(qualificationVersion, fundingOfferId, startDate, endDate, "comments");
        var expected = QualificationFunding.Create(qualificationVersion, fundingOfferId, startDate.AddYears(1), endDate.AddYears(2), "updated comments");
        expected.Id = qualificationFunding.Id;

        // Act
        var result = qualificationFunding.Update(startDate.AddYears(1), endDate.AddYears(2), "updated comments");

        // Assert
        result.ShouldBeEquivalentTo(expected);
    }
}