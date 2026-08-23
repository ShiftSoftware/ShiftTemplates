using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;

namespace StockPlusPlus.Test.Tests.Parity;

/// <summary>One side of the comparison: an object that maps, plus what it actually is.</summary>
public sealed record MapperArm(object Mapper, MappingTriple Triple, string Description)
{
    public Type MapperType => Mapper.GetType();
}

/// <summary>
/// Builds the two arms a parity run compares, and answers the question that decides whether the run means
/// anything: <b>is the "generated" arm actually the AutoMapper arm wearing a different hat?</b>
/// <para>
/// <c>ShiftRepository.InitCommon</c> falls back to wrapping the registered <c>IMapper</c> in an
/// <c>AutoMapperShiftEntityMapper</c> whenever nothing else is configured. So the most natural implementation
/// of this harness — resolve the repository, call <c>MapToView</c>, compare against <c>IMapper</c> — reports
/// perfect parity on every un-migrated triple while measuring the same object twice. It would be green, it
/// would be worthless, and once AutoMapper is deleted nobody could ever tell.
/// </para>
/// </summary>
public static class ParityArms
{
    /// <summary>
    /// The AutoMapper baseline, resolved from the RUNNING HOST rather than a hand-built container.
    /// <para>
    /// That is more faithful, not less. <c>AddShiftEntity</c> — the only code that composes the three profiles
    /// in order (repository scan, then user profiles, then endpoint default maps deduped against both) — is
    /// <c>internal</c>. A hand-rolled baseline would have to reimplement that ordering and would then drift
    /// from it silently. It would also miss ShiftIdentity's half entirely, which is registered inside
    /// <c>AddShiftIdentityDashboard</c> and is invisible from <c>Program.cs</c>.
    /// </para>
    /// </summary>
    public static IMapper Baseline(IServiceScope scope) => scope.ServiceProvider.GetRequiredService<IMapper>();

    /// <summary>
    /// Resolves the non-AutoMapper mapping for a triple, and classifies it. The classification is the
    /// deliverable of this method — a caller that ignores <see cref="ArmKind"/> can compare AutoMapper to
    /// itself and never know.
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
            if (inner is not null && !IsAutoMapperBacked(inner.GetType()))
                return (new MapperArm(inner, site.Triple, inner.GetType().Name), ArmKind.Configured);
        }

        // The repository resolved the AutoMapper fallback (or could not be built at all). The registry may
        // still hold a generated mapper that nothing wires up yet — that is gap B-1, which Step D1 fixes.
        // Measure THAT, and label the row so the difference is visible rather than assumed.
        RuntimeHelpers.RunModuleConstructor(site.Triple.Entity.Module.ModuleHandle);

        var generated = ShiftEntityMapperRegistry.Find(site.Triple.Entity, site.Triple.ListDto, site.Triple.ViewDto);
        if (generated is not null)
            return (new MapperArm(Activator.CreateInstance(generated)!, site.Triple, generated.Name), ArmKind.RegistryOnly);

        return (null, repo is null ? ArmKind.None : ArmKind.AutoMapperFallback);
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

    private static bool IsAutoMapperBacked(Type t) =>
        t.IsGenericType && t.GetGenericTypeDefinition() == typeof(AutoMapperShiftEntityMapper<,,>);

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
