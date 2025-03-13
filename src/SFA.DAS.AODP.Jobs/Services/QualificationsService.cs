using System.Reflection.Metadata.Ecma335;
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

        public QualificationsService(ILogger<QualificationsService> logger, IMapper mapper, 
            IApplicationDbContext appDbContext)
        {
            _logger = logger;
            _mapper = mapper;
            _applicationDbContext = appDbContext;
        }

        public async Task AddQualificationsStagingRecords(List<string> qualificationsJson)
        {
            try
            {
                _logger.LogInformation($"[{nameof(QualificationsService)}] -> [{nameof(AddQualificationsStagingRecords)}] -> Adding regulated qualification records...");

                var qualificationsJsonEntities = qualificationsJson
                    .Select(json => new QualificationImportStaging
                    {
                        JsonData = json,
                        Id = Guid.NewGuid(),
                        CreatedDate = DateTime.Now
                    }).ToList();

                await _applicationDbContext.QualificationImportStaging.AddRangeAsync(qualificationsJsonEntities);

                _logger.LogInformation($"[{nameof(QualificationsService)}] -> [{nameof(AddQualificationsStagingRecords)}] ->  Successfully added regulated qualification records.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{nameof(QualificationsService)}] -> [{nameof(AddQualificationsStagingRecords)}] -> An error occurred while adding regulated qualification records.");
                throw;
            }
        }

        public async Task SaveQualificationsStagingAsync()
        {
            try
            {
                _logger.LogInformation($"[{nameof(QualificationsService)}] -> [{nameof(SaveQualificationsStagingAsync)}] -> Saving regulated qualification records...");

                await _applicationDbContext.SaveChangesAsync();

                _logger.LogInformation($"[{nameof(QualificationsService)}] -> [{nameof(SaveQualificationsStagingAsync)}] -> Successfully saved regulated qualification records.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{nameof(QualificationsService)}] -> [{nameof(SaveQualificationsStagingAsync)}] -> An error occurred while saving regulated qualification records.");
                throw;
            }
        }

        public async Task<List<QualificationDTO>> GetStagedQualificationsBatchAsync(int batchSize, int processedCount)
        {
            try
            {
                _logger.LogInformation($"[{nameof(QualificationsService)}] -> [{nameof(GetStagedQualificationsBatchAsync)}] -> Retrieving next batch of {batchSize} staged qualifications from record {processedCount}...");

                var stagedQualifications = await _applicationDbContext.QualificationImportStaging
                    .OrderBy(q => q.Id)
                    .Skip(processedCount)
                    .Take(batchSize)
                    .ToListAsync();              

                return stagedQualifications
                    .Select(q => JsonSerializer.Deserialize<QualificationDTO>(
                        q.JsonData ?? "",
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? throw new Exception($"[{nameof(QualificationsService)}] -> [{nameof(GetStagedQualificationsBatchAsync)}] -> Unable to serialize import json into dto for id {q.Id}"))
                    .Where(dto => dto != null)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{nameof(QualificationsService)}] -> [{nameof(GetStagedQualificationsBatchAsync)}] -> An error occurred while retrieving batch of import records.");
                throw;
            }
        }

    }
}
