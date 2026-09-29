
using SFA.DAS.AODP.Common.Enum;
using SFA.DAS.AODP.Data.Entities.Files;

namespace SFA.DAS.AODP.Data.Repositories.Jobs;

public interface IFileRecordRepository
{
    /// <summary>
    /// Gets the file record for a given category.
    /// There should only ever be one.
    /// </summary>
    Task<FileRecord?> GetByCategoryAsync(FileCategory category);

    /// <summary>
    /// Gets the list of files pending a malware scan.
    /// </summary>
    Task<List<FileRecord>> GetPendingScanAsync(DateTime cutoff);
    
    /// <summary>
    /// Updates the file record.
    /// </summary>
    Task UpdateAsync(FileRecord file);


    Task<FileRecord?> GetByPathAsync(string container, string path);

    Task InsertAsync(FileRecord file);
}