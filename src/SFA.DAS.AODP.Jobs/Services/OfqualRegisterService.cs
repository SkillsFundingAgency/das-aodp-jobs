namespace SFA.DAS.AODP.Jobs.Services;

public class OfqualRegisterService(
    ILogger<QualificationsService> logger,
    IOfqualRegisterApi apiClient,
    IOptions<AodpJobsConfiguration> configuration)
    : IOfqualRegisterService
{
    public async Task<PaginatedResult<QualificationDTO>> SearchPrivateQualificationsAsync(QualificationsQueryParameters parameters)
    {
        logger.LogInformation($"[{nameof(OfqualRegisterService)}] -> [{nameof(SearchPrivateQualificationsAsync)}] -> Starting search for qualifications using ofqual api...");

        try
        {
            if (parameters == null)
            {
                logger.LogError($"[{nameof(OfqualRegisterService)}] -> [{nameof(SearchPrivateQualificationsAsync)}] -> Parameters cannot be null...");
                throw new ArgumentNullException(nameof(parameters), "Parameters cannot be null.");
            }

            return await apiClient.SearchPrivateQualificationsAsync(
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
            logger.LogError(ex, $"[{nameof(OfqualRegisterService)}] -> [{nameof(SearchPrivateQualificationsAsync)}] -> An error occurred while retrieving qualification records.");
            throw;
        }
    }

    public List<QualificationDTO> ExtractQualificationsList(PaginatedResult<QualificationDTO> paginatedResult)
    {
        logger.LogInformation($"[{nameof(OfqualRegisterService)}] -> [{nameof(ExtractQualificationsList)}] -> Extracting qualifications from ofqual api response data...");

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
        logger.LogInformation($"[{nameof(OfqualRegisterService)}] -> [{nameof(ParseQueryParameters)}] -> Parsing function query parameters...");

        var defaultImportPage = configuration.Value.DefaultImportPage;
        var defaultImportLimit = configuration.Value.DefaultImportLimit;

        if (query == null || query.Count == 0)
        {
            logger.LogInformation($"[{nameof(OfqualRegisterService)}] -> [{nameof(ParseQueryParameters)}] -> Url parameters are empty. Defaulting Page to {defaultImportPage} and Limit to {defaultImportLimit}");

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