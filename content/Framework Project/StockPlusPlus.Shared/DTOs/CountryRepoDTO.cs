using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace StockPlusPlus.Shared.DTOs;

// The triple for the CountryRepository demo (a plain repository whose four maps are the AUTOMATIC ones — nothing
// configured). A DISTINCT type, for the same reason the other Country DTOs are distinct: maps — and the
// entity-driven hooks — are keyed by the (entity, list, view) triple.
//
// It specifically must NOT share CountryGeneratedDTO's triple: Country configures THAT triple from the entity
// (IConfiguresShiftRepository), and a pair is configured in ONE place — two configurations of one pair are a build
// ERROR (SM0050), and a repository passing an options builder takes over the entity's hooks besides — keeping this
// demo on its own triple is what keeps both demos honest.
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class CountryRepoDTO : ShiftEntityMixedDTO
{
    public override string? ID { get; set; }
    public string Name { get; set; } = default!;
}
