namespace SFA.DAS.AODP.Infrastructure.Repositories;

public class ImportRepository(IApplicationDbContext context, ILogger<ImportRepository> logger)
    : IImportRepository
{
    public async Task BulkInsertAsync<T>(IEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        if (items == null) return;

        if (typeof(T) == typeof(DefundingList))
        {
            context.DefundingLists.AddRange((List<DefundingList>)items);
        }
        else
        {
            context.Pldns.AddRange((List<Pldns>)items);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteDuplicateAsync(string spName, string? qan = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var qanParam = qan != null ? $"'{qan}'" : "NULL";
            var sql = $"EXEC {spName} @qan = {qanParam}";
            await context.DeleteDuplicateAsync(sql, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while deleting duplicates from {SpName}", spName);
        }
    }
}