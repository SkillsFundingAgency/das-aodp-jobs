using System;
using System.Collections.Generic;
using System.Text;

namespace SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility
{
    public record QualificationProcessorSettings
    {
        public Guid NoActionRequiredStatusId { get; init; }
        public Guid DecisionRequiredStatusId { get; init; }
        public Guid NewLifecycleStageId { get; init; }
        public Guid ChangedLifecycleStageId { get; init; }
        public Guid ActionTypeDecisionId { get; init; }
        public Guid ActionTypeNoActionId { get; init; }
        public Guid ApprovedStatusId { get; init; }
        public Guid RejectedStatusId { get; init; }
        public Guid OnHoldStatusId { get; init; }
    }
}
