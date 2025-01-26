using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Jobs.Interfaces;

namespace SFA.DAS.AODP.Jobs.Services.CSV
{
    public class CsvReaderService : ICsvReaderService
    {
        private readonly ILogger<CsvReaderService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public CsvReaderService(ILogger<CsvReaderService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
          
        }

        public List<T> ReadCSVFromFilePath<T, TMap>(string filePath) where TMap : ClassMap<T>
        {
            _logger.LogInformation("Searching for CSV file for processing");

            var fundedCsvRecords = new List<T>();
            if (File.Exists(filePath))
            {
                fundedCsvRecords
                       = ReadCsv<T, TMap>(filePath);
                Console.WriteLine($"Total Records Read: {fundedCsvRecords.Count}");
            }
            else
            {
                _logger.LogError("File not found: {FilePath}", filePath);
            }
            return fundedCsvRecords;
        }

        public async Task<List<T>> ReadCsvFileFromUrlAsync<T, TMap>(string urlFilePath) where TMap : ClassMap<T>
        {
            _logger.LogInformation("Downloading CSV file from url: {UrlFilePath}", urlFilePath);

            var records = new List<T>();

            try
            {
                var response = await GetDataFromUrl(urlFilePath);

                using var approvedResponseStream = await response.Content.ReadAsStreamAsync();

                 records
                    = ReadCsv<T, TMap>(approvedResponseStream);

                _logger.LogInformation("Total Records Read: {fundedCsvRecords}", records.Count);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error downloading CSV file from url: {UrlFilePath}", urlFilePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading CSV file from url: {UrlFilePath}", urlFilePath);
            }
            return records;
        }

        private async Task<HttpResponseMessage> GetDataFromUrl(string approvedUrlFilePath)
        {
            var _httpClient = _httpClientFactory.CreateClient();
            var response = await _httpClient.GetAsync(approvedUrlFilePath);
            response.EnsureSuccessStatusCode();
            return response;
        }

        private List<T> ReadCsv<T, TMap>(dynamic data) where TMap :ClassMap<T>
        {
            using var streamReader = new StreamReader(data);
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
            csvReader.Read();
            csvReader.ReadHeader();
            var customHeaders=csvReader.HeaderRecord.Where(header=>header.Contains("_FundingAvailable")).ToList();
            if (customHeaders.Any())
            {
                var classMap = (TMap)Activator.CreateInstance(typeof(TMap), customHeaders);
                csvReader.Context.RegisterClassMap(classMap);
            }
            else
            {
                csvReader.Context.RegisterClassMap<TMap>();
            }
            return csvReader.GetRecords<T>().ToList();
        }
    }
}

