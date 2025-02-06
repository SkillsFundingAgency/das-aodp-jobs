using System.Text.Json;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class QualificationsService : IQualificationsService
    {
        private readonly ILogger<QualificationsService> _logger;
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IMapper _mapper;

        public QualificationsService(ILogger<QualificationsService> logger, IApplicationDbContext applicationDbContext, IMapper mapper, 
            IApplicationDbContext appDbContext)
        {
            _logger = logger;
            _mapper = mapper;
            _applicationDbContext = appDbContext;
        }

        public async Task SaveQualificationsStagingAsync(List<string> qualificationsJson)
        {
            try
            {
                _logger.LogInformation("Saving regulated qualification records...");

                var qualificationsJsonEntities = qualificationsJson
                    .Select(json => new QualificationImportStaging
                    {
                        Id = Guid.NewGuid(),
                        JsonData = json,
                        CreatedDate = DateTime.Now
                    }).ToList();

                await _applicationDbContext.BulkInsertAsync(qualificationsJsonEntities);
                
                //_applicationDbContext.QualificationsImportStaging.AddRange(qualificationsJsonEntities);
                //await _applicationDbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully saved regulated qualification records.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving regulated qualification records.");
                throw;
            }
        }

        public async Task<List<QualificationDTO>> GetStagedQualificationsBatchAsync(int batchSize, int processedCount)
        {
            try
            {
                _logger.LogInformation($"Retrieving next batch of {batchSize} staged qualifications from record {processedCount}...");

                var stagedQualifications = await _applicationDbContext.QualificationImportStaging
                    .OrderBy(q => q.Id)
                    .Skip(processedCount)
                    .Take(batchSize)
                    .ToListAsync();

                return stagedQualifications
                    .Select(q => JsonSerializer.Deserialize<QualificationDTO>(
                        q.JsonData,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
                    .Where(dto => dto != null)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving qualification records.");
                throw;
            }
        }

    }
}
