using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace StockPlusPlus.Test.Tests.Parity.RepositoryMapping;

/// <summary>
/// Builds a fully populated object graph for ANY entity or DTO type, deterministically, so every triple in the
/// inventory can be pushed through every mapping direction without a hand-written fixture per entity.
/// <para>
/// Every value is a function of a running counter and the member's name — no <c>Now</c>, no <c>NewGuid</c>, no
/// randomness — so two builds of the same type produce byte-identical graphs, which is what a golden needs.
/// Members are visited in ordinal name order, not reflection order, so the counter assigns the same number to
/// the same member on every run and every machine.
/// </para>
/// <para>
/// <b>A string is filled with something the OTHER side can parse.</b> The old generator converts
/// <c>string ↔ long/Guid/decimal/enum/DateTime</c> and <c>string ↔ List&lt;ShiftFileDTO&gt;</c> by convention, and
/// throws on malformed input by design. So the builder is handed the counterpart type(s) of the object it is
/// filling — the DTOs for an entity, the entity for a DTO — and a string member whose same-named counterpart is
/// numeric gets a number, one whose counterpart is a file list gets JSON, and so on. Without that, the first
/// <c>Latitude</c> (a string column read into a <c>decimal?</c> DTO member) would throw before anything was pinned.
/// </para>
/// <para>
/// What it cannot build it leaves at its default and RECORDS in <see cref="Notes"/>, so a skipped member is a
/// visible line in the golden rather than a silent hole: a class with no parameterless constructor (a
/// NetTopologySuite point), an interface, an <c>object</c>-typed member.
/// </para>
/// </summary>
public sealed class DeterministicGraph
{
    /// <summary>Default levels below the root that are filled: root → lines → product → brand is three.</summary>
    public const int DefaultMaxDepth = 3;

    private readonly int maxDepth;

    /// <summary>
    /// Two elements at the root and one level down — enough to see a collection mapped as a collection and to
    /// reach a grandchild through each — and one element below that, where a second element would only fan the
    /// graph out (2 × 2 × 2 navigation collections made a 160 KB golden of mostly repeated leaves).
    /// </summary>
    private static int CollectionSizeAt(int depth) => depth < 2 ? 2 : 1;

    private static readonly DateTimeOffset Epoch = new(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);

    /// <summary>
    /// Members never assigned. <c>ReloadAfterSave</c> / <c>AuditFieldsAreSet</c> are request-scoped flags the
    /// save pipeline owns; <c>Data</c> is the untyped bag on <see cref="ShiftEntitySelectDTO"/> and <see cref="ShiftFileDTO"/>.
    /// </summary>
    private static readonly HashSet<string> Skipped = new(StringComparer.Ordinal)
    {
        "ReloadAfterSave", "AuditFieldsAreSet", "Data",
    };

    private readonly long seed;
    private readonly IReadOnlyDictionary<string, Type> counterparts;
    private readonly List<string> notes = new();
    private long counter;

    /// <param name="seed">Offset for every numeric value, so two graphs of the same type (an "existing" entity
    /// and the DTO written onto it) are distinguishable in the output.</param>
    /// <param name="counterpartTypes">The types on the other side of the map; their members decide how a string
    /// member here is filled.</param>
    public DeterministicGraph(long seed, params Type[] counterpartTypes) : this(seed, DefaultMaxDepth, counterpartTypes) { }

    /// <param name="maxDepth">Levels below the root to fill. The "existing" row a DTO is written onto needs only
    /// one — enough to show what the write leaves alone — where three would pin a page of untouched fixture.</param>
    public DeterministicGraph(long seed, int maxDepth, params Type[] counterpartTypes)
    {
        this.seed = seed;
        this.maxDepth = maxDepth;
        this.counterparts = CounterpartMembers(counterpartTypes);
    }

    /// <summary>What could not be built, in the order it was met. Written into the golden.</summary>
    public IReadOnlyList<string> Notes => this.notes;

    public T Build<T>() => (T)Build(typeof(T))!;

