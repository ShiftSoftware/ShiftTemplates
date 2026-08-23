using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;

namespace StockPlusPlus.Test.Tests.Parity;

/// <summary>
/// Every (entity, list, view) triple in the framework-owned code, from BOTH places one can come from:
/// attribute endpoints and <c>ShiftRepository&lt;,,,&gt;</c> subclasses.
/// <para>
/// Enumerated rather than listed, deliberately. A hand-maintained list is exactly the "whatever the author
/// remembered" failure the parity harness exists to eliminate — a new entity would silently not be measured,
/// and <c>ShiftEntityMapperRegistry</c> has no enumeration API to cross-check against.
/// </para>
/// </summary>
public static class TripleEnumerator
{
    /// <summary>
    /// EXACTLY these two assemblies. Never <c>AppDomain.GetAssemblies()</c>: the test assembly declares types
    /// that exist to prove discovery FAILS correctly (an entity with no generated mapper, which makes
    /// <c>Discover</c> throw by design) plus scratch repositories that a naive scan would count as real
    /// triples.
    /// </summary>
    public static readonly Assembly[] Assemblies =
    {
        typeof(StockPlusPlus.Data.Marker).Assembly,
        typeof(ShiftSoftware.ShiftIdentity.Data.Marker).Assembly,
    };

    public static IReadOnlyList<TripleSite> All()
    {
        var sites = new List<TripleSite>();
        var seen = new HashSet<MappingTriple>();

        // (a) Attribute endpoints. The repository type is closed exactly as the endpoint mapper closes it, so
        //     the harness resolves the same service the request would.
        foreach (var spec in ShiftEntityEndpointDiscovery.Discover(Assemblies))
        {
            var triple = new MappingTriple(spec.Entity, spec.ListDto, spec.ViewDto);
            if (!seen.Add(triple)) continue;

            sites.Add(new TripleSite(
                triple,
                spec.Repository ?? typeof(ShiftRepository<,,,>)
                    .MakeGenericType(typeof(DB), spec.Entity, spec.ListDto, spec.ViewDto),
                spec.Repository is null ? "attribute endpoint (built-in repository)" : "attribute endpoint (custom repository)"));
        }

        // (b) ShiftRepository<,,,> subclasses. Uses the GENERATOR's rule — walk the whole base chain and read
        //     the type arguments positionally — not the AutoMapper profile scanner's, which reads only the
        //     direct base and resolves ListDto to null for any DTO deriving from ShiftEntityMixedDTO (a
        //     sibling of ShiftEntityListDTO, not a subtype). The generated mapper exists exactly where the
        //     GENERATOR saw a triple, so that is the rule the harness has to match.
        foreach (var type in Assemblies.SelectMany(SafeTypes))
        {
            if (!type.IsClass || type.IsAbstract || type.ContainsGenericParameters) continue;

            var closed = ClosedShiftRepositoryBase(type);
            if (closed is null) continue;

            var args = closed.GetGenericArguments();     // <DB, Entity, ListDTO, ViewAndUpsertDTO>
            var triple = new MappingTriple(args[1], args[2], args[3]);
            if (!seen.Add(triple)) continue;

            sites.Add(new TripleSite(triple, type, "repository subclass"));
        }

        return sites.OrderBy(s => s.Triple.Entity.Name, StringComparer.Ordinal).ToList();
    }

    /// <summary>Full base-chain walk. A repository two levels below <c>ShiftRepository</c> is still a triple.</summary>
    private static Type? ClosedShiftRepositoryBase(Type type)
    {
        for (var t = type.BaseType; t is not null; t = t.BaseType)
            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ShiftRepository<,,,>))
                return t;

        return null;
    }

    private static IEnumerable<Type> SafeTypes(Assembly a)
    {
        // A single unloadable type must not blind the whole enumeration.
        try { return a.GetTypes(); }
        catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t is not null)!; }
    }
}
