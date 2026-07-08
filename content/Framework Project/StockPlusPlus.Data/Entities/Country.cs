using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model;
using StockPlusPlus.Data.Mappers;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs;

namespace StockPlusPlus.Data.Entities;

// Attribute-driven endpoint: Country has no controller and no repository — the secure CRUD endpoints
// are generated from these attributes (built-in repository), gated by the StockPlusPlusActionTree.Country
// TypeAuth node. DI is wired by RegisterShiftRepositories(...) and the routes are mapped by
// app.MapShiftEntityEndpoints<DB>() in Program.cs.
//
// Two endpoints over the same table demonstrate both mapping paths:
//   - "api/country"       -> ShiftEntitySecureEndpoint: built-in repository + default AutoMapper mapping.
//   - "api/countrymapped" -> ShiftEntitySecureEndpointWithMapper: built-in repository but AutoMapper is
//                            replaced by the hand-written CountryMapper (a distinct DTO keeps the two isolated).
[TemporalShiftEntity]
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
[ShiftEntitySecureEndpoint<CountryDTO, CountryDTO, StockPlusPlusActionTree>("api/country", nameof(StockPlusPlusActionTree.Country))]
[ShiftEntitySecureEndpointWithMapper<CountryMappedDTO, CountryMappedDTO, StockPlusPlusActionTree, CountryMapper>("api/countrymapped", nameof(StockPlusPlusActionTree.Country))]
public class Country : ShiftEntity<Country>, IEntityHasIdempotencyKey<Country>
{
    public string Name { get; set; } = default!;
    public Guid? IdempotencyKey { get; set; }
}
