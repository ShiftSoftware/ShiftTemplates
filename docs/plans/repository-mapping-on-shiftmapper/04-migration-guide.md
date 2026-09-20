# Migration guide — from ShiftEntity's generated mapping to ShiftMapper

Written to be published with the release notes of the first framework release carrying the change. Applies to
any project that uses ShiftEntity's generated mapping: `ADP.*`, `Menu` (the StockPlusPlus sample and
ShiftIdentity are already migrated). **There is no transition release** (Q7): in that release the old spellings —
`UseGeneratedMapper(...)`, `UseGeneratedMapper = true`, `[ShiftEntityMapper]`, `[ShiftEntityMapperIgnore]`,
`[ShiftEntityMapperMaxDepth]`, `SelectWithTags` — no longer exist, so the build tells you exactly what is left to
migrate, and a project that is not ready stays on the previous framework version.

## The one-paragraph version

Your repository's four maps (entity ↔ view DTO, entity → list DTO, entity → entity) are now declared for you
by ShiftMapper, from the repository's type arguments — nested children included, no attribute, no class.
Delete every `UseGeneratedMapper(...)` call and every `UseGeneratedMapper = true`. Where you customized a
member, write an ordinary ShiftMapper mapper class (`: ShiftMapperBase`) declaring **only the pairs you
customize**; each `CreateMap` replaces the automatic map for that pair and the others stay automatic. **The
repository itself configures nothing about its maps** except, optionally, how deep they nest
(`o.Mapping(m => m.Nested(n))`). Add `builder.Services.AddShiftMapper();` to the host. Everything else —
repository overrides, hand-written `IShiftEntityMapper`, `MappingHelpers` — is unchanged. The maps are also
available to any code that injects `Mapper`.

## Line by line

