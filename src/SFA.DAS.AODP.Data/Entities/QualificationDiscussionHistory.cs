namespace SFA.DAS.AODP.Data.Entities;

public partial class QualificationDiscussionHistory
{
    public Guid Id { get; set; }

    public Guid QualificationId { get; set; }

    public Guid ActionTypeId { get; set; }

    public string? UserDisplayName { get; set; }

    public string? Notes { get; set; }

    public DateTime? Timestamp { get; set; }

    public virtual ActionType ActionType { get; set; } = null!;

    public virtual Qualification Qualification { get; set; } = null!;
}

