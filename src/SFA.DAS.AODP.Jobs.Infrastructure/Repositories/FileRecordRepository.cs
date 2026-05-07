using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Data.Entities.Files;
using SFA.DAS.AODP.Infrastructure.Context;
namespace SFA.DAS.AODP.Data.Repositories.Jobs
{
    public class FileRecordRepository : IFileRecordRepository
    {
        private readonly IApplicationDbContext _context;

        public FileRecordRepository(IApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Gets the single file for a given category.
        /// Assumes there is only one record per category.
        /// </summary>
        public async Task<FileRecord?> GetByCategoryAsync(FileCategory category)
        {
            return await _context.FileRecords
                .SingleOrDefaultAsync(f => f.FileCategory == category);
        }

        /// <summary>
        /// Gets files that are still waiting to be scanned.
        /// Uses a cutoff to avoid polling very old records indefinitely.
        /// </summary>
        public async Task<List<FileRecord>> GetPendingScanAsync(DateTime cutoff)
        {
            return await _context.FileRecords
                .Where(f =>
                    f.ScanResult == MalwareScanStatus.NotScanned &&
                    f.UploadedAt >= cutoff)
                .OrderBy(f => f.UploadedAt)
                .ToListAsync();
        }

        /// <summary>
        /// Updates a file record.
        /// </summary>
        public async Task UpdateAsync(FileRecord file)
        {
            _context.FileRecords.Update(file);
            await _context.SaveChangesAsync();
        }

        public async Task<FileRecord?> GetByPathAsync(string container, string path)
             => await _context.FileRecords
                .SingleOrDefaultAsync(f =>
                f.BlobContainer == container &&
                f.BlobPath == path);

        public async Task InsertAsync(FileRecord file)
        {
            if (file == null)
                throw new ArgumentNullException(nameof(file));

            await _context.FileRecords.AddAsync(file);
            await _context.SaveChangesAsync();
        }
    }
}
