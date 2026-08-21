# AutoMapper Removal — Status

**Last updated:** 2026-08-21 — Stage 0 and Stage A complete, plus a widened conversion set. Stage B is next.

> **This is the primary working copy of this plan.** It lives in `ShiftTemplates/docs/plans/automapper-removal/`
> and is the one to read and update. A mirror is kept in the private `.shift` repo
> (`.shift/repos/shift-entity/automapper-removal/`) for cross-repo visibility; both are updated together.
> Two passages carry more detail in the `.shift` mirror than here — Q7 and gap C-3 — because this repo is
> public and the change they describe has not shipped yet. Everything else is identical.

Update this file as steps land. Keep it factual: what shipped, what it changed, what surprised you.
Plan: [`01-steps.md`](01-steps.md) · Evidence: [`00-gap-register.md`](00-gap-register.md) · Decisions: [`02-open-decisions.md`](02-open-decisions.md)

**Legend:** ⬜ not started · 🟡 in progress · ✅ done · ⛔ blocked · ➖ dropped (say why)

---

## Stage 0 — Prerequisite

| Step | Status | Notes |
|------|--------|-------|
| 0.1 Confirm generator reaches package-mode consumers | ✅ | **Yes it does.** Verified 2026-08-21 on all four ADP data projects. See Q1 for evidence. |
| 0.2 Explicit analyzer pack item | ➖ | Not needed — conditional on 0.1 answering *no*. |

## Stage A — Make failure visible

| Step | Status | Notes |
|------|--------|-------|
| A1 Behavioral generator test harness | ✅ | `MapperGeneratorHarness` runs generated mappers as objects. SHENGEN004 now has 5 tests (was 0). |
| A2 Diagnostic source locations | ✅ | Falls back to the repository / endpoint attribute. Cycle warning fixed too. |
| A3 Reserved names gated by declaring type | ✅ | `IsFrameworkMember` replaces 9 name lookups. Domain `Tags`/`Revisions` columns map again. |
| A4 Inverse scalar conventions | ✅ | string→long/Guid, int→enum, all nullable forms. Throws on bad input. Guid→string added for symmetry. |
| A5 List unmapped diagnostic (SHENGEN007) | ✅ | Message synthesizes a paste-ready `ForList` line. 8 warnings on the sample project. |
| A6 Entity asymmetry diagnostic (SHENGEN008) | ✅ | Reports view-reads-but-entity-never-writes. 6 warnings on the sample project. |
| A7 Fail-closed fluent config | ✅ | SHENGEN009 at build time + `VerifyBaked` at runtime for cross-assembly config. |
| A8 Deep-write diagnostic + `ForEntity(existing)` + `AfterEntity` | ✅ | SHENGEN010 + both hooks. Built on Q4's recommendation (default-on) — **confirm Q4**. |
| A9 Soft-deleted children excluded from auto-deep | ➖ | **Dropped from the generator — wrong layer.** Soft delete is owned by the repository + OData. The GAP is real (see below) and moves to Stage B. |
| A10 Low-severity generator cleanups | 🟡 | 6 of 8 bullets done — arrays and the collection-container fix landed with the conversion work. Remaining: defensive copies, constructor-only DTO error. |

## Stage B — Close framework-owned holes

| Step | Status | Notes |
|------|--------|-------|
| B1 CI gate | ⬜ | **Do first in this stage** — nothing is verified until it exists. |
| B2 `dotnet new shiftentity` emits a mapper | ⬜ | **Broken right now**, independent of the removal. |
| B3 `ShiftTagMapper` | ⬜ | No repository change needed — ctor already exists. |
| B4 De-eagerize replication ctors | ⬜ | ~3 lines. Ship standalone. |
| B5 `AsNoTracking` into `OdataList` | ⬜ | |
| B6 Tags-in-list splice into `OdataList` | ⬜ | Also fixes the Core→EFCore layering inversion. |
| B7 `MapToList` base-member contract | ⬜ | |
| B8 `ToForeignKey` throws 400 | ⬜ | Scope depends on Q3. |
| B9 `CopyEntity` throws | ⬜ | Land `ProductRepository.CopyEntity` first. |
| B10 Deep children bypass the soft-delete filter | ⬜ | **New, from the A9 review.** See the log entry below. |

