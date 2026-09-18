# Steps

Small steps in dependency order. Each says **what** it does, **why**, and shows **what it produces**.
Stages run in order; steps inside a stage are mostly independent and say so when they are not.

Repositories touched: `ShiftMapper` (Stage 1), `ShiftEntity` (Stages 2–4), `ShiftTemplates` (Stages 0, 3, 5),
`ShiftIdentity` (Stage 3). Nothing in `ADP.*` or `Menu` is edited by this plan; they get
[`04-migration-guide.md`](04-migration-guide.md).

Names of new types and methods below are **proposals**. Keep the shape; rename freely while building.
**The programmer never writes an attribute for mapping** — every attribute mentioned here lives inside
ShiftEntity or is generated.

---

## Stage 0 — Freeze the oracle *(ShiftTemplates, ShiftIdentity)*

The old generator is the only thing that knows exactly what it produces. Once it is deleted there is no way
to check that ShiftMapper produces the same thing. So the first step is to write that knowledge down as tests.

### 0.1 Inventory every triple and every nested pair

One markdown table per repository — sample, `ShiftIdentity.Data` — listing every `(entity, list, view)`
triple, how it maps today (generated / partial class / override / hand-written), every nested pair the
generator discovered, every fluent customization, and every use of the three attributes. This is the
checklist Stage 3 migrates against. **Done 2026-09-18 — [`05-inventory.md`](05-inventory.md) §1, 23 triples.** Known at planning time:

```
| Triple                                   | Door             | Nested pairs                          | Customizations / attributes |
|------------------------------------------|------------------|---------------------------------------|-----------------------------|
| Invoice / InvoiceListDTO / InvoiceDTO    | generated + repo | InvoiceLine↔InvoiceLineDTO, …         | ForListChildren(InvoiceLines → ForChild(Product)) |
| ProductBrand / …ListDTO / …DTO           | partial class    | —                                     | [ShiftEntityMapper]; ForList(Code); MapToEntity takeover |
| 10 ShiftIdentity entities                | endpoint attr    | …                                     | UseGeneratedMapper = true |
| ADP: 2 files with [ShiftEntityMapper], 20 repositories with UseGeneratedMapper(map => …) — evidence only |
```

### 0.2 Parity goldens

`StockPlusPlus.Test/Tests/RepositoryMappingParityTests.cs`, over BOTH assemblies (the sample host registers
ShiftIdentity's repositories and endpoints, so their real, configured mappers resolve from the same scope — a
second suite in `ShiftIdentity.Tests` would need a second host and pin the same objects): for every triple, a
fixed entity graph goes through all four directions and the result is compared to a frozen file:

- `MapToView` → JSON of the DTO;
- `MapToEntity` over a fixed `existing` entity → JSON of the entity afterwards;
- `MapToList` → the projection over an in-memory list (LINQ-to-Objects) → JSON; **and** the expression tree's
  `ToString()` so the *shape* is pinned too (`SelectWithTags`, correlated child projections);
- `CopyEntity` → JSON of the target.

Same recipe as `ReplicationMappingParityTests.cs`. The goldens are written by the OLD generator, committed, and
never regenerated; Stage 2.8 diffs ShiftMapper against them. **Done 2026-09-18 — see [`05-inventory.md`](05-inventory.md) §2**
for the fixture rules, the capture switch and what the files visibly pin.

### 0.3 Baseline the diagnostics

Build the sample and ShiftIdentity once and save the `SHENGEN` warnings they print today. Each of those must
later be either a ShiftMapper `SM` warning on the same member or a recorded decision that it is no longer
needed. **Done 2026-09-18 — 11 distinct warnings, each with its target `SM` rule, in [`05-inventory.md`](05-inventory.md) §3.**

---

## Stage 1 — ShiftMapper features *(ShiftMapper repo, released as 0.3.0)*

None of these mentions ShiftEntity. Each is a general feature with its own generator tests in
`ShiftMapper.Generator.Tests` and a section in `docs/extension-points.md`. **M1 blocks M2 and M3; M4, M5,
M6 are independent.**

### M1 Implicit maps from a marked generic type

**What.** A framework marks an open generic class or attribute class — in its own package, once:

