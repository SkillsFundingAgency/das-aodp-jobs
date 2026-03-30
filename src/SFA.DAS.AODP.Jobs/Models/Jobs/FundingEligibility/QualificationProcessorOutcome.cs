using System;
using System.Collections.Generic;
using System.Text;

namespace SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility
{
    [ExcludeFromCodeCoverage]
    public record QualificationProcessorOutcome(
        Guid StatusId,
        Guid StageId,
        Guid ActionId,
        string BaseNote,
        bool ReviewRequired,
        bool IncludeFieldChanges,
        bool IncludeEligibilityReasons,
        bool HasFunding
    );
}
