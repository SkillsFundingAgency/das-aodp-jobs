using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Services;
using SFA.DAS.AODP.Models.Qualification;

namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IChangeDetectionService
    {
        ChangeDetectionService.DetectionResults DetectChanges(QualificationDTO newRecord, QualificationVersions qualificationVersion, AwardingOrganisation awardingOrganisation, Qualification qualification);
    }
}