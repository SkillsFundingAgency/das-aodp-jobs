using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility;
using SFA.DAS.AODP.Models.Qualification;
using static SFA.DAS.AODP.Jobs.Services.QualificationProcessor;
namespace SFA.DAS.AODP.Jobs.Interfaces;

public interface IQualificationProcessor
{
    QualificationProcessorResult? Process(
        QualificationDTO importRecord,
        QualificationVersions? existingVersion,
        Guid qualificationId,
        Guid organisationId,
        bool hasActiveApps,
        bool hasActiveFunding,
        QualificationProcessorSettings settings);
}