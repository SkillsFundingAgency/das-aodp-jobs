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
        var loggerMock = new Mock<ILogger<ImportRepository>>();
        var repo = new ImportRepository(context, loggerMock.Object);

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
        var loggerMock = new Mock<ILogger<ImportRepository>>();
        var repo = new ImportRepository(context, loggerMock.Object);

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
        var loggerMock = new Mock<ILogger<ImportRepository>>();
        var repo = new ImportRepository(contextMock.Object, loggerMock.Object);

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
        var loggerMock = new Mock<ILogger<ImportRepository>>();
        var repo = new ImportRepository(contextMock.Object, loggerMock.Object);

        // Act / Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => repo.DeleteDuplicateAsync("sp", null, CancellationToken.None));
    }

    //[Fact]
    //public async Task DeleteDuplicateAsync_WhenSqlReturnsDeletedRows_ReturnsParsedInt()
    //{
    //    // Arrange - use Sqlite in-memory connection so we can execute SQL and return a scalar row
    //    var connection = new SqliteConnection("DataSource=:memory:");
    //    await connection.OpenAsync();

    //    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    //        .UseSqlite(connection)
    //        .Options;

    //    await using (var context = new ApplicationDbContext(options))
    //    {
    //        // Ensure database created (not strictly required for this raw SQL)
    //        await context.Database.EnsureCreatedAsync();

    //        var loggerMock = new Mock<ILogger<ImportRepository>>();
    //        var repo = new ImportRepository(context, loggerMock.Object);

    //        // We will pass raw SELECT as spName. The repository sets CommandType = StoredProcedure,
    //        // but Sqlite will still execute the command text.
    //        var result = await repo.DeleteDuplicateAsync("SELECT 7 AS DeletedRows", null, CancellationToken.None);

    //        Assert.Equal(7, result);
    //    }

    //    await connection.CloseAsync();
    //}

    //[Fact]
    //public async Task DeleteDuplicateAsync_WhenSqlReturnsNoRows_ReturnsZero()
    //{
    //    var connection = new SqliteConnection("DataSource=:memory:");
    //    await connection.OpenAsync();

    //    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    //        .UseSqlite(connection)
    //        .Options;

    //    await using (var context = new ApplicationDbContext(options))
    //    {
    //        await context.Database.EnsureCreatedAsync();

    //        var loggerMock = new Mock<ILogger<ImportRepository>>();
    //        var repo = new ImportRepository(context, loggerMock.Object);

    //        // Query returns no rows
    //        var result = await repo.DeleteDuplicateAsync("SELECT NULL AS DeletedRows WHERE 0=1", null, CancellationToken.None);

    //        Assert.Equal(0, result);
    //    }

    //    await connection.CloseAsync();
    //}

    //[Fact]
    //public async Task DeleteDuplicateAsync_WhenDeletedRowsNotInt_ReturnsZero()
    //{
    //    var connection = new SqliteConnection("DataSource=:memory:");
    //    await connection.OpenAsync();

    //    var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    //        .UseSqlite(connection)
    //        .Options;

    //    await using (var context = new ApplicationDbContext(options))
    //    {
    //        await context.Database.EnsureCreatedAsync();

    //        var loggerMock = new Mock<ILogger<ImportRepository>>();
    //        var repo = new ImportRepository(context, loggerMock.Object);

    //        // Use parameter in SQL so we can exercise parameter usage.
    //        // If parameter value is non-numeric, parsing will fail and method should return 0.
    //        var result = await repo.DeleteDuplicateAsync("SELECT @Qan AS DeletedRows", "not-an-int", CancellationToken.None);

    //        Assert.Equal(0, result);
    //    }

    //    await connection.CloseAsync();
    //}
}
