using Microsoft.AspNetCore.Mvc.Filters;
using SFA.DAS.AODP.Data.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace SFA.DAS.AODP.Jobs.Models.Jobs.FundingEligibility
{
    [ExcludeFromCodeCoverage]
    public record QualificationProcessorResult(
        QualificationVersions NewVersion,
        QualificationDiscussionHistory Discussion,
        VersionFieldChanges FieldChange,
        QualificationFundingTracker? FundingTracker = null,
        bool TitleChanged = false
    );
}
