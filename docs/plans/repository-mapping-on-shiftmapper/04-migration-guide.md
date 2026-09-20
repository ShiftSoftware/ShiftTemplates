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
member, either keep the configuration **in the repository** as `o.Mapping(m => …)` — same place, ShiftMapper's
vocabulary — or write an ordinary ShiftMapper mapper class declaring **only the pair you customize**, which
overrides the repository for that pair. Add `builder.Services.AddShiftMapper();` to the host. Everything
else — repository overrides, hand-written `IShiftEntityMapper`, `MappingHelpers` — is unchanged. The maps are
also available to any code that injects `Mapper`.

## Line by line

| You have | Write instead | Notes |
|---|---|---|
| `x => x.UseGeneratedMapper()` | *(nothing)* | the default is ShiftMapper |
| `UseGeneratedMapper = true` on an endpoint attribute | remove the property | always automatic |
| `UseGeneratedMapper(map => map …)` | `o.Mapping(m => { … })` in the same place; the lines below go inside | or a mapper class — see the end of the table |
| `map.ForView(d => d.X, e => …)` | `m.View.ForMember(d => d.X, o => o.MapFrom(e => …))` | |
| `map.ForView(d => d.X, (e, ctx) => ctx.Services.GetService<T>()…)` | inject `T` into the repository constructor and use it in `MapFrom` | captured variables are fine in `o.Mapping` |
| `map.ForEntity(e => e.X, dto => …)` | `m.Entity.ForMember(e => e.X, o => o.MapFrom(dto => …))` | |
| `map.ForEntity(e => e.X, (dto, existing, ctx) => …)` | `m.Entity.ForMember(e => e.X, o => o.Ignore()).AfterMap((dto, entity) => entity.X = …)` | `entity` IS the existing tracked row |
| `map.AfterEntity((dto, existing, ctx) => …)` | `m.Entity.AfterMap((dto, entity) => …)` | |
| `ctx.ActionType` | inject `IShiftEntityMappingContext` and read `.ActionType` | |
| `map.ForList(d => d.X, e => expr)` | `m.List.ForMember(d => d.X, o => o.MapFrom(e => expr))` | still an expression; still runs in SQL |
| `map.ForCopy(e => e.X, src => …)` | `m.Copy.ForMember(e => e.X, o => o.MapFrom(src => …))` | |
| `map.Ignore(d => d.X)` (all directions) | `m.View.ForMember(d => d.X, o => o.Ignore())` and the same on `m.Entity` / `m.List` / `m.Copy` where the member exists | usually one or two directions |
| `map.IgnoreView(d => d.X)` | `m.View.ForMember(d => d.X, o => o.Ignore())` | |
| `map.IgnoreEntity(e => e.X)` | `m.Entity.ForMember(e => e.X, o => o.Ignore())` | |
| `map.IgnoreList(d => d.X)` | `m.List.ForMember(d => d.X, o => o.Ignore())` | |
| `map.IgnoreCopy(e => e.X)` | `m.Copy.ForMember(e => e.X, o => o.Ignore())` | |
| `[ShiftEntityMapperIgnore]` on a property | the `Ignore` line above for each direction | no attribute replaces it |
| `map.ForViewChildren(d => d.Lines, e => e.Lines)` / `ForEntityChildren` / `ForListChildren` (no `configureChild`) | *(nothing)* — children nest automatically | |
| `… , child => child.For(c => c.X, …)` | customize the child pair once, in a mapper class: `CreateMap<Line, LineDTO>().ForMember(c => c.X, …)` | applies inside every parent |
| `map.MaxDepth(n)` / `[ShiftEntityMapperMaxDepth(n)]` | `m.Nested(n)` | no attribute replaces it; a constant, read at build time |
| `map.CaseSensitive()` | in a mapper class: `CreateMap<…>(o => o.Matching = PropertyMatching.CaseSensitive)` | |
| `[ShiftEntityMapper] partial class Foo : IShiftEntityMapper<E,L,V>` with `Configure` | `class FooMapper : ShiftMapperBase` declaring the customized pairs | not partial, no attribute, no interface; overrides the repository for those pairs |
| `public E MapToEntity(...) { existing = MapToEntityGenerated(...); …; return existing; }` | `CreateMap<V, E>().AfterMap((dto, entity) => …)` | "conventions first, then my code" |
| a whole method taken over with no base call | keep it as a repository override (`override MapToView(...)`), or `ConvertUsing` on the pair | |
| `[ShiftEntityMapper] partial class Foo : IShiftObjectMapper<Child, ChildDto>` | `CreateMap<Child, ChildDto>()` in any mapper class | |
| `queryable.SelectWithTags(e => new ListDTO { … })` in a hand-written `MapToList` | `queryable.Select(e => new ListDTO { …, Tags = e.Tags.Select(t => new TagDTO { … }).ToList() })` or map the list through `IMapper` | `SelectWithTags` is obsolete for one release |

