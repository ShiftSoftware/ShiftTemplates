using Microsoft.Extensions.DependencyInjection;
using ShiftMapper;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Core.DTOs.AccessTree;
using ShiftSoftware.ShiftIdentity.Core.DTOs.App;
using ShiftSoftware.ShiftIdentity.Core.DTOs.City;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Company;
using ShiftSoftware.ShiftIdentity.Core.DTOs.CompanyBranch;
using ShiftSoftware.ShiftIdentity.Core.DTOs.CompanyCalendar;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Country;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Region;
using ShiftSoftware.ShiftIdentity.Core.DTOs.Team;
using ShiftSoftware.ShiftIdentity.Core.DTOs.User;
using ShiftSoftware.ShiftIdentity.Data.Entities;
using ShiftSoftware.ShiftIdentity.Data.Repositories;
using System.Linq;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// DB-free discovery coverage for ShiftIdentity's attribute-driven CRUD endpoints (Phases 1–3). ShiftIdentity ships
/// no test project of its own, and its entities live in ShiftIdentity.Data (moved out of .Core so they can carry the
/// source-generated mapper + repository hooks) — so discovery finds them on the DATA assembly. These tests pin:
///   • all 12 endpoints (Brand/Service/Department; Country/Region/City/App; AccessTree/Team/Company/CompanyBranch/
///     CompanyCalendar) discover, are secure, and are TypeAuth-action-gated,
///   • the exact route strings clients depend on (byte-identical to the old api/[controller] output),
///   • the built-in-repository (Rung A/B) endpoints attach NO custom repository and NO mapper — their maps are the
///     automatic ShiftMapper ones the attribute declares, which the container's IMapper covers,
///   • the Rung-C endpoints (Company, CompanyBranch) route through their THIN custom repository (kept for
///     ApplyPostODataProcessing) and carry no attribute mapper (the repository customizes the automatic maps itself).
/// End-to-end CRUD + the entity hooks run against a live DB in the ShiftIdentity dashboard host; here we prove wiring.
/// </summary>
public class IdentityAttributeEndpointDiscoveryTests
{
    private static System.Reflection.Assembly IdentityDataAssembly => typeof(ShiftSoftware.ShiftIdentity.Data.Marker).Assembly;

    private static ShiftEntityEndpointSpec DiscoverRoute(string route) =>
        ShiftEntityEndpointDiscovery.Discover(new[] { IdentityDataAssembly }).Single(s => s.Route == route);

    [Theory]
    // Phase 1
    [InlineData("api/IdentityBrand")]
    [InlineData("api/IdentityService")]
    [InlineData("api/IdentityDepartment")]
    // Phase 2
    [InlineData("api/IdentityCountry")]
    [InlineData("api/IdentityRegion")]
    [InlineData("api/IdentityCity")]
    [InlineData("api/IdentityApp")]
    // Phase 3
    [InlineData("api/IdentityAccessTree")]
    [InlineData("api/IdentityTeam")]
    [InlineData("api/IdentityCompany")]
    [InlineData("api/IdentityCompanyBranch")]
    [InlineData("api/IdentityCompanyCalendar")]
    // Phase 4
    [InlineData("api/IdentityUser")]
    public void AllIdentityEndpoints_AreDiscovered_Secure_AndActionGated(string route)
    {
        var spec = DiscoverRoute(route);

        Assert.True(spec.Secure);
        Assert.Equal(typeof(ShiftIdentityActions), spec.ActionTreeType);
        Assert.False(string.IsNullOrEmpty(spec.ActionName));
    }

