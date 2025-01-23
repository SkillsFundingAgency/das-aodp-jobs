using System.Diagnostics;
using AutoMapper;
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
        private readonly IMapper _autoMapper;

        public FundedQualificationsDataFunction(ILogger<FundedQualificationsDataFunction> logger, IApplicationDbContext applicationDbContext, ICsvReaderService csvReaderService,IMapper autoMapper)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _csvReaderService = csvReaderService;
            _autoMapper = autoMapper;
        }

        [Function("ApprovedQualificationsDataFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "api/approvedQualificationsImport")] HttpRequestData req)
        {
            var insertTimer = new Stopwatch();
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

            insertTimer.Start();
            await _applicationDbContext.BulkInsertAsync<FundedQualification>(_autoMapper.Map<List<FundedQualification>>(approvedQualifications));
            _logger.LogInformation($"{approvedQualifications.Count()} Total imported in {insertTimer.Elapsed.Seconds} seconds", approvedQualifications.Count());

            var archivedQualifications = await _csvReaderService.ReadQualifications(archivedQualificationsUrl);

            await _applicationDbContext.BulkInsertAsync<FundedQualification>(_autoMapper.Map<List<FundedQualification>>(archivedQualifications));
            _logger.LogInformation($"{archivedQualifications.Count()} Total imported in {insertTimer.Elapsed.Seconds} seconds", archivedQualifications.Count());
            

            var successResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            _logger.LogInformation($"{approvedQualifications.Count()+archivedQualifications.Count()} Total imported in {insertTimer.Elapsed.TotalSeconds} seconds", approvedQualifications.Count());
            await successResponse.WriteStringAsync($"Inserted {approvedQualifications.Count()+archivedQualifications.Count()} records. Time Taken {insertTimer.Elapsed.TotalSeconds} seconds");
            insertTimer.Stop();
            return successResponse;
        }
    }
}


