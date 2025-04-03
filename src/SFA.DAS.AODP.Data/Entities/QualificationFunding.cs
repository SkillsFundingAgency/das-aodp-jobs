using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities
{
    [Table("QualificationFundings", Schema = "funded")]
    public class QualificationFunding
    {
        public Guid Id { get; set; }
        public Guid QualificationVersionId { get; set; }
        public Guid FundingOfferId { get; set; }
        public DateOnly? StartDate { get; set; }
        public DateOnly? EndDate { get; set; }
        public string? Comments { get; set; }

        public virtual QualificationVersions QualificationVersion { get; set; }
        public virtual FundingOffer FundingOffer { get; set; }
    }
}
