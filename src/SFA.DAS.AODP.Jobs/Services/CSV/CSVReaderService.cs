using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using SFA.DAS.AODP.Jobs.Interfaces;
using System.Globalization;

namespace SFA.DAS.AODP.Jobs.Services.CSV
{
    public class CsvReaderService : ICsvReaderService
    {
        private readonly ILogger<CsvReaderService> _logger;

        public CsvReaderService(ILogger<CsvReaderService> logger)
        {
            _logger = logger;
        }

        public List<T> ReadCSVFromFilePath<T, TMap>(string filePath, params object[] additionalParameters) where TMap : ClassMap<T>
        {
            _logger.LogInformation("Searching for CSV file for processing");

            var fundedCsvRecords = new List<T>();
            if (File.Exists(filePath))
            {
                fundedCsvRecords = ReadCsv<T, TMap>(filePath, additionalParameters);
                Console.WriteLine($"Total Records Read: {fundedCsvRecords.Count}");
            }
            else
            {
                _logger.LogError("File not found: {FilePath}", filePath);
            }
            return fundedCsvRecords;
        }

        public Task<List<T>> ReadCsvFileFromStreamAsync<T, TMap>(Stream stream, params object[] additionalParameters) where TMap : ClassMap<T>
        {
            var records = new List<T>();

            try
            {
                records = ReadCsv<T, TMap>(stream, additionalParameters);

                _logger.LogInformation("Total Records Read: {fundedCsvRecords}", records.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reading stream");
            }

            return Task.FromResult(records);
        }

        private List<T> ReadCsv<T, TMap>(dynamic data, params object[] additionalParameters) where TMap : ClassMap<T>
        {
            using var streamReader = new StreamReader(data);
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);

            csvReader.Read();
            csvReader.ReadHeader();

            var customHeaders = csvReader.HeaderRecord
                .Where(header => header.Contains("_FundingAvailable"))
                .ToList();

            var parameters = new List<object> { customHeaders };

            if (additionalParameters != null)
            {
                parameters.AddRange(additionalParameters);
            }

            var constructor = typeof(TMap).GetConstructors()
                .FirstOrDefault(c => c.GetParameters().Length == parameters.Count);

            if (constructor == null)
            {
                throw new InvalidOperationException($"No matching constructor found for {typeof(TMap).Name} with {parameters.Count} parameters.");
            }

            var classMap = (TMap)constructor.Invoke(parameters.ToArray());
            csvReader.Context.RegisterClassMap(classMap);

            var records = new List<T>();
            var skippedCount = 0;

            while (csvReader.Read())
            {
                try
                {
                    var record = csvReader.GetRecord<T>();                    

                    records.Add(record);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading CSV record");
                    skippedCount++;
                }
            }

            _logger.LogInformation("Total Records Read: {RecordCount}, Skipped: {SkippedCount}",
                records.Count, skippedCount);

            return records;
        }

    }
}