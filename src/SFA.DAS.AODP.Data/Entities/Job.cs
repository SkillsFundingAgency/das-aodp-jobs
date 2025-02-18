using System;
using System.Collections.Generic;

namespace SFA.DAS.AODP.Data.Entities;

public partial class Job
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public bool Enabled { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? LastRunTime { get; set; }

    public virtual ICollection<JobConfiguration> JobConfigurations { get; set; } = new List<JobConfiguration>();

    public virtual ICollection<JobRun> JobRuns { get; set; } = new List<JobRun>();
}
