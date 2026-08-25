using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Mappers;
using StockPlusPlus.Shared.DTOs;
using System.Net;
using System.Reflection;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// Integration tests for the attribute-driven endpoints. Country carries
/// <c>[ShiftEntitySecureEndpoint&lt;CountryDTO, CountryDTO, StockPlusPlusActionTree&gt;("api/country", …)]</c>
/// and has NO controller and NO repository — the DI side (built-in repository, DTO-map entry, and a mapper
/// registration only when the attribute names one) is wired by <c>RegisterShiftRepositories(...)</c>, the
/// mapping otherwise coming from the source-generated mapper, and the routes are mapped by
/// <c>app.MapShiftEntityEndpoints&lt;DB&gt;()</c>.
/// </summary>
[Collection("API Collection")]
public class AttributeEndpointTests
{
    private readonly CustomWebApplicationFactory factory;

    public AttributeEndpointTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    // RegisterShiftRepositories registered the framework's built-in repository (open generic) for the
    // repository-less, attribute-driven Country endpoint, backed by the app's DB.
    [Fact]
    public void AttributeEndpoint_RegistersBuiltInRepository_ForRepositoryLessEntity()
    {
        using var scope = factory.Services.CreateScope();

        var repo = scope.ServiceProvider.GetRequiredService<ShiftRepository<DB, Country, CountryDTO, CountryDTO>>();
        var db = scope.ServiceProvider.GetRequiredService<DB>();

        Assert.NotNull(repo);
        Assert.Same(db, repo.db);
    }

    // Country implements IConfiguresShiftRepository<Country, CountryGeneratedDTO, CountryGeneratedDTO>, so the
    // BUILT-IN repository for that triple is configured by the entity (a small mapper tweak) with no repository
    // class. The config is keyed by the triple, so only this endpoint is affected.
    [Fact]
    public void AttributeEndpoint_EntityConfiguresBuiltInRepository_ViaInterface()
    {
        using var scope = factory.Services.CreateScope();

        var repo = scope.ServiceProvider.GetRequiredService<ShiftRepository<DB, Country, CountryGeneratedDTO, CountryGeneratedDTO>>();

        // The entity configures the built-in repository via ForList, so the list projection carries the tweak.
        var row = repo.MapToList(new[] { new Country { Name = "Testland" } }.AsQueryable()).Single();

        Assert.Equal("Testland (via IConfiguresShiftRepository)", row.Name);
    }

    // Country also implements IUpsertsShiftRepository for the CountryGeneratedDTO triple, so the BUILT-IN
    // repository's upsert is driven by the entity — still no repository class. context.Base() runs the framework
    // default (which maps the DTO onto the entity); the hook's own code runs around it.
    [Fact]
    public async Task AttributeEndpoint_EntityHooksBuiltInRepositoryUpsert_ViaInterface()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider
            .GetRequiredService<ShiftRepository<DB, Country, CountryGeneratedDTO, CountryGeneratedDTO>>();

        var saved = await repo.UpsertAsync(new Country(), new CountryGeneratedDTO { Name = "  Testland  " },
            ActionTypes.Insert, userId: null, idempotencyKey: null,
            disableDefaultDataLevelAccess: true, disableGlobalFilters: true);

