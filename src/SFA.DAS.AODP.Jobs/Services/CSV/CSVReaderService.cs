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
        private readonly HttpClient _httpClient;

        public CsvReaderService(ILogger<CsvReaderService> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public List<T> ReadCSVFromFilePath<T, TMap>(string filePath) where TMap : ClassMap<T>
        {
            _logger.LogInformation("Searching for CSV file for processing");

            var records = new List<T>();
            if (File.Exists(filePath))
            {
                var approvedCsvData
                       = ReadCsv<T, TMap>(filePath);
                Console.WriteLine($"Total Records Read: {records.Count}");
            }
            else
            {
                _logger.LogError("File not found: {FilePath}", filePath);
            }
            return records;
        }

        public async Task<List<T>> ReadApprovedAndArchivedFromUrlAsync<T, TMap>(string approvedUrl, string archivedUrl) where TMap : ClassMap<T>
        {
            _logger.LogInformation("Downloading CSV file from url: {ApprovedUrlFilePath} {ArchivedUrlFilePath}", approvedUrl, archivedUrl);

            var totalRecords = new List<T>();

            try
            {
                HttpResponseMessage response;
                response = await GetDataFromUrl(approvedUrl);

                using var approvedResponseStream = await response.Content.ReadAsStreamAsync();

                var approvedCsvData
                    = ReadCsv<T, TMap>(approvedResponseStream);

                _logger.LogInformation("Total approved Records Read: {Records}", approvedCsvData.Count);

                response = await GetDataFromUrl(archivedUrl);
                using var archivedResponseStream = await response.Content.ReadAsStreamAsync();

                var archivedCsvData = ReadCsv<T, TMap>(archivedResponseStream);

                _logger.LogInformation("Total archived Records Read: {Records}", archivedCsvData.Count);

                totalRecords.AddRange(approvedCsvData);
                totalRecords.AddRange(archivedCsvData);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error downloading CSV file from url: {UrlFilePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading CSV file from url: {UrlFilePath}");
            }

            return totalRecords;
        }

        public async Task<List<T>> ReadCsvFileFromUrlAsync<T, TMap>(string urlFilePath) where TMap : ClassMap<T>
        {
            _logger.LogInformation("Downloading CSV file from url: {UrlFilePath}", urlFilePath);

            var records = new List<T>();

            try
            {
                var response = await GetDataFromUrl(urlFilePath);

                using var approvedResponseStream = await response.Content.ReadAsStreamAsync();

                var approvedCsvData
                    = ReadCsv<T, TMap>(approvedResponseStream);

                _logger.LogInformation("Total Records Read: {Records}", records.Count);
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
            var response = await _httpClient.GetAsync(approvedUrlFilePath);
            response.EnsureSuccessStatusCode();
            return response;
        }

        private List<T> ReadCsv<T, TMap>(dynamic stream) where TMap : ClassMap<T>
        {
            using var streamReader = new StreamReader(stream);
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);
            csvReader.Context.RegisterClassMap<TMap>();
            return csvReader.GetRecords<T>().ToList();
        }
    }
}

