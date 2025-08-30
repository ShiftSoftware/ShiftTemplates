using AutoMapper;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using ShiftSoftware.ShiftIdentity.Core.DTOs.CompanyBranch;

namespace StockPlusPlus.Data.AutoMapperProfiles;

public class CompanyBranch : Profile
{
    public CompanyBranch()
    {
        CreateMap<CompanyBranchModel, CompanyBranchListDTO>()
            .ForMember(x => x.City, x => x.MapFrom(x => x.City.Name))
            .ForMember(x => x.Company, x => x.MapFrom(x => x.Company.Name));
    }
}