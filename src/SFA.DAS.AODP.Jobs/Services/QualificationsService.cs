﻿using System.Text.Json;
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

        public async Task CompareAndUpdateQualificationsAsync(List<QualificationDTO> importedQualifications, List<QualificationDTO> processedQualifications)
        {
            var columnsToCompare = new Dictionary<string, Func<QualificationDTO, object>>()
            {
                { "OrganisationName", x => x.OrganisationName },
                { "Title", x => x.Title },
                { "QualificationLevelCode", x => x.Level },
                { "QualificationType", x => x.Type },
                { "QualCredit", x => x.TotalCredits },
                { "QualSSADescription", x => x.Ssa },
                { "OverallGradingType", x => x.GradingType },
                { "OfferedInEngland", x => x.OfferedInEngland },
                { "PreSixteen", x => x.PreSixteen },
                { "SixteenToEighteen", x => x.SixteenToEighteen },
                { "EighteenPlus", x => x.EighteenPlus },
                { "NineteenPlus", x => x.NineteenPlus },
                { "FundingInEngland", x => x.OfferedInEngland },
                { "QualGLH", x => x.Glh },
                { "QualMinimumGLH", x => x.MinimumGlh },
                { "TQT", x => x.Tqt },
                { "OperationalEndDate", x => x.OperationalEndDate },
                { "LastUpdatedDate", x => x.LastUpdatedDate },
                { "Version", x => x.Version },
                { "OfferedInternationally", x => x.OfferedInternationally }
            };

            foreach (var importRow in importedQualifications)
            {
                var processedRow = processedQualifications.FirstOrDefault(p => p.QualificationNumberNoObliques == importRow.QualificationNumberNoObliques);

                if (processedRow != null)
                {
                    var changedFields = new List<string>();

                    foreach (var column in columnsToCompare)
                    {
                        var importValue = column.Value(importRow);
                        var processedValue = column.Value(processedRow);

                        if (!Equals(importValue, processedValue))
                        {
                            changedFields.Add(column.Key);
                        }
                    }

                    if (changedFields.Any())
                    {
                        importRow.ChangedFields = string.Join(", ", changedFields);
                        importRow.ImportStatus = "Updated";
                    }
                }
            }

            // only save updated records
            if (importedQualifications.Any(q => q.ImportStatus == "Updated"))
            {
                await _applicationDbContext.SaveChangesAsync();
            }
        }

        public async Task SaveQualificationsStagingAsync(List<string> qualificationsJson)
        {
            try
            {
                _logger.LogInformation("Saving regulated qualification records...");

                var qualificationsJsonEntities = _mapper.Map<List<StagedQualifications>>(qualificationsJson);

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

        public async Task<List<QualificationDTO>> GetStagedQualifcationsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving staged qualifications...");

                var stagedQualifications = await _applicationDbContext.StagedQualifications.ToListAsync();

                return stagedQualifications
                    .Select(q => JsonSerializer.Deserialize<QualificationDTO>(q.JsonData, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }))
                    .Where(dto => dto != null)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving regulated qualification records.");
                throw;
            }
        }

    }
}
