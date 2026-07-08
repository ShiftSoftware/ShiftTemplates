using ShiftSoftware.ShiftEntity.Core;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs;

namespace StockPlusPlus.Data.Mappers;

// Demonstrates the MAPPER form of the attribute-driven endpoint: Country carries a second endpoint
//   [ShiftEntitySecureEndpointWithMapper<CountryMappedDTO, CountryMappedDTO, StockPlusPlusActionTree, CountryMapper>("api/countrymapped", ...)]
// which keeps the framework's built-in repository (no repository class) but swaps AutoMapper for this
// hand-written mapper. RegisterShiftRepositories(...) registers it as
// IShiftEntityMapper<Country, CountryMappedDTO, CountryMappedDTO>; the built-in repository resolves and
// prefers it over the AutoMapper default (see ShiftRepository.InitCommon).
public class CountryMapper : IShiftEntityMapper<Country, CountryMappedDTO, CountryMappedDTO>
{
    public CountryMappedDTO MapToView(Country entity, IServiceProvider? serviceProvider = null)
    {
        return new CountryMappedDTO
        {
            Name = entity.Name,
        }.MapBaseFields(entity);
    }

    public Country MapToEntity(CountryMappedDTO dto, Country existing, IServiceProvider? serviceProvider = null)
    {
        existing.Name = dto.Name;
        return existing;
    }

    public IQueryable<CountryMappedDTO> MapToList(IQueryable<Country> query, IServiceProvider? serviceProvider = null)
    {
        return query.Select(e => new CountryMappedDTO
        {
            ID = e.ID.ToString(),
            Name = e.Name,
            IsDeleted = e.IsDeleted,
        });
    }

    public void CopyEntity(Country source, Country target, IServiceProvider? serviceProvider = null)
    {
        source.ShallowCopyTo(target);
    }
}
