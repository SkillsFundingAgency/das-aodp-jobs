using SFA.DAS.AODP.Data.Entities;

namespace SFA.DAS.AODP.Jobs.Services;

/// <summary>
/// Default implementation for <see cref="IQaaQualificationImportService"/>
/// </summary>
/// <param name="logger">The logger to log to.</param>
/// <param name="qaaApiClient">The named HttpClient client interface.</param>
public class QaaQualificationImportService(ILogger<QaaQualificationImportService> logger, IQaaApiClient qaaApiClient, IQaaRepository qaaRepository) : IQaaQualificationImportService
{
    private readonly ILogger<QaaQualificationImportService> _logger = logger;
    private readonly IQaaApiClient _qaaApiClient = qaaApiClient;
    private readonly IQaaRepository _qaaRepository = qaaRepository;

    /// <inheritdoc/>.
    public async Task<int> ImportDataAsync(CancellationToken cancellationToken)
    {
        var totalCountOfRecordsProcessed = 0;
        try
        {
            var proposedQualifications = await _qaaApiClient.GetQualificationsAsync(cancellationToken);

            if (proposedQualifications.Any())
            {
                var rowsDeleted = await _qaaRepository.RunPrerequisitesForImportAsync(cancellationToken);

                _logger.LogInformation("{RowsDeleted} were deleted, ready for fresh import", rowsDeleted);

                var qualificationsToCreate = new List<RegulatedQaaQualification>();

                foreach (var proposedQualification in proposedQualifications)
                {
                    var ssa = SectorSubjectArea.FromTiers(proposedQualification.SsaTier1, proposedQualification.SsaTier2)!;

                    var regulatedQualification = RegulatedQaaQualification.Create(
                        proposedQualification.AimCode,
                        proposedQualification.DiplomaTitle, 
                        proposedQualification.AwardingBody,
                        proposedQualification.StartDateOfQualification,
                        proposedQualification.LastDateForRegistrations,
                        ssa);

                    qualificationsToCreate.Add(regulatedQualification);
                }

                if (qualificationsToCreate.Count <= 0)
                {
                    _logger.LogInformation("No qualifications found to import, nothing to do.");
                }
                else
                {
                    await _qaaRepository.RunImportAsync(qualificationsToCreate, cancellationToken);
                    totalCountOfRecordsProcessed = qualificationsToCreate.Count;

                    _logger.LogInformation("Finished import, created {NumberOfRecordsCreated}", qualificationsToCreate.Count);
                }
            }

            _logger.LogInformation("No qualifications found from QAA Api, nothing to do.");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Could not call the Qaa API, got status {Status}", ex.StatusCode);
            throw;
        }
       
        return totalCountOfRecordsProcessed;
    }
}