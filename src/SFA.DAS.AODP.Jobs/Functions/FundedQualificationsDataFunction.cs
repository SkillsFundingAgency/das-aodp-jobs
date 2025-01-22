using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;

namespace SFA.DAS.AODP.Functions
{
    public class FundedQualificationsDataFunction
    {
        private readonly ILogger<FundedQualificationsDataFunction> _logger;
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly ICsvReaderService _csvReaderService;

        public FundedQualificationsDataFunction(ILogger<FundedQualificationsDataFunction> logger, IApplicationDbContext applicationDbContext, ICsvReaderService csvReaderService)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _csvReaderService = csvReaderService;
        }

        [Function("ApprovedQualificationsDataFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "api/approvedQualificationsImport")] HttpRequestData req)
        {
            var stopWatch = new Stopwatch();
            string? approvedQualificationsUrl = Environment.GetEnvironmentVariable("FundedQualificationsImportUrl");
            string? archivedQualificationsUrl = Environment.GetEnvironmentVariable("ArchivedFundedQualificationsImportUrl");

            if (string.IsNullOrEmpty(approvedQualificationsUrl) || string.IsNullOrEmpty(archivedQualificationsUrl))
            {
                _logger.LogInformation("Environment variable 'ApprovedQualificationsImportUrl' is not set or empty.");
                var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                return notFoundResponse;
            }

            await _applicationDbContext.DeleteFromTable("FundedQualifications");

            var approvedQualifications = await _csvReaderService.ReadQualifications(approvedQualificationsUrl);

            stopWatch.Start();
            await _applicationDbContext.BulkInsertAsync<FundedQualification>(approvedQualifications);
            stopWatch.Stop();
            _logger.LogInformation($"{approvedQualificationsUrl.Count()} records imported in {stopWatch.ElapsedMilliseconds / 1000}");

            var archivedQualifications = await _csvReaderService.ReadQualifications(archivedQualificationsUrl);

            stopWatch.Restart();
            await _applicationDbContext.BulkInsertAsync<FundedQualification>(archivedQualifications);
            stopWatch.Stop();
            _logger.LogInformation($"{archivedQualificationsUrl.Count()} records imported in {stopWatch.ElapsedMilliseconds / 1000}");

            var successResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            _logger.LogInformation("{Count} records imported successfully", approvedQualifications.Count());
            await successResponse.WriteStringAsync($"{approvedQualifications.Count()} records imported successfully");
            return successResponse;
        }
    }
}


