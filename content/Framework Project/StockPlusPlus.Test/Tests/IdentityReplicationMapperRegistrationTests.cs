using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using ShiftMapper;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using ShiftSoftware.ShiftIdentity.Core;
using ShiftSoftware.ShiftIdentity.Dashboard.AspNetCore.Extentsions;
using ShiftSoftware.ShiftIdentity.Data.Entities;
using ShiftSoftware.ShiftIdentity.Data.Replication;
using StockPlusPlus.Data.DbContext;
using Xunit;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// Pins the things a host relies on without ever spelling them out: that hosting identity
/// (<c>AddShiftIdentityDashboard&lt;DB&gt;()</c>) puts ONE replication mapper where the replication pipeline looks for it
/// (<see cref="IMapper"/>) — and only one, however many times the registration is reached — and that the
/// mapper declares every pair <c>SetUpAllIdentityReplications</c> / <c>ReplicateAllAsync</c> map without a delegate.
/// The pipeline checks each pair with <c>CanMap</c> before writing anything, so a pair missing here would fail loudly
/// at the first sync — this test moves that failure to the build. (The Functions worker's <c>AddShiftIdentity</c>
/// registers the mapper through the same idempotent call; its builder type is not referenced here.)
/// <para>
/// No host, no database. The last fact cross-checks the hand-kept list below against what the assembly actually
/// declares, so a pair ADDED to <see cref="ShiftIdentityReplicationMapper"/> has to be listed here — and golden'd in
/// <see cref="ReplicationMappingParityTests"/> — before the build goes green.
/// </para>
/// <para>
/// The registration is ShiftMapper's "package registers itself" shape: made from ShiftIdentity.Data, the mapper's own
/// assembly, through an inline <c>AddShiftMapper(o =&gt; …)</c> lambda its generator read at build time. What lands in
/// the container is that assembly's GENERATED mapper — the class the generator wrote, named by the assembly's own
/// <see cref="ShiftMapperGeneratedAttribute"/>, never the mapper class itself — plus <see cref="Mapper"/> and
/// <see cref="IMapper"/> over everything registered. ShiftMapper keeps one registry per collection and a second
/// registration of the same generated mapper changes nothing, which is why the idempotency below needs no guard of
/// the identity registration's own.
/// </para>
/// </summary>
public class IdentityReplicationMapperRegistrationTests
{
    /// <summary>The generated mapper of ShiftIdentity.Data — the one thing <c>AddShiftIdentityReplicationMapper()</c> registers of its own.</summary>
    private static readonly Type Generated = Mapper.GeneratedIn(typeof(ShiftIdentityReplicationMapper).Assembly)
        ?? throw new InvalidOperationException("ShiftIdentity.Data was built without the ShiftMapper generator.");

    private static int Registrations(IServiceCollection services, Type serviceType) =>
        services.Count(descriptor => descriptor.ServiceType == serviceType);

    [Fact]
    public void HostingIdentity_RegistersTheReplicationMapper_WithoutAnExplicitCall()
    {
        //What the template's API host does and nothing more: AddShiftIdentityDashboard<DB>() with a default
        //configuration. The mapper has to come out of that alone, because the replication wiring
        //(SetUpAllIdentityReplications) runs over an already-built container and cannot register it.
        var services = new ServiceCollection();

        services.AddControllers().AddShiftIdentityDashboard<DB>(new ShiftIdentityConfiguration());

        Assert.Equal(1, Registrations(services, Generated));
        Assert.Equal(1, Registrations(services, typeof(Mapper)));
        Assert.Equal(1, Registrations(services, typeof(IMapper)));
        Assert.Equal(0, Registrations(services, typeof(ShiftIdentityReplicationMapper)));
    }

