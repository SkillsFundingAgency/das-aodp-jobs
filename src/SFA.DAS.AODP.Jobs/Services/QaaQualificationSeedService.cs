using CsvHelper;
using SFA.DAS.AODP.Jobs.Models;
using System.Globalization;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Services;

public class QaaQualificationSeedService(
    ILogger<QaaQualificationSeedService> logger,
    QaaSeedDataConfiguration configuration,
    IQaaSeedCsvBlobReader blobReader,
    AodpJobsConfiguration options,
    IApplicationDbContext dbContext,
    ICsvReaderService csvReaderService,
    IQualificationVersionRepository qualificationVersionRepository) : IQaaQualificationSeedService
{
    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        EnsureConfigured(configuration);

        var qualificationCache = await qualificationVersionRepository.GetLatestQualificationVersionSnapshotsAsync();

        var seedRecords = await csvReaderService.ReadCsvFileFromUrlAsync<FundedQualificationDTO, QaaFundedQualificationsImportClassMap>("http://127.0.0.1:10000/devstoreaccount1/funded-qualifications-import/Output%20file.csv?sv=2018-03-28&spr=https%2Chttp&st=2026-06-21T11%3A25%3A02Z&se=2026-06-30T11%3A25%3A00Z&sr=b&sp=r&sig=LrAx1nr05r532xvQMlMqh%2Bw%2FYQQaxl9d6s4urip1JgU%3D", qualificationCache, logger);

        await using var stream2 = await blobReader.OpenReadAsync(
            configuration.ContainerName!,
            configuration.BlobName!,
            cancellationToken);

        var qaaRecords = new List<QaaSeedCsvRecord>();
        using (var reader = new StreamReader(stream2))
        using (var csvReader2 = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            csvReader2.Context.RegisterClassMap<QaaSeedCsvRecordClassMap>();
            qaaRecords = csvReader2.GetRecords<QaaSeedCsvRecord>().ToList();
        }
        
        if (qaaRecords.Count == 0)
        {
            logger.LogInformation("No QAA seed records found in blob {ContainerName}/{BlobName}.", configuration.ContainerName, configuration.BlobName);
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

    private static void EnsureConfigured(QaaSeedDataConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.ContainerName))
        {
            throw new InvalidOperationException("QaaSeedData:ContainerName must be configured.");
        }

        if (string.IsNullOrWhiteSpace(configuration.BlobName))
        {
            throw new InvalidOperationException("QaaSeedData:BlobName must be configured.");
        }
    }
}