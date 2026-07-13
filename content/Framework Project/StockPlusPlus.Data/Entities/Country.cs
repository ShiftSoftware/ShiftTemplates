using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.EFCore;
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
// Three endpoints over the same table demonstrate the mapping paths (a distinct DTO per endpoint keeps
// them isolated — mappers are keyed by the (entity, list, view) triple):
//   - "api/country"           -> ShiftEntitySecureEndpoint: built-in repository + default AutoMapper mapping.
//   - "api/countrymapped"     -> ShiftEntitySecureEndpointWithMapper: built-in repository but AutoMapper is
//                                replaced by the hand-written CountryMapper.
//   - "api/country-generated" -> UseGeneratedMapper = true: built-in repository + the SOURCE-GENERATED
//                                mapper the generator auto-discovers and emits for the triple — no mapper
//                                class is declared anywhere.
[TemporalShiftEntity]
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
[ShiftEntitySecureEndpoint<CountryDTO, CountryDTO, StockPlusPlusActionTree>("api/country", nameof(StockPlusPlusActionTree.Country))]
[ShiftEntitySecureEndpointWithMapper<CountryMappedDTO, CountryMappedDTO, StockPlusPlusActionTree, CountryMapper>("api/countrymapped", nameof(StockPlusPlusActionTree.Country))]
[ShiftEntitySecureEndpoint<CountryGeneratedDTO, CountryGeneratedDTO, StockPlusPlusActionTree>("api/country-generated", nameof(StockPlusPlusActionTree.Country), UseGeneratedMapper = true)]
public class Country : ShiftEntity<Country>, IEntityHasIdempotencyKey<Country>,
    // Small config for the built-in (attribute-endpoint) repository WITHOUT a repository class. Keyed by the
    // DTO triple, so this applies only to the "api/country-generated" endpoint — the AutoMapper endpoint
    // ("api/country") and the custom-mapper endpoint ("api/countrymapped") are untouched (Country doesn't
    // implement the interface for their triples).
    IConfiguresShiftRepository<Country, CountryGeneratedDTO, CountryGeneratedDTO>
{
    public string Name { get; set; } = default!;
    public Guid? IdempotencyKey { get; set; }

    public void ConfigureRepository(ShiftRepositoryConfigurationContext<Country, CountryGeneratedDTO, CountryGeneratedDTO> context)
    {
        // A small custom mapping on the built-in repository, no repository class needed. context.Services is
        // the request scope — resolve scoped services here if the config needs them (current user, tenant, …),
        // e.g. context.Services.GetService<ICurrentUserProvider>().
        context.Options.UseGeneratedMapper(map => map.ForList(d => d.Name, e => e.Name + " (via IConfiguresShiftRepository)"));
    }
}
