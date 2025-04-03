using AutoMapper;
using SFA.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Data.Entities;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<Qualifications, FundedQualificationDTO>().ReverseMap()
            .ForMember(dest => dest.QualificationId, opt => opt.MapFrom(src => src.QualificationId))
            .ForMember(dest => dest.QualificationOffers, opt => opt.MapFrom(src => src.Offers));
        CreateMap<QualificationOffer, FundedQualificationOfferDTO>().ReverseMap();
    }
}
