# Repository mapping on ShiftMapper — Status

**Last updated:** 2026-09-19 — **Stage 3 done (flipped and migrated)**: ShiftMapper is the repository's default
mapper, ahead of the old registry (which stays for one release, Q7); the sample, the item template's files and
ShiftIdentity.Data are migrated — no `UseGeneratedMapper(...)`, no `UseGeneratedMapper = true`, no
`[ShiftEntityMapper]` partial anywhere in framework-owned code, every customization as `o.Mapping(m => …)` or an
ordinary mapper class. **All 23 goldens are green through ShiftMapper** (138 theories), re-frozen with exactly three
recorded changes (Q10, Q15, the Invoice `Total` demonstration) after every other difference was fixed — including all
13 identity triples matching the old generator member for member — and the expression-shape golden was replaced
by the SQL each list projection translates to. Six more ShiftMapper additions (0.3.0, still unreleased). Stages 0–2
done 2026-09-18/19. Stages 4–5 not started; Stage 4 is the deletion, one framework release after this ships.

Update this file as steps land. Keep it factual: what shipped, what it changed, what surprised you.
Plan: [`01-steps.md`](01-steps.md) · Decisions: [`02-open-decisions.md`](02-open-decisions.md) ·
Coverage: [`03-coverage.md`](03-coverage.md) · Consumer guide: [`04-migration-guide.md`](04-migration-guide.md)

**Legend:** ⬜ not started · 🟡 in progress · ✅ done · ⛔ blocked · ➖ dropped (say why)

---

## Stage 0 — Freeze the oracle

| Step | Status | Notes |
|------|--------|-------|
| 0.1 Inventory every triple, nested pair and attribute use (sample + ShiftIdentity) | ✅ | **2026-09-18.** 23 triples (10 sample, 13 identity), arms resolved from the host, nested members read off the goldens, every fluent call and attribute use listed. `[ShiftEntityMapperIgnore]` and a live `[ShiftEntityMapperMaxDepth]` have zero framework-owned uses. [`05-inventory.md`](05-inventory.md) §1. |
| 0.2 Parity goldens written by the OLD generator | ✅ | **2026-09-18.** `StockPlusPlus.Test/Tests/RepositoryMappingParityTests.cs` + `Tests/Parity/RepositoryMapping/` (deterministic fixture builder, runner, 23 golden files, 536 KB). Six theories per triple, 138 tests, ~2 s. ONE suite over both assemblies — the sample host resolves ShiftIdentity's configured mappers too, so the planned second suite in `ShiftIdentity.Tests` was not needed. Capture switch `SHIFT_TEST_CAPTURE_REPOSITORY_MAPPING_GOLDENS=1`. The window closes at Stage 4. |
| 0.3 Baseline the SHENGEN warnings | ✅ | **2026-09-18.** 11 distinct warnings (004 ×2, 007 ×4, 008 ×3, 010 ×2), each with its target `SM` rule; one (Product SHENGEN007) is noise because the repository overrides `MapToList`. [`05-inventory.md`](05-inventory.md) §3. |

## Stage 1 — ShiftMapper features (0.3.0)