| You have | Write instead | Notes |
|---|---|---|
| `x => x.UseGeneratedMapper()` | *(nothing)* | the default is ShiftMapper |
| `UseGeneratedMapper = true` on an endpoint attribute | remove the property | always automatic |
| `UseGeneratedMapper(map => map …)` | a class `FooMapper : ShiftMapperBase` next to the repository; the lines below go in its constructor, on `CreateMap<E, V>()` (view), `.ReverseMap()` (write), `CreateMap<E, L>()` (list), `CreateMap<E, E>()` (copy) | the repository's base call becomes `base(db)` (or keeps its Includes) |
| `map.ForView(d => d.X, e => …)` | `CreateMap<E, V>().ForMember(d => d.X, o => o.MapFrom(e => …))` | |
| `map.ForView(d => d.X, (e, ctx) => ctx.Services.GetService<T>()…)` | `var t = new Lazy<T>(() => Services.GetRequiredService<T>());` in the constructor, `t.Value` inside `MapFrom` | not a constructor parameter — see "different, not renamed" |
| `map.ForEntity(e => e.X, dto => …)` | `CreateMap<E, V>()….ReverseMap().ForMember(e => e.X, o => o.MapFrom(dto => …))` | the write map is the REVERSE of the view map, as the automatic one is |
| `map.ForEntity(e => e.X, (dto, existing, ctx) => …)` | `….ReverseMap().ForMember(e => e.X, o => o.Ignore()).AfterMap((dto, entity) => entity.X = …)` | `entity` IS the existing tracked row |
| `map.AfterEntity((dto, existing, ctx) => …)` | `….ReverseMap().AfterMap((dto, entity) => …)` | |
| `ctx.ActionType` | `IShiftEntityMappingContext` from `Services` (lazily), `.ActionType` inside the value | |
| `map.ForList(d => d.X, e => expr)` | `CreateMap<E, L>().ForMember(d => d.X, o => o.MapFrom(e => expr))` | still an expression; still runs in SQL |
| `map.ForCopy(e => e.X, src => …)` | `CreateMap<E, E>().ForMember(e => e.X, o => o.MapFrom(src => …))` | |
| `map.Ignore(d => d.X)` (all directions) | `.ForMember(d => d.X, o => o.Ignore())` on each map the member exists in | usually one or two directions |
| `map.IgnoreView(d => d.X)` | `CreateMap<E, V>().ForMember(d => d.X, o => o.Ignore())` | |
| `map.IgnoreEntity(e => e.X)` | `….ReverseMap().ForMember(e => e.X, o => o.Ignore())` | |
| `map.IgnoreList(d => d.X)` | `CreateMap<E, L>().ForMember(d => d.X, o => o.Ignore())` | |
| `map.IgnoreCopy(e => e.X)` | `CreateMap<E, E>().ForMember(e => e.X, o => o.Ignore())` | |
| `[ShiftEntityMapperIgnore]` on a property | the `Ignore` line above for each direction | no attribute replaces it |
| `map.ForViewChildren(d => d.Lines, e => e.Lines)` / `ForEntityChildren` / `ForListChildren` (no `configureChild`) | *(nothing)* — children nest automatically | |
| `… , child => child.For(c => c.X, …)` | customize the child pair once, in a mapper class: `CreateMap<Line, LineDTO>().ForMember(c => c.X, …)` | applies inside every parent |
| `map.MaxDepth(n)` / `[ShiftEntityMapperMaxDepth(n)]` | `o.Mapping(m => m.Nested(n))` in the repository's builder (or `context.Options.Mapping(...)` in the entity's `ConfigureRepository`) | the ONE thing a repository says about its maps; no attribute replaces it; a constant, read at build time |
| `map.CaseSensitive()` | in a mapper class: `CreateMap<…>(o => o.Matching = PropertyMatching.CaseSensitive)` | |
| `[ShiftEntityMapper] partial class Foo : IShiftEntityMapper<E,L,V>` with `Configure` | `class FooMapper : ShiftMapperBase` declaring the customized pairs | not partial, no attribute, no interface; replaces the automatic map for those pairs |
| `public E MapToEntity(...) { existing = MapToEntityGenerated(...); …; return existing; }` | `CreateMap<V, E>().AfterMap((dto, entity) => …)` | "conventions first, then my code" |
| a whole method taken over with no base call | keep it as a repository override (`override MapToView(...)`), or `ConvertUsing` on the pair | |
| `[ShiftEntityMapper] partial class Foo : IShiftObjectMapper<Child, ChildDto>` | `CreateMap<Child, ChildDto>()` in any mapper class | |
| `queryable.SelectWithTags(e => new ListDTO { … })` in a hand-written `MapToList` | `queryable.Select(e => new ListDTO { …, Tags = e.Tags.Select(t => new TagDTO { … }).ToList() })` or map the list through `IMapper` | `SelectWithTags` is obsolete for one release |

**Why not in the repository?** What a member maps from is not the repository's responsibility, and a
customization that lives in the map works everywhere the pair is mapped with nothing constructed first. The
repository keeps two things: its Includes, and — optionally — how deep the automatic maps nest.

**Four things that are different, not merely renamed:**

- **One DTO type as both list and view is one map.** The old generator had four methods, so `ForList` on such
  a triple touched the list only; ShiftMapper maps pairs, and `CreateMap<E, TheOneDto>` is then the view AND
  the list. Use two DTO types when the list and the view must differ.
- **The write map is the reverse of the view map.** Declare it as `CreateMap<E, V>()….ReverseMap()`, not as a
  forward `CreateMap<V, E>()`: a DTO is a subset of its entity, and on a reverse map the entity-only members
  it leaves alone are the quiet SM0006 — on a forward map each is an SM0001 warning.
- **A `CreateMap` flattens by default; the automatic map did not.** If your map must behave exactly as the
  automatic one (no `OrderDto.CustomerName` from `Order.Customer.Name` by name alone), add
  `protected override void ConfigureDefaults(MapOptions o) => o.Flattening = false;` to the class.
