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
/// The oracle of the repository mapping — <c>docs/plans/repository-mapping-on-shiftmapper</c>.
/// <para>
/// For every (entity, list, view) triple the framework-owned assemblies declare — enumerated, never listed, by
/// <see cref="TripleEnumerator"/> — a deterministic fixture graph goes through the mapper the triple ACTUALLY
/// uses (resolved from the running host by <see cref="ParityArms"/>, so a repository's <c>Mapping(m =&gt; …)</c>
/// configuration, a mapper class and a repository's method overrides are all measured) in all four directions,
/// plus the SQL the list projection translates to, and the result is compared member-by-member against a frozen
/// file under <c>Tests/Parity/RepositoryMapping/Goldens</c>.
/// </para>
/// <para>
/// <b>The goldens were captured from the ShiftEntity source generator (Stage 0), diffed against ShiftMapper
/// until every difference was fixed or decided, and re-frozen from ShiftMapper (Stage 3.6).</b> A regenerated
/// golden is the implementation under test restating itself, so set
/// <c>SHIFT_TEST_CAPTURE_REPOSITORY_MAPPING_GOLDENS=1</c> to rewrite them only when the output is MEANT to
/// change, and record why in the plan's decisions.
/// </para>
/// <para>
/// One theory per direction rather than one fact per triple, so a divergence names the direction and the
/// member path — "Invoice / InvoiceListDTO / InvoiceDTO, List, InvoiceLines[0].Product.Name".
/// </para>
/// </summary>
[Collection("API Collection")]
public class RepositoryMappingParityTests
{
    private readonly CustomWebApplicationFactory factory;
    private readonly ITestOutputHelper output;

    public RepositoryMappingParityTests(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        this.factory = factory;
        this.output = output;
    }

    /// <summary>Stable, file-name-safe key per triple. The assembly tag is what keeps the sample's <c>Country</c> apart from ShiftIdentity's.</summary>
    public static string KeyOf(MappingTriple t) =>
        $"{AssemblyTag(t.Entity)}.{t.Entity.Name}__{t.ListDto.Name}__{t.ViewDto.Name}";

    private static string AssemblyTag(Type type) =>
        (type.Assembly.GetName().Name ?? "Unknown").Replace(".Data", "", StringComparison.Ordinal).Replace("ShiftSoftware.", "", StringComparison.Ordinal);

    public static TheoryData<string> Triples
    {
        get
        {
            var data = new TheoryData<string>();
            foreach (var site in TripleEnumerator.All())
                data.Add(KeyOf(site.Triple));
            return data;
        }
    }

    // One run per triple per process: the four theories over a triple share it, so the mapper is exercised
    // once and the comparison is against one consistent capture.
    private static readonly ConcurrentDictionary<string, (RepositoryMappingGolden Actual, RepositoryMappingGolden? Expected, ArmKind Kind)> runs = new();

    private (RepositoryMappingGolden Actual, RepositoryMappingGolden? Expected, ArmKind Kind) RunOnce(string key) =>
        runs.GetOrAdd(key, k =>
        {
            var site = TripleEnumerator.All().Single(s => KeyOf(s.Triple) == k);

            using var scope = factory.Services.CreateScope();
            var (arm, kind) = ParityArms.GeneratedArm(scope, site);

            Assert.True(arm is not null, $"{site.Triple} resolves no mapper of any kind ({kind}); nothing to pin.");

            var actual = RepositoryMappingRun.Run(arm!, scope.ServiceProvider);
            actual.CapturedBy = "ShiftSoftware.ShiftMapper";

            if (RepositoryMappingGolden.CaptureRequested)
            {
                actual.CapturedBy += $" — captured {DateTime.UtcNow:yyyy-MM-dd}";
                actual.Save(k);
                output.WriteLine($"captured {RepositoryMappingGolden.PathFor(k)}");
            }

            return (actual, RepositoryMappingGolden.Load(k), kind);
        });

    [Theory]
    [MemberData(nameof(Triples))]
    public void MapToView_MatchesGolden(string key) => AssertSection(key, "View", g => g.View);

    [Theory]
    [MemberData(nameof(Triples))]
    public void MapToEntity_MatchesGolden(string key) => AssertSection(key, "Entity", g => g.Entity);

    [Theory]
    [MemberData(nameof(Triples))]
    public void MapToList_MatchesGolden(string key) => AssertSection(key, "List", g => g.List);

    [Theory]
    [MemberData(nameof(Triples))]
    public void CopyEntity_MatchesGolden(string key) => AssertSection(key, "Copy", g => g.Copy);

    /// <summary>
    /// The SQL the list projection translates to. The one assertion the in-memory result cannot stand in for:
    /// LINQ-to-objects executes what SQL Server refuses, so this is where a projection that stopped translating,
    /// lost a join, or started leaving a member out shows up.
    /// </summary>
    [Theory]
    [MemberData(nameof(Triples))]
    public void MapToList_SqlMatchesGolden(string key)
    {
        var (actual, expected, _) = RunOnce(key);
        var golden = RequireGolden(key, expected);

        Assert.True(string.Equals(golden.ListSql, actual.ListSql, StringComparison.Ordinal),
            $"{actual.Triple}: the list projection's SQL diverged from the golden.\n  expected: {golden.ListSql}\n  actual:   {actual.ListSql}");
    }

    /// <summary>
    /// A fixture hole is a golden hole. The notes are pinned too, so a member that silently stopped being built
    /// — or started being built — shows up as a change rather than as a golden that quietly measures less.
    /// </summary>
    [Theory]
    [MemberData(nameof(Triples))]
    public void FixtureNotes_MatchGolden(string key)
    {
        var (actual, expected, _) = RunOnce(key);
        var golden = RequireGolden(key, expected);

        Assert.Equal(golden.FixtureNotes, actual.FixtureNotes);
    }

    private void AssertSection(string key, string section, Func<RepositoryMappingGolden, JsonNode?> pick)
    {
        var (actual, expected, kind) = RunOnce(key);
        var golden = RequireGolden(key, expected);

        var expectedNode = pick(golden);
        var actualNode = pick(actual);

        // A direction the old generator could not run on the fixture is pinned as its exception type; that is
        // a fact about the fixture worth seeing in the output, not a silent pass.
        if (expectedNode is JsonObject { Count: 1 } o && o.ContainsKey("error"))
            output.WriteLine($"{actual.Triple} [{kind}] {section}: pinned as {o["error"]}");

        var diffs = MemberPathDiff.CompareJson(expectedNode, actualNode, section);

        Assert.True(diffs.Count == 0,
            $"{actual.Triple} [{kind}] {section} diverged from the golden in {diffs.Count} place(s):\n  " +
            string.Join("\n  ", diffs.Select(d => d.ToString())));
    }

    private static RepositoryMappingGolden RequireGolden(string key, RepositoryMappingGolden? expected)
    {
        Assert.True(expected is not null,
            $"No golden for {key} at {RepositoryMappingGolden.PathFor(key)}. Every triple in the inventory must be " +
            $"pinned; capture it with {RepositoryMappingGolden.CaptureVariable}=1 while the ShiftEntity generator " +
            "is still the mapper, and commit the file.");
        return expected!;
    }
}
