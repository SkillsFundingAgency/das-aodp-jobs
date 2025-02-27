using AutoMapper;
using SFA.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Data.Entities;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<Qualifications, FundedQualificationDTO>().ReverseMap();
        CreateMap<QualificationOffer, FundedQualificationOfferDTO>().ReverseMap();

        CreateMap<FundedQualificationDTO, Qualifications>()
            .ForMember(dest => dest.AwardingOrganisation, opt => opt.MapFrom(src =>
                new AwardingOrganisation { NameOfqual = src.AwardingOrganisation }));
    }
}
