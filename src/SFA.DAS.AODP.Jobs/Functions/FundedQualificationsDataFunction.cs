using System.Diagnostics;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Services.CSV;

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
            string? approvedUrlFilePath = Environment.GetEnvironmentVariable("FundedQualificationsImportUrl");
            string? archivedUrlFilePath = Environment.GetEnvironmentVariable("ArchivedFundedQualifcationsImportUrl");

            if (string.IsNullOrEmpty(approvedUrlFilePath) || string.IsNullOrEmpty(archivedUrlFilePath))
            {
                _logger.LogInformation("Environment variable 'ApprovedQualificationsImportUrl' is not set or empty.");
                var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                return notFoundResponse;
            }

            var approvedQualifications = await _csvReaderService.ReadApprovedAndArchivedFromUrlAsync<FundedQualificationsImport, FundedQualificationsImportClassMap>(approvedUrlFilePath, archivedUrlFilePath);
            var stopWatch = new Stopwatch();
            
            if (approvedQualifications.Any())
            {
                await _applicationDbContext.TruncateTable("FundedQualificationsImport");
                stopWatch.Start();
                await _applicationDbContext.BulkInsertAsync(approvedQualifications);
                stopWatch.Stop();
            }
            else
            {
                _logger.LogInformation("No CSV file found at this location {FilePath}");
                var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                return notFoundResponse;
            }
            _logger.LogInformation($"{approvedQualifications.Count} records imported in {stopWatch.ElapsedMilliseconds/1000}");

            var successResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            _logger.LogInformation("{Count} records imported successfully", approvedQualifications.Count);
            await successResponse.WriteStringAsync($"{approvedQualifications.Count} records imported successfully");
            return successResponse;
        }
    }
}


