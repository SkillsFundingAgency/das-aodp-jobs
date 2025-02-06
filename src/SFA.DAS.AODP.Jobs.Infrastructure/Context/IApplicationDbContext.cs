using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Data.Entities;

namespace SFA.DAS.AODP.Infrastructure.Context
{
    public interface IApplicationDbContext
    {
        DbSet<ActionType> ActionTypes { get; set; }
        DbSet<LifecycleStage> LifecycleStages { get; set; }
        DbSet<AwardingOrganisation> AwardingOrganisation { get; set; }
        DbSet<ProcessStatus> ProcessStatus { get; set; }
        DbSet<Qualification> Qualification { get; set; }
        DbSet<Qualifications> FundedQualifications { get; set; }
        DbSet<QualificationDiscussionHistory> QualificationDiscussionHistories { get; set; }
        DbSet<QualificationOffer> QualificationOffers { get; set; }
        DbSet<QualificationVersions> QualificationVersions { get; set; }
        DbSet<VersionFieldChange> VersionFieldChanges { get; set; }
        DbSet<QualificationImportStaging> QualificationImportStaging { get; set; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task BulkInsertAsync<T>(IEnumerable<T> entities, CancellationToken cancellationToken = default) where T : class;
        Task TruncateTable<T>() where T : class;
    }
}