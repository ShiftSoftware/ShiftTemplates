using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace StockPlusPlus.Shared.DTOs;

// Same shape as CountryDTO, but a DISTINCT type so the "custom mapper" endpoint at "api/country-mapped"
// is fully isolated from the AutoMapper endpoint at "api/country" (both over the same Country table).
// The custom mapper is registered as IShiftEntityMapper<Country, CountryMappedDTO, CountryMappedDTO>,
// which is keyed on the DTO type — reusing CountryDTO would make BOTH endpoints share that mapper.
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class CountryMappedDTO : ShiftEntityMixedDTO
{
    public override string? ID { get; set; }
    public string Name { get; set; } = default!;
}
