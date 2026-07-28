using System.Collections.Specialized;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class OfqualRegisterService : IOfqualRegisterService
    {
        private readonly ILogger<QualificationsService> _logger;
        private readonly IOfqualRegisterApi _apiClient;
        private readonly IOptions<AodpJobsConfiguration> _configuration;

        public OfqualRegisterService(ILogger<QualificationsService> logger, IOfqualRegisterApi apiClient,
             IOptions<AodpJobsConfiguration> configuration)
        {
            _logger = logger;
            _apiClient = apiClient;
            _configuration = configuration;
        }

        public async Task<PaginatedResult<QualificationDTO>> SearchPrivateQualificationsAsync(QualificationsQueryParameters parameters)
        {
            _logger.LogInformation($"[{nameof(OfqualRegisterService)}] -> [{nameof(SearchPrivateQualificationsAsync)}] -> Starting search for qualifications using ofqual api...");

            try
            {
                if (parameters == null)
                {
                    _logger.LogError($"[{nameof(OfqualRegisterService)}] -> [{nameof(SearchPrivateQualificationsAsync)}] -> Parameters cannot be null...");
                    throw new ArgumentNullException(nameof(parameters), "Parameters cannot be null.");
                }

                return await _apiClient.SearchPrivateQualificationsAsync(
                    parameters.Title,
                    parameters.Page,
                    parameters.Limit,
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
            catch (ApiException ex)
            {
                _logger.LogError(ex, $"[{nameof(OfqualRegisterService)}] -> [{nameof(SearchPrivateQualificationsAsync)}] -> An error occurred while retrieving qualification records.");
                throw;
            }
        }

        public QualificationsQueryParameters ParseQueryParameters(NameValueCollection query)
        {
            _logger.LogInformation($"[{nameof(OfqualRegisterService)}] -> [{nameof(ParseQueryParameters)}] -> Parsing function query parameters...");

            var defaultImportPage = _configuration.Value.DefaultImportPage;
            var defaultImportLimit = _configuration.Value.DefaultImportLimit;

            if (query == null || query.Count == 0)
            {
                _logger.LogInformation($"[{nameof(OfqualRegisterService)}] -> [{nameof(ParseQueryParameters)}] -> Url parameters are empty. Defaulting Page to {defaultImportPage} and Limit to {defaultImportLimit}");

                return new QualificationsQueryParameters
                {
                    Page = defaultImportPage,
                    Limit = defaultImportLimit
                };
            }

            return new QualificationsQueryParameters
            {
                Page = ParseInt(query["page"], defaultImportPage),
                Limit = ParseInt(query["limit"], defaultImportLimit),
                Title = query["title"],
                AssessmentMethods = query["assessmentMethods"],
                GradingTypes = query["gradingTypes"],
                AwardingOrganisations = query["awardingOrganisations"],
                Availability = query["availability"],
                QualificationTypes = query["qualificationTypes"],
                QualificationLevels = query["qualificationLevels"],
                NationalAvailability = query["nationalAvailability"],
                SectorSubjectAreas = query["sectorSubjectAreas"],
                MinTotalQualificationTime = ParseNullableInt(query["minTotalQualificationTime"] ?? ""),
                MaxTotalQualificationTime = ParseNullableInt(query["maxTotalQualificationTime"] ?? ""),
                MinGuidedLearningHours = ParseNullableInt(query["minGuidedLearninghours"] ?? ""),
                MaxGuidedLearningHours = ParseNullableInt(query["maxGuidedLearninghours"] ?? "")
            };
        }

        private int ParseInt(string value, int defaultValue) =>
            int.TryParse(value, out var result) ? result : defaultValue;

        private int? ParseNullableInt(string value) =>
            int.TryParse(value, out var result) ? (int?)result : null;
    }
}
