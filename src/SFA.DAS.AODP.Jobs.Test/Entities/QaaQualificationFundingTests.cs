namespace SFA.DAS.AODP.Jobs.UnitTests.Entities;

public class QaaQualificationFundingTests : UnitTest
{
    [Fact]
    public void Create_SetsFundingScopedValues()
    {
        var qualificationId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var startDate = new DateOnly(2025, 8, 1);
        var endDate = new DateOnly(2026, 7, 31);
        var createdAt = new DateTime(2026, 1, 1);

        var funding = QaaQualificationFunding.Create(
            qualificationId,
            fundingOfferId,
            startDate,
            endDate,
            "Approved",
            createdAt,
            "Seeded");

        Assert.NotEqual(Guid.Empty, funding.Id);
        Assert.Equal(qualificationId, funding.QaaQualificationId);
        Assert.Equal(fundingOfferId, funding.FundingOfferId);
        Assert.Equal(startDate, funding.StartDate);
        Assert.Equal(endDate, funding.EndDate);
        Assert.Equal("Approved", funding.FundingStatus);
        Assert.Equal("Seeded", funding.Comments);
        Assert.Equal(createdAt, funding.CreatedAt);
        Assert.Equal(createdAt, funding.UpdatedAt);
    }
}
