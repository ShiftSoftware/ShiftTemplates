using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs;
using System.Net;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// Integration tests for the attribute-driven endpoints. Country carries
/// <c>[ShiftEntitySecureEndpoint&lt;CountryDTO, CountryDTO, StockPlusPlusActionTree&gt;("api/country", …)]</c>
/// and has NO controller and NO repository — the DI side (built-in repository, default map, DTO-map
/// entry) is wired by <c>RegisterShiftRepositories(...)</c> and the routes are mapped by
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

    // The default Country -> CountryDTO map is synthesized from the attribute (no repository class and
    // no custom profile exist for Country), so the repository's ViewAsync/AutoMapper path works.
    [Fact]
    public void AttributeEndpoint_SynthesizesDefaultMap()
    {
        using var scope = factory.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var dto = mapper.Map<CountryDTO>(new Country { Name = "Testland" });

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
}
