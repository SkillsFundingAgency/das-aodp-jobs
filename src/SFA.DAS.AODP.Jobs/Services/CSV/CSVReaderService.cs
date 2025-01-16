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

        public async Task<List<T>> ReadApprovedAndArchivedFromUrlAsync<T, TMap>(string approvedUrlFilePath, string archivedUrlFilePath) where TMap : ClassMap<T>
        {
            _logger.LogInformation("Downloading CSV file from url: {ApprovedUrlFilePath} {ArchivedUrlFilePath}", approvedUrlFilePath, archivedUrlFilePath);

            var ApprovedRecords = new List<T>();
            var ArchivedRecords = new List<T>();
            var TotalRecords = new List<T>();

            try
            {
                var ApprovedClient = _httpClient.CreateClient();

                var ApprovedResponse = await ApprovedClient.GetAsync(approvedUrlFilePath);
                ApprovedResponse.EnsureSuccessStatusCode();

                using var stream = await ApprovedResponse.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream);
                using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
                csv.Context.RegisterClassMap<TMap>();
                ApprovedRecords = csv.GetRecords<T>().ToList();
                _logger.LogInformation("Total Records Read: {Records}", ApprovedRecords.Count);

                var ArchivedClient = _httpClient.CreateClient();

                var ArchivedResponse = await ArchivedClient.GetAsync(archivedUrlFilePath);
                ArchivedResponse.EnsureSuccessStatusCode();

                using var stream2 = await ArchivedResponse.Content.ReadAsStreamAsync();
                using var reader2 = new StreamReader(stream2);
                using var csv2 = new CsvReader(reader2, CultureInfo.InvariantCulture);
                csv2.Context.RegisterClassMap<TMap>();
                ArchivedRecords = csv2.GetRecords<T>().ToList();
                _logger.LogInformation("Total Records Read: {Records}", ArchivedRecords.Count);


                TotalRecords.AddRange(ArchivedRecords);
                TotalRecords.AddRange(ApprovedRecords);

            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HTTP request error downloading CSV file from url: {UrlFilePath}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading CSV file from url: {UrlFilePath}");
            }

            return TotalRecords;
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

