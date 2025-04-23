using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.AODP.Common.Enum
{
    public static class ImportReason
    {
        public const string DecisionRequired = "Decision Required - New Qualification";
        public const string NoAction = "No Action Required - New Qualification";
        public const string NoGLHOrTQT = "No Action required - New Qualification - GLH or TQT missing";
    }
}
