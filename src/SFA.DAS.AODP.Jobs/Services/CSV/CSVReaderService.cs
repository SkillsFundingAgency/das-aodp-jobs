﻿using System.Data;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using SAF.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Jobs.Interfaces;
using static System.Net.Mime.MediaTypeNames;
using FundedQualification = SAF.DAS.AODP.Models.Qualification.FundedQualification;

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
        public async Task<List<FundedQualification>> ReadQualifications(string url)
        {
            _logger.LogInformation($"Downloading CSV file from url: {url}");

            var totalRecords = new List<FundedQualification>();

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

        private async Task<List<FundedQualification>> ReadCsv(dynamic data)
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

        private static List<FundedQualification> BuildFundedQualificationRecord(DataTable dataTable, List<string> identifiedOffersColumnNames)
        {
            var fundedQualifications = new List<FundedQualification>();
            foreach (DataRow row in dataTable.Rows)
            {
                List<FundedQualificationOffer> offerRecords = GetOfferRecords(identifiedOffersColumnNames, row);
                fundedQualifications.Add(new FundedQualification
                {
                    AwardingOrganisation = row["DateOfOfqualDataSnapshot"].ToString(),
                    QualificationName = row["QualificationName"].ToString(),
                    DateOfOfqualDataSnapshot = DateTime.Parse(row["DateOfOfqualDataSnapshot"].ToString()),
                    AwardingOrganisationURL = row["AwardingOrganisationURL"].ToString(),
                    Level = row["level"].ToString(),
                    QualificationNumber = row["QualificationNumber"].ToString(),
                    Offers = offerRecords
                });
            }
            return fundedQualifications;
        }

        private static List<FundedQualificationOffer> GetOfferRecords(List<string> identifiedOffersColumnNames, DataRow row)
        {
            List<FundedQualificationOffer> offerRecords = new List<FundedQualificationOffer>();
            foreach (var offer in identifiedOffersColumnNames)
            {
                var offerName = offer.Split("_")[0];
                var offerRecord = new FundedQualificationOffer
                {
                    FundingAvailable = row[row.Table.Columns[offerName + "_" + "FundingAvailable"].Ordinal].ToString(),
                    Name = offerName,
                    Notes = row[row.Table.Columns[offerName + "_" + "Notes"].Ordinal].ToString(),
                    FundingApprovalEndDate = DateTime.TryParse(row[row.Table.Columns[offerName + "_" + "FundingApprovalEndDate"].Ordinal].ToString(), out DateTime end) ? end : (DateTime?)null,
                    FundingApprovalStartDate = DateTime.TryParse(row[row.Table.Columns[offerName + "_" + "FundingApprovalEndDate"].Ordinal].ToString(), out DateTime start) ? start : (DateTime?)null
                };
                offerRecords.Add(offerRecord);
            }
            return offerRecords;
        }
    }

}

