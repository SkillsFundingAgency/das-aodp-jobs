using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Models.Qualification;
using static SFA.DAS.AODP.Jobs.Services.QualificationProcessor;
namespace SFA.DAS.AODP.Jobs.Interfaces;

// Processes imported qualifications from the Ofqual api to determine if a new version is required
// in the QFAST database.
// 
// Evaluates funding eligibility, detects changes, and applies business rules to
// decide lifecycle stage, status, and actions. Creates new qualification versions
// and discussion history records when needed.
public interface IQualificationProcessor
{
    QualificationProcessorResult? Process(
        QualificationDTO importRecord,
        QualificationVersions? existingVersion,
        Guid qualificationId,
        Guid organisationId,
        bool hasApplicationsInProgress,
        bool hasFundingWhichHasNotEnded);
}