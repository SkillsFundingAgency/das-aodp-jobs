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
}