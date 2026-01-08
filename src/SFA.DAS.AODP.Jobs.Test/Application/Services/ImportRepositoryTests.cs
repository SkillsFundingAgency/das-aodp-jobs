using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Repositories;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services;

public class ImportRepositoryTests
{
    [Fact]
    public async Task BulkInsertAsync_WithDefundingList_AddsEntitiesAndSaves()
    {
        // Arrange - use EF in-memory provider
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var repo = new ImportRepository(context);

        var items = new List<DefundingList>
            {
                new DefundingList { Qan = "QAN-1", Title = "T1"},
                new DefundingList { Qan = "QAN-2", Title = "T2"}
            };

        // Act
        await repo.BulkInsertAsync(items, CancellationToken.None);

        // Assert - persisted to context
        var stored = context.DefundingLists.ToList();
        Assert.Equal(2, stored.Count);
        Assert.Contains(stored, s => s.Qan == "QAN-1");
        Assert.Contains(stored, s => s.Qan == "QAN-2");
    }

    [Fact]
    public async Task BulkInsertAsync_WithPldns_AddsEntitiesAndSaves()
    {
        // Arrange - use EF in-memory provider
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var context = new ApplicationDbContext(options);
        var repo = new ImportRepository(context);

        var items = new List<Pldns>
            {
                new Pldns { ImportDate = DateTime.UtcNow, Qan = "qan1" },
                new Pldns { ImportDate = DateTime.UtcNow, Qan = "qan2" }
            };

        // Act
        await repo.BulkInsertAsync(items, CancellationToken.None);

        // Assert - persisted to context
        var stored = context.Pldns.ToList();
        Assert.Equal(2, stored.Count);
    }

    [Fact]
    public async Task BulkInsertAsync_WithNullItems_DoesNotCallSave()
    {
        // Arrange - mock IApplicationDbContext, items null => early return
        var contextMock = new Mock<IApplicationDbContext>();
        var repo = new ImportRepository(contextMock.Object);

        // Act
        await repo.BulkInsertAsync<object>(null!, CancellationToken.None);

        // Assert - SaveChangesAsync should not be invoked
        contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteDuplicateAsync_WithNonApplicationDbContext_ThrowsInvalidOperationException()
    {
        // Arrange - context is not ApplicationDbContext
        var contextMock = new Mock<IApplicationDbContext>();
        var repo = new ImportRepository(contextMock.Object);

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.DeleteDuplicateAsync("sp", null, CancellationToken.None));
    }
}
