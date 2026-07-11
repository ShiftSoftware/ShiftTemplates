using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace StockPlusPlus.Shared.DTOs;

// Same shape as CountryDTO, but a DISTINCT type so the SOURCE-GENERATED endpoint at "api/country-generated"
// (UseGeneratedMapper = true) and the CountryRepository demo are fully isolated from the AutoMapper endpoint
// at "api/country" and the custom-mapper endpoint at "api/countrymapped" — mappers are keyed by the
// (entity, list, view) triple, i.e. by DTO type.
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class CountryGeneratedDTO : ShiftEntityMixedDTO
{
    public override string? ID { get; set; }
    public string Name { get; set; } = default!;
}
