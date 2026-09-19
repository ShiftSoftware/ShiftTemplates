using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.DependencyInjection;
using ShiftMapper;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace StockPlusPlus.Test.Tests.Parity;

/// <summary>One side of the comparison: an object that maps, plus what it actually is.</summary>
public sealed record MapperArm(object Mapper, MappingTriple Triple, string Description)
{
    public Type MapperType => Mapper.GetType();
}

/// <summary>
/// Resolves the mapper a triple actually uses, and classifies HOW it was resolved.
/// <para>
/// The classification used to carry the weight of this harness. While AutoMapper was still the fallback,
/// <c>ShiftRepository.InitCommon</c> wrapped the registered <c>IMapper</c> whenever nothing else was
/// configured, so the most natural parity run — resolve the repository, compare against <c>IMapper</c> —
/// measured the same object twice and reported perfect parity on every un-migrated triple. Green, worthless,
/// and undetectable the day AutoMapper was deleted. That fallback is gone, so the vacuous arm is now
/// unreachable rather than merely detected; what stays useful is <see cref="ArmKind.None"/>, a triple that
/// fails startup validation.
/// </para>
/// </summary>
public static class ParityArms
{
    /// <summary>
    /// Resolves the mapping a triple actually uses, and classifies it. The classification is the deliverable
    /// of this method — while AutoMapper was still the fallback, a caller that ignored
    /// <see cref="ArmKind"/> could compare AutoMapper to itself and never know.
    /// </summary>
    public static (MapperArm? Arm, ArmKind Kind) GeneratedArm(IServiceScope scope, TripleSite site)
    {
        object? repo = null;
        try { repo = scope.ServiceProvider.GetService(site.RepositoryType!); }
        catch { /* a repository that cannot be constructed is reported below, not thrown from here */ }

        if (repo is not null)
        {
            // A repository that overrides the mapping methods IS the mapper; there is no inner one to inspect.
            if (OverridesAnyMappingMethod(site.RepositoryType!))
                return (new MapperArm(repo, site.Triple, $"{site.RepositoryType!.Name} (method override)"), ArmKind.RepositoryOverride);

            var inner = InnerMapper(repo);
            if (inner is not null)
                return (new MapperArm(inner, site.Triple, inner.GetType().Name), ArmKind.Configured);
        }

        // The repository resolved nothing, or could not be built at all. The registry may still hold a
        // generated mapper that nothing wires up — that was gap B-1, closed by Step D1 when ShiftRepository
        // started consulting the registry itself. Measure THAT, and label the row so it stays visible.
        RuntimeHelpers.RunModuleConstructor(site.Triple.Entity.Module.ModuleHandle);

        var generated = ShiftEntityMapperRegistry.Find(site.Triple.Entity, site.Triple.ListDto, site.Triple.ViewDto);
        if (generated is not null)
            return (new MapperArm(Activator.CreateInstance(generated)!, site.Triple, generated.Name), ArmKind.RegistryOnly);

        return (null, ArmKind.None);
    }

    /// <summary>
    /// The SAME triple through ShiftMapper: the host's registered <see cref="IMapper"/> behind the adapter the
    /// repository itself resolves when nothing older covers the triple (<see cref="ShiftMapperEntityMapper{EntityType, ListDTO, ViewAndUpsertDTO}"/>).
    /// Stage 2 of <c>docs/plans/repository-mapping-on-shiftmapper</c>: the old generated mapper still wins in the
    /// repository, so this arm is what the goldens are DIFFED against, not what they were captured from. Null
    /// when the host registered no ShiftMapper or its generated mapper does not declare all four maps.
    /// <para>
    /// The repository is resolved first so a <c>Mapping(...)</c> configuration it carries reaches the mapper
    /// before the maps run — exactly the order a request sees.
    /// </para>
    /// </summary>
    public static MapperArm? ShiftMapperArm(IServiceScope scope, TripleSite site)
    {
        if (scope.ServiceProvider.GetService<IMapper>() is not { } mapper)
            return null;

        if (site.RepositoryType is not null)
        {
            try { scope.ServiceProvider.GetService(site.RepositoryType); }
            catch { /* the repository's own construction problems are the generated arm's finding, not this one's */ }
        }

        var adapter = typeof(ShiftMapperEntityMapper<,,>).MakeGenericType(site.Triple.Entity, site.Triple.ListDto, site.Triple.ViewDto);

        if (adapter.GetMethod("Covers")!.Invoke(null, new object[] { mapper }) is not true)
            return null;

        var instance = Activator.CreateInstance(adapter, mapper, scope.ServiceProvider.GetService<ShiftEntityMappingContext>())!;

        return new MapperArm(instance, site.Triple, "ShiftMapper (IMapper behind ShiftMapperEntityMapper)");
    }

    /// <summary>
    /// <c>ShiftRepositoryOptions.Mapper</c> has a public getter reached through the repository's public
    /// <c>ShiftRepositoryOptions</c> property — no reflection into private state required, so this does not
    /// break the next time the field is renamed.
    /// </summary>
    private static object? InnerMapper(object repository)
    {
        var options = repository.GetType()
            .GetProperty("ShiftRepositoryOptions", BindingFlags.Public | BindingFlags.Instance)?
            .GetValue(repository);

        return options?.GetType()
            .GetProperty("Mapper", BindingFlags.Public | BindingFlags.Instance)?
            .GetValue(options);
    }

    private static readonly string[] MappingMethods = { "MapToView", "MapToEntity", "MapToList", "CopyEntity" };

    /// <summary>True when the repository declares a mapping method itself rather than inheriting the base one.</summary>
    private static bool OverridesAnyMappingMethod(Type repositoryType)
    {
        for (var t = repositoryType; t is not null && t != typeof(object); t = t.BaseType)
        {
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ShiftRepository<,,,>))
                return false;   // reached the base without finding an override

            if (t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                 .Any(m => MappingMethods.Contains(m.Name)))
                return true;
        }

        return false;
    }
}
