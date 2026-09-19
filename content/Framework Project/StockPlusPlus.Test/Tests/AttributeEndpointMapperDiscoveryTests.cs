using Microsoft.Extensions.DependencyInjection;
using ShiftMapper;
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
/// runtime behaviour (the built-in repository preferring the attribute's mapper over the generated one) is
/// covered by <see cref="AttributeEndpointTests"/>, which run against the live test database.
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

    // The plain (no trailing generic) "api/country" endpoint attaches neither a repository nor a mapper: it
    // stays on the built-in repository, which resolves the triple's automatic ShiftMapper maps itself.
    [Fact]
    public void Discover_PlainEndpoint_HasNeitherRepositoryNorMapper()
    {
        var specs = ShiftEntityEndpointDiscovery.Discover(new[] { DataAssembly });

        var plain = specs.Single(s => s.Route == "api/country");
        Assert.Null(plain.Mapper);
        Assert.Null(plain.Repository);
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

    // The distinct-DTO isolation: CountryMapper is keyed by CountryMappedDTO, so nothing is registered in DI
    // for the plain endpoint's (Country, CountryDTO) triple. The built-in repository resolves that triple's
    // automatic maps through IMapper instead.
    [Fact]
    public void RegisterShiftRepositories_DoesNotRegisterMapper_ForPlainEndpoint()
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

    // Country's "api/country-generated" endpoint attaches nothing either: its maps are declared by the attribute
    // itself (ShiftMapper reads the marker on the attribute class in the data project's build), customized from
    // the entity's ConfigureRepository, and resolved by the built-in repository through the host's IMapper.
    [Fact]
    public void Discover_AutomaticEndpoint_HasNeitherRepositoryNorMapper()
    {
        var specs = ShiftEntityEndpointDiscovery.Discover(new[] { DataAssembly });

        var generated = specs.Single(s => s.Route == "api/country-generated");
        Assert.Null(generated.Mapper);
        Assert.Null(generated.Repository);
        Assert.Equal(typeof(Country), generated.Entity);
    }

    // RegisterShiftRepositories registers ShiftMapper for the scanned assembly: the container's IMapper declares
    // the four maps of every plain endpoint's triple — and NOT the WithMapper endpoint's, whose mapping is the
    // hand-written class. Proven here without a DbContext.
    [Fact]
    public void RegisterShiftRepositories_RegistersShiftMapper_CoveringThePlainEndpointsTriples()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.RegisterShiftRepositories(DataAssembly);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();

        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        foreach (var (list, view) in new[] { (typeof(CountryDTO), typeof(CountryDTO)), (typeof(CountryGeneratedDTO), typeof(CountryGeneratedDTO)) })
        {
            Assert.True(mapper.CanMap(typeof(Country), view));
            Assert.True(mapper.CanMap(view, typeof(Country)));
            Assert.True(mapper.CanMap(typeof(Country), list));
            Assert.True(mapper.CanMap(typeof(Country), typeof(Country)));
        }

        Assert.False(mapper.CanMap(typeof(Country), typeof(CountryMappedDTO)));
    }
}
