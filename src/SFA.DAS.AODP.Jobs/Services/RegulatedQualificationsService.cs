using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class RegulatedQualificationsService : IRegulatedQualificationsService
    {
        private readonly ILogger<RegulatedQualificationsService> _logger;
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IMapper _mapper;

        public RegulatedQualificationsService(ILogger<RegulatedQualificationsService> logger, IApplicationDbContext applicationDbContext, IMapper mapper, 
            IApplicationDbContext appDbContext)
        {
            _logger = logger;
            _mapper = mapper;
            _applicationDbContext = appDbContext;
        }

        public async Task CompareAndUpdateQualificationsAsync(List<RegulatedQualificationDTO> importedQualifications, List<RegulatedQualificationDTO> processedQualifications)
        {
            var processedQualificationsDict = processedQualifications.ToDictionary(p => p.Id);

            var columnsToCompare = new Dictionary<string, Func<RegulatedQualificationDTO, object>>()
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
                if (processedQualificationsDict.TryGetValue(importRow.Id, out var processedRow))
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

        public async Task SaveRegulatedQualificationsAsync(List<RegulatedQualificationDTO> qualifications)
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
                throw; // Rethrow the exception to let the caller handle it
            }
        }

    }
}
