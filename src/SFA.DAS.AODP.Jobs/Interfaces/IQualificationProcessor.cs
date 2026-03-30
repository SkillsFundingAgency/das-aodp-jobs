using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using SFA.DAS.AODP.Models.Qualification;
using static SFA.DAS.AODP.Jobs.Services.ChangeDetectionService;
using static SFA.DAS.AODP.Jobs.Services.OfqualImportService;
namespace SFA.DAS.AODP.Jobs.Interfaces;

public interface IQualificationProcessor
{
    ProcessingResult? Process(
        QualificationDTO importRecord,
        QualificationVersions? existingVersion,
        Guid qualificationId,
        Guid organisationId,
        bool hasActiveApps,
        bool hasActiveFunding);
}