| Step | Status | Notes |
|------|--------|-------|
| M1 Implicit maps from a marked generic type | ✅ | **2026-09-18.** `[ShiftMapperDeclaresMap("TEntity", "TView", Reverse, Nested, Flattening, Rules)]` on a class or attribute class; read in `ShiftMapperGenerator.Implicit.cs`; declared by a generated public `ImplicitMapper` class so the maps travel as any package mapper's (`Implicit = true` in the metadata, contract **3**). Explicit wins with SM0047 (Info); the reverse direction is built as a reverse map (SM0006 notes, not SM0001). 13 tests in `ImplicitMapTests.cs`, including running the maps and the two-package hop. |
| M2 Nested declaration | ✅ | **2026-09-18.** Nested pairs are captured as symbols during matching and declared recursively to the marker's depth (or `m.Nested(n)`); a member a convention or `ForMember` claims is not nested; a cycle is cut with SM0048 (Info); beyond the cap the member is left out silently, as today. |
| M3 Configuration surface on the closing type (`o.Mapping`) | ✅ | **2026-09-18.** `ShiftMapperConfigurationSurface` (runtime base with a store and `Map<S,D>()` handles, `Nested(n)`); any lambda over a subclass is read for shape, chains anchored on its `MapExpression` properties through the same `ReadChain`; `IMapper.Configure(surface)` merges the store (overriding) into every generated mapper, including ones built later; a customized member's lookup names the configurator (`Value(member, typeof(X))`) and pulls it from DI on a miss — `IShiftMapperConfiguratorResolver` for a framework that resolves it otherwise; SM0035/SM0050/SM0051/SM0052. 10 tests in `ConfigurationSurfaceTests.cs`, including the DI pull. |
| M4 Pack-level member ignore rules | ✅ | **2026-09-18.** `IgnoreMember<T>(x => x.M, role)` / `IgnoreMember(typeof(T<>), "M", role)` on packs and mapper classes; `MemberRole` Both/Source/Destination; applied wherever the type declares, inherits or implements the member; `ShiftMapperDeclaredIgnore` in metadata (unbound generics nameable — a bug the test found). 7 tests in `IgnoreRuleTests.cs`. |
| M5 Collection member conventions | ✅ | **2026-09-18.** `.ForEachElement().Fill(...).FillIfPossible(...)` on a member convention; fills `IEnumerable<T>` members element by element, inline on both backends (`ToListOrEmpty` / `Enumerable.Select(...).ToList()`); the container is the member's own (list, array, set); write side reported as SM0002 rather than SM0011; `ElementFill` in metadata. 6 tests in `ElementConventionTests.cs`. |
| M6 Update map rebuilds a nested collection — Info | ✅ | **2026-09-18.** SM0049, once per map, naming the collection members; not on value-type or `ConvertUsing` destinations. 2 tests in `UpdateRebuildTests.cs`. |
| 1.7 Release `release-shiftmapper` 0.3.0 | 🟡 | `ShiftMapperVersion` bumped to 0.3.0 in `ShiftFrameworkGlobalSettings.props` and ShiftMapper's `Directory.Build.props`; README, `docs/extension-points.md` and `AnalyzerReleases.Unshipped.md` updated. **Not tagged** — the tag is a pipeline action for the owner. Until it is published, package-mode consumers of the template cannot restore 0.3.0; dev mode (sibling checkouts) is unaffected. |

## Stage 2 — ShiftEntity side, additive

