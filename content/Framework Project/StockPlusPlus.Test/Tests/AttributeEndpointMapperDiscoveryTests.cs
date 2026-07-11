using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Mappers;
using StockPlusPlus.Shared.DTOs;
using System.Linq;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// Verifies the <c>…EndpointWithMapper&lt;…, TMapper&gt;</c> attribute at the discovery + DI-registration
/// level. These tests need NO database (no WebApplicationFactory / EnsureCreated), so they exercise the
/// mapper wiring even where the app's SQL-Server-2025 (native json) schema can't be created. The end-to-end
/// runtime behaviour (the built-in repository preferring the mapper over AutoMapper) is covered by
/// <see cref="AttributeEndpointTests"/>, which run against the live test database.
/// </summary>
public class AttributeEndpointMapperDiscoveryTests
{
    private static System.Reflection.Assembly DataAssembly => typeof(StockPlusPlus.Data.Marker).Assembly;

    // Country's "api/countrymapped" endpoint uses ShiftEntitySecureEndpointWithMapper<…, CountryMapper>,
    // so discovery must populate the spec's Mapper (and leave Repository null).
    [Fact]
    public void Discover_WithMapperAttribute_PopulatesMapper()
    {
        var specs = ShiftEntityEndpointDiscovery.Discover(new[] { DataAssembly });

        var mapped = specs.Single(s => s.Route == "api/countrymapped");
        Assert.Equal(typeof(CountryMapper), mapped.Mapper);
        Assert.Null(mapped.Repository);
        Assert.Equal(typeof(Country), mapped.Entity);
        Assert.Equal(typeof(CountryMappedDTO), mapped.ListDto);
        Assert.Equal(typeof(CountryMappedDTO), mapped.ViewDto);
    }

    // The plain (no trailing generic) "api/country" endpoint stays on the built-in repository + AutoMapper:
    // neither a repository nor a mapper is attached.
    [Fact]
    public void Discover_PlainEndpoint_HasNeitherRepositoryNorMapper()
    {
        var specs = ShiftEntityEndpointDiscovery.Discover(new[] { DataAssembly });

        var auto = specs.Single(s => s.Route == "api/country");
        Assert.Null(auto.Mapper);
        Assert.Null(auto.Repository);
    }

    // RegisterShiftRepositories registers the attribute's mapper as IShiftEntityMapper<Entity, List, View>,
    // which is what the built-in repository resolves at construction. Proven here without a DbContext.
    [Fact]
    public void RegisterShiftRepositories_RegistersAttributeMapper_AsIShiftEntityMapper()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.RegisterShiftRepositories(DataAssembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var mapper = scope.ServiceProvider
            .GetService<IShiftEntityMapper<Country, CountryMappedDTO, CountryMappedDTO>>();

        Assert.IsType<CountryMapper>(mapper);
    }

    // The distinct-DTO isolation: no mapper is registered for the AutoMapper endpoint's (Country, CountryDTO)
    // triple, so the built-in repository there falls back to AutoMapper.
    [Fact]
    public void RegisterShiftRepositories_DoesNotRegisterMapper_ForAutoMapperEndpoint()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.RegisterShiftRepositories(DataAssembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var mapper = scope.ServiceProvider
            .GetService<IShiftEntityMapper<Country, CountryDTO, CountryDTO>>();

        Assert.Null(mapper);
    }

    // Country's "api/country-generated" endpoint sets UseGeneratedMapper = true: discovery must resolve
    // the AUTO-GENERATED mapper from the registry (the generator discovers the triple from the attribute
    // itself and emits + registers the mapper — no mapper class is declared anywhere).
    [Fact]
    public void Discover_UseGeneratedMapperFlag_ResolvesGeneratedMapperFromRegistry()
    {
        var specs = ShiftEntityEndpointDiscovery.Discover(new[] { DataAssembly });

        var generated = specs.Single(s => s.Route == "api/country-generated");
        Assert.NotNull(generated.Mapper);
        Assert.True(typeof(IShiftEntityMapper<Country, CountryGeneratedDTO, CountryGeneratedDTO>).IsAssignableFrom(generated.Mapper));
        Assert.StartsWith("Generated_", generated.Mapper!.Name);
        Assert.Null(generated.Repository);
        Assert.Equal(typeof(Country), generated.Entity);
    }

    // The flag flows through the same registration path as WithMapper: the generated mapper ends up
    // registered as IShiftEntityMapper<Entity, List, View>, which the built-in repository resolves.
    [Fact]
    public void RegisterShiftRepositories_RegistersGeneratedMapper_AsIShiftEntityMapper()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.RegisterShiftRepositories(DataAssembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var mapper = scope.ServiceProvider
            .GetService<IShiftEntityMapper<Country, CountryGeneratedDTO, CountryGeneratedDTO>>();

        Assert.NotNull(mapper);
        Assert.StartsWith("Generated_", mapper!.GetType().Name);
    }

    // Setting the flag on a triple the generator never saw must fail loudly at discovery,
    // not silently fall back to AutoMapper.
    [Fact]
    public void Discover_UseGeneratedMapperFlag_WithoutRegisteredMapper_Throws()
    {
        var ex = Assert.Throws<System.InvalidOperationException>(() =>
            ShiftEntityEndpointDiscovery.Discover(new[] { typeof(AttributeEndpointMapperDiscoveryTests).Assembly }));

        Assert.Contains("no source-generated mapper", ex.Message);
        Assert.Contains(nameof(FlagWithoutGeneratedMapperEntity), ex.Message);
    }
}

// Deliberately broken: UseGeneratedMapper = true, but the source generator does not run on the test
// assembly, so no mapper exists for this triple. Only
// Discover_UseGeneratedMapperFlag_WithoutRegisteredMapper_Throws scans the test assembly.
[ShiftEntityEndpointAttribute<StockPlusPlus.Shared.DTOs.ProductBrand.ProductBrandListDTO, StockPlusPlus.Shared.DTOs.ProductBrand.ProductBrandDTO>("api/flag-without-generated-mapper", UseGeneratedMapper = true)]
public class FlagWithoutGeneratedMapperEntity : ShiftEntity<FlagWithoutGeneratedMapperEntity>
{
}