        // Base() ran the default mapping ("  Testland  " landed on the entity), then the hook trimmed it.
        Assert.Equal("Testland", saved.Name);
        Assert.Equal("Saved", repo.ResponseMessage!.Title);
    }

    // The delete twin: Base() applies the framework's soft delete, the hook decorates the response.
    [Fact]
    public async Task AttributeEndpoint_EntityHooksBuiltInRepositoryDelete_ViaInterface()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider
            .GetRequiredService<ShiftRepository<DB, Country, CountryGeneratedDTO, CountryGeneratedDTO>>();

        var deleted = await repo.DeleteAsync(new Country { Name = "Testland" }, userId: null,
            disableDefaultDataLevelAccess: true, disableGlobalFilters: true);

        Assert.True(deleted.IsDeleted);
        Assert.Equal("Deleted", repo.ResponseMessage!.Title);
    }

    // The write hooks are keyed by DTO triple just like the config one: Country implements them only for
    // CountryGeneratedDTO, so the CountryDTO endpoint's upsert stays the plain framework default.
    [Fact]
    public async Task AttributeEndpoint_EntityWriteHooks_DoNotLeakToOtherTriples()
    {
        using var scope = factory.Services.CreateScope();
        var repo = scope.ServiceProvider
            .GetRequiredService<ShiftRepository<DB, Country, CountryDTO, CountryDTO>>();

        var saved = await repo.UpsertAsync(new Country(), new CountryDTO { Name = "  Testland  " },
            ActionTypes.Insert, userId: null, idempotencyKey: null,
            disableDefaultDataLevelAccess: true, disableGlobalFilters: true);

        Assert.Equal("  Testland  ", saved.Name);   // untrimmed — the hook didn't run for this triple
        Assert.Null(repo.ResponseMessage);
    }

    // The interface is keyed by DTO triple: Country does NOT implement it for the CountryDTO triple, so that
    // endpoint's built-in repository is untouched (still the plain generated mapping).
    [Fact]
    public void AttributeEndpoint_EntityInterface_DoesNotLeakToOtherTriples()
    {
        using var scope = factory.Services.CreateScope();

        var repo = scope.ServiceProvider.GetRequiredService<ShiftRepository<DB, Country, CountryDTO, CountryDTO>>();

        var dto = repo.MapToView(new Country { Name = "Testland" });

        Assert.Equal("Testland", dto.Name);   // no suffix — the config didn't apply to this triple
    }

    // An attribute-driven endpoint maps without anyone writing a mapper: Country has no repository class and
    // no hand-written mapper for this triple, so the built-in repository resolves the SOURCE-GENERATED one out
    // of ShiftEntityMapperRegistry.
    //
    // This used to assert the same thing about a default AutoMapper map synthesized from the attribute. That
    // synthesis is gone along with AutoMapper, and the property worth keeping is the one a user would notice:
    // the endpoint maps, and nobody had to write the mapping.
    [Fact]
    public void AttributeEndpoint_MapsWithoutAHandWrittenMapper()
    {
        using var scope = factory.Services.CreateScope();

        var repo = scope.ServiceProvider
            .GetRequiredService<ShiftRepository<DB, Country, CountryDTO, CountryDTO>>();

        Assert.NotNull(repo.ShiftRepositoryOptions.Mapper);

        var dto = repo.MapToView(new Country { Name = "Testland" });

        Assert.NotNull(dto);
        Assert.Equal("Testland", dto.Name);
    }

    // The secure CRUD endpoints at "api/country" are generated from the entity attribute and mapped by
    // app.MapShiftEntityEndpoints<DB>(...). A mapped secure endpoint returns 200 when authorized or
    // 401/403 otherwise, but NEVER 404. (Bearer auth isn't exercisable in every environment, so we
    // assert the route exists rather than a specific authorized status.)
    [Fact]
    public async Task AttributeEndpoint_IsMapped()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("api/country");

        // 200 (authorized), 401, or 403 all prove the secure endpoint is mapped and its auth pipeline
        // ran. 404 would mean it wasn't generated/served; a 5xx would mean it's mis-wired.
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
    }

    // Country's SECOND attribute — [ShiftEntitySecureEndpointWithMapper<CountryMappedDTO, CountryMappedDTO,
    // StockPlusPlusActionTree, CountryMapper>("api/countrymapped", …)] — passes a mapper. RegisterShiftRepositories(...)
    // registers it as the IShiftEntityMapper for that (entity, DTO) triple, keeping the built-in repository.
    [Fact]
    public void AttributeEndpoint_WithMapper_RegistersCustomMapperInDI()
    {
        using var scope = factory.Services.CreateScope();

        var mapper = scope.ServiceProvider
            .GetRequiredService<IShiftEntityMapper<Country, CountryMappedDTO, CountryMappedDTO>>();

        Assert.IsType<CountryMapper>(mapper);
    }

    // The built-in repository for the mapper endpoint resolves the registered CountryMapper and uses it as
    // its inner mapper — i.e. the custom mapper is PREFERRED over the source-generated mapper (proving the
    // trailing-generic mapper path, not just that the mapper is in DI).
    [Fact]
    public void AttributeEndpoint_WithMapper_BuiltInRepositoryPrefersCustomMapper()
    {
        using var scope = factory.Services.CreateScope();

        var repo = scope.ServiceProvider
            .GetRequiredService<ShiftRepository<DB, Country, CountryMappedDTO, CountryMappedDTO>>();

        Assert.IsType<CountryMapper>(GetInnerMapper(repo));
    }

    // Isolation: the plain endpoint (api/country, CountryDTO) is unaffected by the mapper registered for the
    // mapped endpoint — it keeps its own mapping. This is only true because the mapper endpoint uses a
    // DISTINCT DTO; the mapper is keyed on the DTO, so sharing one would silently hand both endpoints the same
    // mapper. That is the whole reason CountryMappedDTO exists.
    [Fact]
    public void AttributeEndpoint_PlainEndpoint_DoesNotPickUpTheOtherEndpointsMapper()
    {
        using var scope = factory.Services.CreateScope();

        var repo = scope.ServiceProvider
            .GetRequiredService<ShiftRepository<DB, Country, CountryDTO, CountryDTO>>();

        var inner = GetInnerMapper(repo);

        Assert.NotNull(inner);
        Assert.IsNotType<CountryMapper>(inner);
    }

    // The mapper endpoint's routes are generated and served (same assertion rationale as
    // AttributeEndpoint_IsMapped above, which covers the plain endpoint).
    [Fact]
    public async Task AttributeEndpoint_WithMapper_IsMapped()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("api/countrymapped");

        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden });
    }

    // innerMapper is a protected member of ShiftRepository; the mapper chosen at construction (custom vs
    // source-generated) is exactly what we want to assert, so read it via reflection.
    private static object? GetInnerMapper(object repository)
    {
        var prop = repository.GetType().GetProperty("innerMapper", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(prop);
        return prop!.GetValue(repository);
    }
}
