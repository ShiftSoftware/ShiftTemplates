using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json.Nodes;

namespace StockPlusPlus.Test.Tests.Parity;

/// <summary>One reported difference, addressed by the path that reaches it.</summary>
public sealed record Difference(string MemberPath, string? Expected, string? Actual, string Kind)
{
    public override string ToString() => $"{MemberPath}  [{Kind}]  expected={Expected ?? "<none>"}  actual={Actual ?? "<none>"}";
}

/// <summary>
/// Compares two payloads and reports WHICH MEMBER differs, not that they differ.
/// <para>
/// Blob equality on serialized JSON is the obvious approach and it is why parity suites get abandoned: one
/// property moves between a base class and a derived one, every assertion fails at once for a reason that has
/// nothing to do with mapping, and the suite gets deleted rather than debugged. A member path survives that.
/// </para>
/// <para>
/// Two walks over the same rules: <see cref="Compare"/> over CLR objects (Step C1's differ) and
/// <see cref="CompareJson"/> over <see cref="JsonNode"/> (Step C2's goldens). They must stay one implementation
/// — a second comparer with slightly different null or ordering semantics would let a real divergence pass in
/// whichever suite happened to be more forgiving.
/// </para>
/// </summary>
public static class MemberPathDiff
{
    private const int MaxDepth = 12;

    // ── CLR objects ───────────────────────────────────────────────────────────────────────────────────────

    public static IReadOnlyList<Difference> Compare(object? expected, object? actual, string root)
    {
        var found = new List<Difference>();
        Walk(expected, actual, root, found, 0, new HashSet<(object, object)>(ReferencePairComparer.Instance));
        return found;
    }

    private static void Walk(object? e, object? a, string path, List<Difference> found, int depth,
        HashSet<(object, object)> seen)
    {
        if (depth > MaxDepth) return;

        if (e is null || a is null)
        {
            // Null vs non-null reports HERE and does not descend: every leaf underneath would otherwise be
            // reported too, burying the one fact that matters.
            if (!(e is null && a is null))
                found.Add(new Difference(path, Format(e), Format(a), "null-mismatch"));
            return;
        }

        if (IsLeaf(e.GetType()) || IsLeaf(a.GetType()))
        {
            if (!LeafEquals(e, a))
                found.Add(new Difference(path, Format(e), Format(a), "value"));
            return;
        }

        if (!seen.Add((e, a))) return;   // cycles: Invoice -> Lines -> Invoice

        if (e is IEnumerable ee && a is IEnumerable ae)
        {
            // Ordinal position, deliberately. A different ORDER is a different payload on the wire.
            var el = ee.Cast<object?>().ToList();
            var al = ae.Cast<object?>().ToList();

            if (el.Count != al.Count)
                found.Add(new Difference(path, $"count={el.Count}", $"count={al.Count}", "count"));

            for (var i = 0; i < Math.Min(el.Count, al.Count); i++)
                Walk(el[i], al[i], $"{path}[{i}]", found, depth + 1, seen);

            return;
        }

        foreach (var p in e.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                           .Where(p => p.GetIndexParameters().Length == 0 && p.CanRead)
                           .OrderBy(p => p.Name, StringComparer.Ordinal))
        {
            var counterpart = a.GetType().GetProperty(p.Name, BindingFlags.Public | BindingFlags.Instance);
            if (counterpart is null || !counterpart.CanRead) continue;

            Walk(Read(p, e), Read(counterpart, a), $"{path}.{p.Name}", found, depth + 1, seen);
        }
    }

    private static object? Read(PropertyInfo p, object target)
    {
        // A throwing getter is a finding, not a crash — otherwise one bad property aborts the whole run.
        try { return p.GetValue(target); }
        catch (TargetInvocationException ex) { return $"<threw {(ex.InnerException ?? ex).GetType().Name}>"; }
        catch (Exception ex) { return $"<threw {ex.GetType().Name}>"; }
    }

    // ── JsonNode ──────────────────────────────────────────────────────────────────────────────────────────

