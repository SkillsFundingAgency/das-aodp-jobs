using SFA.DAS.AODP.Jobs.Enum;

namespace SFA.DAS.AODP.Jobs.Models
{
    public class LifecycleCalculationResult
    {
        public string ProcessStatus { get; set; } = Enum.ProcessStatus.NoActionRequired;
        public string LifecycleStage { get; set; } = LifeCycleStage.Changed;
        public Guid ActionId { get; set; } = Guid.NewGuid();
        public string Notes { get; set; } = "";
    }
}
