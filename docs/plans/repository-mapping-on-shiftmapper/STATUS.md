# Repository mapping on ShiftMapper — Status

**Last updated:** 2026-09-18 — **Stage 0 done**: 23 triples inventoried, goldens captured from the old generator
(138 assertions, byte-identical on recapture), 11 SHENGEN warnings baselined — all in [`05-inventory.md`](05-inventory.md).
Stages 1–5 not started.

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
| M1 Implicit maps from a marked generic type | ⬜ | Blocks M2, M3. Needs Q1, Q4, Q9. |
| M2 Nested declaration | ⬜ | |
| M3 Configuration surface on the closing type (`o.Mapping`) | ⬜ | Needs Q2, Q3. |
| M4 Pack-level member ignore rules | ⬜ | |
| M5 Collection member conventions | ⬜ | |
| M6 Update map rebuilds a nested collection — Info | ⬜ | |
| 1.7 Release `release-shiftmapper` 0.3.0 | ⬜ | |

## Stage 2 — ShiftEntity side, additive

| Step | Status | Notes |
|------|--------|-------|
| 2.1 Reference ShiftMapper from Model + Core | ⬜ | |
| 2.2 `ShiftEntityConversions` pack (ignores, select convention, files) | ⬜ | |
| 2.3 `ShiftEntityFrameworkMaps` (tag pairs) | ⬜ | |
| 2.4 Marker on `ShiftRepository<,,,>` + endpoint attributes; `ShiftEntityMapping<,,>` surface; `Options.Mapping(...)` | ⬜ | |
| 2.5 `IShiftEntityMappingContext` | ⬜ | |
| 2.6 Repository maps through `IMapper` (behind the registry) + 400 translation | ⬜ | |
| 2.7 Registration (`ShareConversions`; scanned assemblies; host `AddShiftMapper()`) | ⬜ | Needs Q8. |
| 2.8 Golden diff | ⬜ | Q5, Q10, Q11 answered here. |
| 2.9 Startup validation via `CanMap` | ⬜ | |

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
| Q2 two repositories configuring one pair → error | ⬜ | recommended: error |
| Q3 the mapper may construct the repository from DI | ⬜ | recommended: accept |
| Q4 flattening off on implicit maps | ⬜ | recommended: off |
| Q5 null collections → empty | ⬜ | recommended: accept ShiftMapper's default |
| Q6 SHENGEN006 → analyzer | ⬜ | recommended: re-home |
| Q7 one release with both spellings | ⬜ | recommended: yes |
| Q8 who registers the generated mapper | ⬜ | recommended: both |
| Q9 signal on the base class: attribute inside ShiftEntity | ⬜ | recommended: attribute (invisible to the programmer) |
| Q10 `CopyEntity` no longer copies `Tags` | ⬜ | recommended: accept pending goldens |
| Q11 blank select on nullable FK clears; on required FK is a 400 | ⬜ | recommended: keep today's behaviour |

## Log

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
