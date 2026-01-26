using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces;

namespace SFA.DAS.AODP.Infrastructure.Repositories;

public class ImportRepository : IImportRepository
{
    private readonly IApplicationDbContext _context;
    private readonly ILogger<ImportRepository> _logger;

    public ImportRepository(IApplicationDbContext context, ILogger<ImportRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task BulkInsertAsync<T>(IEnumerable<T> items, CancellationToken cancellationToken = default)
    {
        if (items == null) return;

        if (typeof(T) == typeof(DefundingList))
        {
            _context.DefundingLists.AddRange((List<DefundingList>)items);
        }
        else
        {
            _context.Pldns.AddRange((List<Pldns>)items);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteDuplicateAsync(string spName, string? qan = null, CancellationToken cancellationToken = default)
    {
        try
        {
            var qanParam = qan != null ? $"'{qan}'" : "NULL";
            var sql = $"EXEC {spName} @qan = {qanParam}";
            await _context.DeleteDuplicateAsync(sql, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error while deleting duplicates from {spName}: {ex.Message}");
        }
    }
}