    [Theory]
    // Rung A/B: built-in repository + the automatic ShiftMapper maps the attribute declares.
    [InlineData("api/IdentityCountry", typeof(Country), typeof(CountryListDTO), typeof(CountryDTO), nameof(ShiftIdentityActions.Countries))]
    [InlineData("api/IdentityRegion", typeof(Region), typeof(RegionListDTO), typeof(RegionDTO), nameof(ShiftIdentityActions.Regions))]
    [InlineData("api/IdentityCity", typeof(City), typeof(CityListDTO), typeof(CityDTO), nameof(ShiftIdentityActions.Cities))]
    [InlineData("api/IdentityApp", typeof(App), typeof(AppDTO), typeof(AppDTO), nameof(ShiftIdentityActions.Apps))]
    [InlineData("api/IdentityAccessTree", typeof(AccessTree), typeof(AccessTreeListDTO), typeof(AccessTreeDTO), nameof(ShiftIdentityActions.AccessTrees))]
    [InlineData("api/IdentityTeam", typeof(Team), typeof(TeamListDTO), typeof(TeamDTO), nameof(ShiftIdentityActions.Teams))]
    [InlineData("api/IdentityCompanyCalendar", typeof(CompanyCalendar), typeof(CompanyCalendarListDTO), typeof(CompanyCalendarDTO), nameof(ShiftIdentityActions.CompanyCalendars))]
    public void BuiltInRepoEndpoint_HasNeitherRepositoryNorMapper_AndShiftMapperCoversIt(string route, System.Type entity, System.Type listDto, System.Type viewDto, string actionName)
    {
        var spec = DiscoverRoute(route);

        Assert.Equal(entity, spec.Entity);
        Assert.Equal(listDto, spec.ListDto);
        Assert.Equal(viewDto, spec.ViewDto);
        Assert.Equal(actionName, spec.ActionName);

        Assert.Null(spec.Repository);
        Assert.Null(spec.Mapper);

        var mapper = Host().GetRequiredService<IMapper>();
        Assert.True(mapper.CanMap(entity, viewDto));
        Assert.True(mapper.CanMap(viewDto, entity));
        Assert.True(mapper.CanMap(entity, listDto));
        Assert.True(mapper.CanMap(entity, entity));
    }

    private static ServiceProvider? host;

    /// <summary>One container over RegisterShiftRepositories(identity data assembly), for the CanMap questions.</summary>
    private static IServiceProvider Host()
    {
        if (host is null)
        {
            var services = new ServiceCollection();
            services.AddOptions();
            services.RegisterShiftRepositories(IdentityDataAssembly);
            host = services.BuildServiceProvider();
        }

        return host;
    }

    [Theory]
    // Rung C: the endpoint routes through the THIN custom repository (kept for ApplyPostODataProcessing); no
    // attribute mapper (the repository customizes the automatic maps in its own builder).
    [InlineData("api/IdentityCompany", typeof(Company), typeof(CompanyListDTO), typeof(CompanyDTO), typeof(CompanyRepository), nameof(ShiftIdentityActions.Companies))]
    [InlineData("api/IdentityCompanyBranch", typeof(CompanyBranch), typeof(CompanyBranchListDTO), typeof(CompanyBranchDTO), typeof(CompanyBranchRepository), nameof(ShiftIdentityActions.CompanyBranches))]
    // Phase 4: User's endpoint routes through the surviving heavy repository (IUserRepository + public methods).
    [InlineData("api/IdentityUser", typeof(User), typeof(UserListDTO), typeof(UserDTO), typeof(UserRepository), nameof(ShiftIdentityActions.Users))]
    public void CustomRepoEndpoint_UsesThinRepository_NoAttributeMapper(string route, System.Type entity, System.Type listDto, System.Type viewDto, System.Type repository, string actionName)
    {
        var spec = DiscoverRoute(route);

        Assert.Equal(entity, spec.Entity);
        Assert.Equal(listDto, spec.ListDto);
        Assert.Equal(viewDto, spec.ViewDto);
        Assert.Equal(actionName, spec.ActionName);

        Assert.Equal(repository, spec.Repository);
        Assert.Null(spec.Mapper);
    }

    [Fact]
    public void RegisterShiftRepositories_RegistersNoIShiftEntityMapper_ForBuiltInRepoTriples()
    {
        // Nothing is registered by type for the automatic triples: the repository resolves them through
        // IMapper, and the only IShiftEntityMapper registrations are the ones a WithMapper attribute names.
        using var scope = Host().CreateScope();
        var sp = scope.ServiceProvider;

        Assert.Null(sp.GetService<IShiftEntityMapper<Country, CountryListDTO, CountryDTO>>());
        Assert.Null(sp.GetService<IShiftEntityMapper<Team, TeamListDTO, TeamDTO>>());
        Assert.Null(sp.GetService<IShiftEntityMapper<CompanyCalendar, CompanyCalendarListDTO, CompanyCalendarDTO>>());
    }
}
