using CsvHelper;
using CsvHelper.Configuration;
using SFA.DAS.AODP.Jobs.Services.CSV;
using SFA.DAS.AODP.Models.Qualification;
using System.Globalization;
using static SFA.DAS.AODP.Infrastructure.Repositories.QualificationVersionRepository;

namespace SFA.DAS.AODP.Jobs.UnitTests.Application.Services
{
    public class FundedQualificationsImportClassMapTests
    {
        private static List<FundedQualificationDTO> ParseCsv(
            string csv,
            List<string> headers,
            List<QualificationLookupItem> lookup,
            ILogger logger)
        {
            using var reader = new StringReader(csv);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HeaderValidated = null,
                MissingFieldFound = null
            };

            using var csvReader = new CsvReader(reader, config);


            csvReader.Context.RegisterClassMap(
                new FundedQualificationsImportClassMap(headers, lookup, logger));

            return csvReader.GetRecords<FundedQualificationDTO>().ToList();
        }


        [Fact]
        public void Should_Map_Qualification_And_Organisation_From_Lookup()
        {
            var logger = Mock.Of<ILogger>();

            var qId = Guid.NewGuid();
            var orgId = Guid.NewGuid();

            var lookup = new List<QualificationLookupItem>
            {
                new("Q1", qId, orgId)
            };

            var csv = @"QualificationNumber

Q1";

            var result = ParseCsv(csv, new List<string>(), lookup, logger);

            var item = result.Single();

            Assert.Equal(qId, item.QualificationId);
            Assert.Equal(orgId, item.AwardingOrganisationId);
        }

        [Fact]
        public void Should_Return_Null_When_QAN_Not_In_Lookup()
        {
            var logger = Mock.Of<ILogger>();

            var lookup = new List<QualificationLookupItem>();

            var csv = @"QualificationNumber
Q1";

            var result = ParseCsv(csv, new List<string>(), lookup, logger);

            var item = result.Single();

            Assert.Null(item.QualificationId);
            Assert.Null(item.AwardingOrganisationId);
        }

        [Fact]
        public void Should_Log_Warning_And_Return_Default_When_QAN_Empty()
        {
            var loggerMock = new Mock<ILogger>();

            var lookup = new List<QualificationLookupItem>
            {
                new("Q1", Guid.NewGuid(), Guid.NewGuid())
            };

            var csv = @"QualificationNumber
";

            var result = ParseCsv(csv, new List<string>(), lookup, loggerMock.Object);

            Assert.Empty(result);
        }

        [Fact]
        public void Should_Create_Offers_From_Headers()
        {
            var logger = Mock.Of<ILogger>();

            var qId = Guid.NewGuid();

            var lookup = new List<QualificationLookupItem>
            {
                new("Q1", qId, Guid.NewGuid() )
            };

            var headers = new List<string>
            {
                "OfferA"
            };

            var csv = @"QualificationNumber,OfferA_FundingApprovalStartDate,OfferA_FundingApprovalEndDate,OfferA_Notes,OfferA_FundingAvailable
Q1,01/01/2024,01/01/2025,Note1,Yes";

            var result = ParseCsv(csv, headers, lookup, logger);

            var item = result.Single();

            Assert.Single(item.Offers);

            var offer = item.Offers.First();

            Assert.Equal("OfferA", offer.Name);
            Assert.Equal("Note1", offer.Notes);
            Assert.Equal("Yes", offer.FundingAvailable);
        }

        [Fact]
        public void Should_Null_Invalid_Or_Too_Early_Dates()
        {
            var logger = Mock.Of<ILogger>();

            var qId = Guid.NewGuid();

            var lookup = new List<QualificationLookupItem>
            {
                new("Q1", qId, Guid.NewGuid())
            };

            var headers = new List<string>
            {
                "OfferA"
            };

            var csv = @"QualificationNumber,OfferA_FundingApprovalStartDate,OfferA_FundingApprovalEndDate,OfferA_Notes,OfferA_FundingAvailable
Q1,01/01/1700,not-a-date,Note1,Yes";

            var result = ParseCsv(csv, headers, lookup, logger);

            var offer = result.Single().Offers.Single();

            Assert.Null(offer.FundingApprovalStartDate);
            Assert.Null(offer.FundingApprovalEndDate);
        }

        [Fact]
        public void Should_Log_Warning_When_QAN_Missing_For_Offers()
        {
            var loggerMock = new Mock<ILogger>();

            var lookup = new List<QualificationLookupItem>();

            var headers = new List<string>
            {
                "OfferA_FundingApprovalStartDate"
            };

            var csv = @"QualificationNumber,OfferA_FundingApprovalStartDate
,01/01/2024";

            ParseCsv(csv, headers, lookup, loggerMock.Object);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Empty qualification number")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public void Should_Return_No_Offers_When_QualificationId_Is_Null()
        {
            var logger = Mock.Of<ILogger>();

            var lookup = new List<QualificationLookupItem>
            {
                new("Q1", null,Guid.NewGuid())
            };

            var headers = new List<string>
            {
                "OfferA_FundingApprovalStartDate",
                "OfferA_FundingApprovalEndDate",
                "OfferA_Notes",
                "OfferA_FundingAvailable"
            };

            var csv = @"QualificationNumber,OfferA_FundingApprovalStartDate
Q1,01/01/2024";

            var result = ParseCsv(csv, headers, lookup, logger);

            var item = result.Single();

            Assert.Empty(item.Offers);
        }

    }
}
