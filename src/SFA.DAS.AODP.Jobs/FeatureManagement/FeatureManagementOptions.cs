namespace SFA.DAS.AODP.Jobs.FeatureManagement
{
    [ExcludeFromCodeCoverage]
    public record FeatureManagementOptions
    {
        public const string SectionName = "FeatureManagement";

        // Left blank intentionally as we may use this in future for short term feature flagging.
        public bool DefenderPollingEnabled { get; set; } = false;
    }
}
