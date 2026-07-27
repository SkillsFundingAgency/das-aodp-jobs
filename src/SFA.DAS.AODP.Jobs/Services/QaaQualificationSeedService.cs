using CsvHelper;
using SFA.DAS.AODP.Jobs.Models;
using System.Globalization;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services;

// This is temporary code only existing for the initial seeding process and can be removed when the seed function has ran in production.
[ExcludeFromCodeCoverage(Justification = "This is temporary code")]
public class QaaQualificationSeedService(
    ILogger<QaaQualificationSeedService> logger,
    IQaaSeedCsvBlobReader blobReader,
    IApplicationDbContext dbContext,
    ICsvReaderService csvReaderService,
    IQualificationVersionRepository qualificationVersionRepository) : IQaaQualificationSeedService
{
    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        var qualificationCache = await qualificationVersionRepository.GetLatestQualificationVersionSnapshotsAsync();

        await using var outputFileStream = await blobReader.OpenReadAsync(
            "funded-qualifications-import",
            "Output file.csv",
            cancellationToken);

        var seedRecords = await csvReaderService.ReadCsvFileFromStreamAsync<FundedQualificationDTO, QaaFundedQualificationsImportClassMap>(outputFileStream, qualificationCache, logger);

        await using var qaaRawDataStream = await blobReader.OpenReadAsync(
            "funded-qualifications-import",
            "QAA Annual Report with correct date formatting.csv",
            cancellationToken);

        var qaaRecords = new List<QaaSeedCsvRecord>();
        using (var reader = new StreamReader(qaaRawDataStream))
        using (var csvReader2 = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            csvReader2.Context.RegisterClassMap<QaaSeedCsvRecordClassMap>();
            qaaRecords = csvReader2.GetRecords<QaaSeedCsvRecord>().ToList();
        }
        
        if (qaaRecords.Count == 0)
        {
            logger.LogInformation("No QAA seed records found in blob {ContainerName}/{BlobName}.", "funded-qualifications-import", "QAA Annual Report with correct date formatting.csv");
            return 0;
        }

        var qaaQualifications = seedRecords.Where(x => x.QualificationType == "Access to Higher Education").ToList();

        foreach (var fundedQualificationDto in qaaQualifications)
        {
            var row = qaaRecords.SingleOrDefault(o => o.AimCode == fundedQualificationDto.Qan);
            var age1619Offer = fundedQualificationDto.Offers.Single(o => o.Name!.StartsWith("Age1619"))
                .FundingApprovalEndDate!.Value;
            var advancedLearnerLoansOffer = fundedQualificationDto.Offers.Single(o => o.Name!.StartsWith("Age1619"))
                .FundingApprovalEndDate!.Value;
            var legalEntitlementL2L3 = fundedQualificationDto.Offers.Single(o => o.Name!.StartsWith("LegalEntitlementL2L3"))
                .FundingApprovalEndDate!.Value;

            if (row is null)
            {
                continue;
            }

            var qual = RegulatedQaaQualification.CreateFromExisting(
                fundedQualificationDto.DateOfOfqualDataSnapshot!.Value,
                fundedQualificationDto.Qan,
                fundedQualificationDto.QualificationName,
                fundedQualificationDto.AwardingOrganisationName,
                DateOnly.FromDateTime(fundedQualificationDto.Offers.Min(o => o.FundingApprovalStartDate).Value),
                row.FullLastDateForRegistration,
                SectorSubjectArea.FromName(fundedQualificationDto.SectorSubjectArea),
                row.DiscontinuedDate,
                DateOnly.FromDateTime(age1619Offer), 
                DateOnly.FromDateTime(advancedLearnerLoansOffer), 
                DateOnly.FromDateTime(legalEntitlementL2L3)
                );

            dbContext.RegulatedQaaQualification.Add(qual);
        }

        return await dbContext.SaveChangesAsync(cancellationToken);
    }
}