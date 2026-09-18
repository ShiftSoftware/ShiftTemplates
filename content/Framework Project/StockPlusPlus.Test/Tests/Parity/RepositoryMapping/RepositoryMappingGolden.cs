using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using ShiftSoftware.ShiftEntity.Core;

namespace StockPlusPlus.Test.Tests.Parity.RepositoryMapping;

/// <summary>
/// One triple's frozen output through all four mapping directions — the file on disk that
/// <c>RepositoryMappingParityTests</c> compares against.
/// <para>
/// Captured ONCE from the ShiftEntity source generator (Stage 0.2 of
/// <c>docs/plans/repository-mapping-on-shiftmapper</c>) and committed. When ShiftMapper takes the four
/// directions over, these files are the only statement of what "the same result" means; the generator that
/// wrote them will not exist to ask. Regenerate a file only when its output is MEANT to change, and record why
/// in the plan's decisions — a golden regenerated from the implementation under test pins nothing.
/// </para>
/// </summary>
public sealed class RepositoryMappingGolden
{
    /// <summary>The environment variable that switches the parity tests from asserting to capturing.</summary>
    public const string CaptureVariable = "SHIFT_TEST_CAPTURE_REPOSITORY_MAPPING_GOLDENS";

    public string Triple { get; set; } = default!;

    /// <summary>How the triple's mapper was resolved when this was captured — informational, never asserted.</summary>
    public string Arm { get; set; } = default!;

    public string CapturedBy { get; set; } = default!;

    /// <summary>Members the fixture builder could not fill. A hole in the fixture is a hole in the golden; say so.</summary>
    public List<string> FixtureNotes { get; set; } = new();

    /// <summary><c>MapToView(entity)</c>.</summary>
    public JsonNode? View { get; set; }

    /// <summary><c>MapToEntity(dto, existing)</c> — the populated <c>existing</c> entity afterwards.</summary>
    public JsonNode? Entity { get; set; }

    /// <summary><c>MapToList(source)</c> run over an in-memory source (LINQ-to-Objects).</summary>
    public JsonNode? List { get; set; }

    /// <summary>
    /// The projection's expression tree, as text. Pins the SHAPE — <c>SelectWithTags</c>, the correlated child
    /// projections, the inlined select-DTO member-inits — which the result alone cannot show. Expected to be
    /// retired when ShiftMapper produces the projection (its tree is a different tree), in favour of SQL-text
    /// goldens in the LongRunning suite.
    /// </summary>
    public string? ListShape { get; set; }

    /// <summary><c>CopyEntity(source, target)</c> — the populated <c>target</c> afterwards.</summary>
    public JsonNode? Copy { get; set; }

    // ── serialization ─────────────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Indented for reviewable diffs; cycles ignored because an entity graph has back-references. Nothing else:
    /// with no hash-id service in these options every hash-id converter short-circuits and IDs serialize raw,
    /// exactly as the replication goldens are captured.
    /// </summary>
    public static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
    };

    public static JsonNode? ToNode(object? value) => value is null ? null : JsonSerializer.SerializeToNode(value, Json);

    // ── files ─────────────────────────────────────────────────────────────────────────────────────────────

    public static string Directory([CallerFilePath] string thisFile = "") =>
        Path.Combine(Path.GetDirectoryName(thisFile)!, "Goldens");

    public static string PathFor(string key) => Path.Combine(Directory(), key + ".json");

    public static bool CaptureRequested =>
        Environment.GetEnvironmentVariable(CaptureVariable) is { } v && (v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase));

    public static RepositoryMappingGolden? Load(string key)
    {
        var path = PathFor(key);
        return File.Exists(path) ? JsonSerializer.Deserialize<RepositoryMappingGolden>(File.ReadAllText(path), Json) : null;
    }

    public void Save(string key)
    {
        System.IO.Directory.CreateDirectory(Directory());
        File.WriteAllText(PathFor(key), JsonSerializer.Serialize(this, Json) + Environment.NewLine);
    }
}