## Stage C — Parity harness *(window closes when AutoMapper is deleted)*

| Step | Status | Notes |
|------|--------|-------|
| C1 Triple differ | ⬜ | Deliverable = the reviewed `KnownDivergence` table. |
| C2 Replication goldens | ⬜ | Capture **before** porting each pair. |
| C3 SQL translation tests for deep lists | ⬜ | Currently zero coverage. |

## Stage D — Wiring & enforcement

| Step | Status | Notes |
|------|--------|-------|
| D1 `MappingMode` + registry resolution | ⬜ | Default stays `AutoMapperFirst`. |
| D2 Attribute-endpoint default flip | ⬜ | Needs the three-way `spec.Repository` split. |
| D3 Startup validation | ⬜ | Where "required" is actually enforced. |
| D4 Codegen ABI stamp + check | ⬜ | |
| D5 Registry conflict detection | ⬜ | |

## Stage E — Service migration

| Service | Triples | Status | Notes |
|---------|---------|--------|-------|
| ADP.Surveys | — | ⬜ | Cleanest — no `AfterMap`. Start here. |
| ADP.WarrantyClaims | — | ⬜ | Differ already found 3 regressions here. |
| ADP.ClaimableItems | — | ⬜ | + 5 replication sites. |
| ADP.Menus | — | ⬜ | Worst — 5 `AfterMap` blocks. |
| Menu | 11 | ⬜ | Only if still alive — see Q5. |
| E2 Template's 12 replication sites | — | ⬜ | **Early** — stops the bleed into new services. |
| E3 ADP replication ports + required delegate | — | ⬜ | |

## Stage F — Delete

| Step | Status | Notes |
|------|--------|-------|
| F1 Compat package + obsoletions | ⬜ | |
| F2 ShiftIdentity's 11 ad-hoc `Map<T>` sites | ⬜ | |
| F3 Project template detached | ⬜ | |
| F4 ADP.SyncAgent | ⬜ | Separate workstream. See Q6. |
| F5 Package references + docs | ⬜ | Replication and SyncAgent first (NU1903), Core last. |

---

## Open decisions

| # | Question | Status | Answer |
|---|----------|--------|--------|
| Q1 | Generator reaches package-mode consumers? | ✅ | **Yes.** Analyzer resolves, generator runs, mapper types are in the built assemblies. |
| Q2 | Nullable FK — clear or preserve? | ❓ | *(rec: clear + per-member opt-out)* |
| Q3 | Empty select DTO — `null` or `{Value:""}`? | ❓ | *(rec: make it global)* |
| Q4 | Entity auto-deep — default-on or opt-in? | 🟡 | A8 shipped on the recommendation (default-on + SHENGEN010). Confirm, or A8 needs revisiting. |
| Q5 | Is `Menu` retired? | ❓ | |
| Q6 | SyncAgent — delete or migrate? | ❓ | *(rec: lean delete)* |
| Q7 | Audit-field narrowing — note or advisory? | ❓ | |
| Q8 | Richer list payloads — accept? | ❓ | *(rec: accept, then measure)* |

---

## Log

**2026-08-19** — Plan created from a full cross-repo audit (14 repos, 68 raw findings, 59 surviving
adversarial verification). No code changed. Key correction to the earlier assumption that replication needs a
new mapper abstraction: it does not — the merge overload already exists on both pipelines, and the real
blocker is one eager `GetRequiredService<IMapper>()` in a constructor.

