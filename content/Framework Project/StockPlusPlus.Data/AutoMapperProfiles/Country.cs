
using AutoMapper;
using StockPlusPlus.Shared.DTOs;

namespace StockPlusPlus.Data.AutoMapperProfiles;

public class Country : Profile
{
    public Country()
    {
        CreateMap<Entities.Country, CountryDTO>().ReverseMap();
    }
}
