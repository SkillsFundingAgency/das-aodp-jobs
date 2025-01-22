using AutoMapper;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Models.Qualification;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<RegulatedQualificationsImport, RegulatedQualification>().ReverseMap();
        CreateMap<ProcessedRegulatedQualification, RegulatedQualification>();
    }
}