**2026-08-21** — **Step 0.1 done. Answer: yes, the generator runs in package-mode consumers.** All four ADP
data projects (`ClaimableItems`, `Menus`, `Surveys`, `WarrantyClaims`) had `obj/` deleted and were rebuilt from
clean against `ShiftSoftware.ShiftEntity.EFCore` **2026.7.31.1** as a `PackageReference`. All four built with
0 errors, the generator emitted `SHENGEN004` warnings during compilation, and the generated mapper types are
present in the resulting assemblies — 37 mappers total (5 / 18 / 4 / 10), byte-for-byte the same set that was
there before the clean. So ADP's migration is *"the registry already has a mapper for every triple — flip a
switch and audit"*, not *"the analyzer has never run there."*

Two corrections came out of establishing this, both worth carrying forward:

1. **Step 0.1's own proposed test is invalid — it produces a false negative.** `EmitCompilerGeneratedFiles`
   is not set anywhere in ADP, so generated sources are held in memory and never written to disk. After the
   clean rebuild `obj/**/generated/` stayed **empty in all four projects while the generator was demonstrably
   running**. The pre-existing `.g.cs` files were artifacts of a build that had the flag turned on (their
   timestamps, 2026-08-19 11:25–11:27, put them inside the audit window that produced this plan). Rebuilding
   with `-p:EmitCompilerGeneratedFiles=true` reproduced the identical four filenames in `ADP.Surveys.Data`.
   **Do not use `obj/**/generated/` as the signal.** Use one of: the `SHENGEN*` diagnostics in build output,
   the mapper type names in the built assembly, or `@(Analyzer)` resolved via `ResolveReferences`.

2. **The original `-getItem:Analyzer` negative was a methodology artifact.** `dotnet msbuild
   -t:ResolvePackageAssets -getItem:Analyzer` returns only the two implicit SDK analyzers, because the target
   that folds package analyzers into `@(Analyzer)` has not run yet. Query it through `-t:ResolveReferences`
   (or a full build) and `ShiftSoftware.ShiftEntity.SourceGenerator.dll` appears, alongside
   `ResolvedAnalyzers`. That single flag explains the whole contradiction the plan opened with.

Also settled while here:

- **The `exclude="Build,Analyzers"` nuspec line is a red herring.** It is the default `dotnet pack` emits for
  every `PackageReference`, and it is on *every* edge reaching `ShiftSoftware.ShiftEntity` (EFCore, Cosmos,
  ShiftIdentity.Core, Web, Blazor). It does not suppress analyzer resolution: a throwaway project whose only
  reference is `ShiftSoftware.ShiftEntity.EFCore` still resolves the generator. So a **fresh** consumer — a new
  service, or a `dotnet new shift` project — gets the generator with no extra wiring.
