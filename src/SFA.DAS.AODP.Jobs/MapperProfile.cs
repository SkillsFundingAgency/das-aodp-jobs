using AutoMapper;
using SAF.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Data.Entities;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<FundedQualification, FundedQualificationDTO>().ReverseMap();
        CreateMap<FundedQualificationOffer, FundedQualificationOfferDTO>().ReverseMap();
        CreateMap<string, QualificationImportStaging>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.JsonData, opt => opt.MapFrom(src => src));
    }
}