| Step | Status | Notes |
|------|--------|-------|
| 2.1 Reference ShiftMapper from Core + EFCore | ✅ | **2026-09-19.** From `ShiftEntity.Core` and `ShiftEntity.EFCore`, NOT Model: Model is `netstandard2.0` and ShiftMapper is `net10.0`-only, so the pack moved to Core (`Core/Mapping/`). CosmosDbReplication's pattern (project when the checkout exists, package at `$(ShiftMapperVersion)` otherwise) plus the generator as an analyzer in dev mode; Core's `DependencyInjection.Abstractions` to 10.0.11. Core's build writes the pack's declarations (contract 3), EFCore's writes its generated mapper, the shared pack and the tag maps. |
| 2.2 `ShiftEntityConversions` pack | ✅ | **2026-09-19.** `ShiftEntity.Core/Mapping/ShiftEntityConversions.cs`: seven ignore rules (`ID`/`IdempotencyKey`/`Tags`/`Revisions` as destinations, `ReloadAfterSave`/`AuditFieldsAreSet` both ways), the select convention with a `Name` fallback and the element half, the files conversions (memory only), and `string? → long` **throwing a 400 on blank or non-numeric text, naming the field** (Q11) — through a ShiftMapper conversion that takes the mapping. |
| 2.3 `ShiftEntityFrameworkMaps` (tag pairs) | ✅ | **2026-09-19.** `ShiftEntity.EFCore/Tagging/ShiftEntityFrameworkMaps.cs`, two `CreateMap`s. `ProductDTO.Tags` maps through it with nothing written; `SelectWithTags` is no longer needed on the ShiftMapper path. `ShiftTagMapper` (DI) still serves the tag endpoints themselves. |
| 2.4 Markers, surface, `Options.Mapping(...)` | ✅ | **2026-09-19.** Three markers on `ShiftRepository<,,,>` (view + reverse, list, copy — the copy nests nothing, so it stays the reference-copy `ShallowCopyTo` was) and on `ShiftEntityEndpointAttribute<,>` / `ShiftEntitySecureEndpointAttribute<,,>` with `this`; `ShiftEntityMapping<E,L,V>` (`View`/`Entity`/`List`/`Copy`) in EFCore; `ShiftRepositoryOptions.Mapping(...)` (composable); `InitCommon` runs the lambda and calls `IMapper.Configure` before choosing a mapper, so the configuration is in the store whichever mapper serves the repository. `ShiftEntityConfiguratorResolver` (`IShiftMapperConfiguratorResolver`) constructs a repository class from DI, or the built-in repository closed over the host's `DbContextOptions.ContextType` for an `IConfiguresShiftRepository` entity. |
| 2.5 `IShiftEntityMappingContext` | ✅ | **2026-09-19.** Interface in Core (`Core/Mapping/`), scoped `ShiftEntityMappingContext` in EFCore; the adapter publishes `Insert`/`Update` around `MapToEntity` and restores what was there after. |
| 2.6 Repository maps through `IMapper` (behind the registry) + 400 translation | ✅ | **2026-09-19.** `ShiftMapperEntityMapper<E,L,V>` (public, EFCore) resolved fourth in `InitCommon` when `IMapper` can map all four pairs. `ShiftMapperConversionException` (new in ShiftMapper: value, target type, mapping, `SourceMember`) → `ShiftEntityException` 400 `Model Validation Error`, `For` = the DTO member the client sent. Old behaviour for a non-numeric scalar was an uncaught `ShiftEntityMappingException` (a 500); this is the improvement the plan asked for. 11 tests in `ShiftEntity.Tests/Repository/ShiftMapperResolutionTests.cs`. |
| 2.7 Registration | ✅ | **2026-09-19.** `RegisterShiftRepositories`: `AddShiftMapper(o => o.ShareConversions<ShiftEntityConversions>())` (inline lambda, from EFCore), `AddShiftMapper(assembly)` per scanned assembly (Q8: the new ShiftMapper overload), the context and the resolver. The sample's host needed **no line** — `RegisterShiftRepositories(typeof(Marker).Assembly)` covers it; the template's own `builder.Services.AddShiftMapper()` is left to 3.2, where the host gains maps of its own. |
| 2.8 Golden diff | ✅ | **2026-09-19.** `StockPlusPlus.Test/Tests/RepositoryMappingShiftMapperDiffTests.cs`: 23 `Report` theories (never fail; the per-member worklist for Stage 3) + 40 asserting theories over the 10 triples that configure nothing, all green. Findings and decisions below (Q5, Q10 extended, Q11, Q13, Q14). |
| 2.9 Startup validation via `CanMap` | ✅ | **2026-09-19.** `ShiftEntityMapperValidation` builds `Mapper.Create(...)` over the generated mappers the collection registers and accepts a triple when all four pairs `CanMap`; the message names `RegisterShiftRepositories`/`AddShiftMapper()` first. Exercised by every sample host boot; its own test arrives with Stage 3, when the registry link goes and every triple runs through it. `IMapper.CanMap` on a mapper with nothing registered now answers false instead of throwing. |

## Stage 3 — Flip and migrate

