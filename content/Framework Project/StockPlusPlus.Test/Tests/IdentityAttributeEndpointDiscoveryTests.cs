using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.App;
using ShiftSoftware.ShiftIdentity.Core.DTOs.City;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Country;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Region;
using ShiftSoftware.ShiftIdentity.Data.Entities;
using System.Linq;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// DB-free discovery coverage for ShiftIdentity's attribute-driven CRUD endpoints. ShiftIdentity ships no test
/// project of its own, and its entities were moved from ShiftIdentity.Core to ShiftIdentity.Data so they can
/// carry the source-generated mapper + repository hooks without leaking EF Core into the Blazor RCLs — so
/// discovery must now find them on the DATA assembly. These tests pin:
///   • the Phase-2 migration (Country / Region / City / App → attribute + built-in repository + generated mapper),
///   • the Phase-1 entities (Brand / Service / Department) still discover after the Core→Data relocation,
///   • the exact route strings clients depend on (byte-identical to the old <c>api/[controller]</c> output),
///   • that the (entity, list, view) triples resolve a "Generated_*" mapper and NO custom repository.
/// End-to-end CRUD behaviour + the entity hooks (City's CountryID derivation, App's duplicate-AppId check) run
/// against a live DB in the ShiftIdentity dashboard host; here we prove the wiring without one.
/// </summary>
public class IdentityAttributeEndpointDiscoveryTests
{
    private static System.Reflection.Assembly IdentityDataAssembly => typeof(ShiftSoftware.ShiftIdentity.Data.Marker).Assembly;

    private static ShiftEntityEndpointSpec DiscoverRoute(string route) =>
        ShiftEntityEndpointDiscovery.Discover(new[] { IdentityDataAssembly }).Single(s => s.Route == route);

    [Theory]
    // Phase 1 (relocation regression guard) — these already shipped; they must still discover from the Data assembly.
    [InlineData("api/IdentityBrand")]
    [InlineData("api/IdentityService")]
    [InlineData("api/IdentityDepartment")]
    // Phase 2 — the newly migrated entities.
    [InlineData("api/IdentityCountry")]
    [InlineData("api/IdentityRegion")]
    [InlineData("api/IdentityCity")]
    [InlineData("api/IdentityApp")]
    public void AllIdentityEndpoints_AreDiscovered_Secure_AndActionGated(string route)
    {
        var spec = DiscoverRoute(route);

        Assert.True(spec.Secure);
        Assert.Equal(typeof(ShiftIdentityActions), spec.ActionTreeType);
        Assert.False(string.IsNullOrEmpty(spec.ActionName));
    }

    [Theory]
    // Each migrated triple must resolve the auto-generated mapper (UseGeneratedMapper = true) and NO custom repository.
    [InlineData("api/IdentityCountry", typeof(Country), typeof(CountryListDTO), typeof(CountryDTO), nameof(ShiftIdentityActions.Countries))]
    [InlineData("api/IdentityRegion", typeof(Region), typeof(RegionListDTO), typeof(RegionDTO), nameof(ShiftIdentityActions.Regions))]
    [InlineData("api/IdentityCity", typeof(City), typeof(CityListDTO), typeof(CityDTO), nameof(ShiftIdentityActions.Cities))]
    // App uses the same DTO for both list and view.
    [InlineData("api/IdentityApp", typeof(App), typeof(AppDTO), typeof(AppDTO), nameof(ShiftIdentityActions.Apps))]
    public void MigratedEndpoint_UsesGeneratedMapper_BuiltInRepository(string route, System.Type entity, System.Type listDto, System.Type viewDto, string actionName)
    {
        var spec = DiscoverRoute(route);

        Assert.Equal(entity, spec.Entity);
        Assert.Equal(listDto, spec.ListDto);
        Assert.Equal(viewDto, spec.ViewDto);
        Assert.Equal(actionName, spec.ActionName);

        // Built-in repository (no custom TRepository) + source-generated mapper.
        Assert.Null(spec.Repository);
        Assert.NotNull(spec.Mapper);
        Assert.StartsWith("Generated_", spec.Mapper!.Name);
    }

    [Fact]
    public void RegisterShiftRepositories_RegistersGeneratedMappers_ForMigratedTriples()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.RegisterShiftRepositories(IdentityDataAssembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        Assert.StartsWith("Generated_", sp.GetService<IShiftEntityMapper<Country, CountryListDTO, CountryDTO>>()!.GetType().Name);
        Assert.StartsWith("Generated_", sp.GetService<IShiftEntityMapper<Region, RegionListDTO, RegionDTO>>()!.GetType().Name);
        Assert.StartsWith("Generated_", sp.GetService<IShiftEntityMapper<City, CityListDTO, CityDTO>>()!.GetType().Name);
        Assert.StartsWith("Generated_", sp.GetService<IShiftEntityMapper<App, AppDTO, AppDTO>>()!.GetType().Name);
    }
}
