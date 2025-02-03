using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;

namespace SFA.DAS.AODP.Infrastructure.Context
{
    public interface IApplicationDbContext
    {
        DbSet<ActionType> ActionTypes { get; set; }
        DbSet<LifecycleStage> LifecycleStages { get; set; }
        DbSet<Organisation> Organisation { get; set; }
        DbSet<ProcessStatus> ProcessStatuses { get; set; }
        DbSet<Qualification> Qualification { get; set; }
        DbSet<Qualifications> FundedQualifications { get; set; }
        DbSet<QualificationDiscussionHistory> QualificationDiscussionHistories { get; set; }
        DbSet<QualificationOffer> QualificationOffers { get; set; }
        DbSet<QualificationVersion> QualificationVersions { get; set; }
        DbSet<VersionFieldChange> VersionFieldChanges { get; set; }
        DbSet<StagedQualifications> StagedQualifications { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;
        Task DeleteTable<T>() where T : class;
    }
}
