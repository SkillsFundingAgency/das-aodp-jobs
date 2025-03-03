using AutoMapper;
using SFA.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Data.Entities;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<Qualifications, FundedQualificationDTO>().ReverseMap();
        CreateMap<QualificationOffer, FundedQualificationOfferDTO>().ReverseMap();
    }
}
