using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Data.Entities.Rollover;
using SFA.DAS.AODP.Data.Entities.Rollover.Enums;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces.Rollover;
using SFA.DAS.AODP.Infrastructure.Repositories.Rollover;
using SFA.DAS.AODP.Infrastructure.Services;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services.Rollover;

public class FundingDomainEventDispatcherTests
{
    private static readonly DateTime Now = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task DispatchAsync_WhenOfqualFundingMoves_PreservesCandidateDecision()
    {
        // Arrange
        await using var context = CreateContext();
        var oldVersionId = Guid.NewGuid();
        var newVersionId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        var candidate = RolloverCandidate.CreateInitialRound(
            RolloverSourceTypes.Ofqual,
            oldVersionId,
            fundingOfferId,
            "2025/26",
            Now.AddDays(-1),
            new DateOnly(2026, 7, 31));
        SetPrivateProperty(
            candidate,
            nameof(RolloverCandidate.RolloverStatus),
            RolloverStatus.Extended);
        SetPrivateProperty(
            candidate,
            nameof(RolloverCandidate.NewFundingEndDate),
            new DateTime(2027, 7, 31));
        context.RolloverCandidates.Add(candidate);
        await context.SaveChangesAsync();
        var sut = new FundingDomainEventDispatcher(
            new FakeSystemClockService(),
            NullLogger<FundingDomainEventDispatcher>.Instance);

        // Act
        await sut.DispatchAsync(
            context,
            [new FundingChangedDomainEvent(
                RolloverSourceTypes.Ofqual,
                newVersionId,
                fundingOfferId,
                oldVersionId)],
            CancellationToken.None);

        // Assert
        candidate.SourceQualificationId.ShouldBe(newVersionId);
        candidate.RolloverStatus.ShouldBe(RolloverStatus.Extended);
        candidate.NewFundingEndDate.ShouldBe(new DateTime(2027, 7, 31));
        candidate.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task SaveChangesAsync_WhenQaaFundingChanges_DispatchesAtContextBoundary()
    {
        // Arrange
        var dispatcher = new Mock<IFundingDomainEventDispatcher>();
        dispatcher
            .Setup(instance => instance.DispatchAsync(
                It.IsAny<ApplicationDbContext>(),
                It.IsAny<IReadOnlyCollection<FundingDomainEvent>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new ApplicationDbContext(options, dispatcher.Object);
        var qualificationId = Guid.NewGuid();
        var fundingOfferId = Guid.NewGuid();
        context.QaaQualificationFundings.Add(QaaQualificationFunding.Create(
            qualificationId,
            fundingOfferId,
            null,
            new DateOnly(2026, 7, 31),
            "Approved",
            Now));

        // Act
        await context.SaveChangesAsync();

        // Assert
        dispatcher.Verify(instance => instance.DispatchAsync(
            context,
            It.Is<IReadOnlyCollection<FundingDomainEvent>>(events =>
                events.OfType<FundingChangedDomainEvent>().Any(domainEvent =>
                    domainEvent.SourceType == RolloverSourceTypes.Qaa &&
                    domainEvent.SourceQualificationId == qualificationId &&
                    domainEvent.FundingOfferId == fundingOfferId)),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    private static void SetPrivateProperty<T>(T instance, string propertyName, object value)
    {
        typeof(T).GetProperty(propertyName)!.SetValue(instance, value);
    }

    private sealed class FakeSystemClockService : ISystemClockService
    {
        public DateTime UtcNow => Now;
        public DateOnly Today => DateOnly.FromDateTime(Now);
    }
}
