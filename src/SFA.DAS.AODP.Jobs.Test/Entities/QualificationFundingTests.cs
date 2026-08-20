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
        Assert.Single(qualificationFunding.FundingDomainEvents);
    }

    [Fact]
    public void UpdateFunding_UpdatesFieldsAndRecordsAnotherEvent()
    {
        var qualificationFunding = QualificationFunding.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, null);
        qualificationFunding.ClearFundingDomainEvents();

        qualificationFunding.UpdateFunding(new DateOnly(2026, 8, 1), new DateOnly(2028, 7, 31), "Extended");

        Assert.Equal(new DateOnly(2026, 8, 1), qualificationFunding.StartDate);
        Assert.Equal(new DateOnly(2028, 7, 31), qualificationFunding.EndDate);
        Assert.Equal("Extended", qualificationFunding.Comments);
        Assert.Single(qualificationFunding.FundingDomainEvents);
    }

    [Fact]
    public void Archive_SetsEndDateAndCommentsAndRecordsEvent()
    {
        var qualificationFunding = QualificationFunding.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, null);
        qualificationFunding.ClearFundingDomainEvents();

        qualificationFunding.Archive(new DateOnly(2027, 7, 31), "No longer offered");

        Assert.Equal(new DateOnly(2027, 7, 31), qualificationFunding.EndDate);
        Assert.Equal("No longer offered", qualificationFunding.Comments);
        Assert.Single(qualificationFunding.FundingDomainEvents);
    }

    [Fact]
    public void MoveToQualificationVersion_UpdatesIdAndRecordsPreviousVersionOnTheEvent()
    {
        var previousVersionId = Guid.NewGuid();
        var newVersionId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var qualificationFunding = QualificationFunding.Create(previousVersionId, fundingOfferId, null, null, null);
        qualificationFunding.ClearFundingDomainEvents();

        qualificationFunding.MoveToQualificationVersion(newVersionId);

        Assert.Equal(newVersionId, qualificationFunding.QualificationVersionId);
        var domainEvent = Assert.Single(qualificationFunding.FundingDomainEvents);
        var changeEvent = Assert.IsType<FundingChangedDomainEvent>(domainEvent);
        Assert.Equal(newVersionId, changeEvent.SourceQualificationId);
        Assert.Equal(fundingOfferId, changeEvent.FundingOfferId);
        Assert.Equal(previousVersionId, changeEvent.PreviousSourceQualificationId);
    }

    [Fact]
    public void ClearFundingDomainEvents_EmptiesTheRecordedEvents()
    {
        var qualificationFunding = QualificationFunding.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, null);
        Assert.NotEmpty(qualificationFunding.FundingDomainEvents);

        qualificationFunding.ClearFundingDomainEvents();

        Assert.Empty(qualificationFunding.FundingDomainEvents);
    }
}