- **`RunAnalyzers=false` does not disable the generator.** ADP `obj/` trees carry a
  `.csproj.BuildWithSkipAnalyzers` marker (Visual Studio's skip-analyzers build optimization), which raised
  the question. Rebuilding with `-p:RunAnalyzers=false` still generated all 4 `ADP.Surveys.Data` mappers and
  still reported `SHENGEN` diagnostics. Not a hole.

No source files changed. The four `obj/` trees were left repopulated with their original generated output.

**2026-08-21** — **Stage A complete** (A10 partial). The generator now reports what it cannot do instead of
failing silently, and two live data-loss bugs are fixed. Framework builds clean; **429 tests pass** (up from
401 — 28 new). The sample project builds with 0 errors and reports 8 SHENGEN007, 6 SHENGEN008 and 4 SHENGEN010
warnings, every one a real finding.

**A1** — `ShiftEntity.Tests/Mapping/MapperGeneratorHarness.cs`. Emits the compilation to memory, loads it, and
runs generated mappers as real objects, so a test can assert on the RESULT rather than on emitted text. All
new behavioural tests use it. `SHENGEN004` went from **zero tests to five** (firing, custom-configured,
attribute-ignored, cycle-skipped, and navigable-location).

**A2** — diagnostics now fall back to the repository declaration, or to the endpoint attribute that declared
the triple. Previously `Location.None` for every `UseGeneratedMapper()` triple — the dominant shape — meaning
no file, no line, no local suppression. The cycle warning (SHENGEN003) had the same defect and was fixed with it.

**A3** — one predicate, `IsFrameworkMember`, replaces nine string-name lookups. It resolves overrides back to
their original definition (DTOs routinely override `ID`) and resolves `Tags` through `IShiftEntityTaggable`,
which the entity itself declares. **Two live bugs closed:** a domain column named `Tags` or `Revisions` was
silently dropped from view, entity AND list; and a pair mapper's `Map` disagreed with its own `Projection`
about `ID`.

**A4** — `string→long`, `string→Guid`, `int→enum` and every nullable form, mirroring the read direction. These
returned null before, and a null convention emits NO ASSIGNMENT AT ALL — the member read back perfectly and
never saved. They throw (`ShiftEntityMappingException`) rather than writing a default, because a silent `0` in
a required FK saves a row pointing at the wrong parent. **`Guid→string` had to be added to the read side too**:
it existed in neither direction, and a one-sided conversion is the exact asymmetry this work removes.

**A5 / A6** — the list and entity directions had no unmapped channel at all. SHENGEN007 synthesizes the fix
line (`map.ForList(d => d.ProductBrandName, e => e.ProductBrand.Name)` — it fills in the flattened path when
the member name resolves to one), which is what makes the decision NOT to implement flattening affordable.
SHENGEN008 deliberately does not mirror SHENGEN004: it reports the ASYMMETRY (DTO members the view reads and
the entity never writes), because mirroring would warn on every internal column and be switched off.

**A7** — `Ignore` was decoration. A registration the generator could not read was dropped, and dropping it
bakes the plain convention, so the call compiled, ran, and did nothing. Now SHENGEN009 (error) for the
statically visible shapes, and `ShiftMapperBuilder.VerifyBaked` throws at first use for cross-assembly
configuration, which no compilation-local analysis can ever see. **Switching it on immediately broke the
framework's own build** — `ShiftChildMapperBuilder` forwards between overloads with open-generic receivers by
construction. Those are API definitions, not registrations, and are now exempt; the exemption is pinned by a test.

**A8** — SHENGEN010 fires when auto-deep write composes a TRACKED child with a required FK back to the parent,
where replace-with-new either throws on the FK or orphans rows. Plus the two escape hatches: an
`existing`-aware `ForEntity` overload and an `AfterEntity(dto, entity, context)` hook baked as a trailing call.
Without those, ADP's 16 `AfterMap` blocks have nowhere to go and Stage E stalls. It found the
Invoice→InvoiceLines case in the sample project immediately. **This assumed Q4's recommendation** (keep
default-on, add the diagnostic) — the step is built and tested, but Q4 should be confirmed rather than inherited.

**A9** — deep-composed children are filtered to non-deleted rows in both the view and the list projection, with
`[ShiftEntityMapperIncludeDeleted]` for audit/history members. Keyed off the FRAMEWORK's `IsDeleted`, not any
property with that name — the same mistake A3 just fixed.

**A10** — 4 of 8 bullets: `[ShiftEntityKeyAndName].Text` is read instead of hardcoding `.Name`; init-only DTO
members are now visible to the unmapped scan (they can never be assigned, and were silently dropped AND
invisible); pair hint names are namespace-prefixed (same-named pairs in two namespaces made the generator fail);
and the `ForCopyChild`/`ForCopyChildren` names — documented but never implemented — are gone.
**Still open:** array collections (`ToArray()`), defensive copies of dictionaries/same-typed lists in the view
direction, a proper error for constructor-only DTOs instead of `CS7036`, and matching `HasUserMethod` by full
signature. All cosmetic; nothing depends on them.

