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
}
