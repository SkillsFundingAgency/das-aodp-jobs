using System.Collections.Specialized;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestEase;
using SFA.DAS.AODP.Data;
using SFA.DAS.AODP.Jobs.Client;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Config;
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
            try
            {
                if (parameters == null)
                {
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

        public List<QualificationDTO> ExtractQualificationsList(PaginatedResult<QualificationDTO> paginatedResult)
        {
            return paginatedResult.Results.Select(q => new QualificationDTO
            {
                QualificationNumber = q.QualificationNumber,
                QualificationNumberNoObliques = q.QualificationNumberNoObliques ?? "",
                Title = q.Title,
                Status = q.Status,
                OrganisationName = q.OrganisationName,
                OrganisationAcronym = q.OrganisationAcronym,
                OrganisationRecognitionNumber = q.OrganisationRecognitionNumber,
                Type = q.Type,
                Ssa = q.Ssa,
                Level = q.Level,
                SubLevel = q.SubLevel,
                EqfLevel = q.EqfLevel,
                GradingType = q.GradingType,
                GradingScale = q.GradingScale,
                TotalCredits = q.TotalCredits,
                Tqt = q.Tqt,
                Glh = q.Glh,
                MinimumGlh = q.MinimumGlh,
                MaximumGlh = q.MaximumGlh,
                RegulationStartDate = q.RegulationStartDate,
                OperationalStartDate = q.OperationalStartDate,
                OperationalEndDate = q.OperationalEndDate,
                CertificationEndDate = q.CertificationEndDate,
                ReviewDate = q.ReviewDate,
                OfferedInEngland = q.OfferedInEngland,
                OfferedInNorthernIreland = q.OfferedInNorthernIreland,
                OfferedInternationally = q.OfferedInternationally,
                Specialism = q.Specialism,
                Pathways = q.Pathways,
                AssessmentMethods = q.AssessmentMethods != null
                     ? string.Join(",", q.AssessmentMethods)
                     : null,
                ApprovedForDelfundedProgramme = q.ApprovedForDelfundedProgramme,
                LinkToSpecification = q.LinkToSpecification,
                ApprenticeshipStandardReferenceNumber = q.ApprenticeshipStandardReferenceNumber,
                ApprenticeshipStandardTitle = q.ApprenticeshipStandardTitle,
                RegulatedByNorthernIreland = q.RegulatedByNorthernIreland,
                NiDiscountCode = q.NiDiscountCode,
                GceSizeEquivalence = q.GceSizeEquivalence,
                GcseSizeEquivalence = q.GcseSizeEquivalence,
                EntitlementFrameworkDesignation = q.EntitlementFrameworkDesignation,
                LastUpdatedDate = q.LastUpdatedDate,
                UiLastUpdatedDate = q.UiLastUpdatedDate,
                InsertedDate = q.InsertedDate,
                Version = q.Version,
                AppearsOnPublicRegister = q.AppearsOnPublicRegister,
                OrganisationId = q.OrganisationId,
                LevelId = q.LevelId,
                TypeId = q.TypeId,
                SsaId = q.SsaId,
                GradingTypeId = q.GradingTypeId,
                GradingScaleId = q.GradingScaleId,
                PreSixteen = q.PreSixteen,
                SixteenToEighteen = q.SixteenToEighteen,
                EighteenPlus = q.EighteenPlus,
                NineteenPlus = q.NineteenPlus,
                ImportStatus = "New"
            }).ToList();
        }

        public QualificationsQueryParameters ParseQueryParameters(NameValueCollection query)
        {
            var defaultImportPage = _configuration.Value.DefaultImportPage;
            var defaultImportLimit = _configuration.Value.DefaultImportLimit;

            if (query == null || query.Count == 0)
            {
                _logger.LogInformation($"Url parameters are empty. Defaulting Page: {defaultImportPage} and Limit: {defaultImportLimit}");
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