```csharp
// in ShiftEntity.EFCore — the programmer never sees or writes this
[ShiftMapperDeclaresMap("TEntity", "TView", Reverse = true, Nested = 10, Flattening = false,
                        Rules = typeof(ShiftEntityConversions), ConfiguredBy = typeof(ShiftEntityMapping<,,>))]
[ShiftMapperDeclaresMap("TEntity", "TList", Nested = 10, Flattening = false, Rules = typeof(ShiftEntityConversions), ConfiguredBy = …)]
[ShiftMapperDeclaresMap("TEntity", "TEntity", Rules = typeof(ShiftEntityConversions), ConfiguredBy = …)]
public abstract class ShiftRepository<DB, TEntity, TList, TView> ...

// on an attribute class: "this" means the type the attribute is applied to
[ShiftMapperDeclaresMap("this", "TView", Reverse = true, Nested = 10, Flattening = false, Rules = …, ConfiguredBy = …)]
[ShiftMapperDeclaresMap("this", "TList", Nested = 10, Flattening = false, Rules = …, ConfiguredBy = …)]
[ShiftMapperDeclaresMap("this", "this", Rules = …, ConfiguredBy = …)]
public class ShiftEntityEndpointAttribute<TList, TView> : Attribute ...
```

When the generator compiles a project containing

```csharp
public class InvoiceRepository : ShiftRepository<DB, Invoice, InvoiceListDTO, InvoiceDTO> { … }

[ShiftEntitySecureEndpoint<CountryDTO, CountryDTO, StockPlusPlusActionTree>("api/country", …)]
public class Country : ShiftEntity<Country> { … }
```

it declares, exactly as if a mapper class in the project had written them:

```csharp
CreateMap<Invoice, InvoiceDTO>().ReverseMap();   CreateMap<Invoice, InvoiceListDTO>();   CreateMap<Invoice, Invoice>();
CreateMap<Country, CountryDTO>().ReverseMap();   CreateMap<Country, CountryDTO>();       CreateMap<Country, Country>();
```

**How.** The generator scans the project's classes for base types / attributes whose *definition* carries the
marker (read from metadata, like every other declaration attribute), substitutes the type arguments, and folds
the result into a **generated mapper class** — `ShiftMapper.Implicit.<AssemblyName>ImplicitMaps :
ShiftMapperBase`, internal, parameterless — which is written to the assembly's declaration metadata like any
mapper class. So a referencing project's mapper carries the implicit maps too, re-baked with its own rules,
through the machinery that already exists.

**Rules.**
- `Rules = typeof(Pack)` gives the implicit maps that pack, as `AddConversions<Pack>()` in a constructor would.
- `Flattening = false` is what ShiftEntity sets (Q4): a repository never silently reaches two levels into an
  entity. A mapper class can turn it on per pair.
- A pair declared by **two** sources (two repositories over one triple, a repository and an endpoint) is
  declared once.
- A pair the project declares in its own mapper class **replaces** the implicit one, for that pair only.
  Reported as `SM0047` Info ("the implicit map from X to Y is replaced by the one in AppMapper"), so the
  override is visible in the build without being a warning — it is the intended customization path.
- Diagnostics about an implicit map (unmapped members, lost projection) land on the **closing declaration**:
  the repository class or the attribute application — the same location the old generator used.
- Duplicate pair from two of the project's *own* mapper classes stays SM0042. Implicit maps never take part
  in SM0042 or SM0027.
- Contract version bumps (the metadata gains a mapper kind the old reader would misread) — SM0033 refuses an
  old package whole, as designed.

**Test.** A `Contoso.Platform`-style base class in the test scaffold; a closing class; assert the four maps
exist, that an application `CreateMap` replaces one, that a second closing class over the same pair adds
nothing, and that the maps travel to a referencing compilation.

### M2 Nested declaration

**What.** `Nested = n` on the marker. For every implicit map, every member pair `(source member type,
destination member type)` where both are classes — or collections of classes — and no map is declared for the
pair, gets an implicit map of its own, in the same direction, recursively, up to depth *n*. Reverse maps nest
in reverse.

