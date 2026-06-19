using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs;

namespace StockPlusPlus.Data.Entities;

// Attribute-driven endpoint: Country has no controller and no repository — the secure CRUD endpoints
// at "api/country" are generated from this attribute (built-in repository + default mapper), gated by
// the StockPlusPlusActionTree.Country TypeAuth node. DI is wired by RegisterShiftRepositories(...) and
// the routes are mapped by app.MapShiftEntityEndpoints<DB>() in Program.cs.
[TemporalShiftEntity]
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
[ShiftEntitySecureEndpoint<CountryDTO, CountryDTO, StockPlusPlusActionTree>("api/country", nameof(StockPlusPlusActionTree.Country))]
public class Country : ShiftEntity<Country>, IEntityHasIdempotencyKey<Country>
{
    public string Name { get; set; } = default!;
    public Guid? IdempotencyKey { get; set; }
}
