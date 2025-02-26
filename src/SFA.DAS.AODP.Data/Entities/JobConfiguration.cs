using System;
using System.Collections.Generic;

namespace SFA.DAS.AODP.Data.Entities;

public partial class JobConfiguration
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public string Value { get; set; } = null!;

    public Guid JobId { get; set; }

    public virtual Job Job { get; set; } = null!;
}