    public object? Build(Type type) => BuildObject(type, 0, new Stack<Type>(), type.Name);

    // ── objects ───────────────────────────────────────────────────────────────────────────────────────────

    private object? BuildObject(Type type, int depth, Stack<Type> path, string where)
    {
        if (type == typeof(ShiftEntitySelectDTO))
        {
            var n = Next();
            return new ShiftEntitySelectDTO { Value = (this.seed + n).ToString(CultureInfo.InvariantCulture), Text = $"{where}-text-{n}" };
        }

        if (type == typeof(ShiftFileDTO))
            return File(Next());

        if (type.IsInterface || type.IsAbstract)
        {
            this.notes.Add($"{where}: {type.Name} is abstract or an interface — left null");
            return null;
        }

        if (type.GetConstructor(Type.EmptyTypes) is null)
        {
            this.notes.Add($"{where}: {type.Name} has no parameterless constructor — left null");
            return null;
        }

        if (path.Contains(type))
            return null;   // a back-reference (InvoiceLine.Invoice) — the graph stays a tree

        var instance = Activator.CreateInstance(type)!;
        path.Push(type);

        foreach (var property in SettableProperties(type))
        {
            if (Skipped.Contains(property.Name))
                continue;

            var value = BuildValue(property, depth, path, $"{where}.{property.Name}");
            if (value is not null)
                property.SetValue(instance, value);
        }

        path.Pop();
        return instance;
    }

    private object? BuildValue(PropertyInfo property, int depth, Stack<Type> path, string where)
    {
        var type = property.PropertyType;
        var underlying = Nullable.GetUnderlyingType(type) ?? type;

        if (underlying == typeof(string))
            return BuildString(property.Name, where);

        if (underlying.IsEnum)
            return Enum.GetValues(underlying).Cast<object>().Last();

        if (TryBuildScalar(underlying, property.Name, out var scalar))
            return scalar;

        if (underlying == typeof(object) || typeof(Delegate).IsAssignableFrom(underlying) || underlying == typeof(Type))
        {
            this.notes.Add($"{where}: {underlying.Name} — left null");
            return null;
        }

        if (TryGetDictionary(underlying, out var keyType, out var valueType))
            return BuildDictionary(underlying, keyType, valueType, depth, path, where);

        if (TryGetElement(underlying, out var element))
            return BuildCollection(underlying, element, depth, path, where);

        // A class member. Composed one level deeper, up to the cap; a child DTO / navigation beyond it stays null.
        if (depth >= this.maxDepth)
            return null;

        return BuildObject(underlying, depth + 1, path, where);
    }

    // ── scalars ───────────────────────────────────────────────────────────────────────────────────────────

    private bool TryBuildScalar(Type type, string name, out object? value)
    {
        var n = 0L;
        long N() => n == 0 ? (n = Next()) : n;

        value = type switch
        {
            _ when type == typeof(bool) => name != "IsDeleted",   // a deleted root would be filtered by a repository, never by a mapper
            _ when type == typeof(long) => this.seed + N(),
            _ when type == typeof(int) => (int)(this.seed + N()),
            _ when type == typeof(short) => (short)N(),
            _ when type == typeof(byte) => (byte)(N() % 200),
            _ when type == typeof(decimal) => this.seed + N() + 0.5m,
            _ when type == typeof(double) => this.seed + N() + 0.25,
            _ when type == typeof(float) => (float)(this.seed + N() + 0.75),
            _ when type == typeof(char) => (char)('a' + N() % 26),
            _ when type == typeof(Guid) => GuidOf(this.seed + N()),
            _ when type == typeof(DateTimeOffset) => Epoch.AddDays(N()),
            _ when type == typeof(DateTime) => Epoch.UtcDateTime.AddDays(N()),
            _ when type == typeof(DateOnly) => DateOnly.FromDateTime(Epoch.UtcDateTime).AddDays((int)N()),
            _ when type == typeof(TimeOnly) => TimeOnly.FromDateTime(Epoch.UtcDateTime).AddMinutes(N()),
            _ when type == typeof(TimeSpan) => TimeSpan.FromMinutes(N()),
            _ => null,
        };

        return value is not null;
    }