```
Invoice → InvoiceDTO         nests   InvoiceLine → InvoiceLineDTO
InvoiceDTO → Invoice         nests   InvoiceLineDTO → InvoiceLine
Invoice → InvoiceListDTO     nests   InvoiceLine → InvoiceLineListDTO   nests   Product → InvoiceLineProductListDTO
```

**Rules** (all of them are today's behaviour, restated):
- A member with an explicit `ForMember` is not nested. A member a **member convention claims** is not nested
  (so `ProductBrand → ShiftEntitySelectDTO` is never declared — the select convention answers instead).
- A pair that already has a declared map — in this project or a referenced package — is used as is; its
  customizations apply inside the parent, in memory and in the projection. **That is how "configure a child"
  works now: customize the child's pair once.**
- A cycle (`A → B → A` through DTO members) stops at the member that would close it, reported as `SM0048` Info
  naming the member — not the SM0012 error, because the framework asked for nesting and a cycle in a DTO
  graph is an ordinary thing to have.
- `m.Nested(n)` in the configuration surface (M3) overrides the marker's depth for that closing type;
  `m.Nested(0)` turns nesting off. No attribute.
- Depth counts levels below the root, root = 0; default in the marker is the framework's choice (ShiftEntity
  keeps 10).

**Test.** Three-level scaffold; assert maps at each level, the depth cap, the cycle stop, "explicit child map
wins", and "convention-claimed member is not nested".

### M3 A configuration surface on the closing type

**What.** The marker's `ConfiguredBy` names a generic *surface* type the framework ships, whose properties
are the implicit maps' `MapExpression<,>`:

```csharp
// in ShiftEntity.EFCore — the surface the programmer types against
public sealed class ShiftEntityMapping<TEntity, TList, TView> : ShiftMapperConfigurationSurface
{
    public MapExpression<TEntity, TView>   View   { get; }
    public MapExpression<TView, TEntity>   Entity { get; }
    public MapExpression<TEntity, TList>   List   { get; }
    public MapExpression<TEntity, TEntity> Copy   { get; }
    public ShiftEntityMapping<TEntity, TList, TView> Nested(int depth) => this;   // build-time marker, like MaxDepth today
}
```

and a method on the closing type's options that takes a lambda over it — `ShiftRepositoryOptions.Mapping(Action<ShiftEntityMapping<E,L,V>>)`.
The programmer writes, in the repository:

```csharp
public InvoiceRepository(DB db, IHashIdService hashIds) : base(db, o => o.Mapping(m =>
{
    m.List.ForMember(d => d.Total, opt => opt.MapFrom(e => e.InvoiceLines.Sum(l => l.Price)));
    m.Entity.ForMember(e => e.InvoiceNo, opt => opt.Ignore())
            .AfterMap((dto, entity) => entity.ManualReference = entity.ManualReference?.Trim());
    m.View.ForMember(d => d.PublicKey, opt => opt.MapFrom(e => hashIds.Encode(e.ID)));
}))
{ }
```

**How** — the same split ShiftMapper uses for a package mapper:
- **Build time.** The generator finds every lambda handed to a `Mapping(...)` whose parameter type is a
  surface, walks the property chains (`m.List.ForMember(...)`) with the code that already walks a
  `CreateMap` chain, and records the *shape* — customized members, ignores, hooks, `Nested(n)` — against
  the implicit maps of that closing type. All existing diagnostics apply.
- **Run time.** The closing type (the repository) implements a small interface,
  `IShiftMapperCustomizationSource { MapCustomizations Customizations { get; } }`; running the lambda
  (which the repository already does in `InitCommon`) fills that store with the `MapFrom` trees and hooks.
  The generated map reads them as `Customizations.Value<Invoice, InvoiceListDTO, decimal>("Total")`, the same
  line a package's `MapFrom` produces. When a map with such customizations is used before its closing type
  has been constructed in the scope, the generated mapper resolves the closing type from DI — exactly as it
  constructs a mapper class from DI on first use.

**Rules.**
- The lambda must be inline and unconditional — SM0035, the rule SHENGEN005 enforces today.
- **One closing type per pair** may configure it. Two repositories configuring `Invoice → InvoiceListDTO`
  is `SM0050`, an error: the map is shared, so which configuration applied would depend on construction
  order. Fix: a mapper class, or distinct DTOs.
