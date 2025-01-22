using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Data;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Functions.Interfaces;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class RegulatedQualificationsService : IRegulatedQualificationsService
    {
        private readonly ILogger<RegulatedQualificationsService> _logger;
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly IOfqualRegisterApi _apiClient;
        private readonly IMapper _mapper;

        public RegulatedQualificationsService(ILogger<RegulatedQualificationsService> logger, IOfqualRegisterApi apiClient, 
            IApplicationDbContext applicationDbContext, IMapper mapper, IApplicationDbContext appDbContext)
        {
            _logger = logger;
            _apiClient = apiClient;
            _mapper = mapper;
            _applicationDbContext = appDbContext;
        }

        public async Task<RegulatedQualificationsPaginatedResult<RegulatedQualification>> SearchPrivateQualificationsAsync(RegulatedQualificationsQueryParameters parameters, int page, int limit)
        {
            if (parameters == null)
            {
                throw new ArgumentNullException(nameof(parameters), "Parameters cannot be null.");
            }

            return await _apiClient.SearchPrivateQualificationsAsync(
                parameters.Title,
                page,
                limit,
                parameters.AssessmentMethods,
                parameters.GradingTypes,
                parameters.AwardingOrganisations,
                parameters.Availability,
                parameters.QualificationTypes,
                parameters.QualificationLevels,
                parameters.NationalAvailability,
                parameters.SectorSubjectAreas,
                parameters.MinTotalQualificationTime,
                parameters.MaxTotalQualificationTime,
                parameters.MinGuidedLearningHours,
                parameters.MaxGuidedLearningHours
            );
        }

        public async Task CompareAndUpdateQualificationsAsync(List<RegulatedQualification> importedQualifications, List<RegulatedQualification> processedQualifications)
        {
            var columnsToCompare = new Dictionary<string, Func<RegulatedQualification, object>>()
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
                var processedRow = processedQualifications.FirstOrDefault(p => p.Id == importRow.Id);

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

            await _applicationDbContext.SaveChangesAsync();
        }

    }
}
