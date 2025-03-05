using AutoMapper;
using SFA.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Data.Entities;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<Qualifications, FundedQualificationDTO>().ReverseMap()
            .ForMember(dest => dest.QualificationOffers, opt => opt.MapFrom(src => src.Offers));
        CreateMap<QualificationOffers, FundedQualificationOfferDTO>().ReverseMap();
    }
}