**Note for Stage B onward:** the three new warnings arrive in bulk the first time a service builds. That is the
deliverable, not a problem — it is how Stage E gets sized honestly instead of by grepping profiles.

**2026-08-21 (later)** — **Follow-up after review. Three changes, all driven by the team's feedback.**

**1. A9 reverted — soft delete is not the mapper's job.** Correct call, and worth writing down properly.
Soft-delete enforcement lives in exactly two places, both outside mapping: `ApplyDefaultSoftDeleteFilter`
(`ShiftEntity.Web/Extensions/IQueryableExtensions.cs:120`) on the projected DTO queryable, and
`ShiftEntityCrudHandler.cs:498` on the entity queryable. AutoMapper never did it, so the source generator
should not either. The generator-side filtering and the `[ShiftEntityMapperIncludeDeleted]` attribute are gone.

**The gap it was covering is real and remains open** — recorded as Step B10. Both existing filters cover ROOT
rows only. There is no `HasQueryFilter` anywhere in the ecosystem, and the repository issues plain unfiltered
`.Include(...)`, so a child composed deep inside a payload is not filtered by anything. It is latent today
because the AutoMapper profile declares no child pair maps, so nothing composes deep in production yet — but
the day a service switches to generated deep mapping, every parent starts returning its deleted children. The
fix belongs in the repository/EF layer: filtered includes, or an EF global query filter (wider blast radius —
revisions, audit and replication would need `IgnoreQueryFilters()`).

**2. The scalar conversion set is now general, not a hand-list.** The team asked why `long` ↔ `string` worked
and `int` ↔ `string` did not. There was no reason — each direction hand-listed a few pairs. Replaced by one
engine (`ScalarConversion`) that all three directions share, so they cannot drift apart again:

