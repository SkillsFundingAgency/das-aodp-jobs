using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Functions;
using SFA.DAS.AODP.Jobs.LoggerMessages;
using QaaQualificationImportServiceLoggerMessages = SFA.DAS.AODP.Jobs.LoggerMessages.QaaQualificationImportServiceLoggerMessages;

namespace SFA.DAS.AODP.Jobs.Services;

/// <summary>
/// Default implementation for <see cref="IQaaQualificationImportService"/>
/// </summary>
/// <param name="logger">The logger to log to.</param>
/// <param name="qaaApiClient">The named HttpClient client interface.</param>
/// <param name="qaaRepository">Defines the data access layer to manage access to Qaa data.</param>
/// <param name="clockService">Provides an abstraction to retrieve the current datetime.</param>
public class QaaQualificationImportService(ILogger<QaaQualificationImportService> logger, IQaaApiClient qaaApiClient, IQaaRepository qaaRepository, ISystemClockService clockService) : IQaaQualificationImportService
{
    private readonly ILogger<QaaQualificationImportService> _logger = logger;
    private readonly IQaaApiClient _qaaApiClient = qaaApiClient;
    private readonly IQaaRepository _qaaRepository = qaaRepository;
    private readonly ISystemClockService _clockService = clockService;

    /// <inheritdoc/>.
    public async Task<int> ImportDataAsync(CancellationToken cancellationToken)
    {
        var totalCountOfRecordsProcessed = 0;
        try
        {
            var proposedQualifications = await _qaaApiClient.GetQualificationsAsync(cancellationToken);

            if (!proposedQualifications.Any())
            {
                QaaQualificationImportServiceLoggerMessages.NoQaaQualificationsFound(_logger);
                return 0;
            }

            var dateOfSnapshot = _clockService.UtcNow;
            var rowsDeleted = await _qaaRepository.RunPrerequisitesForImportAsync(cancellationToken);

            QaaQualificationImportServiceLoggerMessages.DeletedExistingRows(_logger, rowsDeleted);

            var qualificationsToCreate = new List<RegulatedQaaQualification>();

            foreach (var proposedQualification in proposedQualifications)
            {
                var ssa = SectorSubjectArea.FromTiers(proposedQualification.SsaTier1, proposedQualification.SsaTier2)!;

                var regulatedQualification = RegulatedQaaQualification.Create(
                    dateOfSnapshot,
                    proposedQualification.AimCode,
                    proposedQualification.DiplomaTitle, 
                    proposedQualification.AwardingBody,
                    proposedQualification.StartDateOfQualification,
                    proposedQualification.LastDateForRegistrations,
                    ssa);

                qualificationsToCreate.Add(regulatedQualification);
            }

            await _qaaRepository.RunImportAsync(qualificationsToCreate, cancellationToken);
            totalCountOfRecordsProcessed = qualificationsToCreate.Count;

            QaaQualificationImportServiceLoggerMessages.FinishedImport(_logger, totalCountOfRecordsProcessed);
            
        }
        catch (HttpRequestException ex)
        {
            QaaQualificationImportServiceLoggerMessages.FailedToCallQaaApi(_logger, ex);
            throw;
        }
       
        return totalCountOfRecordsProcessed;
    }
}