- A mapper class declaring the pair **replaces** the implicit map, and the surface's configuration for
  that pair is ignored — `SM0051` Warning naming both, so nothing is lost in silence.
- Captured variables are fine (the lambda runs inside the repository, as today). A `MapFrom` on the `List`
  map must still be a translatable expression — SM0030/SM0037 say so, as they do for any map.

**Test.** Surface scaffold; a closing class with a lambda; assert the shape is baked, the expression is read
from the closing type's store, DI construction on first use, SM0050 and SM0051.

### M4 Pack-level member ignore rules

**What.** A `ShiftMapperConversions` pack can say "this member is never read / never written", for every
map whose source or destination is assignable to the declaring type — interfaces included:

```csharp
public class ShiftEntityConversions : ShiftMapperConversions
{
    public ShiftEntityConversions()
    {
        IgnoreMember<ShiftEntityBase>(e => e.ID, MemberRole.Destination);          // never written from a request, never copied
        IgnoreMember<IShiftEntityTaggable>(e => e.Tags, MemberRole.Destination);   // owned by the tagging pipeline
        …
    }
}
```

An ignored destination member is treated exactly like `opt.Ignore()` — omitted, and not reported as SM0001.
An ignored source member is simply not a candidate. Travels in the pack's metadata like a conversion.
This is what lets the framework state its rules **as code**, once, with no attribute on any type.

**Test.** Direct, inherited, interface-implemented; each role; the SM0001 suppression; cross-assembly.

### M5 Collection member conventions

**What.** `CreateMemberConvention<T>()` also claims destination members typed `IEnumerable<T>` (list, array,
set). The source is the collection member the map would have matched by name; each element is filled by the
convention's entries, with paths relative to the **element**:

```csharp
CreateMemberConvention<ShiftEntitySelectDTO>()
    .NameFrom<ShiftEntityKeyAndNameAttribute>("Text")
    .Fill(d => d.Value, "{Member}ID")                 // single member:   Brand  = { Value = BrandID, Text = Brand.Name }
    .FillIfPossible(d => d.Text, "{Member}.{NameOf}")
    .ForEachElement()                                 // collection member: Departments = Departments.Select(x => { Value = x.ID, Text = x.Name })
        .Fill(d => d.Value, "ID")
        .FillIfPossible(d => d.Text, "{NameOf}");
```

```csharp
// what the generator writes, in memory and in the projection
Departments = ValueConverter.ToListOrEmpty(source.Departments, x => new ShiftEntitySelectDTO { Value = ToInvariantString(x.ID), Text = x.Name })
```

**Write direction:** not derived for collections — a many-to-many is a reconciliation (add / remove rows), not
an assignment, and that belongs in an `AfterMap` or the repository, exactly as today's SHENGEN010 says. The
collection member on the write map is left alone and reported as SM0001 unless ignored.

**Test.** List / array / set destination; nominated name present and absent; projection shape.

### M6 Update map rebuilds a nested collection — Info

**What.** On the **update** overload (`Map(source, destination)`), a nested collection member is rebuilt from
scratch (`destination.Lines = ToListOrEmpty(source.Lines, MapToLine)`). Report `SM0049` Info on the map naming
the member: "the update overload replaces `Lines` with new objects; if they are tracked rows, reconcile them
in `AfterMap`". This is the successor of SHENGEN010, phrased without knowing what a tracked row is. One
Info per member, once.

### 1.7 Release

`ShiftMapper 0.3.0` — bump `ShiftMapperVersion` in `ShiftFrameworkGlobalSettings.props`, tag
`release-shiftmapper`. The framework does not reference the new features until Stage 2, so this release is
inert for consumers.

---

## Stage 2 — The ShiftEntity side, additive *(ShiftEntity; nothing removed yet)*

At the end of this stage both mappers exist on the same host: the old generated mapper still wins, ShiftMapper
sits behind it in the resolution order, and the goldens can be diffed against ShiftMapper's output.

