using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using RestEase;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Models.Qualification;
using System.Diagnostics;

namespace SFA.DAS.AODP.Functions.Functions
{
    public class RegisteredQualificationsDataFunction
    {
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly ILogger<RegisteredQualificationsDataFunction> _logger;
        private readonly IQualificationsApiService _qualificationsApiService;

        public RegisteredQualificationsDataFunction(
            ILogger<RegisteredQualificationsDataFunction> logger, 
            IApplicationDbContext appDbContext, 
            IQualificationsApiService qualificationsApiService)
        {
            _logger = logger;
            _applicationDbContext = appDbContext;
            _qualificationsApiService = qualificationsApiService;
        }

        [Function("RegisteredQualificationsDataFunction")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "gov/RegisteredQualificationsImport")] HttpRequest req)
        {
            _logger.LogInformation($"Processing {nameof(RegisteredQualificationsDataFunction)} request...");

            try
            {
                int page = 1;
                int limit = 5000;
                int totalProcessed = 0;

                var queryParameters = ParseQueryParameters(req.Query);

                // debug
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                while (true)
                {
                    var paginatedResult = await _qualificationsApiService.SearchPrivateQualificationsAsync(queryParameters, page, limit);

                    if (paginatedResult.Results == null || !paginatedResult.Results.Any())
                    {
                        _logger.LogInformation("No more qualifications to process.");
                        break;
                    }

                    _logger.LogInformation($"Processing page {page}. Retrieved {paginatedResult.Results.Count} qualifications.");

                    var qualifications = paginatedResult.Results.Select(q => new RegisteredQualificationsImport
                    {
                        QualificationNumber = q.QualificationNumber,
                        QualificationNumberNoObliques = q.QualificationNumberNoObliques,
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
                        InsertedDate= q.InsertedDate,
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

                    // Save qualifications to the database using bulk insert
                    await _applicationDbContext.BulkInsertAsync(qualifications);

                    totalProcessed += qualifications.Count;

                    if (paginatedResult.Results.Count < limit)
                    {
                        _logger.LogInformation("Reached the end of the results set.");
                        break;
                    }

                    page++;
                }

                // debug
                stopwatch.Stop();
                _logger.LogInformation($"Total Time Taken: {stopwatch.ElapsedMilliseconds} ms");

                _logger.LogInformation($"Total qualifications processed: {totalProcessed}");
                return new OkObjectResult($"Successfully processed {totalProcessed} qualifications.");
            }
            catch (ApiException ex)
            {
                _logger.LogError($"");
                return new StatusCodeResult((int)ex.StatusCode);
            }
            catch (SystemException ex)
            {
                _logger.LogError($"");
                return new StatusCodeResult(500);
            }
        }

        private RegisteredQualificationQueryParameters ParseQueryParameters(IQueryCollection query)
        {
            return new RegisteredQualificationQueryParameters
            {
                Title = query["title"],
                PageNumber = ParseInt(query["page"], 1),
                PageSize = ParseInt(query["limit"], 10),
                AssessmentMethods = query["assessmentMethods"],
                GradingTypes = query["gradingTypes"],
                AwardingOrganisations = query["awardingOrganisations"],
                Availability = query["availability"],
                QualificationTypes = query["qualificationTypes"],
                QualificationLevels = query["qualificationLevels"],
                NationalAvailability = query["nationalAvailability"],
                SectorSubjectAreas = query["sectorSubjectAreas"],
                MinTotalQualificationTime = ParseNullableInt(query["minTotalQualificationTime"]),
                MaxTotalQualificationTime = ParseNullableInt(query["maxTotalQualificationTime"]),
                MinGuidedLearningHours = ParseNullableInt(query["minGuidedLearninghours"]),
                MaxGuidedLearningHours = ParseNullableInt(query["maxGuidedLearninghours"])
            };
        }

        private int ParseInt(string value, int defaultValue) =>
            int.TryParse(value, out var result) ? result : defaultValue;

        private int? ParseNullableInt(string value) =>
            int.TryParse(value, out var result) ? (int?)result : null;

    }
}
