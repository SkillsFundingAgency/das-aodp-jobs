using System.Data;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using SAF.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Jobs.Interfaces;
using static System.Net.Mime.MediaTypeNames;

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

        public async Task<IEnumerable<FundedQualificationDTO>> ReadQualifications(string url)
        {
            _logger.LogInformation($"Downloading CSV file from url: {url}");

            var totalRecords = new List<FundedQualificationDTO>();

            try
            {
                HttpResponseMessage response;
                response = await GetDataFromUrl(url);

                using var qualificationsStream = await response.Content.ReadAsStreamAsync();

                var qualificationRecords
                    = await ReadCsv(qualificationsStream);

                _logger.LogInformation("Total  Recrds Read: {Records}", qualificationRecords.Count());

                totalRecords.AddRange(qualificationRecords);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP request error downloading CSV file from url: {url}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error downloading CSV file from url: {url}");
            }

            return totalRecords;
        }

        private async Task<HttpResponseMessage> GetDataFromUrl(string url)
        {
            var _httpClient = _httpClientFactory.CreateClient();
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            return response;
        }

        private async Task<List<FundedQualificationDTO>> ReadCsv(dynamic data)
        {
            using var streamReader = new StreamReader(data);
            using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);

            using var csv = new CsvReader(streamReader, CultureInfo.InvariantCulture);
            using var dataReader = new CsvDataReader(csv);
            var dataTable = new DataTable();
            dataTable.Load(dataReader);

            var identifiedOfferColumns = (from DataColumn x
            in dataTable.Columns.Cast<DataColumn>()
                                          select x.ColumnName).Where(t => t.Contains("_FundingAvailable")).ToList();

            return BuildFundedQualificationRecord(dataTable, identifiedOfferColumns);
        }

        private static List<FundedQualificationDTO> BuildFundedQualificationRecord(DataTable dataTable, List<string> identifiedOffersColumnNames)
        {
            var fundedQualifications = new List<FundedQualificationDTO>();
            foreach (DataRow row in dataTable.Rows)
            {
                List<FundedQualificationOfferDTO> offerRecords = GetOfferRecords(identifiedOffersColumnNames, row);
                fundedQualifications.Add(new FundedQualificationDTO
                {
                    AwardingOrganisation = row["DateOfOfqualDataSnapshot"].ToString(),
                    QualificationName = row["QualificationName"].ToString(),
                    DateOfOfqualDataSnapshot = DateTime.Parse(row["DateOfOfqualDataSnapshot"].ToString()),
                    AwardingOrganisationURL = row["AwardingOrganisationURL"].ToString(),
                    Level = row["level"].ToString(),
                    QualificationType = row["QualificationType"].ToString(),
                    SectorSubjectArea = row["SectorSubjectArea"].ToString(),
                    Status = row["Status"].ToString(),
                    Subcategory = row["Subcategory"].ToString(),
                    QualificationNumber = row["QualificationNumber"].ToString(),
                    Offers = offerRecords
                });
            }
            return fundedQualifications;
        }

        private static List<FundedQualificationOfferDTO> GetOfferRecords(List<string> identifiedOffersColumnNames, DataRow row)
        {
            List<FundedQualificationOfferDTO> offerRecords = new List<FundedQualificationOfferDTO>();
            foreach (var offer in identifiedOffersColumnNames)
            {
                var offerName = offer.Split("_")[0];
                var offerRecord = new FundedQualificationOfferDTO
                {
                    FundingAvailable = row[row.Table.Columns[$"{offerName}_FundingAvailable"].Ordinal].ToString(),
                    Name = offerName,
                    Notes = row[row.Table.Columns[$"{offerName}_Notes"].Ordinal].ToString(),
                    FundingApprovalEndDate = DateTime.TryParse(row[row.Table.Columns[$"{offerName}_FundingApprovalEndDate"].Ordinal].ToString(), out DateTime end) ? end : (DateTime?)null,
                    FundingApprovalStartDate = DateTime.TryParse(row[row.Table.Columns[$"{offerName }_FundingApprovalEndDate"].Ordinal].ToString(), out DateTime start) ? start : (DateTime?)null
                };
                offerRecords.Add(offerRecord);
            }
            return offerRecords;
        }

    }

}