### 2.1 Reference ShiftMapper from `ShiftEntity.Model` and `ShiftEntity.Core`

Same TypeAuth pattern as `ShiftEntity.CosmosDbReplication`: `ProjectReference` when the sibling checkout
exists, `PackageReference` at `$(ShiftMapperVersion)` otherwise; `Microsoft.Extensions.DependencyInjection.Abstractions`
at 10.0.11 wherever it is referenced directly (NU1605). The generator must run over `ShiftEntity.Model`,
`ShiftEntity.Core` and `ShiftEntity.EFCore`, or their declarations are invisible to consumers (SM0028).

### 2.2 The rules pack — `ShiftEntityConversions`

`ShiftEntity.Model/Mapping/ShiftEntityConversions.cs` (Model, because it names only Model types; the
`IShiftEntityTaggable` and `IEntityHasIdempotencyKey` rules go in a second pack in Core if those interfaces
live there):

```csharp
public class ShiftEntityConversions : ShiftMapperConversions
{
    public ShiftEntityConversions()
    {
        // ── members the framework owns (M4). No attribute on any type. ──
        IgnoreMember<ShiftEntityBase>(e => e.ID, MemberRole.Destination);                    // never written from a request; never copied
        IgnoreMember<ShiftEntity<object>>(e => e.ReloadAfterSave, MemberRole.Both);           // request-scoped flags
        IgnoreMember<ShiftEntity<object>>(e => e.AuditFieldsAreSet, MemberRole.Both);
        IgnoreMember<IEntityHasIdempotencyKey<object>>(e => e.IdempotencyKey, MemberRole.Destination);
        IgnoreMember<IShiftEntityTaggable>(e => e.Tags, MemberRole.Destination);             // owned by TaggingPipeline on both legs
        IgnoreMember<ShiftEntityListDTO>(d => d.Revisions, MemberRole.Destination);          // loaded on demand
        IgnoreMember<ShiftEntityViewAndUpsertDTO>(d => d.Revisions, MemberRole.Destination);

        // ── FK ↔ ShiftEntitySelectDTO. Read: Value = BrandID, Text = Brand.Name (from [ShiftEntityKeyAndName]).
        //    Write is DERIVED: BrandID = Parse(dto.Brand.Value); the navigation beside it is left alone.
        //    M5: a List<ShiftEntitySelectDTO> from a navigation collection, read side. ──
        CreateMemberConvention<ShiftEntitySelectDTO>()
            .NameFrom<ShiftEntityKeyAndNameAttribute>(nameof(ShiftEntityKeyAndNameAttribute.Text))
            .Fill(d => d.Value, "{Member}ID")
            .FillIfPossible(d => d.Text, "{Member}.{NameOf}")
            .ForEachElement()
                .Fill(d => d.Value, "ID")
                .FillIfPossible(d => d.Text, "{NameOf}");

        // ── Files stored as JSON. No query form on purpose: a database cannot parse JSON into objects, so a
        //    LIST DTO carrying files loses its projection and the build says so (SM0030) — today the same
        //    member is "unmapped in list" (SHENGEN007). ──
        CreateConversion<string?, List<ShiftFileDTO>>(memory: MappingHelpers.ToShiftFiles);
        CreateConversion<List<ShiftFileDTO>?, string?>(memory: MappingHelpers.ToJsonString);
    }
}
```

`IsDeleted` and the audit columns are deliberately **not** ignored, per the Q7 decision of the AutoMapper
removal: the mapper maps them and the repository restores `IsDeleted` on update. Everything else the old
generator converted (`long`/`long?`/`Guid`/enum ↔ `string`, `int?` → `int`, collections) is ShiftMapper's
built-in table — see [`03-coverage.md`](03-coverage.md).

### 2.3 The framework's own pairs — `ShiftEntityFrameworkMaps`

`ShiftEntity.EFCore/Tagging/ShiftEntityFrameworkMaps.cs`:

```csharp
public class ShiftEntityFrameworkMaps : ShiftMapperBase
{
    public ShiftEntityFrameworkMaps()
    {
        CreateMap<Tag, TagDTO>();
        CreateMap<Tag, TagListDTO>();
    }
}
```

