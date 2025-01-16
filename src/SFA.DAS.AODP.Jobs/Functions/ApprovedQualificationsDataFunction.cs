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
    public class ApprovedQualificationsDataFunction
    {
        private readonly ILogger<ApprovedQualificationsDataFunction> _logger;
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly ICsvReaderService _csvReaderService;

        public ApprovedQualificationsDataFunction(ILogger<ApprovedQualificationsDataFunction> logger, IApplicationDbContext applicationDbContext, ICsvReaderService csvReaderService)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _csvReaderService = csvReaderService;
        }

        [Function("ApprovedQualificationsDataFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "api/approvedQualificationsImport")] HttpRequestData req)
        {
            string? urlFilePath = Environment.GetEnvironmentVariable("ApprovedQualificationsImportUrl");

            if (string.IsNullOrEmpty(urlFilePath))
            {
                _logger.LogInformation("Environment variable 'ApprovedQualificationsImportUrl' is not set or empty.");
                var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                return notFoundResponse;
            }

            var approvedQualifications = await _csvReaderService.ReadCsvFileFromUrlAsync<ApprovedQualificationsImport, ApprovedQualificationsImportClassMap>(urlFilePath);

            if (approvedQualifications.Any())
            {
                await _applicationDbContext.BulkInsertAsync(approvedQualifications);
            }
            else
            {
                _logger.LogInformation("No CSV file found at this location {FilePath}", urlFilePath);
                var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                return notFoundResponse;
            }

            var successResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            _logger.LogInformation("{Count} records imported successfully", approvedQualifications.Count);
            await successResponse.WriteStringAsync($"{approvedQualifications.Count} records imported successfully");
            return successResponse;
        }
    }
}


