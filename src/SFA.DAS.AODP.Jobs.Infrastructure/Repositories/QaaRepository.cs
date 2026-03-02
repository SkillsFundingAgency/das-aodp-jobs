using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;

namespace SFA.DAS.AODP.Infrastructure.Repositories;

/// <summary>
/// Default implementation for <see cref="IQaaRepository"/>.
/// </summary>
/// <param name="logger"></param>
/// <param name="dbContextFactory"></param>
public class QaaRepository(ILogger<QaaRepository> logger, IDbContextFactory<ApplicationDbContext> dbContextFactory) : IQaaRepository
{
    private readonly ILogger<QaaRepository> _logger = logger;
    private readonly IDbContextFactory<ApplicationDbContext> _applicationDbContext = dbContextFactory;

    /// <inheritdoc/>.
    public async Task<int> RunPrerequisitesForImportAsync(CancellationToken cancellationToken)
    {
        await using var context = await _applicationDbContext.CreateDbContextAsync(cancellationToken);

        _logger.LogInformation("Executing delete on {TableName}", nameof(RegulatedQaaQualification));

        return await context.RegulatedQaaQualification.ExecuteDeleteAsync(cancellationToken);
    }

    /// <inheritdoc/>.
    public async Task RunImportAsync(IEnumerable<RegulatedQaaQualification> entries, CancellationToken cancellationToken)
    {
        await using var context = await _applicationDbContext.CreateDbContextAsync(cancellationToken);

        context.StartingBulkInsert();

        await context.RegulatedQaaQualification.AddRangeAsync(entries, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        context.FinishedBulkInsert();
    }
}