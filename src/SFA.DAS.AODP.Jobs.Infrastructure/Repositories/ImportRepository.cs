using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces;
using System.Data;

namespace SFA.DAS.AODP.Infrastructure.Repositories;

public class ImportRepository : IImportRepository
{
    private readonly IApplicationDbContext _context;

    public ImportRepository(IApplicationDbContext context)
    {
        _context = context;
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

    public async Task<int> DeleteDuplicateAsync(string spName, string? qan = null, CancellationToken cancellationToken = default)
    {
        if (!(_context is ApplicationDbContext dbContext))
            throw new InvalidOperationException("Unable to execute stored procedure: unexpected DbContext type.");

        var conn = dbContext.Database.GetDbConnection();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = spName;
        cmd.CommandType = CommandType.StoredProcedure;

        var param = cmd.CreateParameter();
        param.ParameterName = "@Qan";
        param.Value = (object?)qan ?? DBNull.Value;
        cmd.Parameters.Add(param);

        if (conn.State != ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        {
            var deleted = reader["DeletedRows"];
            if (deleted != DBNull.Value && int.TryParse(deleted.ToString(), out var deletedCount))
            {
                return deletedCount;
            }
        }

        return 0;
    }
}