using System.Diagnostics;
using AutoMapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SAF.DAS.AODP.Models.Qualification;
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
        private readonly IMapper _mapper;

        public FundedQualificationsDataFunction(ILogger<FundedQualificationsDataFunction> logger, IApplicationDbContext applicationDbContext, ICsvReaderService csvReaderService, IMapper mapper)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _csvReaderService = csvReaderService;
            _mapper = mapper;
        }

        [Function("ApprovedQualificationsDataFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "api/approvedQualificationsImport")] HttpRequestData req)
        {
            string? approvedUrlFilePath = Environment.GetEnvironmentVariable("FundedQualificationsImportUrl");
            string? archivedUrlFilePath = Environment.GetEnvironmentVariable("ArchivedFundedQualificationsImportUrl");

            if (string.IsNullOrEmpty(approvedUrlFilePath) || string.IsNullOrEmpty(archivedUrlFilePath))
            {
                _logger.LogInformation("Environment variable 'ApprovedQualificationsImportUrl' is not set or empty.");
                var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                return notFoundResponse;
            }
            var approvedQualifications = await _csvReaderService.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(approvedUrlFilePath);
            var stopWatch = new Stopwatch();
            if (approvedQualifications.Any())
            {
                await _applicationDbContext.DeleteTable<FundedQualification>();

                await WriteQualifications(approvedQualifications, stopWatch);
            }
            else
            {
                _logger.LogInformation("No data found found in approved qualifications csv");
                var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                return notFoundResponse;
            }

            var archivedQualifications = await _csvReaderService.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(archivedUrlFilePath);
            if (archivedQualifications.Any())
            {
                await WriteQualifications(archivedQualifications, stopWatch);
            }
            else
            {
                _logger.LogInformation("No data found in archived qualifications csv");
                var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                return notFoundResponse;
            }
            _logger.LogInformation($"{archivedQualifications.Count()} records imported in {stopWatch.ElapsedMilliseconds / 1000}");

            var successResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            _logger.LogInformation($"{archivedQualifications.Count()} archived records imported successfully");
            await successResponse.WriteStringAsync($"{approvedQualifications.Count()} approved qualifications imported \n{archivedQualifications.Count()} archived qualifications imported\n{approvedQualifications.Count() + archivedQualifications.Count()} Total Records");
            return successResponse;
        }

        private async Task WriteQualifications(List<FundedQualificationDTO> approvedQualifications, Stopwatch stopWatch)
        {
            stopWatch.Restart();
            await _applicationDbContext.BulkInsertAsync(_mapper.Map<List<FundedQualification>>(approvedQualifications));
            stopWatch.Stop();
        }
    }
}