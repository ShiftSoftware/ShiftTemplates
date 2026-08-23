using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace StockPlusPlus.Test.Tests.Parity;

/// <summary>
/// Step C1, first half: establish WHAT there is to compare, and prove the comparison would not be vacuous.
/// <para>
/// The parity differ's whole value rests on one fact that is easy to get wrong and impossible to notice once
/// AutoMapper is deleted: <c>ShiftRepository</c> falls back to wrapping the registered <c>IMapper</c> when
/// nothing else is configured, so a differ can compare AutoMapper against AutoMapper and report perfect parity
/// having measured nothing at all. These tests print the arm every triple actually resolves, so "what did this
/// run measure?" is answerable by reading output rather than by reading code.
/// </para>
/// <para>
/// The inventory is independently useful even if the differ is never finished: it says which triples are
/// migrated, which have a generated mapper that nothing resolves yet (gap B-1 / Step D1), and which have no
/// mapper at all and would therefore throw the day the AutoMapper fallback is removed.
/// </para>
/// </summary>
[Collection("API Collection")]
public class TripleInventoryTests
{
    private readonly CustomWebApplicationFactory factory;
    private readonly ITestOutputHelper output;

    public TripleInventoryTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        this.factory = factory;
        this.output = output;
    }

    /// <summary>
    /// Enumeration finds something from both sources. A silent zero here — an assembly that failed to load, a
    /// discovery rule that stopped matching — would make every other parity assertion pass by measuring an
    /// empty set.
    /// </summary>
    [Fact]
    public void Enumeration_FindsTriples_FromBothSources()
    {
        var sites = TripleEnumerator.All();

        Assert.NotEmpty(sites);
        Assert.Contains(sites, s => s.Origin.StartsWith("attribute endpoint"));
        Assert.Contains(sites, s => s.Origin == "repository subclass");

        // Both framework-owned assemblies are represented; losing one silently halves the harness.
        Assert.Contains(sites, s => s.Triple.Entity.Assembly == typeof(StockPlusPlus.Data.Marker).Assembly);
        Assert.Contains(sites, s => s.Triple.Entity.Assembly == typeof(ShiftSoftware.ShiftIdentity.Data.Marker).Assembly);
    }

    /// <summary>
    /// The inventory itself. Prints one row per triple and fails only on the case that cannot be lived with —
    /// a triple with no mapper of any kind, which throws per request the moment the AutoMapper fallback goes.
    /// </summary>
    [Fact]
    public void Inventory_EveryTripleResolvesAMapper_AndTheArmIsNotAutoMapper()
    {
        using var scope = factory.Services.CreateScope();

        var rows = new List<string>();
        var counts = new Dictionary<ArmKind, int>();
        var unmapped = new List<string>();
        var vacuous = new List<string>();

        foreach (var site in TripleEnumerator.All())
        {
            var (arm, kind) = ParityArms.GeneratedArm(scope, site);

            counts[kind] = counts.GetValueOrDefault(kind) + 1;
            rows.Add($"{site.Triple,-64} {kind,-20} {arm?.Description ?? "—"}");

            if (kind == ArmKind.None) unmapped.Add(site.Triple.ToString());
            if (kind == ArmKind.AutoMapperFallback) vacuous.Add(site.Triple.ToString());
        }

        output.WriteLine($"{TripleEnumerator.All().Count} triples\n");
        output.WriteLine(string.Join("\n", rows));
        output.WriteLine("\n" + string.Join("   ", counts.OrderBy(c => c.Key.ToString()).Select(c => $"{c.Key}={c.Value}")));

        // ArmKind.RegistryOnly is NOT a failure. It is the accurate statement of gap B-1: the generated mapper
        // exists, and nothing resolves it yet because ShiftRepository never consults the registry. Step D1 is
        // what changes those rows to Configured — this inventory is how that progress becomes visible.

        Assert.True(vacuous.Count == 0,
            "These triples resolve AutoMapper as their 'generated' arm, so comparing them would measure the " +
            "same object twice and pass vacuously:\n  " + string.Join("\n  ", vacuous));

        Assert.True(unmapped.Count == 0,
            "These triples have no mapper of any kind — they will throw per request once the AutoMapper " +
            "fallback is removed:\n  " + string.Join("\n  ", unmapped));
    }

    /// <summary>
    /// The baseline arm exists and is the host's own. If this cannot resolve, the oracle is already gone and
    /// every parity assertion downstream is re-deriving its expectation from the code under test.
    /// </summary>
    [Fact]
    public void Baseline_ResolvesTheHostsOwnAutoMapper()
    {
        using var scope = factory.Services.CreateScope();

        var baseline = ParityArms.Baseline(scope);

        Assert.NotNull(baseline);
    }

    /// <summary>
    /// Enumeration is stable. Two calls must agree, or a run's findings cannot be compared with the previous
    /// run's and the divergence table rots.
    /// </summary>
    [Fact]
    public void Enumeration_IsDeterministic()
    {
        var a = TripleEnumerator.All().Select(s => s.Triple.ToString()).ToList();
        var b = TripleEnumerator.All().Select(s => s.Triple.ToString()).ToList();

        Assert.Equal(a, b);
        Assert.Equal(a.Count, a.Distinct().Count());   // no triple counted twice across the two sources
    }
}
