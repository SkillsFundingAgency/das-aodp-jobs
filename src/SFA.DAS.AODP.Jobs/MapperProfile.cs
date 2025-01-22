using AutoMapper;
using SAF.DAS.AODP.Models.Qualification;
using SFA.DAS.AODP.Data.Entities;
using SFA.DAS.AODP.Models.Qualification;

public class MapperProfile : Profile
{
    public MapperProfile()
    {
        CreateMap<RegulatedQualificationsImport, RegulatedQualification>().ReverseMap();
        CreateMap<SFA.DAS.AODP.Data.Entities.ProcessedRegulatedQualification, RegulatedQualification>();
        CreateMap<FundedQualification, FundedQualificationDTO>().ReverseMap();
        CreateMap<FundedQualificationOffer, FundedQualificationOfferDTO>().ReverseMap();
    }
}
