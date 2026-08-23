using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Xunit;

namespace StockPlusPlus.Test.Tests.Parity;

/// <summary>
/// The comparer's own guard rails. Everything else in Stage C reports "no differences" as success, so a
/// comparer that silently returns an empty list makes the entire parity suite green and worthless — and once
/// AutoMapper is deleted nobody can tell that run was empty. These tests are what stops that.
/// </summary>
public class MemberPathDiffTests
{
    private sealed class Leafy { public string? Name { get; set; } public int Count { get; set; } }
    private sealed class Nested { public Leafy? Inner { get; set; } public List<Leafy>? Items { get; set; } }

    // ── the comparer actually reports ─────────────────────────────────────────────────────────────────────

    [Fact]
    public void ReportsAChangedLeaf_AtItsPath()
    {
        var diffs = MemberPathDiff.Compare(new Leafy { Name = "a" }, new Leafy { Name = "b" }, "Leafy");

        var d = Assert.Single(diffs);
        Assert.Equal("Leafy.Name", d.MemberPath);
        Assert.Equal("a", d.Expected);
        Assert.Equal("b", d.Actual);
    }

    [Fact]
    public void ReportsNothing_WhenEqual()
    {
        Assert.Empty(MemberPathDiff.Compare(
            new Leafy { Name = "a", Count = 1 }, new Leafy { Name = "a", Count = 1 }, "Leafy"));
    }

    [Fact]
    public void ReportsANestedLeaf_WithTheFullPath()
    {
        var diffs = MemberPathDiff.Compare(
            new Nested { Inner = new Leafy { Count = 1 } },
            new Nested { Inner = new Leafy { Count = 2 } }, "Nested");

        Assert.Contains(diffs, d => d.MemberPath == "Nested.Inner.Count");
    }

    [Fact]
    public void ReportsCollectionElements_ByOrdinalPosition()
    {
        // Order IS payload. Two collections holding the same set in a different order are a different response.
        var diffs = MemberPathDiff.Compare(
            new Nested { Items = new() { new Leafy { Name = "x" }, new Leafy { Name = "y" } } },
            new Nested { Items = new() { new Leafy { Name = "y" }, new Leafy { Name = "x" } } }, "Nested");

        Assert.Contains(diffs, d => d.MemberPath == "Nested.Items[0].Name");
        Assert.Contains(diffs, d => d.MemberPath == "Nested.Items[1].Name");
    }

    [Fact]
    public void ReportsACountMismatch_AtTheCollectionNode()
    {
        var diffs = MemberPathDiff.Compare(
            new Nested { Items = new() { new Leafy(), new Leafy() } },
            new Nested { Items = new() { new Leafy() } }, "Nested");

        Assert.Contains(diffs, d => d.MemberPath == "Nested.Items" && d.Kind == "count");
    }

    // A null subtree reporting every leaf underneath buries the one fact that matters.
    [Fact]
    public void ReportsNullVsNonNull_AtTheNode_AndDoesNotDescend()
    {
        var diffs = MemberPathDiff.Compare(
            new Nested { Inner = null },
            new Nested { Inner = new Leafy { Name = "a", Count = 3 } }, "Nested");

        var d = Assert.Single(diffs);
        Assert.Equal("Nested.Inner", d.MemberPath);
        Assert.Equal("null-mismatch", d.Kind);
    }

    [Fact]
    public void SurvivesACycle()
    {
        // Invoice -> Lines -> Invoice is a real shape here; a naive walk stack-overflows on it.
        var a = new Cyclic { Name = "a" }; a.Self = a;
        var b = new Cyclic { Name = "b" }; b.Self = b;

        var diffs = MemberPathDiff.Compare(a, b, "Cyclic");

        Assert.Contains(diffs, d => d.MemberPath.EndsWith("Name"));
    }

    private sealed class Cyclic { public string? Name { get; set; } public Cyclic? Self { get; set; } }

    // ── the JSON walk, which the replication goldens depend on ────────────────────────────────────────────

    [Fact]
    public void Json_ReportsAChangedValue_AtItsPath()
    {
        var diffs = MemberPathDiff.CompareJson(
            JsonNode.Parse("""{"Name":"Acme","BrandID":77}"""),
            JsonNode.Parse("""{"Name":"Acme","BrandID":78}"""), "BrandModel");

        var d = Assert.Single(diffs);
        Assert.Equal("BrandModel.BrandID", d.MemberPath);
    }

    // The reason CompareJson exists rather than string equality: property order is a serializer detail.
    [Fact]
    public void Json_IgnoresPropertyOrder()
    {
        Assert.Empty(MemberPathDiff.CompareJson(
            JsonNode.Parse("""{"a":1,"b":2}"""),
            JsonNode.Parse("""{"b":2,"a":1}"""), "X"));
    }

    [Fact]
    public void Json_ReportsAMemberPresentOnOneSideOnly()
    {
        var diffs = MemberPathDiff.CompareJson(
            JsonNode.Parse("""{"a":1,"dropped":2}"""),
            JsonNode.Parse("""{"a":1}"""), "X");

        var d = Assert.Single(diffs);
        Assert.Equal("X.dropped", d.MemberPath);
        Assert.Equal("member-absent", d.Kind);
        Assert.Equal("<absent>", d.Actual);
    }

    [Fact]
    public void Json_DistinguishesNullFromAbsentFromZero()
    {
        // A mapper that writes null where it used to write 0 changed the document. So did one that stopped
        // writing the member at all. Those are different findings and must not collapse into each other.
        Assert.Equal("null-mismatch", Assert.Single(MemberPathDiff.CompareJson(
            JsonNode.Parse("""{"a":null}"""), JsonNode.Parse("""{"a":0}"""), "X")).Kind);

        Assert.Equal("member-absent", Assert.Single(MemberPathDiff.CompareJson(
            JsonNode.Parse("""{"a":null}"""), JsonNode.Parse("""{}"""), "X")).Kind);
    }

    [Fact]
    public void Json_ReportsArrayElements_ByOrdinalPosition()
    {
        var diffs = MemberPathDiff.CompareJson(
            JsonNode.Parse("""{"t":["a","b"]}"""),
            JsonNode.Parse("""{"t":["a","c"]}"""), "X");

        Assert.Equal("X.t[1]", Assert.Single(diffs).MemberPath);
    }
}
