using CsvHelper;
using Microsoft.EntityFrameworkCore;
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

        var fundingOfferIdsByName = await dbContext.FundingOffers
            .AsNoTracking()
            .ToDictionaryAsync(offer => offer.Name, offer => offer.Id, cancellationToken);

        foreach (var fundedQualificationDto in qaaQualifications)
        {
            var row = qaaRecords.SingleOrDefault(o => o.AimCode == fundedQualificationDto.Qan);

            if (row is null)
            {
                continue;
            }

            var qual = RegulatedQaaQualification.CreateFromExisting(
                fundedQualificationDto.DateOfOfqualDataSnapshot!.Value,
                fundedQualificationDto.Qan,
                fundedQualificationDto.QualificationName,
                fundedQualificationDto.AwardingOrganisationName,
                row.FullStartDateOfQualification,
                row.FullLastDateForRegistration,
                SectorSubjectArea.FromName(fundedQualificationDto.SectorSubjectArea),
                row.DiscontinuedDate);

            dbContext.RegulatedQaaQualification.Add(qual);

            foreach (var offer in fundedQualificationDto.Offers)
            {
                if (offer.Name is null ||
                    !fundingOfferIdsByName.TryGetValue(offer.Name, out var fundingOfferId) ||
                    !IsFundingAvailable(offer.FundingAvailable))
                {
                    continue;
                }

                dbContext.QaaQualificationFundings.Add(QaaQualificationFunding.Create(
                    qual.Id,
                    fundingOfferId,
                    offer.FundingApprovalStartDate.HasValue
                        ? DateOnly.FromDateTime(offer.FundingApprovalStartDate.Value)
                        : null,
                    offer.FundingApprovalEndDate.HasValue
                        ? DateOnly.FromDateTime(offer.FundingApprovalEndDate.Value)
                        : null,
                    fundedQualificationDto.Status,
                    DateTime.UtcNow,
                    offer.Notes));
            }
        }

        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static bool IsFundingAvailable(string? value) =>
        value?.Trim() is { } normalized &&
        (normalized.Equals("true", StringComparison.OrdinalIgnoreCase) ||
         normalized.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
         normalized == "1");
}
