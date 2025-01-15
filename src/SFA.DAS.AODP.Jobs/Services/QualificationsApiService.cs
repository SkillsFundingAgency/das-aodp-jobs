using SFA.DAS.AODP.Data;
using SFA.DAS.AODP.Functions.Interfaces;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class QualificationsApiService : IQualificationsApiService
    {
        private readonly IOfqualRegisterApi _apiClient;

        public QualificationsApiService(IOfqualRegisterApi apiClient)
        {
            _apiClient = apiClient;
        }

        public async Task<PaginatedResult<RegisteredQualification>> SearchPrivateQualificationsAsync(RegisteredQualificationQueryParameters parameters, int page, int limit)
        {
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
    }
}
