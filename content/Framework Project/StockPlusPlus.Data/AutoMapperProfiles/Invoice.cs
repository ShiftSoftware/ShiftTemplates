using AutoMapper;
using StockPlusPlus.Shared.DTOs.Invoice;

namespace StockPlusPlus.Data.AutoMapperProfiles;

public class Invoice : Profile
{
    public Invoice()
    {
        CreateMap<Entities.InvoiceLine, InvoiceLineDTO>()
            .DefaultEntityToDtoAfterMap()
            .ReverseMap()
            .DefaultDtoToEntityAfterMap();
    }
}