    /// <summary>
    /// A string the counterpart can read back: the same-named member on the other side decides the shape.
    /// <c>ID</c>-suffixed names are numeric even with no counterpart, because that is what every framework
    /// convention (<c>ToSelectDTO</c>, hash ids) expects of them.
    /// </summary>
    private object BuildString(string name, string where)
    {
        var n = Next();

        if (!this.counterparts.TryGetValue(name, out var other))
            return EndsWithId(name) ? (this.seed + n).ToString(CultureInfo.InvariantCulture) : $"{name}-{n}";

        var target = Nullable.GetUnderlyingType(other) ?? other;

        if (target == typeof(List<ShiftFileDTO>))
            return JsonSerializer.Serialize(new List<ShiftFileDTO> { File(n), File(Next()) });

        if (target == typeof(long) || target == typeof(int) || target == typeof(short) || target == typeof(byte))
            return (this.seed + n).ToString(CultureInfo.InvariantCulture);

        if (target == typeof(decimal) || target == typeof(double) || target == typeof(float))
            return (this.seed + n).ToString(CultureInfo.InvariantCulture) + ".5";

        if (target == typeof(bool))
            return "true";

        if (target == typeof(Guid))
            return GuidOf(this.seed + n).ToString();

        if (target == typeof(DateTimeOffset) || target == typeof(DateTime))
            return Epoch.AddDays(n).ToString("O", CultureInfo.InvariantCulture);

        if (target == typeof(DateOnly))
            return DateOnly.FromDateTime(Epoch.UtcDateTime).AddDays((int)n).ToString("O", CultureInfo.InvariantCulture);

        if (target == typeof(TimeOnly))
            return TimeOnly.FromDateTime(Epoch.UtcDateTime).AddMinutes(n).ToString("O", CultureInfo.InvariantCulture);

        if (target == typeof(TimeSpan))
            return TimeSpan.FromMinutes(n).ToString("c", CultureInfo.InvariantCulture);

        if (target.IsEnum)
            return Enum.GetNames(target).Last();

        if (target == typeof(ShiftEntitySelectDTO))
            return (this.seed + n).ToString(CultureInfo.InvariantCulture);

        return EndsWithId(name) ? (this.seed + n).ToString(CultureInfo.InvariantCulture) : $"{name}-{n}";
    }

    // ── collections ───────────────────────────────────────────────────────────────────────────────────────

    private object? BuildCollection(Type declared, Type element, int depth, Stack<Type> path, string where)
    {
        var elementUnderlying = Nullable.GetUnderlyingType(element) ?? element;
        var isClass = elementUnderlying.IsClass && elementUnderlying != typeof(string);

        if (isClass && (depth >= this.maxDepth || path.Contains(elementUnderlying)))
            return null;

        var items = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(element))!;

        for (var i = 0; i < CollectionSizeAt(depth); i++)
        {
            object? item;

            if (elementUnderlying == typeof(string))
                item = $"{where}[{i}]-{Next()}";
            else if (elementUnderlying.IsEnum)
                item = Enum.GetValues(elementUnderlying).GetValue(i % Enum.GetValues(elementUnderlying).Length);
            else if (TryBuildScalar(elementUnderlying, where, out var scalar))
                item = scalar;
            else if (isClass)
                item = BuildObject(elementUnderlying, depth + 1, path, $"{where}[{i}]");
            else
            {
                this.notes.Add($"{where}: cannot build elements of {element.Name} — left empty");
                break;
            }

            if (item is null)
                return null;   // an element type that cannot be built: the whole member stays default

            items.Add(item);
        }