With these declared, `ProductListDTO.Tags` (`List<TagDTO>`) maps from `Product.Tags` (`ICollection<Tag>`) as an
ordinary nested collection — in memory and inside the projection — so `SelectWithTags` is no longer needed.

### 2.4 The marker and the configuration surface

- The marker (M1) on `ShiftRepository<DB, TEntity, TList, TView>` and on
  `ShiftEntityEndpointAttribute<TList, TView>` / `ShiftEntitySecureEndpointAttribute<TList, TView, TActionTree>`
  — **not** on the `WithMapper` variants, whose mapping is the hand-written `TMapper`. `Nested = 10`,
  `Flattening = false`, `Rules = typeof(ShiftEntityConversions)`, `ConfiguredBy = typeof(ShiftEntityMapping<,,>)`.
- `ShiftEntityMapping<E, L, V>` (M3 surface) in `ShiftEntity.EFCore`, and
  `ShiftRepositoryOptions.Mapping(Action<ShiftEntityMapping<E, L, V>>)`. Available in the base-constructor
  builder and in `IConfiguresShiftRepository.ConfigureRepository` (`context.Options.Mapping(...)`).
- `ShiftRepository` implements `IShiftMapperCustomizationSource`; `InitCommon` runs the lambda into the store.

### 2.5 `IShiftEntityMappingContext`

A scoped service in `ShiftEntity.Core`:

```csharp
public interface IShiftEntityMappingContext
{
    ActionTypes? ActionType { get; }     // Insert / Update during MapToEntity; null otherwise
}
```

The repository sets it around every `Map` call (an internal setter on the implementation). Repositories and
mapper classes inject it. It replaces `MappingContext.ActionType` for ShiftMapper-based mapping;
`MappingContext` itself stays for `IShiftEntityMapper`.

### 2.6 The repository maps through `IMapper`

In `ShiftRepository.InitCommon`, after the DI `IShiftEntityMapper` lookup and — **for this stage only** —
after the registry lookup:

```csharp
else if (MapperServiceProvider.GetService<IMapper>() is { } shiftMapper && ShiftMapperCovers(shiftMapper))
    this.ShiftRepositoryOptions.Mapper = new ShiftMapperEntityMapper<EntityType, ListDTO, ViewAndUpsertDTO>(shiftMapper, mappingContext);
```

`ShiftMapperEntityMapper` is a small internal `IShiftEntityMapper<E,L,V>`:

```csharp
MapToView(entity)               => mapper.Map<E, V>(entity)
MapToEntity(dto, existing, ctx) => { context.ActionType = ctx.ActionType; return mapper.Map<V, E>(dto, existing); }
MapToList(query)                => mapper.ProjectTo<E, L>(query)
CopyEntity(source, target)      => mapper.Map<E, E>(source, target)
```

`ShiftMapperCovers` = `CanMap(E,V) && CanMap(V,E) && CanMap(E,L) && CanMap(E,E)`.

**Exception translation.** `MappingHelpers.ToForeignKey` turns a blank or non-numeric select into a **400**
naming the field. ShiftMapper's derived write throws its own parse exception. The upsert path catches
ShiftMapper's conversion exception around `MapToEntity` and rethrows the same `ShiftEntityException` shape
(`Model Validation Error`, `For = member`) — pinned by a test, because a 500 here is the regression a
consumer would notice first.

### 2.7 Registration

`RegisterShiftRepositories(assemblies)` (ShiftEntity.EFCore):

```csharp
services.AddShiftMapper(o => o.ShareConversions<ShiftEntityConversions>());   // inline lambda, from THIS assembly:
                                                                               // registers EFCore's generated mapper (tag pairs)
                                                                               // and shares the pack with every project that registers
foreach (var assembly in assemblies)
    services.AddShiftMapper(assembly);                                         // Q8 — the scanned data assemblies' generated mappers
```

The template's hosts add `builder.Services.AddShiftMapper();` (API, Functions, test hosts) — the place a
host's own rules and mapper classes go.

### 2.8 Diff against the goldens