- **to/from text:** every numeric type, `bool`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`,
  `TimeOnly`, `Guid`, and enums (by name or by number, case-insensitive).
- **number ↔ number:** any pair, in either direction, via an explicit cast.
- **Always INVARIANT.** A DTO value crosses machines and locales; a decimal written on one server has to read
  back the same on another. New helpers: `ToInvariantText`, `ToValue<T>`, `ToNullableValue<T>`, `ToEnum<T>`,
  `ToNullableEnum<T>`, all throwing `ShiftEntityMappingException` on bad input rather than writing a default.
- **The list direction is restricted to what SQL can do** — casts and `ToString()`, never a helper call. Text
  parsing has no trustworthy SQL equivalent, so it is reported by SHENGEN007 instead of emitted as a
  projection that would fail at query time.

**3. Collections of simple values now convert, and containers are adapted.** Previously only an exact type
match worked (and was assigned by reference). Now the element type and the container type are handled
separately: `ICollection<int>` → `List<int>`, `List<int>` → `List<string>`, `List<string>` → `List<long>`,
and arrays in either direction. Materialisation goes through `MaterializeCollection`, which respects the
target's own container — `ToList`, `ToArray`, or `new HashSet<T>(...)`.

That fixed two older holes at the same time, both A10 bullets: **arrays were not recognised as collections in
any direction**, and everything was materialised with `ToList()` regardless of target, so **an entity with a
`HashSet` navigation generated code that did not compile** (`CS0029`). Pinned by
`CollectionConversionTests.HashSetOfComplexChildren_GeneratesCompilableCode`.

**4. ShiftIdentity: six hand-written mappings deleted, one live bug fixed with them.**
`CompanyBranchRepository` had `ForView`/`ForEntity` pairs for `Latitude` and `Longitude` (entity `string`,
DTO `decimal?`) — now convention-covered. **The hand-written pair used `decimal.Parse` and `.ToString()`
with no culture**, so on a server whose locale uses a comma for the decimal point, `"51.5074"` read back as
`515074` and saved coordinates were unparsable. The convention is invariant, so that is fixed.
`CompanyBranch.PublishTargets` and `Team.Tags` were both `IReadOnlyCollection<T>` on the DTO and `List<T>` on
the entity, each carrying a comment saying the write side was not convention-covered and would be silently
dropped on save. The container convention covers both now.

**Verified:** ShiftEntity 0 errors / **437 tests pass**; ShiftIdentity.Data 0 errors with none of the four
members flagged; StockPlusPlus Data + API + Test all 0 errors.

### Conversion + collection coverage, measured (2026-08-21, after the widening)

Probed directly against the generator. Recording it so nobody re-derives it.

**Scalar — supported both directions:** every numeric type ↔ text; `bool`, `DateTime`, `DateTimeOffset`,
`TimeSpan`, `DateOnly`, `TimeOnly`, `Guid` ↔ text; enums ↔ text (name or number) and ↔ any numeric type;
numeric ↔ numeric in any combination; implicit widening; `T?` → `T` via `?? default`. Text is invariant, and
malformed input throws `ShiftEntityMappingException` naming the member.

**Scalar — deliberately NOT supported in the LIST direction:** parsing text into a value. There is no
trustworthy SQL equivalent, so SHENGEN007 reports it and the programmer writes a `ForList`. Formatting a
number as text IS supported there (a SQL CAST); formatting a date or bool is not, because the provider's
result would differ from the in-memory one.

**Collections — supported:** any pairing of `List<T>`, `ICollection<T>`, `IEnumerable<T>`, `IReadOnlyList<T>`,
`IReadOnlyCollection<T>`, `HashSet<T>` and arrays, for both simple and complex element types, with element
conversion applied when the element types differ.

**Which methods this covers.** A mapper has four methods, and conversion applies to three of them. `MapToView`,
`MapToEntity` and `MapToList` all route through the same `ScalarConversion` / `CollectionConversion` pair, so
they cannot disagree about what converts. **`CopyEntity` needs no conversions at all** — its signature is
`CopyEntity(TEntity source, TEntity target)`, entity to entity of the SAME type, so every property is copied
into a property of identical type (`target.X = source.X`). There is nothing to convert, which is why the
conversion matrix has three columns rather than four.

The same three builders also emit the PAIR mappers used for deep children (`Map`, `MapBack`, `Projection`), so
a child at any depth converts exactly like a root.

**Still open:** same-typed collections and dictionaries are assigned BY REFERENCE (`dto.Tags = entity.Tags`),
so the DTO and the entity share one instance — A10's outstanding defensive-copy bullet. A dictionary whose
key or value type differs is not converted at all.

**2026-08-22** — **Regression found by the sample suite and fixed.**
`AutoDiscoveredGeneratedMapperTests.MapToList_ProjectsNameAndId` failed: `Name` came back null.

Cause was the A5 suppression. To keep SHENGEN007 quiet for a member the programmer had already taken over with
`ForList`, the loop skipped that member entirely — which dropped its convention BINDING as well as its warning.
`ComposeList` layers the customization on at runtime, but only where the configuration is actually applied.
`Country` registers its `ForList` from the ENTITY's `IConfiguresShiftRepository` hook
(`StockPlusPlus.Data/Entities/Country.cs:51`), so the generated mapper carries no `Configure` of its own; a
mapper resolved straight from the registry therefore had nothing but what was baked, and the column was empty.

Fix: suppress the WARNING, keep the BINDING. The convention is baked as before and `ComposeList` replaces it
when configuration is applied. Pinned by
`UnmappedListMemberDiagnosticTests.ForListConfiguredMember_StillGetsItsConventionBinding`, which uses the
repository-configured shape on purpose — a `[ShiftEntityMapper]` partial runs its own `Configure`
automatically and so cannot reproduce the failure.

Worth keeping in mind for the rest of this work: **suppressing a diagnostic must never change what is emitted.**
The two are separate decisions and this conflated them.

Verified: ShiftEntity **438 tests pass**; StockPlusPlus 173/175, the two failures being the Cosmos tests, which
need the emulator on `localhost:8081` and are unrelated to mapping.