    [Fact]
    public void Registration_IsIdempotent_SoAnExplicitHostCallCannotDoubleRegister()
    {
        var services = new ServiceCollection();

        services.AddShiftIdentityReplicationMapper();
        services.AddShiftIdentityReplicationMapper(ServiceLifetime.Scoped);

        //One generated mapper, and the FIRST registration's lifetime — a later call asking for another is a no-op
        //in ShiftMapper's registry, not a second registration. Mapper and IMapper are re-registered on every call
        //(a later call could add a generated mapper), but there is still one of each.
        Assert.Equal(1, Registrations(services, Generated));
        Assert.Equal(1, Registrations(services, typeof(Mapper)));
        Assert.Equal(1, Registrations(services, typeof(IMapper)));
        Assert.Equal(ServiceLifetime.Singleton, services.Single(d => d.ServiceType == Generated).Lifetime);
    }
    /// <summary>
    /// Every (entity, document) pair the identity replication wiring maps through the registered mapper. Kept by
    /// hand on purpose: it is the contract, and the cross-check below is what keeps it honest.
    /// </summary>
    private static readonly (Type Source, Type Destination)[] ExpectedPairs =
    [
        (typeof(Brand), typeof(BrandModel)),
        (typeof(Brand), typeof(CompanyBranchSubItemModel)),
        (typeof(Service), typeof(ServiceModel)),
        (typeof(Service), typeof(CompanyBranchSubItemModel)),
        (typeof(Department), typeof(DepartmentModel)),
        (typeof(Department), typeof(CompanyBranchSubItemModel)),
        (typeof(CompanyBranchService), typeof(CompanyBranchSubItemModel)),
        (typeof(CompanyBranchDepartment), typeof(CompanyBranchSubItemModel)),
        (typeof(CompanyBranchBrand), typeof(CompanyBranchSubItemModel)),
        (typeof(TeamCompanyBranch), typeof(CompanyBranchSubItemModel)),
        (typeof(Country), typeof(CountryModel)),
        (typeof(Region), typeof(RegionModel)),
        (typeof(Region), typeof(CityRegionModel)),
        (typeof(City), typeof(CityModel)),
        (typeof(City), typeof(CityCompanyBranchModel)),
        (typeof(Company), typeof(CompanyModel)),
        (typeof(CompanyBranch), typeof(CompanyBranchModel)),
        (typeof(Team), typeof(TeamModel)),
        (typeof(User), typeof(UserModel)),
    ];

    private static ServiceProvider Host() =>
        new ServiceCollection().AddShiftIdentityReplicationMapper().BuildServiceProvider();

    [Fact]
    public void Registration_ExposesOneInstance_UnderIMapperAndMapper()
    {
        //The interface is the door the replication pipeline resolves; the class is what application code injects.
        //ShiftMapper resolves the interface THROUGH the class registration, so they are one object per scope.
        using var host = Host();

        var byInterface = host.GetRequiredService<IMapper>();
        var byType = host.GetRequiredService<Mapper>();

        Assert.Same(byType, byInterface);
        Assert.Equal(new[] { Generated }, byType.Registered.Select(x => x.GetType()).ToArray());
    }

    [Fact]
    public void Registration_IsASingleton_SharedAcrossScopes()
    {
        //The mapper class has no dependencies and the maps read nothing scoped, so one instance serves the whole
        //host — and its compiled customizations are built once rather than once per request. Mapper takes the
        //shortest lifetime of what it holds, which here is the one Singleton.
        using var host = Host();

        using var first = host.CreateScope();
        using var second = host.CreateScope();

        Assert.Same(
            first.ServiceProvider.GetRequiredService<IMapper>(),
            second.ServiceProvider.GetRequiredService<IMapper>());
    }

    [Fact]
    public void Mapper_DeclaresEveryPairTheReplicationWiringNeeds()
    {
        using var host = Host();
        var mapper = host.GetRequiredService<IMapper>();

        var missing = ExpectedPairs
            .Where(pair => !mapper.CanMap(pair.Source, pair.Destination))
            .Select(pair => $"{pair.Source.Name} -> {pair.Destination.Name}")
            .ToList();

        Assert.True(missing.Count == 0,
            "The registered mapper does not declare these replication pairs:\n  " + string.Join("\n  ", missing));
    }

    [Fact]
    public void ExpectedPairs_MatchWhatTheMapperActuallyDeclares()
    {
        //Read from the declaration metadata ShiftMapper's generator writes into ShiftIdentity.Data — the same
        //attributes a consuming project's generator reads to fold these pairs into ITS generated mapper — rather
        //than from the generated methods, so this is the mapper's contract as the package ships it.
        var declared = typeof(ShiftIdentityReplicationMapper).Assembly
            .GetCustomAttributes(typeof(ShiftMapperDeclaredMapAttribute), inherit: false)
            .Cast<ShiftMapperDeclaredMapAttribute>()
            .Where(attribute => attribute.DeclaredBy == typeof(ShiftIdentityReplicationMapper))
            .Select(attribute => (attribute.Source, attribute.Destination))
            .ToHashSet();

        var expected = ExpectedPairs.ToHashSet();

        var undeclared = expected.Except(declared).Select(Describe).ToList();
        var unlisted = declared.Except(expected).Select(Describe).ToList();

        Assert.True(undeclared.Count == 0,
            "Listed here but not declared by ShiftIdentityReplicationMapper:\n  " + string.Join("\n  ", undeclared));
        Assert.True(unlisted.Count == 0,
            "Declared by ShiftIdentityReplicationMapper but not listed here (list it, and add a golden to " +
            "ReplicationMappingParityTests):\n  " + string.Join("\n  ", unlisted));

        static string Describe((Type Source, Type Destination) pair) => $"{pair.Source.Name} -> {pair.Destination.Name}";
    }
}
