using AutoMapper;
using SAF.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Models.Qualification;
using ProcessedRegulatedQualification = SAF.DAS.AODP.Models.Qualification.ProcessedRegulatedQualification;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<RegulatedQualificationsImport, RegulatedQualification>().ReverseMap();
        CreateMap<ProcessedRegulatedQualification, RegulatedQualification>();
        CreateMap<FundedQualification, FundedQualificationDTO>().ReverseMap();
    }
}
