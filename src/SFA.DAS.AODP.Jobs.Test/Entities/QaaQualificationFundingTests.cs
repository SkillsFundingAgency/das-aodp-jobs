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
        Assert.Single(funding.FundingDomainEvents);
    }

    [Fact]
    public void Create_WhenQaaQualificationIdIsEmpty_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            QaaQualificationFunding.Create(Guid.Empty, Guid.NewGuid(), null, null, "Approved", DateTime.UtcNow));

        Assert.Equal("qaaQualificationId", exception.ParamName);
    }

    [Fact]
    public void Create_WhenFundingOfferIdIsEmpty_ThrowsArgumentException()
    {
        var exception = Assert.Throws<ArgumentException>(() =>
            QaaQualificationFunding.Create(Guid.NewGuid(), Guid.Empty, null, null, "Approved", DateTime.UtcNow));

        Assert.Equal("fundingOfferId", exception.ParamName);
    }

    [Fact]
    public void Update_UpdatesFieldsAndRecordsAnotherEvent()
    {
        var funding = QaaQualificationFunding.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, null, "Approved", DateTime.UtcNow);
        funding.ClearFundingDomainEvents();
        var updatedAt = DateTime.UtcNow.AddDays(30);

        funding.Update(new DateOnly(2026, 8, 1), new DateOnly(2028, 7, 31), "Extended", updatedAt, "Comments");

        Assert.Equal(new DateOnly(2026, 8, 1), funding.StartDate);
        Assert.Equal(new DateOnly(2028, 7, 31), funding.EndDate);
        Assert.Equal("Extended", funding.FundingStatus);
        Assert.Equal("Comments", funding.Comments);
        Assert.Equal(updatedAt, funding.UpdatedAt);
        Assert.Single(funding.FundingDomainEvents);
    }

    [Fact]
    public void Archive_SetsEndDateAndRecordsEvent()
    {
        var funding = QaaQualificationFunding.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, null, "Approved", DateTime.UtcNow);
        funding.ClearFundingDomainEvents();
        var updatedAt = DateTime.UtcNow.AddDays(30);

        funding.Archive(new DateOnly(2027, 7, 31), updatedAt, "No longer offered");

        Assert.Equal(new DateOnly(2027, 7, 31), funding.EndDate);
        Assert.Equal("No longer offered", funding.Comments);
        Assert.Equal(updatedAt, funding.UpdatedAt);
        Assert.Single(funding.FundingDomainEvents);
    }

    [Fact]
    public void ClearFundingDomainEvents_EmptiesTheRecordedEvents()
    {
        var funding = QaaQualificationFunding.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, null, "Approved", DateTime.UtcNow);
        Assert.NotEmpty(funding.FundingDomainEvents);

        funding.ClearFundingDomainEvents();

        Assert.Empty(funding.FundingDomainEvents);
    }
}
