using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.AODP.Common.Enum
{

    public class QualificationReference
    {

        public const string EndPointAssessment = "End Point Assessment";
        public static DateTime MinOperationalDate = new DateTime(2024, 8, 1);

        public static List<string> IneligibleQualifications =
        [
            "Certificate in Education",
            "Professional Graduate Certificate in Education",
            "Postgraduate Diploma in Education",
            "ESOL International",
            "degree",
            "foundation degree",
            "Higher National Certificate",
            "Certificate of Higher Education",
            "Higher National Diploma",
            "Diploma of Higher Education",
            "Diploma in Teaching"
        ];

        public static List<string> IneligibleQualificationsShortForms =
        [
            "CertEd",
            "PGCE",
            "PGDE",
            "HNC",
            "Cert HE",
            "HND",
            "Dip HE",
            "further education and skills"
        ];
    }
}
