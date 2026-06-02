using System.Diagnostics.CodeAnalysis;

namespace SFA.DAS.AODP.Common.Enum;

[ExcludeFromCodeCoverage]
public record QualificationTitle(string Value)
{
    public static readonly QualificationTitle EsolInternational = new("ESOL International");

    public static readonly QualificationTitle Doctor = new("doctor");
    public static readonly QualificationTitle PhD = new("PhD");
    public static readonly QualificationTitle EngD = new("EngD");

    public static readonly QualificationTitle Master = new("master");
    public static readonly QualificationTitle MPhil = new("MPhil");
    public static readonly QualificationTitle MSc = new("MSc");
    public static readonly QualificationTitle MA = new("MA");
    public static readonly QualificationTitle MBA = new("MBA");
    public static readonly QualificationTitle MDes = new("MDes");
    public static readonly QualificationTitle MRes = new("MRes");

    public static readonly QualificationTitle PostgraduateCertificateInEducation = new("Postgraduate Certificate in Education");

    public static readonly QualificationTitle PostgraduateDiplomaInEducation = new("Postgraduate Diploma in Education");

    public static readonly QualificationTitle PGCE = new("PGCE");
    public static readonly QualificationTitle PGDE = new("PGDE");

    public static readonly QualificationTitle Degree = new("degree");
    public static readonly QualificationTitle BA = new("BA");
    public static readonly QualificationTitle BSc = new("BSc");
    public static readonly QualificationTitle BEd = new("BEd");
    public static readonly QualificationTitle BEng = new("BEng");
    public static readonly QualificationTitle BTech = new("BTech");

    public static readonly QualificationTitle ProfessionalGraduateCertificateInEducation = new("Professional Graduate Certificate in Education");

    public static readonly QualificationTitle ProfessionalGraduateDiplomaInEducation = new("Professional Graduate Diploma in Education");

    public static readonly QualificationTitle PgCE = new("PgCE");
    public static readonly QualificationTitle PgDE = new("PgDE");

    public static readonly QualificationTitle FoundationDegree = new("foundation degree");
    public static readonly QualificationTitle HigherNationalDiploma = new("Higher National Diploma");
    public static readonly QualificationTitle DiplomaOfHigherEducation = new("Diploma of Higher Education");
    public static readonly QualificationTitle HND = new("HND");
    public static readonly QualificationTitle DipHE = new("Dip HE");
    public static readonly QualificationTitle FdA = new("FdA");
    public static readonly QualificationTitle FdEng = new("FdEng");
    public static readonly QualificationTitle FdSc = new("FdSc");

    public static readonly QualificationTitle DiplomaInTeachingFurtherEducationAndSkills = new("Diploma in Teaching (Further Education and Skills)");
    public static readonly QualificationTitle DiplomaInTeachingFeAndSkills = new("Diploma in Teaching (FE and Skills)");

    public static readonly QualificationTitle DiplomaInTeachingFe = new("Diploma in Teaching (FE)");
    public static readonly QualificationTitle FurtherEducationAndSkills = new("Further Education and Skills");
    public static readonly QualificationTitle CertificateInEducation = new("Certificate in Education");
    public static readonly QualificationTitle LearningAndSkillsTeacher = new("Learning and Skills Teacher");
    public static readonly QualificationTitle DiT = new("DiT");
    public static readonly QualificationTitle DIT = new("DIT");
    public static readonly QualificationTitle CertEd = new("CertEd");
    public static readonly QualificationTitle CertED = new("CertED");
    public static readonly QualificationTitle LST = new("LST");

    public static readonly QualificationTitle HigherNationalCertificate = new("Higher National Certificate");
    public static readonly QualificationTitle CertificateOfHigherEducation = new("Certificate of Higher Education");
    public static readonly QualificationTitle HNC = new("HNC");
    public static readonly QualificationTitle CertHE = new("Cert HE");
}