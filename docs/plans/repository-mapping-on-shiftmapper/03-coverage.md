# Coverage — what the old generator does, and how ShiftMapper covers it

One row per behaviour of `ShiftEntity.SourceGenerator` (read from `ShiftEntityMapperGenerator.cs` on
2026-09-18), so that nothing is lost by accident when it is deleted. "Built-in" means ShiftMapper does it
today with nothing configured; "pack" means `ShiftEntityConversions` (Step 2.2); "M1–M6" are the ShiftMapper
features in [`01-steps.md`](01-steps.md); "`o.Mapping`" is the repository's configuration surface (M3).

**No row is covered by an attribute the programmer writes.** The framework's own rules are pack code.

The **Verify** column is what Stage 2.8's golden diff must confirm; ✅ there means the golden passed.

## A. Read side — `MapToView` (entity → view DTO, in memory)

| # | Old generator | ShiftMapper | Verify |
|---|---|---|---|
| A1 | `ShiftEntitySelectDTO` ← `{Member}ID` (`long`/`long?`) + `{Member}.<Text>` where `<Text>` comes from `[ShiftEntityKeyAndName]` on the navigation's type; `null` FK → `null` DTO | **pack**: member convention `Fill(Value, "{Member}ID")` + `FillIfPossible(Text, "{Member}.{NameOf}")`, `NameFrom<ShiftEntityKeyAndNameAttribute>("Text")` | null FK → null DTO (not an empty DTO) |
| A2 | `List<ShiftFileDTO>` ← JSON `string` (`ToShiftFiles`; null/blank → empty list) | **pack**: `CreateConversion<string?, List<ShiftFileDTO>>(memory: …)` | blank → empty list |
| A3 | name match, exact first then case-insensitive; ambiguity → member skipped + SHENGEN011 | built-in (`PropertyMatching.CaseInsensitive` default; SM0007) | — |
| A4 | `T?` → `T` as `?? default` | built-in `GetValueOrDefault()` (SM0008 Info) | — |
| A5 | `long`/`long?`/`Guid`/`Guid?` → `string`; enum → `int`/`int?` | built-in | `long?` null → null string |
| A6 | every numeric, `bool`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`, `Guid`, enums ↔ `string`, invariant culture; numeric ↔ numeric | built-in (`docs/conversions.md`); ShiftMapper additionally **refuses** `DateTime`↔`DateTimeOffset` and enum↔enum directly (SM0002) — the old generator never did those either | — |
| A7 | collections: element and container converted independently (`List`/`ICollection`/`IEnumerable`/`IReadOnly*`/`HashSet`/array) | built-in (`ToList`/`ToArray`/`ToHashSet`, `OrEmpty` family) | see Q5 (null → empty) |
| A8 | nested class member / collection of class → composed through the auto pair mapper, up to `MaxDepth` (10), cycle → member skipped + SHENGEN003 | **M2** nested declaration; cycle → SM0048 Info | three levels on `api/invoice-deep` |
| A9 | base fields via `MapBaseFields`: `ID`, `IsDeleted`, `CreateDate`, `LastSaveDate`, `CreatedByUserID`, `LastSavedByUserID` | plain name matches + built-in `long`→`string`, `long?`→`string` | `CreatedByUserID` null → null |
| A10 | `Tags` left to the repository (`ViewAsync` auto-maps them for taggables) | `Tag → TagDTO` framework pair (Step 2.3) makes it an ordinary nested collection; the repository's own auto-map becomes redundant — keep one, not both | no double mapping |
| A11 | `Revisions` left alone | **pack** rule: `IgnoreMember<…DTO>(d => d.Revisions, Destination)` (M4) | — |
| A12 | init-only DTO members reported as unmapped (cannot be assigned) | built-in: `init`, `required`, records, primary constructors are all mapped | an improvement, not a parity item |
| A13 | `ForView(member, (entity, ctx) => …)`; services via `ctx.Services` | `CreateMap<E, V>().ForMember(d => d.X, o => o.MapFrom(e => …))` in a mapper class (a service read from `Services` inside the value, or constructor injection). *Since 2026-09-20 there is no repository spelling (Q16).* | — |
| A14 | `IgnoreView` / `Ignore` / `[ShiftEntityMapperIgnore]` | `CreateMap<E, V>().ForMember(d => d.X, o => o.Ignore())` in a mapper class. **No attribute.** | — |
| A15 | unmapped view members → SHENGEN004 | SM0001 (warning), with the "Ignore" code fix | same members warned |

## B. Write side — `MapToEntity` (view DTO → tracked entity, in memory)

| # | Old generator | ShiftMapper | Verify |
|---|---|---|---|
| B1 | FK `long` ← `SelectDTO.Value` (`ToForeignKey`: blank/non-numeric → **400** naming the field); `long?` ← blank → `null` | derived write of the convention (`BrandID = Parse(dto.Brand.Value)`); the navigation beside the key left alone | **Q11** — blank on `long?` → null; blank on `long` → 400 via Step 2.6 |
| B2 | JSON `string` ← `List<ShiftFileDTO>` (`ToJsonString`; null → null) | **pack**: `CreateConversion<List<ShiftFileDTO>?, string?>(memory: …)` | null → null |
| B3 | `string` → `long`/`long?`/`Guid`/`Guid?`, `int`/`int?` → enum, and the full inverse scalar table; bad input throws `ShiftEntityMappingException` naming the member | built-in text parsing (SM0009 Info); bad input throws ShiftMapper's exception naming the member | Step 2.6 turns it into the same 400 shape |
| B4 | nested children written **replace-with-new** through the pair's `MapBack`; tracked child with a required FK back → SHENGEN010 | update overload rebuilds nested collections (`ToListOrEmpty(source.Lines, MapToLine)`); **M6** SM0049 Info | Invoice lines replaced; `InvoiceRepository.UpsertAsync` keeps its delete-and-recreate |
| B5 | never written: `ID`, `ReloadAfterSave`, `AuditFieldsAreSet`, `IdempotencyKey`, `Tags` (gated by "is a framework member") | **pack** rules (M4): `IgnoreMember<ShiftEntityBase>(e => e.ID, Destination)`, … , `IgnoreMember<IShiftEntityTaggable>(e => e.Tags, Destination)` — a domain column that happens to be called `Tags` is unaffected, because the rule names the framework's member | insert with `ID = null` does not throw |
| B6 | `IsDeleted` and the audit columns ARE written (Q7 of the AutoMapper removal); the repository restores stored `IsDeleted` on update | plain name matches; repository unchanged | — |
| B7 | `ForEntity(member, (dto, ctx) => …)`, `ForEntity(member, (dto, existing, ctx) => …)`, `AfterEntity((dto, existing, ctx) => …)`; `ctx.ActionType` | in a mapper class, on the reverse of the view map: `.ReverseMap().ForMember(MapFrom)`; `.AfterMap((dto, entity) => …)` for anything needing the entity or the action; `IShiftEntityMappingContext.ActionType` (Step 2.5) read from `Services` inside the value | — |
| B8 | view reads a member the entity never writes → SHENGEN008 | `ReverseMap` reports SM0006 (Info) for a destination the reverse leaves unmapped; the conversion table is symmetric so the old asymmetry cannot arise from a one-way conversion | the members SHENGEN008 warns on today appear as SM0006 |
| B9 | `IgnoreEntity` | `.ReverseMap().ForMember(e => e.X, o => o.Ignore())` in a mapper class | — |

## C. List side — `MapToList` (one SQL projection)

| # | Old generator | ShiftMapper | Verify |
|---|---|---|---|
| C1 | `SelectDTO` inlined as member-init with navigation read, SQL-translatable | convention projects: `new ShiftEntitySelectDTO { Value = e.BrandID.ToString(), Text = e.Brand!.Name }` — a join, no second query | `ToQueryString()` shape in the LongRunning suite |
| C2 | nested children projected inline as correlated `Select(...).ToList()`, recursively | nested maps project as one expression | `api/invoice-deep` list SQL |
| C3 | `ID` (`long` → `ToString()`) and `IsDeleted` always bound — OData's soft-delete filter and hash-id `$filter`/`$orderby` run on the projected DTO | plain name matches; `.ToString()` translates | **mandatory** — a projection missing either is a 500 on every list |
| C4 | `Tags` spliced in by `SelectWithTags`; `Revisions` skipped | `Tags` is a nested collection through the framework pair; `Revisions` ignored (pack rule) | tags present in the list projection; `SelectWithTags` retired |
| C5 | conversions restricted to what EF translates (casts, `ToString()`); text parsing in a list DTO → SHENGEN007 instead of a query-time failure | ShiftMapper reports a member/map it cannot project (SM0030 conversion without query form; SM0036/SM0037) instead of failing at query time | list DTOs with `string` → number members, if any |
| C6 | `ForList(member, expr)` spliced into the projection (closures become SQL parameters) | `CreateMap<E, L>().ForMember(d => d.X, o => o.MapFrom(expr))` in a mapper class — spliced, projects | — |
| C7 | `ForListChild(ren)` + `configureChild` | automatic nesting; customize the child pair once | — |
| C8 | `IgnoreList` | `CreateMap<E, L>().ForMember(..., Ignore())` in a mapper class | — |
| C9 | unmapped list members → SHENGEN007 with a paste-ready `map.ForList(d => d.X, e => e.<flattened guess>)` | SM0001 on the list map. The "would flatten to" hint is lost while flattening is off (Q4) — a nice-to-have for ShiftMapper: mention the path flattening *would* have taken in SM0001 | — |
| C10 | `AsNoTracking` before projection (`OdataList`) | repository unchanged | — |

## D. Copy — `CopyEntity` (entity → entity)

| # | Old generator | ShiftMapper | Verify |
|---|---|---|---|
| D1 | property-by-property copy of readable+settable members; navigations copied by reference; skips `ID`, `ReloadAfterSave`, `AuditFieldsAreSet` | `CreateMap<E, E>()` (same-type members are assigned directly, so navigations copy by reference); pack rules skip the three | — |
| D2 | `Tags` IS copied | not copied (pack rule, `Destination` role) | **Q10** |
| D3 | `ForCopy` / `IgnoreCopy` | `m.Copy.ForMember(...)` | — |

## E. Discovery, configuration and diagnostics

| # | Old generator | ShiftMapper | Verify |
|---|---|---|---|
| E1 | triples from `ShiftRepository<,,,>` subclasses and `[ShiftEntity*Endpoint<,>]` attributes (not the `WithMapper` ones) | **M1** marker inside ShiftEntity on the same two places; the programmer's repository is automatic exactly as today | every triple in the 0.1 inventory has its four maps |
| E2 | `[ShiftEntityMapper] partial class` + `Configure(map)` hook + `*Generated` bodies for takeover-with-base-call | an ordinary `ShiftMapperBase` class declaring the pair(s) it customizes — overrides the repository for that pair; "post-process after the conventions" = `AfterMap`; "replace the whole map" = `ConvertUsing` or a repository override. **Attribute deleted, nothing replaces it.** | — |
| E3 | `[ShiftEntityMapper] partial class : IShiftObjectMapper<Child, ChildDto>` (custom pair) | `CreateMap<Child, ChildDto>()` in any mapper class; replaces the implicit nested map (SM0047 Info) | — |
| E4 | `UseGeneratedMapper()` / `UseGeneratedMapper(map => …)` / `UseGeneratedMapper = true` on an attribute | nothing / a mapper class declaring the customized pairs (`o.Mapping(m => m.Nested(n))` for the depth only — Q16) / property deleted — always automatic | — |
| E5 | `MaxDepth(n)` / `[ShiftEntityMapperMaxDepth(n)]` on repository, mapper class, or assembly | `m.Nested(n)` in `o.Mapping` (M2); marker default 10. **Attribute deleted, nothing replaces it.** | — |
| E6 | `CaseSensitive()` | `CreateMap<…>(o => o.Matching = PropertyMatching.CaseSensitive)` in a mapper class, or `ConfigureDefaults` | — |
| E7 | conditional / unbakeable config → SHENGEN005 / SHENGEN009 (errors); cross-assembly config caught at run time by `VerifyBaked` | SM0035 (error) at build, for mapper classes and for `o.Mapping` lambdas alike. No runtime check needed: the shape is read where the lambda is written | — |
| E8 | one mapper **instance per repository**; registry keyed by triple; conflicts recorded and reported at startup; `VerifyBindings` JIT-prepares mappers to catch ABI skew | one map per pair per assembly (Q1); two mapper classes declaring one pair → SM0042 (a repository configures no pair — Q16); SM0027 at build; a consumer built against a ShiftMapper whose conversion was later removed throws on first map (documented as breaking) plus `ConverterApiVersion` for the runtime | — |
| E9 | startup validation: every triple resolves a mapper | same, asking `IMapper.CanMap` for the four pairs (Step 2.9) | uncovered triple still fails at startup with the full list |
| E10 | the generated mapper is reachable only through the repository (or by resolving the registry type by hand) | the maps are ordinary maps: typed `Mapper` methods in the declaring and every referencing project, `IMapper` for libraries (README §6) | a service outside the repository gets the repository's `o.Mapping` customization |

### Diagnostic map

| Old | Meaning | New |
|---|---|---|
| SHENGEN001 / 002 | mapper class not partial / no interface | — (no partial classes, no attribute) |
| SHENGEN003 | deep-mapping cycle, member skipped | SM0048 Info (M2) |
| SHENGEN004 | unmapped view members | SM0001 |
| SHENGEN005 | conditional configuration | SM0035 |
| SHENGEN006 | entity repository configuration suppressed by a builder | analyzer in `ShiftEntity.Analyzers` (Q6) |
| SHENGEN007 | unmapped list members | SM0001 on the list map; SM0030/SM0036 for a lost projection |
| SHENGEN008 | view reads, entity never writes | SM0006 on the reverse map |
| SHENGEN009 | configuration cannot be baked | SM0035 |
| SHENGEN010 | deep write replaces tracked child rows | SM0049 Info (M6) |
| SHENGEN011 | ambiguous case-insensitive match | SM0007 |
| — | (new) mapper class replaces an implicit map | SM0047 Info |
| — | ~~(new) two repositories configure one pair~~ | ~~SM0050 error~~ — unreachable since Q16; SM0042 covers two mapper classes |
| — | ~~(new) mapper class replaces a pair the repository also configured~~ | ~~SM0051 warning~~ — unreachable since Q16 |

### Attributes

| Attribute | Used today by | Fate |
|---|---|---|
| `[ShiftEntityMapper]` | sample `ProductBrandMapper`; 2 ADP files | **deleted**; an ordinary `ShiftMapperBase` class |
| `[ShiftEntityMapperIgnore]` | nobody | **deleted**; `ForMember(..., Ignore())` |
| `[ShiftEntityMapperMaxDepth]` | a commented-out demo on `Invoice` | **deleted**; `m.Nested(n)` |
| `UseGeneratedMapper = true` (attribute property) | sample `Country`, `Invoice`; 10 ShiftIdentity entities | **deleted**; always automatic |
| `[ShiftEntityKeyAndName]` | entities, UI | **stays** — a model attribute; the select convention reads it |

## F. Gaps this plan fills (things the old generator could not do)

| # | Gap | How |
|---|---|---|
| F1 | **A collection of `ShiftEntitySelectDTO`** from a navigation collection (`List<ShiftEntitySelectDTO> Departments` ← `ICollection<Department>`), read side, in memory and in SQL | **M5** collection member convention, in the pack |
| F2 | The same through an explicit **junction entity** (`CompanyBranchDepartments` → `Department`): the source collection's name does not match and the key is on the junction row | one `.ForMember(d => d.Departments, o => o.MapFrom(e => e.CompanyBranchDepartments.Select(x => new ShiftEntitySelectDTO { Value = x.DepartmentID.ToString(), Text = x.Department!.Name })))` on the CompanyBranch view map in `ShiftIdentityMapper` — projects; stays explicit on purpose |
| F3 | Write side of a many-to-many | still a reconciliation, in `AfterMap` or the repository/entity upsert hook (as `CompanyBranch` already does); the member is ignored on the write map. SM0049 says so if it is forgotten |
| F4 | Customize a child once, for every parent that nests it | native — the child pair's own map |
| F5 | A host overriding a **package's** map (e.g. an app reshaping an identity DTO) | native — near beats far, SM0027 says so |
| F6 | Use the repository's maps outside the repository (a service, a report, replication) with the same customization | native — README §6 |
| F7 | DTOs as records / `init` / `required` members | native |
| F8 | Dictionaries (`CompanyBranch.CustomFields` ↔ `Dictionary<string, CustomFieldDTO>`), today a hand-written `ForView` | native dictionary mapping with a nested `CustomField → CustomFieldDTO` map (the password-blanking rule stays a `ForMember`) |
| F9 | Flattening on demand, audited | native, per map (Q4) |
| F10 | `ProjectTo` call-site diagnostics (SM0037) and code fixes for SM0001/SM0011 | native |
