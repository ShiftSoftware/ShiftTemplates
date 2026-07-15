using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace StockPlusPlus.Shared.DTOs;

// The triple for the CountryRepository demo (a plain repository that opts into the SOURCE-GENERATED mapper via
// options.UseGeneratedMapper() in its constructor). A DISTINCT type, for the same reason the other Country DTOs
// are distinct: mappers — and the entity-driven hooks — are keyed by the (entity, list, view) triple.
//
// It specifically must NOT share CountryGeneratedDTO's triple: Country configures THAT triple from the entity
// (IConfiguresShiftRepository), and a repository passing an options builder configures itself and takes over, so
// the entity's configuration would silently never run. That pairing is a build ERROR (SHENGEN006) — keeping this
// demo on its own triple is what keeps both demos honest.
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class CountryRepoDTO : ShiftEntityMixedDTO
{
    public override string? ID { get; set; }
    public string Name { get; set; } = default!;
}
