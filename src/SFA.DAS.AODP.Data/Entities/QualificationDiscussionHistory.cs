namespace SFA.DAS.AODP.Data.Entities;

public partial class QualificationDiscussionHistory
{
    public int Id { get; set; }

    public int QualificationId { get; set; }

    public int ActionTypeId { get; set; }

    public string? UserDisplayName { get; set; }

    public string? Notes { get; set; }

    public DateTime? Timestamp { get; set; }

    public virtual ActionType ActionType { get; set; } = null!;

    public virtual Qualification Qualification { get; set; } = null!;
}

