using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Infrastructure.Context;
using SFA.DAS.AODP.Jobs.Data.CSV;

namespace SFA.DAS.AODP.Functions
{
    public class ApprovedQualificationsDataFunction(ILoggerFactory loggerFactory, IApplicationDbContext applicationDbContext)
    {
        private readonly ILogger _logger = loggerFactory.CreateLogger<ApprovedQualificationsDataFunction>();
        private readonly IApplicationDbContext _applicationDbContext = applicationDbContext;

        [Function("ApprovedQualificationsDataFunction")]
        public async Task<HttpResponseData> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get","post", Route = "api/approvedQualificationsImport")] HttpRequestData req)
        {
            _logger.LogInformation("Searching for CSV file for processing");

            string filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "approved.csv");

            var approvedQualifications = new List<ApprovedQualificationsImport>();

            if (File.Exists(filePath))
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Context.RegisterClassMap<ApprovedQualificationsImportClassMap>();

                approvedQualifications = csv.GetRecords<ApprovedQualificationsImport>().ToList();
                Console.WriteLine($"Total Records Read: {approvedQualifications.Count}");

                var stopwatch = new Stopwatch();
                stopwatch.Start();
                
                await _applicationDbContext.BulkInsertAsync(approvedQualifications);
                stopwatch.Stop();
                Console.WriteLine($"Total Time Taken: {stopwatch.ElapsedMilliseconds} ms");
            }
            else
            {
                _logger.LogError("File not found: {FilePath}", filePath);
                var notFoundResponse = req.CreateResponse(System.Net.HttpStatusCode.NotFound);
                await notFoundResponse.WriteStringAsync("CSV file not found");
                return notFoundResponse;
            }

            var successResponse = req.CreateResponse(System.Net.HttpStatusCode.OK);
            await successResponse.WriteStringAsync($"{approvedQualifications.Count} imported successfully");
            return successResponse;
        }
    }
}
