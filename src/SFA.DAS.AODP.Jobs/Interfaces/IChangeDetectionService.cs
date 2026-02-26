namespace SFA.DAS.AODP.Jobs.Interfaces
{
    public interface IChangeDetectionService
    {
        ChangeDetectionService.DetectionResults DetectChanges(QualificationDTO newRecord, QualificationVersions qualificationVersion, AwardingOrganisation awardingOrganisation, Qualification qualification);
    }
}