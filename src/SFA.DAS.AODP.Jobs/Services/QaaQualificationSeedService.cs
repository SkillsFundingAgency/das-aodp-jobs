using CsvHelper;
using SFA.DAS.AODP.Jobs.Models;
using System.Globalization;

namespace SFA.DAS.AODP.Jobs.Services;

public class QaaQualificationSeedService(
    ILogger<QaaQualificationSeedService> logger,
    QaaSeedDataConfiguration configuration,
    IQaaSeedCsvBlobReader blobReader,
    IQaaRepository qaaRepository,
    ISystemClockService clockService) : IQaaQualificationSeedService
{
    public async Task<int> SeedAsync(CancellationToken cancellationToken)
    {
        EnsureConfigured(configuration);

        await using var stream = await blobReader.OpenReadAsync(
            configuration.ContainerName!,
            configuration.BlobName!,
            cancellationToken);

        using var streamReader = new StreamReader(stream);
        using var csvReader = new CsvReader(streamReader, CultureInfo.InvariantCulture);

        csvReader.Context.RegisterClassMap<QaaSeedCsvRecordClassMap>();

        var seedRecords = csvReader.GetRecords<QaaSeedCsvRecord>().ToList();
        if (seedRecords.Count == 0)
        {
            logger.LogInformation("No QAA seed records found in blob {ContainerName}/{BlobName}.", configuration.ContainerName, configuration.BlobName);
            return 0;
        }

        var qualifications = seedRecords.Select(ToQaaQualificationResponse).ToList();

        return await qaaRepository.ImportQaaQualificationsAsync(
            qualifications,
            clockService.UtcNow,
            cancellationToken);
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

    private static QaaQualificationResponse ToQaaQualificationResponse(QaaSeedCsvRecord record)
    {
        return new QaaQualificationResponse
        {
            AimCode = record.AimCode,
            AwardingBody = record.AwardingBody,
            DiplomaTitle = record.DiplomaTitle,
            SsaTier1 = record.SsaTier1,
            SsaTier2 = record.SsaTier2,
            StartDateOfQualification = record.FullStartDateOfQualification,
            LastDateForRegistrations = record.FullLastDateForRegistration,
            LastDateForCertifications = record.FullLastDateForCertification,
            AwardStatus = record.AwardStatus,
            DiscontinuedDate = record.DiscontinuedDate
        };
    }
}
