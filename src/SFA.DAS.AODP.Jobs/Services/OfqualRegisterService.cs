﻿using System.Collections.Specialized;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Data;
using SFA.DAS.AODP.Jobs.Client;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services
{

    public class OfqualRegisterService : IOfqualRegisterService
    {
        private readonly ILogger<QualificationsService> _logger;
        private readonly IOfqualRegisterApi _apiClient;
        private readonly IConfiguration _configuration;

        public OfqualRegisterService(ILogger<QualificationsService> logger, IOfqualRegisterApi apiClient,
            IConfiguration configuration)
        {
            _logger = logger;
            _apiClient = apiClient;
            _configuration = configuration;
        }

        public async Task<RegulatedQualificationsPaginatedResult<QualificationDTO>> SearchPrivateQualificationsAsync(RegulatedQualificationsQueryParameters parameters)
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

        public List<QualificationDTO> ExtractQualificationsList(RegulatedQualificationsPaginatedResult<QualificationDTO> paginatedResult)
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

        public RegulatedQualificationsQueryParameters ParseQueryParameters(NameValueCollection query)
        {
            int defaultPage = int.Parse(_configuration["DefaultPage"]);
            int defaultLimit = int.Parse(_configuration["DefaultLimit"]);

            if (query == null || query.Count == 0)
            {
                _logger.LogWarning($"Url parameters are empty. Defaulting Page: {defaultPage} and Limit: {defaultLimit}");
            }

            return new RegulatedQualificationsQueryParameters
            {
                Page = ParseInt(query["page"], defaultPage),
                Limit = ParseInt(query["limit"], defaultLimit),
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
