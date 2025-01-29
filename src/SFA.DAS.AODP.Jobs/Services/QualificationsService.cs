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
            var columnsToCompare = GetColumnsToCompare();

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

        public async Task SaveRegulatedQualificationsAsync(List<QualificationDTO> qualifications)
        {
            try
            {
                _logger.LogInformation("Saving regulated qualification records...");

                var qualificationsEntities = _mapper.Map<List<RegulatedQualificationsImport>>(qualifications);

                await _applicationDbContext.BulkInsertAsync(qualificationsEntities);              
                //_applicationDbContext.RegulatedQualificationsImport.AddRange(qualificationsEntities);
                //await _applicationDbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully saved regulated qualification records.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving regulated qualification records.");
                throw; 
            }
        }

        public async Task SaveRegulatedQualificationsStagingAsync(List<string> qualificationsJson)
        {
            try
            {
                _logger.LogInformation("Saving regulated qualification records...");

                var qualificationsJsonEntities = _mapper.Map<List<RegulatedQualificationsImportStaging>>(qualificationsJson);

                await _applicationDbContext.BulkInsertAsync(qualificationsJsonEntities);
                
                //_applicationDbContext.RegulatedQualificationsImportStaging.AddRange(qualificationsJsonEntities);
                //await _applicationDbContext.SaveChangesAsync();

                _logger.LogInformation("Successfully saved regulated qualification records.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while saving regulated qualification records.");
                throw;
            }
        }

        public async Task<List<QualificationDTO>> GetAllProcessedRegulatedQualificationsAsync()
        {
            _logger.LogInformation("Retreiving all processed regulated qualification records...");

            var processedQualificationsEntities = await _applicationDbContext.ProcessedRegulatedQualifications.ToListAsync();

            return _mapper.Map<List<QualificationDTO>>(processedQualificationsEntities);
        }

        private static Dictionary<string, Func<QualificationDTO, object>> GetColumnsToCompare()
        {
            return new Dictionary<string, Func<QualificationDTO, object>>
            {
                { "OrganisationName", dto => dto.OrganisationName },
                { "Title", dto => dto.Title },
                { "Level", dto => dto.Level },
                { "Type", dto => dto.Type },
                { "TotalCredits", dto => dto.TotalCredits ?? 0 },
                { "Ssa", dto => dto.Ssa },
                { "GradingType", dto => dto.GradingType ?? string.Empty },
                { "OfferedInEngland", dto => dto.OfferedInEngland },
                { "PreSixteen", dto => dto.PreSixteen ?? false },
                { "SixteenToEighteen", dto => dto.SixteenToEighteen ?? false },
                { "EighteenPlus", dto => dto.EighteenPlus ?? false },
                { "NineteenPlus", dto => dto.NineteenPlus ?? false },
                //{ "OfferedInEngland", dto => dto.OfferedInEngland },
                { "QualGlh", dto => dto.Glh ?? 0 },
                { "QualMinimumGLH", dto => dto.MinimumGlh ?? 0 },
                { "TQT", dto => dto.Tqt ?? 0 },
                { "OperationalEndDate", dto => dto.OperationalEndDate ?? DateTime.MinValue },
                { "LastUpdatedDate", dto => dto.LastUpdatedDate },
                { "Version", dto => dto.Version ?? 0 },
                { "OfferedInternationally", dto => dto.OfferedInternationally ?? false }
            };
        }

    }
}