A test host with both mappers resolvable; a switch in the test fixture picks ShiftMapper; every 0.2 golden
runs against it. Every difference is either fixed here or recorded in [`02-open-decisions.md`](02-open-decisions.md)
as an accepted change (null collections, …) with the golden updated **and the decision written down beside it**.

### 2.9 Startup validation

`ShiftEntityMapperValidation.Validate` gains the `IMapper.CanMap` path beside the registry path: a triple
is covered if `UseMapper`/DI covers it, or the registry has it, or `IMapper` can map all four pairs. The
message for an uncovered triple names `AddShiftMapper()` as the first thing to check.

---

## Stage 3 — Flip and migrate

### 3.1 Flip the order

`IMapper` moves **ahead** of the registry in `InitCommon`. The registry link stays for this stage so a project
still carrying a `[ShiftEntityMapper]` partial keeps working while it migrates (Q7).

### 3.2 Migrate the sample (StockPlusPlus)

The sample should demonstrate all three doors afterwards: automatic (Country, ProductCategory), configured
in the repository (Invoice), overridden in a mapper class (ProductBrand) — plus the two unchanged ones
(Product override, CountryMapper).

| Today | After |
|---|---|
| `CountryRepository : base(db, x => x.UseGeneratedMapper())` | `base(db)` |
| `ProductCategoryRepository` — `o.UseGeneratedMapper()` | line removed |
| `InvoiceRepository` — `UseGeneratedMapper(map => map.ForListChildren(…ForChild(…)))` | `o.Mapping(m => m.List.ForMember(d => d.Total, …))` — a real customization worth demonstrating, since nesting needs nothing |
| `Country.ConfigureRepository` — `UseGeneratedMapper(map => map.ForList(d => d.Name, …))` | `context.Options.Mapping(m => m.List.ForMember(d => d.Name, o => o.MapFrom(e => e.Name + " (via IConfiguresShiftRepository)")))` |
| `Invoice.ConfigureRepository` — `UseGeneratedMapper()` | line removed |
| `[…("api/country-generated", …, UseGeneratedMapper = true)]` (Country, Invoice) | property removed from the attribute |
| `ProductBrandMapper` (`[ShiftEntityMapper]` partial) | rewritten as an ordinary `ShiftMapperBase` class (README §5) |
| `// [ShiftEntityMapperMaxDepth(2)]` comment on `Invoice` | `m.Nested(2)` inside `Invoice.ConfigureRepository`'s `Mapping(...)`, as a comment |
| `ProductRepository` overrides, `CountryMapper` | untouched |
| Tests `SourceGeneratedMappingTests`, `DeepMappingTests`, `DeepListMappingTests`, `AttributeEndpointMapperDiscoveryTests`, `IdentityAttributeEndpointDiscoveryTests` | rewritten against `IMapper` and the goldens; `Program.cs` comments mentioning the attributes updated; the `MappingPOC` folder is unaffected |

### 3.3 Migrate the item template (`content/ShiftEntity`)

`ProductBrandMapper.cs` rewritten as above, both `#if (includeItemTemplateContent)` halves; the
`--taggable` variant unchanged (Tags map by the framework pair). Verify with a Builder run — the same
end-to-end check B2 of the AutoMapper removal still owes.

### 3.4 Migrate ShiftIdentity.Data

- The 10 entities carrying `UseGeneratedMapper = true` — property removed.
- `CompanyBranchRepository`, `CompanyRepository`, `UserRepository`, … : each `UseGeneratedMapper(map => …)`
  becomes `o.Mapping(m => …)` in the same place, line for line
  ([`04-migration-guide.md`](04-migration-guide.md)). The `List<ShiftEntitySelectDTO>` members
  (`Departments`, `Services`, `Brands`) stay one `ForMember` each while they read through an explicit
  junction entity (`CompanyBranchDepartments.Select(x => …)`) — M5 covers a direct navigation collection,
  and the junction shape is a `MapFrom` that projects.
- Replication maps (`ShiftIdentityReplicationMapper`) are unaffected: different pairs. Package hosts already
  register through `AddShiftIdentityReplicationMapper()`.

### 3.5 Tagging