    public static IReadOnlyList<Difference> CompareJson(JsonNode? expected, JsonNode? actual, string root)
    {
        var found = new List<Difference>();
        WalkJson(expected, actual, root, found, 0);
        return found;
    }

    private static void WalkJson(JsonNode? e, JsonNode? a, string path, List<Difference> found, int depth)
    {
        if (depth > MaxDepth) return;

        var eNull = e is null || e.GetValueKind() == System.Text.Json.JsonValueKind.Null;
        var aNull = a is null || a.GetValueKind() == System.Text.Json.JsonValueKind.Null;

        if (eNull || aNull)
        {
            if (eNull != aNull)
                found.Add(new Difference(path, eNull ? "<null>" : e!.ToJsonString(), aNull ? "<null>" : a!.ToJsonString(), "null-mismatch"));
            return;
        }

        switch (e)
        {
            case JsonObject eo when a is JsonObject ao:
            {
                // Union of both sides, ordinally sorted: a member present on one side only must surface, and
                // System.Text.Json's property ORDER is a serializer implementation detail, never a payload fact.
                foreach (var name in eo.Select(kv => kv.Key).Union(ao.Select(kv => kv.Key)).OrderBy(n => n, StringComparer.Ordinal))
                {
                    eo.TryGetPropertyValue(name, out var ev);
                    ao.TryGetPropertyValue(name, out var av);

                    if (!eo.ContainsKey(name) || !ao.ContainsKey(name))
                    {
                        found.Add(new Difference($"{path}.{name}",
                            eo.ContainsKey(name) ? ev?.ToJsonString() ?? "<null>" : "<absent>",
                            ao.ContainsKey(name) ? av?.ToJsonString() ?? "<null>" : "<absent>", "member-absent"));
                        continue;
                    }

                    WalkJson(ev, av, $"{path}.{name}", found, depth + 1);
                }
                return;
            }

            case JsonArray ea when a is JsonArray aa:
            {
                if (ea.Count != aa.Count)
                    found.Add(new Difference(path, $"count={ea.Count}", $"count={aa.Count}", "count"));

                for (var i = 0; i < Math.Min(ea.Count, aa.Count); i++)
                    WalkJson(ea[i], aa[i], $"{path}[{i}]", found, depth + 1);

                return;
            }

            default:
            {
                var es = e.ToJsonString();
                var asx = a.ToJsonString();
                if (!string.Equals(es, asx, StringComparison.Ordinal))
                    found.Add(new Difference(path, es, asx, "value"));
                return;
            }
        }
    }

    // ── leaves ────────────────────────────────────────────────────────────────────────────────────────────

    private static bool IsLeaf(Type t) =>
        t.IsPrimitive || t.IsEnum || t == typeof(string) || t == typeof(decimal) || t == typeof(Guid) ||
        t == typeof(DateTime) || t == typeof(DateTimeOffset) || t == typeof(TimeSpan) ||
        t == typeof(DateOnly) || t == typeof(TimeOnly) || Nullable.GetUnderlyingType(t) is { } u && IsLeaf(u);

    private static bool LeafEquals(object e, object a) =>
        string.Equals(Format(e), Format(a), StringComparison.Ordinal);

    /// <summary>Invariant on purpose: a payload value crosses machines and locales.</summary>
    private static string Format(object? v) => v switch
    {
        null => "<null>",
        byte[] b => Convert.ToBase64String(b),
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => Convert.ToString(v, CultureInfo.InvariantCulture) ?? "<null>",
    };

    private sealed class ReferencePairComparer : IEqualityComparer<(object, object)>
    {
        public static readonly ReferencePairComparer Instance = new();
        public bool Equals((object, object) x, (object, object) y) =>
            ReferenceEquals(x.Item1, y.Item1) && ReferenceEquals(x.Item2, y.Item2);
        public int GetHashCode((object, object) o) =>
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o.Item1) * 397 ^
            System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(o.Item2);
    }
}
