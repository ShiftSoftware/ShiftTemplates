using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace StockPlusPlus.Shared.DTOs;

// Same shape as CountryDTO, but a DISTINCT type so the endpoint at "api/country-generated" (automatic maps,
// one of them customized in Mappers/StockPlusPlusMapper.cs) is fully isolated from the plain endpoint at
// "api/country" and the custom-mapper endpoint at "api/countrymapped" — maps are keyed by the (entity, list,
// view) triple, i.e. by DTO type. One type serves as both list and view DTO here, so the entity -> DTO map is
// ONE map: what the mapper class customizes shows on the view and in the list alike.
//
// This is also the triple Country drives from the ENTITY: IConfiguresShiftRepository (the built-in repository's
// shape) plus IUpsertsShiftRepository / IDeletesShiftRepository (the write hooks). The CountryRepository demo
// deliberately lives on its own CountryRepoDTO triple instead — a repository passing an options builder
// configures itself and would silently suppress the entity's configuration here, which is a build error
// (SHENT001).
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class CountryGeneratedDTO : ShiftEntityMixedDTO
{
    public override string? ID { get; set; }
    public string Name { get; set; } = default!;
}