`SelectWithTags` becomes an `[Obsolete]` forwarder to `Select` for one release; `ShiftTagMapper`
(`IShiftEntityMapper<Tag, TagListDTO, TagDTO>`, registered `TryAddScoped`) keeps working through the DI door
and is later simplified to the framework pairs.

### 3.6 Goldens green

Every 0.2 golden passes through ShiftMapper on the sample and ShiftIdentity, with the accepted changes
recorded. **This is the gate for Stage 4.**

---

## Stage 4 — Delete

### 4.1 Remove the old generator, the attributes and everything attached

| Delete | Because | Replaced by |
|---|---|---|
| `ShiftEntity.SourceGenerator/` (project, analyzer packaging lines in `ShiftEntity.Core.csproj`) | ShiftMapper's generator does the work | — |
| `ShiftEntityMapperAttribute` (`[ShiftEntityMapper]`) | no partial mapper classes | an ordinary `ShiftMapperBase` class |
| `ShiftEntityMapperIgnoreAttribute` | no mapping attributes | `ForMember(..., opt => opt.Ignore())` in `o.Mapping(...)` or a mapper class |
| `ShiftEntityMapperMaxDepthAttribute` | no mapping attributes | `m.Nested(n)` |
| `ShiftEntityMapperDefaults` | constants of the old generator | the marker's defaults |
| `UseGeneratedMapper` property on the endpoint attributes | always automatic | — |
| `ShiftRepositoryOptions.UseGeneratedMapper(...)` | the default IS ShiftMapper | `o.Mapping(m => …)` |
| `ShiftEntityMapperRegistry`, `GeneratedMapperFactory`, the module initializers | nothing registers by type any more | `AddShiftMapper()` |
| `ShiftMapperBuilder`, `ShiftChildMapperBuilder`, `IShiftMapperConfigurable`, `IShiftObjectMapper` | the fluent vocabulary is ShiftMapper's | `MapExpression<,>`, nested maps |
| `ShiftEntity.Tests/Mapping/*` generator tests and `MapperGeneratorHarness` | | ShiftMapper generator tests + goldens |
| `SelectWithTags` (after its obsolete release) | | plain `Select` |
| the registry link in `InitCommon` and in `ShiftEntityMapperValidation` | resolution is options → DI → `IMapper` → throw | |

**Keep**: `IShiftEntityMapper<E,L,V>`, `MappingContext`, `MappingHelpers`, the `WithMapper` attributes, the
four virtual methods, `ShiftEntityMapperValidation`, `[ShiftEntityKeyAndName]` (a model attribute, not a
mapping one — the select convention reads it and the UI uses it).

### 4.2 SHENGEN006 finds a home or is dropped

The one old diagnostic that is not about mapping — "the entity's `IConfiguresShiftRepository` will not run
because the repository passes a builder" — can move to a tiny `DiagnosticAnalyzer` in the empty
`ShiftEntity.Analyzers` project, or be dropped. Decision Q6.

---

## Stage 5 — Docs, CLAUDE.md, pipeline

- **5.1** `ShiftTemplates/CLAUDE.md`: rewrite "Active Work: Mapping Abstraction", the SHENGEN section, the
  build-time-baked section and the tagging notes that mention `SelectWithTags` / mapper-does-not-handle-Tags.
- **5.2** `.shift/repos/shift-entity/mapping-abstraction-plan.md`: refresh "What's Done"; `automapper-removal/STATUS.md`
  gets a log line (the removal plan's Stage F assumed the ShiftEntity generator; say what replaced it).
- **5.3** `ShiftFrameworkDocs`: one page "Mapping in a repository" written from the README's before/after samples,
  including §6 "the maps work anywhere".
- **5.4** Pipeline: nothing structural. `ShiftMapper.Generator.Tests` grows (still long-running only);
  the first framework release after Stage 2 must be `release-all` or `release-shiftmapper` first.
- **5.5** Publish [`04-migration-guide.md`](04-migration-guide.md) for `ADP.*` / `Menu`, whose 20
  repositories use `UseGeneratedMapper(map => …)` today (and two files use `[ShiftEntityMapper]`). Landing
  pad: the registry link is deleted only in Stage 4, one framework release after Stage 3 ships, so consumers
  get one release where both spellings work.
