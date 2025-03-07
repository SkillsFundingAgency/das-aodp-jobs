using System.Diagnostics;
using System.Net;
using AutoMapper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Interfaces;
using SFA.DAS.AODP.Jobs.Services.CSV;
using Microsoft.EntityFrameworkCore;
using SFA.DAS.AODP.Jobs.Enum;

namespace SFA.DAS.AODP.Functions
{
    public class FundedQualificationsDataFunction
    {
        private readonly ILogger<FundedQualificationsDataFunction> _logger;
        private readonly IApplicationDbContext _applicationDbContext;
        private readonly ICsvReaderService _csvReaderService;
        private readonly ILoggerFactory _loggerFactory;
        private readonly IMapper _mapper;

        public FundedQualificationsDataFunction(ILogger<FundedQualificationsDataFunction> logger, IApplicationDbContext applicationDbContext, ICsvReaderService csvReaderService, 
            IMapper mapper, ILoggerFactory loggerFactory)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _csvReaderService = csvReaderService;
            _mapper = mapper;
            _loggerFactory = loggerFactory;
        }

        [Function("ApprovedQualificationsDataFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "api/approvedQualificationsImport")] HttpRequestData req)
        {
            string? approvedUrlFilePath = Environment.GetEnvironmentVariable("FundedQualificationsImportUrl");
            string? archivedUrlFilePath = Environment.GetEnvironmentVariable("ArchivedFundedQualificationsImportUrl");
            var fundedQualificationsImportClassMaplogger = _loggerFactory.CreateLogger<FundedQualificationsImportClassMap>();

            if (string.IsNullOrEmpty(approvedUrlFilePath) || string.IsNullOrEmpty(archivedUrlFilePath))
            {
                _logger.LogInformation("Environment variable 'ApprovedQualificationsImportUrl' is not set or empty.");
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                return notFoundResponse;
            }

            var qualifications = await _applicationDbContext.Qualification
                .AsNoTracking()
                .ToListAsync();

            // order organisations by recognition number desc and select by highest recoginition number
            var organisations = await _applicationDbContext.AwardingOrganisation
                .AsNoTracking()
                .OrderByDescending(o => o.RecognitionNumber)
                .GroupBy(o => o.NameOfqual)
                .Select(g => g.First())
                .ToListAsync();

            var stopWatch = new Stopwatch();

            var approvedQualifications = await _csvReaderService.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(approvedUrlFilePath, qualifications, organisations, fundedQualificationsImportClassMaplogger);

            if (approvedQualifications.Any())
            {
                await _applicationDbContext.Truncate_FundedQualifications();

                await WriteQualifications(approvedQualifications, stopWatch);
            }
            else
            {
                _logger.LogInformation("No data found found in approved qualifications csv");
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                return notFoundResponse;
            }

            var archivedQualifications = await _csvReaderService.ReadCsvFileFromUrlAsync<FundedQualificationDTO, FundedQualificationsImportClassMap>(archivedUrlFilePath, qualifications, organisations, fundedQualificationsImportClassMaplogger);
            if (archivedQualifications.Any())
            {
                await WriteQualifications(archivedQualifications, stopWatch);
            }
            else
            {
                _logger.LogInformation("No data found in archived qualifications csv");
                var notFoundResponse = req.CreateResponse(HttpStatusCode.NotFound);
                return notFoundResponse;
            }
            _logger.LogInformation($"{archivedQualifications.Count()} records imported in {stopWatch.ElapsedMilliseconds / 1000}");

            var successResponse = req.CreateResponse(HttpStatusCode.OK);
            _logger.LogInformation($"{archivedQualifications.Count()} archived records imported successfully");
            await successResponse.WriteStringAsync($"{approvedQualifications.Count()} approved qualifications imported \n{archivedQualifications.Count()} archived qualifications imported\n{approvedQualifications.Count() + archivedQualifications.Count()} Total Records");
            return successResponse;
        }

        private async Task WriteQualifications(List<FundedQualificationDTO> qualifications, Stopwatch stopWatch)
        {
            stopWatch.Restart();

            const int _batchSize = 1000;

            for (int i = 0; i < qualifications.Count; i += _batchSize)
            {
                var batch = qualifications
                    .Skip(i)
                    .Take(_batchSize)
                    .ToList();

                var entities = _mapper.Map<List<Qualifications>>(batch);

                await _applicationDbContext.FundedQualifications.AddRangeAsync(entities);
            }

            await _applicationDbContext.SaveChangesAsync();

            stopWatch.Stop();
        }
    }
}