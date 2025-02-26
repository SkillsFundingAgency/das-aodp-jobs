using System;
using System.Collections.Generic;

namespace SFA.DAS.AODP.Data.Entities;

public partial class JobRun
{
    public Guid Id { get; set; }

    public string Status { get; set; } = null!;

    public DateTime StartTime { get; set; }

    public DateTime? EndTime { get; set; }

    public string? User { get; set; }

    public int? RecordsProcessed { get; set; }

    public Guid JobId { get; set; }

    public virtual Job Job { get; set; } = null!;
}
