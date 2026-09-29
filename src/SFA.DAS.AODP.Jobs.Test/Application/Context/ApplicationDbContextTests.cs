using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces.Rollover;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Context;

public class ApplicationDbContextTests
{
    [Fact]
    public async Task SaveChangesAsync_WhenDispatchSucceeds_CommitsTransactionAndPersistsChanges()
    {
        // Arrange - EF's InMemory provider isn't relational, so it never opens a real transaction;
        // this needs SQLite to actually exercise the BeginTransaction/Commit path.
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var dispatcher = new Mock<IFundingDomainEventDispatcher>();
        dispatcher
            .Setup(instance => instance.DispatchAsync(
                It.IsAny<ApplicationDbContext>(),
                It.IsAny<IReadOnlyCollection<FundingDomainEvent>>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        await using var context = new ApplicationDbContext(options, dispatcher.Object);
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF");
        context.QualificationFundings.Add(QualificationFunding.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, new DateOnly(2026, 7, 31), null));

        // Act
        var affectedRows = await context.SaveChangesAsync();

        // Assert
        affectedRows.ShouldBeGreaterThan(0);
        dispatcher.Verify(instance => instance.DispatchAsync(
            context,
            It.IsAny<IReadOnlyCollection<FundingDomainEvent>>(),
            It.IsAny<CancellationToken>()), Times.Once);
        (await context.QualificationFundings.CountAsync()).ShouldBe(1);
    }

    [Fact]
    public async Task SaveChangesAsync_WhenDispatchThrows_RollsBackAndPropagates()
    {
        // Arrange
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;
        var dispatcher = new Mock<IFundingDomainEventDispatcher>();
        dispatcher
            .Setup(instance => instance.DispatchAsync(
                It.IsAny<ApplicationDbContext>(),
                It.IsAny<IReadOnlyCollection<FundingDomainEvent>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Reconciliation failed."));
        await using var context = new ApplicationDbContext(options, dispatcher.Object);
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF");
        context.QualificationFundings.Add(QualificationFunding.Create(
            Guid.NewGuid(), Guid.NewGuid(), null, new DateOnly(2026, 7, 31), null));

        // Act / Assert
        var exception = await Should.ThrowAsync<InvalidOperationException>(() => context.SaveChangesAsync());
        exception.Message.ShouldBe("Reconciliation failed.");

        context.ChangeTracker.Clear();
        (await context.QualificationFundings.CountAsync()).ShouldBe(0);
    }
}
