using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces;

namespace SFA.DAS.AODP.Infrastructure.Repositories
{
    public class QualificationVersionRepository : IQualificationVersionRepository
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<QualificationVersionRepository> _logger;

        public QualificationVersionRepository(IApplicationDbContext context, ILogger<QualificationVersionRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<QualificationLookupItem>> GetLatestQualificationVersionSnapshotsAsync()
        {
            var data = await _context.QualificationVersions
                .AsNoTracking()
                .Include(v => v.Qualification)
                .ToListAsync();

            return data
                .GroupBy(v => v.QualificationId)
                .Select(g =>
                    g.OrderByDescending(v => v.Version).First())
                .Select(v => new QualificationLookupItem(
                    v.Qualification.Qan,
                    v.QualificationId,
                    v.AwardingOrganisationId))
                .ToList();
        }

        public sealed record QualificationLookupItem(
            string Qan,
            Guid? QualificationId,
            Guid? AwardingOrganisationId
        );
    }
}
