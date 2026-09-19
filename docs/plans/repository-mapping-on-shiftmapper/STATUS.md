# Repository mapping on ShiftMapper — Status

**Last updated:** 2026-09-19 — **Stage 2 done (ShiftEntity side, additive)**: `ShiftEntity.Core` and
`ShiftEntity.EFCore` reference ShiftMapper with its generator; the rules pack, the framework tag maps, the markers
on `ShiftRepository<,,,>` and the two built-in-repository endpoint attributes, `o.Mapping(...)`,
`IShiftEntityMappingContext`, the `IMapper` door behind the registry, the registration and the `CanMap` startup
path are in; the sample's data project declares its 40 maps (10 triples + nested) with nothing written in it.
Diffed against the 23 goldens through ShiftMapper: the **10 triples that configure nothing match exactly** (one
accepted difference, Q10), the other 12 differ only where a `UseGeneratedMapper(map => …)`, a mapper partial or an
override is waiting for Stage 3, and one is a `WithMapper` triple ShiftMapper does not declare. Seven small
ShiftMapper additions rode along (0.3.0, still unreleased). Stages 0 and 1 done 2026-09-18. Stages 3–5 not started.

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
| 3.1 `IMapper` ahead of the registry | ⬜ | |
| 3.2 Sample migrated (automatic / `o.Mapping` / mapper class all demonstrated) | ⬜ | |
| 3.3 Item template migrated (Builder run) | ⬜ | |
| 3.4 ShiftIdentity.Data migrated (10 `UseGeneratedMapper = true`; repositories to `o.Mapping`) | ⬜ | |
| 3.5 `SelectWithTags` obsolete | ⬜ | |
| 3.6 Goldens green | ⬜ | Gate for Stage 4. |

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
| Q7 one release with both spellings | ⬜ | recommended: yes |
| Q8 who registers the generated mapper | ✅ | both — `RegisterShiftRepositories` registers what it scans through the new `AddShiftMapper(Assembly)`; the host line is optional and comes with 3.2 |
| Q9 signal on the base class: attribute inside ShiftEntity | ✅ | attribute — agreed in discussion 2026-09-18 and implemented (`ShiftMapperDeclaresMap`) |
| Q10 `CopyEntity` no longer copies `Tags` — nor `IdempotencyKey` | ✅ | accepted from the diff: both are pipeline-owned destinations; the fresh row a copy refreshes from carries the same key. The only difference on the automatic triples. |
| Q11 blank select on nullable FK clears; on required FK is a 400 | ✅ | kept exactly: ShiftMapper's `ParseOrNull` already clears a nullable; the pack's `string? → long` throws the same 400 `ToForeignKey` did, naming the select |
| Q13 `Tag → TagDTO` maps every member, not `TagProjection`'s five | ✅ | accepted: the audit members of a tag are harmless on a DTO and the map is the ordinary one |
| Q14 dictionary-valued nesting (`CustomFields`) | 🟡 | ShiftMapper does not nest dictionary VALUES (`Dictionary<string, CustomField>` → `Dictionary<string, CustomFieldDTO>` is SM0002); the old generator did. Two identity triples. Recommended: add it to ShiftMapper in Stage 3 rather than hand-write two `ForMember`s |

## Log

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
