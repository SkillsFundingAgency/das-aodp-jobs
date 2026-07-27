using System.ComponentModel.DataAnnotations.Schema;

namespace SFA.DAS.AODP.Data.Entities
{
    public class Application
    {
        public Guid Id { get; set; }
        public string? Name { get; set; }
        public string? QualificationNumber { get; set; }
        public string? Status { get; set; }
        public string? AwardingOrganisationName { get; set; }
        public string? AwardingOrganisationUkprn { get; set; }

    }
}