| Step | Status | Notes |
|------|--------|-------|
| 3.1 `IMapper` ahead of the registry | ✅ | **2026-09-19.** `ShiftRepository.InitCommon`: options → DI `IShiftEntityMapper` → **ShiftMapper** → the old registry → nothing. `UseGeneratedMapper(...)` and the attribute's `UseGeneratedMapper` property are `[Obsolete]` with the migration guide in the message; the old generator's output disables CS0618 for its own `SelectWithTags` call. `ShiftMapperResolutionTests` pins the order both ways (ShiftMapper wins; the registry still answers a triple ShiftMapper does not declare). |
| 3.2 Sample migrated | ✅ | **2026-09-19.** Per the table in `01-steps.md`, plus: `InvoiceListDTO.Total` (a real customization, summed in SQL); `ProductRepository.MapToList` writes its own `Tags` binding; `Invoice.ConfigureRepository` shows `m.Nested(2)` as a comment. Tests rewritten against `IMapper`: `SourceGeneratedMappingTests.cs` now holds `MapperClassDoorTests`, `AutomaticMappingTests`, the end-to-end `SourceGeneratedMappingTests` and `RepositoryConfigurationTests` (the configured value reaching a map used first in a scope — repository and entity configurators both, the entity one through `DbContextOptions`); `DeepMappingTests`, `DeepListMappingTests`, the two discovery tests updated; the Stage 2 diff test and arm deleted (the parity suite IS ShiftMapper now). `Program.cs` and the DTO/razor comments say what the code does. |
| 3.3 Item template migrated | 🟡 | **2026-09-19.** The item template takes the sample's `ProductBrandMapper.cs` and `ProductBrandRepository.cs` as they are, so both `#if (includeItemTemplateContent)` halves are migrated with 3.2. The Builder run is **not possible until ShiftMapper 0.3.0 and the framework are published**: `dotnet new shift` restores packages, and nothing on nuget.org carries the markers. Verify with a Builder run after the first `release-all`. |
| 3.4 ShiftIdentity.Data migrated | ✅ | **2026-09-19.** The 10 attribute properties removed; `City`, `Region`, `Team`, `CompanyCalendar` (entities) and `CompanyBranchRepository`, `CompanyRepository`, `UserRepository` converted line for line to `o.Mapping(m => …)`; the calendar groups' hashid-encoded `Departments`/`Brands` are a mapper class (`Mappers/CompanyCalendarGroupMapper.cs`, four child pairs, read from `Services` at map time so `Mapper.Create(identity assembly)` — the replication goldens — still constructs without a container). `User`'s `AccessTrees` is now ignored on the write map explicitly (the hook writes the rows), which the old generator excluded by not converting the junction. The replication maps untouched; `ShiftIdentity.Tests` green. |
| 3.5 `SelectWithTags` obsolete | ✅ | **2026-09-19.** `[Obsolete]`, behaviour kept for one release; `ShiftTagMapper` untouched (DI still wins for the tag endpoints). |
| 3.6 Goldens green | ✅ | **2026-09-19.** 138 theories green through ShiftMapper on all 23 triples (sample + identity), re-frozen after the last diff showed only the three recorded changes: `Copy.IdempotencyKey` on 7 entities (Q10), `View.Name` on `api/country-generated` (Q15 — one DTO type as list and view is one map) and `List[0].Total` on Invoice (the 3.2 demonstration). `ListShape` (the old generator's expression tree) retired; `ListSql` — `ToQueryString()` over the host's DbContext, no connection — pins each list projection's SQL instead, and every one of the 23 translates. **Gate for Stage 4 passed.** |

## Stage 4 — Delete

| Step | Status | Notes |
|------|--------|-------|
| 4.1 Old generator + the three attributes + `UseGeneratedMapper` + registry + builder + tests removed | ⬜ | One release after 3.6 (Q7). No attribute replaces any of them. |
| 4.2 SHENGEN006 re-homed | ⬜ | Needs Q6. |

## Stage 5 — Docs, CLAUDE.md, pipeline

| Step | Status | Notes |
|------|--------|-------|
| 5.1 CLAUDE.md | ⬜ | |
| 5.2 `.shift` plans | ⬜ | |
| 5.3 ShiftFrameworkDocs page | ⬜ | |
| 5.4 Pipeline notes | ⬜ | |
| 5.5 Migration guide published | ⬜ | |

## Open decisions

| Q | Status | Decision |
|---|--------|----------|
| Q1 one map per pair per assembly | ⬜ | recommended: accept |
| Q2 two repositories configuring one pair → error | 🟡 | implemented as recommended (SM0050, error); confirm |
| Q3 the mapper may construct the repository from DI | 🟡 | implemented as recommended, with `IShiftMapperConfiguratorResolver` as the framework's override; confirm |
| Q4 flattening off on implicit maps | ⬜ | recommended: off |
| Q5 null collections → empty | ✅ | accepted; the goldens never exercised it (fixtures fill every collection), so no golden changed |
| Q6 SHENGEN006 → analyzer | ⬜ | recommended: re-home |
| Q7 one release with both spellings | ✅ | in effect: Stage 3's release carries both, Stage 4's deletes the old; `[Obsolete]` messages point at the migration guide |
| Q8 who registers the generated mapper | ✅ | both — `RegisterShiftRepositories` registers what it scans through the new `AddShiftMapper(Assembly)`; the host line is optional and comes with 3.2 |
| Q9 signal on the base class: attribute inside ShiftEntity | ✅ | attribute — agreed in discussion 2026-09-18 and implemented (`ShiftMapperDeclaresMap`) |
| Q10 `CopyEntity` no longer copies `Tags` — nor `IdempotencyKey` | ✅ | accepted from the diff: both are pipeline-owned destinations; the fresh row a copy refreshes from carries the same key. The only difference on the automatic triples. |
| Q11 blank select on nullable FK clears; on required FK is a 400 | ✅ | kept exactly: ShiftMapper's `ParseOrNull` already clears a nullable; the pack's `string? → long` throws the same 400 `ToForeignKey` did, naming the select |
| Q13 `Tag → TagDTO` maps every member, not `TagProjection`'s five | ✅ | accepted: the audit members of a tag are harmless on a DTO and the map is the ordinary one |
| Q14 dictionary-valued nesting (`CustomFields`) | ✅ | added to ShiftMapper (in memory; the projection leaves the member out with SM0030). Identity's `CustomFields` still needs its `ForMember` — the read side strips passwords — so the feature serves the general case, not this one |
| Q15 one DTO type as both list and view is ONE map | ✅ | accepted: `api/country-generated`'s list customization is its view's too; use two DTO types where the two must differ (every other sample triple does) |

## Log

- **2026-09-19 (Stage 3)** — The flip was a three-line reorder; the migration was line for line as the guide
  says; the goldens are where the day went, and they earned it. Once the sample's and identity's configurations
  were moved, the parity suite showed FOUR things the Stage 2 diff could not have shown, all fixed in ShiftMapper:
  (1) several `m.View.ForMember(...)` STATEMENTS from one lambda kept only the first — the surface reader
  treated a second access of the same handle as "the same lambda read twice"; now merged member by member;
  (2) an explicit `CreateMap` for a nested child (the calendar groups) stopped implicit declaration BELOW it —
  its own children were SM0011; now the pairs below an explicit map reached through nesting are declared to the
  marker's depth; (3) a PROJECTION used before the configuring repository ran left the customized members out
  in silence — `Compose` read the store without the pull the create method makes; now the generated projection
  wraps its template in `Customizations.Configured(configurator, members, …)`, which pulls first (and the
  `ShiftEntityConfiguratorResolver` constructs the built-in repository for an entity configurator through the
  host's `DbContextOptions.ContextType`, as designed); (4) `Nullable<T>.ToString()` in the query spelling —
  EF translates it as `COALESCE(CONVERT(...), '')`, so a null key would have arrived as EMPTY TEXT in every list;
  the query now says `x.HasValue ? x.Value.ToString() : null`, which is what the old generator said. Two more
  ShiftMapper additions: dictionary-valued nesting (Q14, in memory) and `m.Nested(n)` honoured when written by
  an entity configuring a repository class's pair. One design fact worth remembering: the generated mapper
  constructs EVERY class it composes on first use, so a mapper class with a constructor dependency makes the
  whole assembly's mapper unusable outside a container (`Mapper.Create`) — read `Services` at map time instead,
  as `CompanyCalendarGroupMapper` does. The SQL goldens replaced the expression-tree shape as planned, and
  double as the proof that all 23 list projections translate. Environmental: the sample's Cosmos tests fail
  without the emulator, as before; nothing else in the 381 sample tests, the 285 identity tests, the 535
  ShiftEntity tests or the 532 + 245 ShiftMapper tests is red.
- **2026-09-19 (Stage 2)** — Everything the stage listed, plus seven ShiftMapper additions the diff and the
  design forced, all small and general: `AddShiftMapper(Assembly)` (Q8; lifetime only, and the generator does not
  read it as a registration of the caller); `ShiftMapperConversionException` with `SourceMember`; a conversion
  that takes the mapping (`Func<TSource, string, TDestination>`, `TakesMapping` in the metadata,
  `ConversionWithMapping` at run time) — without it a blank required select could only be a 400 that names no
  field; the "no key, no value" guard on a shaped member whose required entry reads a nullable source member
  (the old `ToSelectDTO(long?)` returned null; ShiftMapper built a select with a null `Value`); a later
  convention entry for an already-filled target as a fallback (the old generator fell back to `Name` when
  `[ShiftEntityKeyAndName]` was absent — `Product`, `City`, `Company` in the sample and identity); a marker's
  `Rules` pack at the furthest level of EVERY map in a project that closes the marker (an override `CreateMap`
  keeps the framework's rules; without this a data project that never calls `AddShiftMapper` got no rules on its
  own maps); and `CanMap` answering false on an empty mapper. Three things the plan did not foresee: (1)
  `ShiftEntity.Model` is `netstandard2.0`, so the pack lives in Core; (2) the endpoint attributes need the copy
  marker too (`this → this`), since nothing else closes `ShiftRepository<,,,>` for an attribute endpoint; (3)
  ShiftMapper's built-in `Parse<long>` writes 0 for blank text, which for a foreign key is a row pointing at
  nothing — hence the pack conversion and the mapping-aware form. The diff itself: the 10 un-customized
  triples match the old generator's output member for member except `Copy.IdempotencyKey` (Q10); every other
  difference is a customization Stage 3 carries over, the `WithMapper` triple is not ShiftMapper's, and the one
  real gap is dictionary-valued nesting (Q14). The startup validation's `CanMap` path is exercised by every
  sample boot but has no test of its own until Stage 3 removes the registry link. The sample was built in
  Release for the diff because a Visual Studio debug session held the API's Debug output; nothing about the
  result depends on the configuration.
- **2026-09-18 (Stage 1)** — All six ShiftMapper features, in one pass, in the ShiftMapper repo. Three things
  the plan did not foresee: (1) the generated mapper implements `IMapper` explicitly, so adding `Configure` to the
  interface broke every existing test until the generator emitted it — worth remembering for any future `IMapper`
  member; (2) a bare `new Mapper()` builds its generated mappers lazily on the FIRST map, so `Configure` has to be
  remembered and applied to mappers built later, not only forwarded to the ones already there; (3) the harness's
  package assemblies are metadata-only, so a runtime test of a cross-assembly feature has to compile framework and
  application into one snippet — the marker reads the same from a source symbol. One real bug the tests found: an
  open-generic declaring type (`Entity<>`) was judged un-nameable for metadata because its type parameters are; it
  is now written unbound. Design points settled while implementing: implicit reverse maps are analysed as reverse
  maps (SM0006 notes rather than SM0001 warnings on the entity members a DTO cannot feed); the implicit mapper is
  local for reporting purposes, so its maps get the full diagnostics at the closing declaration; the write side of
  an element convention is SM0002 (two types nothing converts between), which is the honest statement and never
  SM0011. The declaration contract is 3 — a 0.2 package is refused whole by a 0.3 consumer, so the first framework
  release after Stage 2 must be `release-all`, as CLAUDE.md already requires.
- **2026-09-18 (Stage 0)** — Goldens captured through the running host rather than from the registry, so a
  repository's `UseGeneratedMapper(map => …)` configuration and a repository's overrides are what is pinned (the
  `Arm` field in each file says which). Fixtures are built by reflection, deterministically, with string members
  filled to whatever the same-named member on the other side can parse — the alternative, a hand-written fixture
  per entity, would have been 23 fixtures nobody would keep honest. Two things were tuned after the first capture
  and are worth knowing: collections below the second level carry one element, and the "existing" row a DTO is
  written onto is built one level deep — a full-depth existing graph pinned 40 KB of untouched fixture per file
  and said nothing about mapping. No fixture holes on any triple. Surprise: none of the identity repositories
  needed anything beyond the host's own registrations to construct; `ParityArms` resolved all 23 without a
  `RegistryOnly` fallback.

- **2026-09-18 (revised)** — After discussion: (1) **no attributes for the programmer** — `[ShiftEntityMapper]`,
  `[ShiftEntityMapperIgnore]`, `[ShiftEntityMapperMaxDepth]`, `ShiftEntityMapperDefaults` and the
  `UseGeneratedMapper = true` attribute property are deleted with no attribute replacing them; the framework's
  own member rules become pack code (`IgnoreMember<…>`), which is a new ShiftMapper feature (M4); the previously
  proposed `[ShiftMapperIgnore]` / `[ShiftMapperNested]` are dropped. (2) **Configuration in the repository stays**:
  `o.Mapping(m => m.List.ForMember(...))` in ShiftMapper's vocabulary, read at build time for shape and run at
  run time in the repository — a new ShiftMapper feature (M3, "configuration surface on the closing type"),
  with one-repository-per-pair as an error. (3) **A mapper class overrides the repository** for the pair it
  declares (SM0047/SM0051). (4) **The maps work anywhere** (`Mapper` typed methods, `IMapper`), with the
  repository's configuration applied — hence Q3. The "Contoso way" (a thin ShiftEntity generator writing
  ShiftMapper metadata into `Data.dll`) was examined and rejected — recorded in Q9. Usage counted for the
  attributes: `[ShiftEntityMapper]` in the sample and 2 ADP files, `[ShiftEntityMapperIgnore]` nowhere,
  `[ShiftEntityMapperMaxDepth]` only as a commented demo, `UseGeneratedMapper = true` on 2 sample entities and
  10 ShiftIdentity entities.
- **2026-09-18** — Plan written after reading `ShiftEntityMapperGenerator.cs` (2,662 lines), `ShiftMapperBuilder.cs`,
  `ShiftEntityMapperRegistry.cs`, `ShiftRepositoryOptions.cs`, the sample's repositories and mappers, ShiftIdentity's
  `CompanyBranchRepository` (the real-world shape of a heavy fluent configuration), and ShiftMapper's README and
  `docs/extension-points.md`. Two findings shaped it: (1) a source generator cannot read another generator's output,
  so the discovery of repositories has to be a ShiftMapper feature, not a ShiftEntity generator that emits mapper
  classes; (2) ShiftMapper's member conventions (`{Member}ID` + `{Member}.{NameOf}`) are already the framework's
  `ShiftEntitySelectDTO` convention, and its package model already carries maps and rules across assemblies — so
  the ShiftEntity side is configuration. Consumer usage counted: 20 repositories in `ADP.*`/`Menu` on
  `UseGeneratedMapper(map => …)`, ~250 fluent calls across ADP/Menu/ShiftIdentity — hence Q7 and the migration guide.
