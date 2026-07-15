using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace StockPlusPlus.Shared.DTOs;

// Same shape as CountryDTO, but a DISTINCT type so the SOURCE-GENERATED endpoint at "api/country-generated"
// (UseGeneratedMapper = true) is fully isolated from the AutoMapper endpoint at "api/country" and the
// custom-mapper endpoint at "api/countrymapped" — mappers are keyed by the (entity, list, view) triple, i.e.
// by DTO type.
//
// This is also the triple Country drives from the ENTITY: IConfiguresShiftRepository (a mapper tweak) plus
// IUpsertsShiftRepository / IDeletesShiftRepository (the write hooks). The CountryRepository demo deliberately
// lives on its own CountryRepoDTO triple instead — a repository passing an options builder configures itself and
// would silently suppress the entity's configuration here, which is a build error (SHENGEN006).
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class CountryGeneratedDTO : ShiftEntityMixedDTO
{
    public override string? ID { get; set; }
    public string Name { get; set; } = default!;
}
