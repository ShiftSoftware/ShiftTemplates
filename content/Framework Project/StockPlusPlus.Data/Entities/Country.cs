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
    // The entity-driven trio, all WITHOUT a repository class and all keyed by the DTO triple — so they apply
    // only to the "api/country-generated" endpoint. The AutoMapper endpoint ("api/country") and the
    // custom-mapper endpoint ("api/countrymapped") are untouched, because Country doesn't implement these
    // interfaces for their triples.
    //   - IConfiguresShiftRepository -> shape the built-in repository (includes, mapping, filters, …)
    //   - IUpsertsShiftRepository    -> take over its upsert   (== overriding UpsertAsync in a repository)
    //   - IDeletesShiftRepository    -> take over its delete   (== overriding DeleteAsync in a repository)
    // All three receive a context deriving from ShiftRepositoryContext, so all three get .Services (the
    // request scope) and .Repository (the repository serving the call).
    IConfiguresShiftRepository<Country, CountryGeneratedDTO, CountryGeneratedDTO>,
    IUpsertsShiftRepository<Country, CountryGeneratedDTO, CountryGeneratedDTO>,
    IDeletesShiftRepository<Country, CountryGeneratedDTO, CountryGeneratedDTO>
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

    // The upsert hook — the entity's version of `override UpsertAsync(...)`. context.Base() IS
    // base.UpsertAsync(...): it runs the framework default (protected-row guard, MapToEntity, tagging, audit
    // stamping, data-level write check). Custom code goes before and/or after it; skip Base() entirely and you
    // replace the default, exactly like an override that never calls base.
    // The signature is ShiftRepository.UpsertAsync's, verbatim, plus the trailing context for what an override
    // would have reached through `this` and `base`. (context.Base(...) takes the full argument list when you
    // want to feed the default something different from what you were handed.)
    public async ValueTask<Country> UpsertAsync(
        Country entity,
        CountryGeneratedDTO dto,
        ActionTypes actionType,
        long? userId,
        Guid? idempotencyKey,
        bool disableDefaultDataLevelAccess,
        bool disableGlobalFilters,
        ShiftRepositoryUpsertContext<Country, CountryGeneratedDTO, CountryGeneratedDTO> context)
    {
        // Before the default: `dto` is the incoming payload, `actionType` tells Insert from Update, and
        // context.Services resolves scoped services (current user, tenant, …).

        var saved = await context.Base();

        // After the default: tidy up what it mapped, before the caller saves it.
        if (saved.Name is not null)
            saved.Name = saved.Name.Trim();

        context.Repository.ResponseMessage = new Message("Saved", $"{actionType} handled by the entity's upsert hook.");

        return saved;
    }

    // The delete twin — ShiftRepository.DeleteAsync's signature plus the context. context.Base() is
    // base.DeleteAsync(...) — the protected-row guard, the data-level delete check, the soft-delete flag and
    // audit stamping.
    public async ValueTask<Country> DeleteAsync(
        Country entity,
        long? userId,
        bool disableDefaultDataLevelAccess,
        bool disableGlobalFilters,
        ShiftRepositoryDeleteContext<Country, CountryGeneratedDTO, CountryGeneratedDTO> context)
    {
        var deleted = await context.Base();

        context.Repository.ResponseMessage = new Message("Deleted", $"'{deleted.Name}' archived by the entity's delete hook.");

        return deleted;
    }
}
