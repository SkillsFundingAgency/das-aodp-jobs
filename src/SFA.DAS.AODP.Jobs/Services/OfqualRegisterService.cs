using System.Collections.Specialized;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Data;
using SFA.DAS.AODP.Functions.Interfaces;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services
{
    public class OfqualRegisterService : IOfqualRegisterService
    {
        private readonly ILogger<RegulatedQualificationsService> _logger;
        private readonly IOfqualRegisterApi _apiClient;

        public OfqualRegisterService(ILogger<RegulatedQualificationsService> logger, IOfqualRegisterApi apiClient,
            IApplicationDbContext appDbContext)
        {
            _logger = logger;
            _apiClient = apiClient;
        }

        public async Task<RegulatedQualificationsPaginatedResult<RegulatedQualificationDTO>> SearchPrivateQualificationsAsync(RegulatedQualificationsQueryParameters parameters, int page, int limit)
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

        public List<RegulatedQualificationDTO> ExtractQualificationsList(RegulatedQualificationsPaginatedResult<RegulatedQualificationDTO> paginatedResult)
        {
            return paginatedResult.Results.Select(q => new RegulatedQualificationDTO
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

        public RegulatedQualificationsQueryParameters ParseQueryParameters(NameValueCollection query)
        {
            if (query == null || query.Count == 0)
            {
                _logger.LogWarning("Query parameters are empty.");
                return new RegulatedQualificationsQueryParameters();
            }

            return new RegulatedQualificationsQueryParameters
            {
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
