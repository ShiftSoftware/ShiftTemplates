using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Microsoft.Extensions.DependencyInjection;
using StockPlusPlus.Test.Tests.Parity;
using StockPlusPlus.Test.Tests.Parity.RepositoryMapping;
using Xunit;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// Stage 2.8 of <c>docs/plans/repository-mapping-on-shiftmapper</c>: every triple's frozen golden — captured
/// from the ShiftEntity source generator — diffed against the SAME fixtures run through ShiftMapper, the mapper
/// the repository switches to in Stage 3.
/// <para>
/// Two kinds of theory. <see cref="Report"/> never fails: it writes every member-path difference of every
/// triple to the test output, which is the worklist for Stage 3 (each difference is a customization to carry
/// over as <c>o.Mapping(...)</c>, or a decision to record). The <c>*_MatchesGolden</c> theories DO fail, for the
/// triples whose repositories configure nothing — the automatic ones, where ShiftMapper has to reproduce the old
/// output on its own — up to the differences <see cref="AcceptedDifferences"/> lists, each of which is a
/// recorded decision in <c>02-open-decisions.md</c>.
/// </para>
/// </summary>
[Collection("API Collection")]
public class RepositoryMappingShiftMapperDiffTests
{
    private readonly CustomWebApplicationFactory factory;
    private readonly ITestOutputHelper output;

    public RepositoryMappingShiftMapperDiffTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        this.factory = factory;
        this.output = output;
    }

    public static TheoryData<string> Triples => RepositoryMappingParityTests.Triples;

    /// <summary>
    /// The triples whose repository (or entity) carries NO mapping configuration and no hand-written mapper —
    /// the ones ShiftMapper must match by convention alone. The rest carry a <c>UseGeneratedMapper(map =&gt; …)</c>,
    /// a <c>[ShiftEntityMapper]</c> partial, a <c>UseMapper</c>, a <c>WithMapper</c> attribute or method overrides
    /// that Stage 3 migrates; until then their differences are reported, not asserted.
    /// </summary>
    public static TheoryData<string> AutomaticTriples
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var key in Automatic)
                data.Add(key);
            return data;
        }
    }

    private static readonly string[] Automatic =
    {
        "StockPlusPlus.Country__CountryDTO__CountryDTO",                       // CountryRepository: UseGeneratedMapper() with no lambda
        "StockPlusPlus.Country__CountryRepoDTO__CountryRepoDTO",               // [ShiftEntityEndpoint<…, CountryRepository>]
        "StockPlusPlus.ProductCategory__ProductCategoryListDTO__ProductCategoryDTO",
        "StockPlusPlus.Invoice__InvoiceDeepListDTO__InvoiceDeepDTO",           // Invoice.ConfigureRepository: UseGeneratedMapper() with no lambda
        "ShiftIdentity.AccessTree__AccessTreeListDTO__AccessTreeDTO",
        "ShiftIdentity.App__AppDTO__AppDTO",
        "ShiftIdentity.Brand__BrandListDTO__BrandDTO",
        "ShiftIdentity.Country__CountryListDTO__CountryDTO",
        "ShiftIdentity.Department__DepartmentListDTO__DepartmentDTO",
        "ShiftIdentity.Service__ServiceListDTO__ServiceDTO",
    };

    /// <summary>
    /// A difference the plan has decided to accept, by section and member path. Each entry names the decision
    /// it comes from; a difference that matches none fails the automatic triples' theories.
    /// </summary>
    private static readonly (string Section, Func<string, bool> Path, string Decision)[] AcceptedDifferences =
    {
        ("Copy", p => p.EndsWith(".Tags", StringComparison.Ordinal) || p.Contains(".Tags[", StringComparison.Ordinal),
            "Q10 — CopyEntity leaves Tags alone (owned by TaggingPipeline; ignored as a destination)"),
        ("Copy", p => p.EndsWith(".IdempotencyKey", StringComparison.Ordinal),
            "Q10 — CopyEntity leaves IdempotencyKey alone (owned by the save pipeline; ignored as a destination)"),
    };

    private static readonly ConcurrentDictionary<string, (RepositoryMappingGolden? Actual, RepositoryMappingGolden? Expected, string Why)> runs = new();

    private (RepositoryMappingGolden? Actual, RepositoryMappingGolden? Expected, string Why) RunOnce(string key) =>
        runs.GetOrAdd(key, k =>
        {
            var site = TripleEnumerator.All().Single(s => RepositoryMappingParityTests.KeyOf(s.Triple) == k);

            using var scope = factory.Services.CreateScope();
            var arm = ParityArms.ShiftMapperArm(scope, site);

            if (arm is null)
                return (null, RepositoryMappingGolden.Load(k), "ShiftMapper declares no map for this triple (no marker closes it — a WithMapper endpoint or a hand-written mapper)");

            var actual = RepositoryMappingRun.Run(arm, scope.ServiceProvider);
            actual.CapturedBy = "ShiftSoftware.ShiftMapper";

            return (actual, RepositoryMappingGolden.Load(k), "");
        });

    [Theory]
    [MemberData(nameof(Triples))]
    public void Report(string key)
    {
        var (actual, expected, why) = RunOnce(key);

        if (actual is null)
        {
            output.WriteLine($"{key}: not measured — {why}");
            return;
        }

        Assert.NotNull(expected);

        var total = 0;

        foreach (var (section, pick) in Sections)
        {
            var diffs = MemberPathDiff.CompareJson(pick(expected!), pick(actual), section);
            total += diffs.Count;

            foreach (var diff in diffs)
                output.WriteLine($"{key} {section}: {diff}{Accepted(section, diff.MemberPath) ?? ""}");
        }

        foreach (var note in actual.FixtureNotes.Except(expected!.FixtureNotes))
            output.WriteLine($"{key} note (ShiftMapper only): {note}");

        output.WriteLine($"{key}: {total} difference(s) [{actual.Arm}]");
    }

    [Theory]
    [MemberData(nameof(AutomaticTriples))]
    public void MapToView_MatchesGolden(string key) => AssertSection(key, "View", g => g.View);

    [Theory]
    [MemberData(nameof(AutomaticTriples))]
    public void MapToEntity_MatchesGolden(string key) => AssertSection(key, "Entity", g => g.Entity);

    [Theory]
    [MemberData(nameof(AutomaticTriples))]
    public void MapToList_MatchesGolden(string key) => AssertSection(key, "List", g => g.List);

    [Theory]
    [MemberData(nameof(AutomaticTriples))]
    public void CopyEntity_MatchesGolden(string key) => AssertSection(key, "Copy", g => g.Copy);

    private static readonly (string Section, Func<RepositoryMappingGolden, JsonNode?> Pick)[] Sections =
    {
        ("View", g => g.View),
        ("Entity", g => g.Entity),
        ("List", g => g.List),
        ("Copy", g => g.Copy),
    };

    private void AssertSection(string key, string section, Func<RepositoryMappingGolden, JsonNode?> pick)
    {
        var (actual, expected, why) = RunOnce(key);

        Assert.True(actual is not null, $"{key}: {why}");
        Assert.NotNull(expected);

        var diffs = MemberPathDiff.CompareJson(pick(expected!), pick(actual!), section)
            .Where(d => Accepted(section, d.MemberPath) is null)
            .ToList();

        Assert.True(diffs.Count == 0,
            $"{actual!.Triple} {section} through ShiftMapper diverged from the golden in {diffs.Count} unaccepted place(s):\n  " +
            string.Join("\n  ", diffs.Select(d => d.ToString())));
    }

    private static string? Accepted(string section, string memberPath)
    {
        foreach (var (s, path, decision) in AcceptedDifferences)
        {
            if ((s == "*" || s == section) && path(memberPath))
                return $"   [accepted: {decision}]";
        }

        return null;
    }
}
