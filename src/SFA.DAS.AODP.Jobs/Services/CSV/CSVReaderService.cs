using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Jobs.Interfaces;

namespace SFA.DAS.AODP.Jobs.Services.CSV
{
    public class CsvReaderService : ICsvReaderService
    {
        private readonly ILogger<CsvReaderService> _logger;
        private readonly IHttpClientFactory _httpClient;

        public CsvReaderService(ILogger<CsvReaderService> logger, IHttpClientFactory httpClient)
        {
            _logger = logger;
            _httpClient = httpClient;
        }

        public List<T> ReadCSVFromFilePath<T, TMap>(string filePath) where TMap : ClassMap<T>
        {
            _logger.LogInformation("Searching for CSV file for processing");

            var records = new List<T>();
            if (File.Exists(filePath))
            {
                using var reader = new StreamReader(filePath);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Context.RegisterClassMap<TMap>();
                records = csv.GetRecords<T>().ToList();
                Console.WriteLine($"Total Records Read: {records.Count}");
            }
            else
            {
                _logger.LogError("File not found: {FilePath}", filePath);
            }
            return records;
        }

        public async Task<List<T>> ReadCsvFileFromUrlAsync<T, TMap>(string urlFilePath) where TMap : ClassMap<T>
        {
            _logger.LogInformation("Downloading CSV file from url: {UrlFilePath}", urlFilePath);

            var records = new List<T>();

            try
            {
                var client = _httpClient.CreateClient();

                var response = await client.GetAsync(urlFilePath);
                response.EnsureSuccessStatusCode();

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Context.RegisterClassMap<TMap>();
                records = csv.GetRecords<T>().ToList();
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
    }
}

