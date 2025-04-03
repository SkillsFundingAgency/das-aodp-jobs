using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Infrastructure.Interfaces;

namespace SFA.DAS.AODP.Data.Repositories.Jobs
{
    public class QualificationsRepository : IQualificationsRepository
    {
        private readonly IApplicationDbContext _context;
        private readonly ILogger<QualificationsRepository> _logger;

        public QualificationsRepository(IApplicationDbContext context, ILogger<QualificationsRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<List<Qualification>> GetQualificationsAsync()
        {
            var qualifications = new List<Qualification>();

            try
            {
                qualifications = await _context.Qualification
                    .AsNoTracking()
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while retrieving Qualifications: {ex.Message}");
            }

            return qualifications;
        }

        public async Task<List<AwardingOrganisation>> GetAwardingOrganisationsAsync()
        {
            var organisations = new List<AwardingOrganisation>();

            try
            {
                organisations = await _context.AwardingOrganisation
                    .AsNoTracking()
                    .OrderByDescending(o => o.RecognitionNumber)
                    .GroupBy(o => o.NameOfqual)
                    .Select(g => g.First())
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while retrieving AwardingOrganisations: {ex.Message}");
            }

            return organisations;
        }

        public async Task TruncateFundingTables()
        {
            try
            {
                await _context.Truncate_FundedQualifications();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error while truncating Funding Tables: {ex.Message}");
            }
        }
    }
}