**Repository or mapper class?** Both take the same lines (`ForMember`, `Ignore`, `AfterMap`). Keep it in the
repository when it is a few lines about this repository. Move it to a mapper class when it is long, when a
child pair is customized (it applies inside every parent), or when two repositories share a pair (only one
may configure it in the repository — the build tells you). A mapper class always wins for the pair it declares.

**Two things that are different, not merely renamed:**

- **One DTO type as both list and view is one map.** The old generator had four methods, so `ForList` on such
  a triple touched the list only; ShiftMapper maps pairs, and `m.List` and `m.View` are then the same map. Use
  two DTO types when the list and the view must differ.
- **A mapper class with a constructor dependency** makes the whole assembly's generated mapper need a
  container — every class it composes is constructed on first use. For a dependency only a map needs (a
  hash-id service), read it from `Services` inside the value instead of injecting it, and `Mapper.Create(assembly)`
  keeps working.

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

// AFTER — option 1: stay in the repository
public class CampaignRepository : ShiftRepository<DB, Campaign, CampaignListDTO, CampaignDTO>
{
    public CampaignRepository(DB db, IShiftEntityMappingContext ctx) : base(db, o => o.Mapping(m =>
    {
        m.List.ForMember(d => d.BrandName, o => o.MapFrom(e => e.Brand != null ? e.Brand.Name : null));
        m.Entity.ForMember(e => e.ApprovedAt, o => o.Ignore())
                .AfterMap((dto, entity) =>
                {
                    if (ctx.ActionType == ActionTypes.Insert) entity.Status = CampaignStatus.Draft;
                });
    }))
    { }
}

// AFTER — option 2: a mapper class (overrides the repository for these two pairs)
public class CampaignRepository : ShiftRepository<DB, Campaign, CampaignListDTO, CampaignDTO>
{
    public CampaignRepository(DB db) : base(db) { }
}

public class CampaignMapper : ShiftMapperBase
{
    public CampaignMapper(IShiftEntityMappingContext ctx)
    {
        CreateMap<Campaign, CampaignListDTO>()
            .ForMember(d => d.BrandName, o => o.MapFrom(e => e.Brand != null ? e.Brand.Name : null));

        CreateMap<CampaignDTO, Campaign>()
            .ForMember(e => e.ApprovedAt, o => o.Ignore())
            .AfterMap((dto, entity) =>
            {
                if (ctx.ActionType == ActionTypes.Insert) entity.Status = CampaignStatus.Draft;
            });
        // Campaign → CampaignDTO and Campaign → Campaign stay automatic
    }
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
- **SM0047** (Info) — your mapper class replaced an automatic map. Expected; it is how overriding works.
- **SM0051** (Warning) — your mapper class replaced a pair the repository *also* configures; the repository's
  lines for that pair are ignored. Delete them.
- **SM0050** (Error) — two repositories configure the same pair. Move it to a mapper class, or use distinct DTOs.
- **SM0030 / SM0036 / SM0037** — a list map lost its projection (a conversion with no query form, an
  `AfterMap` on a list map, …). Move the logic to a `ForMember` that projects, or accept an in-memory list.
- **SM0049** (Info) — an update map rebuilds a child collection. If the children are tracked rows, reconcile
  them in `AfterMap` or the repository, as before.
- **SM0035** (error) — a declaration inside an `if`/loop/lambda. Register unconditionally and put the
  condition inside the value, exactly as SHENGEN005 required.
- **SHENT001** (Error, the one ShiftEntity analyzer left) — the former SHENGEN006, unchanged: an entity's
  `IConfiguresShiftRepository<E, L, V>` while a repository for the same triple passes an options builder. Move
  the configuration into the builder, or drop the builder.