        return Materialize(declared, element, items);
    }

    private object? BuildDictionary(Type declared, Type keyType, Type valueType, int depth, Stack<Type> path, string where)
    {
        if (keyType != typeof(string))
        {
            this.notes.Add($"{where}: dictionary keyed by {keyType.Name} — left null");
            return null;
        }

        var dictionaryType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
        if (!declared.IsAssignableFrom(dictionaryType))
        {
            this.notes.Add($"{where}: {declared.Name} is not a Dictionary<,> — left null");
            return null;
        }

        var dictionary = (IDictionary)Activator.CreateInstance(dictionaryType)!;
        var valueUnderlying = Nullable.GetUnderlyingType(valueType) ?? valueType;

        object? value;
        if (valueUnderlying == typeof(string)) value = $"{where}-value-{Next()}";
        else if (TryBuildScalar(valueUnderlying, where, out var scalar)) value = scalar;
        else if (valueUnderlying.IsClass && depth < this.maxDepth) value = BuildObject(valueUnderlying, depth + 1, path, $"{where}[key1]");
        else value = null;

        if (value is null)
            return null;

        dictionary["key1"] = value;
        return dictionary;
    }

    /// <summary>Turns a <c>List&lt;T&gt;</c> into whatever the member declares — array, <c>HashSet</c>, or any interface a list satisfies.</summary>
    private object? Materialize(Type declared, Type element, IList items)
    {
        if (declared.IsArray)
        {
            var array = Array.CreateInstance(element, items.Count);
            items.CopyTo(array, 0);
            return array;
        }

        if (declared.IsAssignableFrom(items.GetType()))
            return items;

        var hashSet = typeof(HashSet<>).MakeGenericType(element);
        if (declared.IsAssignableFrom(hashSet))
            return Activator.CreateInstance(hashSet, items);

        if (declared.GetConstructor(Type.EmptyTypes) is not null && typeof(IList).IsAssignableFrom(declared))
        {
            var custom = (IList)Activator.CreateInstance(declared)!;
            foreach (var item in items) custom.Add(item);
            return custom;
        }

        this.notes.Add($"{declared.Name}: no way to materialise a collection of {element.Name} into it — left null");
        return null;
    }

    // ── helpers ───────────────────────────────────────────────────────────────────────────────────────────

    private long Next() => ++this.counter;

    private static Guid GuidOf(long n) => Guid.ParseExact($"00000000-0000-0000-0000-{n:D12}", "D");

    private static bool EndsWithId(string name) =>
        name.Length > 2 && name.EndsWith("ID", StringComparison.OrdinalIgnoreCase);

    private static ShiftFileDTO File(long n) => new()
    {
        Name = $"file-{n}.png",
        AccountName = "account",
        ContainerName = "container",
        Blob = $"blob-{n}",
        Url = $"https://files/{n}.png",
        ContentType = "image/png",
        Size = 1000 + n,
        Width = 10,
        Height = 20,
    };

    /// <summary>Public instance properties with a public setter, in ordinal name order.</summary>
    public static IEnumerable<PropertyInfo> SettableProperties(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.SetMethod is { IsPublic: true } && p.GetIndexParameters().Length == 0)
            .OrderBy(p => p.Name, StringComparer.Ordinal);

    /// <summary>Member name → type across the counterpart types; the first type to declare a name wins.</summary>
    private static IReadOnlyDictionary<string, Type> CounterpartMembers(Type[] types)
    {
        var map = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        foreach (var type in types)
            foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                map.TryAdd(property.Name, property.PropertyType);

        return map;
    }

    private static bool TryGetDictionary(Type type, out Type key, out Type value)
    {
        var dictionary = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IDictionary<,>) ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDictionary<,>));

        if (dictionary is null)
        {
            key = value = typeof(object);
            return false;
        }

        var args = dictionary.GetGenericArguments();
        key = args[0];
        value = args[1];
        return true;
    }

    private static bool TryGetElement(Type type, out Type element)
    {
        if (type.IsArray)
        {
            element = type.GetElementType()!;
            return true;
        }

        var enumerable = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>) ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

        if (enumerable is null || type == typeof(string))
        {
            element = typeof(object);
            return false;
        }

        element = enumerable.GetGenericArguments()[0];
        return true;
    }
}