- **A mapper class with a constructor dependency** makes the whole assembly's generated mapper need a
  container — every class it composes is constructed on first use. For a dependency only a map needs (a
  hash-id service, the mapping context), read it from `Services` inside the value instead of injecting it, and
  `Mapper.Create(assembly)` keeps working.

## A complete before/after

```csharp
// BEFORE
public class CampaignRepository : ShiftRepository<DB, Campaign, CampaignListDTO, CampaignDTO>
{
    public CampaignRepository(DB db) : base(db, o => o.UseGeneratedMapper(map => map
        .ForList(d => d.BrandName, e => e.Brand != null ? e.Brand.Name : null)
        .IgnoreEntity(e => e.ApprovedAt)
        .AfterEntity((dto, existing, ctx) =>
        {
            if (ctx.ActionType == ActionTypes.Insert) existing.Status = CampaignStatus.Draft;
        })))
    { }
}

// AFTER — the repository says nothing about mapping (only how deep, if it needs to)
public class CampaignRepository : ShiftRepository<DB, Campaign, CampaignListDTO, CampaignDTO>
{
    public CampaignRepository(DB db) : base(db) { }          // or base(db, o => o.Mapping(m => m.Nested(2)))
}

// … and a mapper class carries the customizations: it replaces the automatic map for each pair it declares
public class CampaignMapper : ShiftMapperBase
{
    public CampaignMapper()
    {
        // read at map time, not injected — see "different, not renamed"
        var ctx = new Lazy<IShiftEntityMappingContext>(() => Services.GetRequiredService<IShiftEntityMappingContext>());

        CreateMap<Campaign, CampaignListDTO>()
            .ForMember(d => d.BrandName, o => o.MapFrom(e => e.Brand != null ? e.Brand.Name : null));

        CreateMap<Campaign, CampaignDTO>()                    // the view map, unchanged from the automatic one …
            .ReverseMap()                                     // … and the write map as its reverse
            .ForMember(e => e.ApprovedAt, o => o.Ignore())
            .AfterMap((dto, entity) =>
            {
                if (ctx.Value.ActionType == ActionTypes.Insert) entity.Status = CampaignStatus.Draft;
            });
        // Campaign → Campaign stays automatic
    }

    protected override void ConfigureDefaults(MapOptions o) => o.Flattening = false;   // as the automatic maps
}
```

```csharp
// Program.cs
builder.Services.AddShiftMapper();
```

```csharp
// Anywhere else — the same maps, same customization:
public class CampaignReport(Mapper mapper, DB db)
{
    public Task<List<CampaignListDTO>> Active() =>
        db.Campaigns.Where(c => c.Status == CampaignStatus.Active).ProjectTo<CampaignListDTO>(mapper).ToListAsync();
}
```

## What the build tells you afterwards

- **SM0001** — a DTO member nothing fills. Map it or `Ignore` it (there is a code fix).
- **SM0047** (Info) — your mapper class replaced an automatic map. Expected; it is how customizing works.
- **SM0042** (Error) — two mapper classes declare the same pair. One class per pair; or use distinct DTOs.
- **SM0006** (Info) — an entity-only member the reverse (write) map leaves alone. Expected on every write map.
- **SM0030 / SM0036 / SM0037** — a list map lost its projection (a conversion with no query form, an
  `AfterMap` on a list map, …). Move the logic to a `ForMember` that projects, or accept an in-memory list.
- **SM0049** (Info) — an update map rebuilds a child collection. If the children are tracked rows, reconcile
  them in `AfterMap` or the repository, as before.
- **SM0035** (error) — a declaration inside an `if`/loop/lambda. Register unconditionally and put the
  condition inside the value, exactly as SHENGEN005 required.
- **SHENT001** (Error, the one ShiftEntity analyzer left) — the former SHENGEN006, unchanged: an entity's
  `IConfiguresShiftRepository<E, L, V>` while a repository for the same triple passes an options builder. Move
  the configuration into the builder, or drop the builder.
