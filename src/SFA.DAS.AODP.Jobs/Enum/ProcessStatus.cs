using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.AODP.Jobs.Enum
{
    public static class ProcessStatus
    {
        public const string DecisionRequired = "Decision Required";
        public const string Approved = "Approved";
        public const string Rejected = "Rejected";
        public const string OnHold = "On Hold";
        public const string NoActionRequired = "No Action Required";
    }
}