/// <summary>
/// Pushes one triple's fixtures through the mapper the triple actually uses and returns the five outputs.
/// <para>
/// Generic over the triple so the call is the real <see cref="IShiftEntityMapper{TEntity, TListDTO, TViewDTO}"/>
/// interface, not reflection over four method names — whichever arm resolved (a repository override, a
/// configured generated mapper, a hand-written mapper) implements that interface and is called through it.
/// </para>
/// </summary>
public static class RepositoryMappingRun
{
    // Seeds keep the three graphs of one triple apart in the output: the entity read (1000…), the DTO written
    // (3000…) and the populated row it is written onto (5000…). A member the write leaves alone still shows
    // its 5000-series value; one it overwrites shows a 3000-series value.
    private const long EntitySeed = 1000, DtoSeed = 3000, ExistingSeed = 5000;

    public static RepositoryMappingGolden Run(MapperArm arm, IServiceProvider services)
    {
        var (entity, list, view) = (arm.Triple.Entity, arm.Triple.ListDto, arm.Triple.ViewDto);

        var method = typeof(RepositoryMappingRun)
            .GetMethod(nameof(RunTyped), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(entity, list, view);

        var golden = (RepositoryMappingGolden)method.Invoke(null, new object[] { arm.Mapper, services })!;
        golden.Triple = arm.Triple.ToString();
        golden.Arm = arm.Description;
        return golden;
    }

    private static RepositoryMappingGolden RunTyped<TEntity, TList, TView>(object mapperObject, IServiceProvider services)
    {
        var mapper = (IShiftEntityMapper<TEntity, TList, TView>)mapperObject;
        var golden = new RepositoryMappingGolden();
        var notes = new List<string>();

        // Every direction gets a FRESH graph. The graphs are deterministic, so "fresh" costs nothing and
        // guarantees one direction cannot see what another one mutated.
        TEntity Entity() => Build<TEntity>(EntitySeed, DeterministicGraph.DefaultMaxDepth, notes, typeof(TView), typeof(TList));
        TEntity Existing() => Build<TEntity>(ExistingSeed, 1, notes, typeof(TView), typeof(TList));
        TView Dto() => Build<TView>(DtoSeed, DeterministicGraph.DefaultMaxDepth, notes, typeof(TEntity));

        var read = new MappingContext(services);
        var write = new MappingContext(services, ActionTypes.Update);

        golden.View = Section(notes, "View", () => mapper.MapToView(Entity(), read));

        golden.Entity = Section(notes, "Entity", () =>
        {
            var existing = Existing();
            return mapper.MapToEntity(Dto(), existing, write) ?? existing;
        });

        IQueryable<TList>? query = null;
        golden.List = Section(notes, "List", () =>
        {
            query = mapper.MapToList(new[] { Entity() }.AsQueryable(), read);
            return query.ToList();
        });
        golden.ListShape = query?.Expression.ToString();

        golden.Copy = Section(notes, "Copy", () =>
        {
            var target = Existing();
            mapper.CopyEntity(Entity(), target, read);
            return target;
        });

        golden.FixtureNotes = notes.Distinct(StringComparer.Ordinal).ToList();
        return golden;
    }

    private static T Build<T>(long seed, int maxDepth, List<string> notes, params Type[] counterparts)
    {
        var graph = new DeterministicGraph(seed, maxDepth, counterparts);
        var value = graph.Build<T>();
        notes.AddRange(graph.Notes);
        return value;
    }

    /// <summary>Runs one direction; a direction that throws is pinned as its exception TYPE rather than lost.</summary>
    private static JsonNode? Section(List<string> notes, string name, Func<object?> run)
    {
        try
        {
            return RepositoryMappingGolden.ToNode(run());
        }
        catch (Exception ex)
        {
            var inner = ex is TargetInvocationException { InnerException: { } i } ? i : ex;
            notes.Add($"{name}: threw {inner.GetType().Name} — {inner.Message}");
            return new JsonObject { ["error"] = inner.GetType().FullName };
        }
    }
}
