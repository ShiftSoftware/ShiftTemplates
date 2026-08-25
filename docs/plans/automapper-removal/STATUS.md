# AutoMapper Removal — Status

**Last updated:** 2026-08-25 — **AutoMapper is removed from the framework.** Stages A, B, D, E and F
complete; Stage C partial (C1 second half outstanding, and its window has now closed — see below).

**Scope:** `ShiftEntity` + `ShiftIdentity` + `ShiftTemplates` + CI. Consumer services (`ADP.*`,
`ADP.SyncAgent`, `Menu`) are out of scope — see [`README.md`](README.md#scope--framework-only).

> **This is the primary working copy of this plan.** It lives in `ShiftTemplates/docs/plans/automapper-removal/`
> and is the one to read and update. A mirror is kept in the private `.shift` repo
> (`.shift/repos/shift-entity/automapper-removal/`) for cross-repo visibility; both are updated together.
> Two passages carry more detail in the `.shift` mirror than here — Q7 and gap C-3 — because this repo is
> public. Everything else is identical.

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
| A12 Framework audit members are mapper payload (Q7) | ✅ | **2026-08-22.** `CreateDate`/`LastSaveDate`/`IsDeleted`/`CreatedByUserID`/`LastSavedByUserID` removed from `EntityExcludedMembers`; `ID`, `Tags`, `ReloadAfterSave`, `AuditFieldsAreSet`, `IdempotencyKey` stay. Repository restores the stored `IsDeleted` on update. 9 new tests; 2 source-text pins inverted. `ViewHandledMembers`, `CopyExcludedMembers` and the list filter deliberately untouched — see the log. |
| A11 Case-insensitive member matching + opt-out | ✅ | **New 2026-08-22, from a field report.** Parity regression — AutoMapper matched across case, the generator does not; already broke 3 live members in `CompanyBranchListDTO`. Optioned like `MaxDepth`, default insensitive, exact-first, conflict → skip + **SHENGEN011**. **Shipped 2026-08-24**; every case-mismatch `ForList` bridge it made redundant is removed (CompanyBranch scope ids x3, Team.CompanyId). |

## Stage B — Close framework-owned holes

| Step | Status | Notes |
|------|--------|-------|
| B1 CI gate | ➖ | Three steps before the pack steps: `dotnet test ShiftEntity.Tests`, build `StockPlusPlus.Data` (the SHENGEN gate — every sample mapper lives there), build `StockPlusPlus.Test`. Release config, so it reuses the same `bin/Release` the pack steps need. **The plan's "restore constraint" was refuted** — every `ShiftSoftware.*` reference already resolves as `"type": "project"`, so no csproj change, no local feed, no `!Exists` guards (which would be wrong anyway: those csprojs are template content). Also dropped: `ShiftIdentity.Data.Tests` does not exist, and `--filter Category!=RequiresSql` matches nothing (zero `[Trait]` attributes). **REVERTED 2026-08-23 at the owner's request — do not touch the CI pipeline.** `azure-pipeline.yml` is byte-identical to HEAD. The findings above are kept because they are what a future attempt needs; the step is not to be re-applied without asking. |
| B2 `dotnet new shiftentity` emits a mapper | 🟡 | Template edits done: `ProductBrandMapper.cs` added to the item template's `include` + `rename`; its sample-only customizations guarded behind `includeItemTemplateContent` (applied literally, the plan's fix produces three CS1061s because `Code` only exists behind that symbol); the project template's `Mappers/` modifier widened so a mapper cannot ship into a sample-app-free project; `Mappers/.keep` added; `ProductBrandForm.razor` restructured so `Key`/`BrandItem` are declared unconditionally. **Not yet verified end to end** — needs a Builder run (`dotnet new install` is a machine-global side effect). |
| B3 `ShiftTagMapper` | ✅ | `ShiftEntity.EFCore/Tagging/ShiftTagMapper.cs`, registered `TryAddScoped` in `AddShiftTagging`. No repository change — DI resolution precedes AutoMapper in `InitCommon`, so it is live now; 10 tagging integration tests pass through it. Two of the plan's three "traps" were wrong: `TagProjection.ToDto` **does** map `ID`, and `MapToEntity` writes `IsDeleted` **and** the audit fields per the Q7 decision. Added `TagProjection.ToDtoSingle` so the tag shape keeps one definition. |
| B4 De-eagerize replication ctors | ✅ | Lazy `FallbackMapper`; both ctors hold the provider. **Neither ~3 lines nor risk-free:** the resolve is HOISTED above the per-row loop in `Replicate`, because the use site sits inside the swallowing `catch` — deferring it there would turn "host forgot AddAutoMapper" into a sweep that reports success while marking every row permanently dirty. The other two sites have no enclosing catch and resolve in place. |
| B5 `AsNoTracking` into `OdataList` | ✅ | One call in `OdataList`. **Plan overstated the problem** — every mapper in the org returns a pure projection and EF does not track those, so nothing tracked before and nothing observably changed; this is a guard against a mapper that materializes first. `AutoMapperShiftEntityMapper` keeps its own call (direct `MapToList` callers bypass the repository). Deliberately NOT on `GetIQueryable` — bulk delete mutates and saves those entities. |
| B6a Layering: tagging projection → Core | ✅ | `TagProjection` + `TaggableProjectionExtensions` moved to `ShiftEntity.Core/Tagging/` (zero EF dependencies), generator constant retargeted, `[Obsolete]` forwarder left in EFCore because mappers baked into published packages call the old name (gap B-10). Pinned by a generator test asserting the emitted call targets Core. |
| B6b Runtime tags-in-list splice | ➖ | **Deferred.** `OdataList` receives an already-built `IQueryable<ListDTO>`, not the projection lambda — so this is not a splice but an `ExpressionVisitor` rewrite of a tree the repository did not author, on the hot list path, against a gap rated LOW with **zero live exposure** and zero framework-side tagging tests. Cheaper substitute: fold "and Tags" into B7's contract. |
| B7 `MapToList` base-member contract | ✅ | **Reduced, per the owner's call.** Contract documented on `IShiftEntityMapper.MapToList`: `IsDeleted` and `ID` must be bound, why (the soft-delete filter and `$orderby`/`$filter` run on the *projected DTO*), and what happens if not — empirically, EF Core 10 **throws**, it does not default, so it is a 500 rather than leaked rows. Generator warning dropped (structurally near-vacuous — the generator binds both deliberately) and the reflection test dropped (both hand-written mappers in the workspace already bind both). |
| B8 `ToForeignKey` throws 400 | ✅ | Both helpers now `TryParse` with the invariant culture and throw `ShiftEntityException(400)` naming the member via `[CallerArgumentExpression]`; the nullable overload still returns null on blank (clearing an optional reference is legitimate) but throws on a non-numeric value. 15 new tests in `ShiftEntity.Tests/Mapping/ForeignKeyGuardTests.cs`. No generator change needed — the list projection inlines the select-DTO member-init and never calls the helper, so no throwing code enters an expression tree. **ABI note (gap B-10):** adding the optional parameter changes both signatures, so an assembly compiled against the old one emits a 1-arg call and would hit `MissingMethodException` under version skew. Everything in scope is rebuilt together; this is exactly the hazard **D4** exists to stamp. Q3 does not gate it — the blank-`{"Value":""}` case is handled either way. |
| B11 Soft delete is not writable through an upsert | ✅ | **2026-08-23, at the owner's request.** `ShiftRepository`'s upsert restores the stored `IsDeleted` after mapping, on **update** only — deleting is gated on `Access.Delete`, an upsert needs only `Access.Write`, and there is no undelete API. A **repository** policy, not a mapping rule: mappers keep mapping the member; the repository decides which writes it keeps. Insert exempt (`AuditStamper` forces it false). 4 tests in `SoftDeleteOnUpdateTests`. Read paths untouched — deleted rows still return by ID, deleted tags still show on the entities carrying them. |
| B9 `CopyEntity` throws | ✅ | `ProductRepository.CopyEntity` landed first, then the throw. **Breaks nothing today** — AutoMapper is registered in every host in scope, so the `else` branch was already dead code; this is a pre-req that makes D1/E safe. Gap-register correction: the taggable auto-include sets `ReloadAfterSave` on every taggable **FindAsync**, not on every insert. |
| ~~B10 Deep children bypass the soft-delete filter~~ | ➖ | **DROPPED 2026-08-23 by the owner — this is not a bug, it is the intended behaviour.** Filtering soft-deleted rows is the repository and OData layer's job; mapping does not do it, and AutoMapper never did either. Attaching a tag and later retiring that tag from the vocabulary does **not** remove it from the entities already carrying it — soft-deleting a tag stops it being attached to anything NEW (`TaggingPipeline` resolves live tags only) and nothing more. Same for deep-composed children. A filter added to `TagProjection`/`TaggableProjectionExtensions` was reverted; `TaggingTests.Product_DeletedTag_IsStillReturnedOnBothTheViewAndTheList` now pins the intended behaviour so it does not come back. |


## Stage C — Parity harness *(window closes at F1; C3 has no window)*

| Step | Status | Notes |
|------|--------|-------|
| C1 Triple differ | 🟡 | **First half landed.** `Tests/Parity/`: enumeration (22 triples, both sources), arm resolution + `ArmKind`, `MemberPathDiff` with 12 guard tests, and an inventory that prints what each triple actually resolves. **Measured: 20 Configured, 1 RegistryOnly (`Country/CountryDTO/CountryDTO` — gap B-1 exactly), 1 RepositoryOverride, 0 AutoMapperFallback, 0 None.** Remaining: object filler, rules layer, mutation self-test, and the reviewed `KnownDivergence` table — which is review time, not engineering time. |
| C2 Replication goldens | ✅ | 24 facts (was 22) against goldens captured from the live AutoMapper arm, compared by member path. +2 tombstone facts (`IsDeleted = true`) — zero coverage before. Fixtures shared with the capture tool so both arms get identical input. Now host-free: 191 ms. **The plan's stated rationale was dead** (the port landed a month before the plan); it was worth doing because the facts asserted agreement, never values — nothing pinned `BranchID`, the Cosmos partition key, surviving an apply-onto. |
| C3 SQL translation tests for deep lists | ✅ | `DeepListTranslationTests`, 4 facts: auto-deep three levels, explicit `ForListChildren`, the `Brands` two-hop aggregation, and the taggable Product list. Asserts exactly ONE root `IsDeleted` predicate — the mirror of A9/B10 being dropped, pinning that composed children are deliberately unfiltered. Names tables, never EF aliases. |

## Stage D — Wiring & enforcement

| Step | Status | Notes |
|------|--------|-------|
| D1 `MappingMode` + registry resolution | ✅ | `ShiftEntityMappingMode` (AutoMapperFirst / GeneratedFirst / GeneratedOnly) on `ShiftEntityOptions`; `ShiftRepository.InitCommon` now consults the registry between DI and AutoMapper, gated on the mode. `GeneratedMapperFactory` caches the ACTIVATOR, never the instance — generated mappers hold per-instance builder state, so a singleton would leak one repository's customization into every consumer of the triple. **Default unchanged, so shipping it changes nothing.** 6 acceptance tests incl. the no-options-configured host and the each-repo-gets-its-own-instance guard. |
| D2 Attribute-endpoint default flip | ✅ | D1 already covers the runtime path — attribute endpoints with no mapper and no custom repository use the built-in `ShiftRepository`, so the registry step serves them. What D2 added is stopping the pointless AutoMapper default-map synthesis for triples the registry covers, which is the last framework path reaching for AutoMapper by default. The filter lives in the DEFERRED AutoMapper factory, not `RegisterShiftRepositories`, because the mode is only final after every `Configure` callback has run. |
| D3 Startup validation | ✅ | `ShiftEntityMapperValidation.Validate` — one aggregate exception listing every uncovered triple, every registry conflict and every ABI-skewed mapper. Coverage accepts a DI descriptor, a registry hit, or a repository that OVERRIDES a mapping method (`DeclaringType`-based — a naive "has a MapToView?" check passes every repository and validates nothing). Runs module initializers first, each wrapped, since reflection alone does not trigger them and the registry would look empty. ~~Uncovered triples are fatal only under `GeneratedOnly`.~~ **Superseded by F1:** `MappingMode` was deleted with the AutoMapper path, so `Validate` has no mode gate left — it is called unconditionally and an uncovered triple fails startup in every host. |
| D4 Version-skew detection | ✅ | **Detected, not declared — nothing to version and nothing to remember.** `ShiftEntityMapperRegistry.VerifyBindings()` JIT-prepares each registered mapper's methods, which resolves their call targets; a member the mapper was compiled against and that no longer exists throws there, at startup, naming itself. Verified against a purpose-built two-assembly skew probe: `PrepareMethod` raises `MissingMethodException` without invoking anything. **Replaced an earlier hand-maintained ABI number**, which had both failure modes of a manual version — it fires on additive changes that break nothing, and stays silent whenever somebody forgets to bump it. Only `MissingMethod`/`MissingField`/`TypeLoad` are treated as skew; anything else is left alone rather than becoming a spurious startup error. |
| D5 Registry conflict detection | ✅ | `Register` is now idempotent for the same type and deterministic for a different one — it prefers the mapper declared alongside its ENTITY, since that is the one whose generator run could see the entity's configuration. Conflicts are RECORDED, never thrown: `Register` runs in a `[ModuleInitializer]`, where an exception becomes an unreadable `TypeInitializationException`. D3 turns them into one readable startup error. |

## Stage E — Migrate framework-owned code

| Target | Size | Status | Notes |
|--------|------|--------|-------|
| E1 Migration recipe | — | ✅ | [`migration-guide.md`](migration-guide.md) — written to be read standalone by a consumer team, since they will run it without us. Covers the six steps, the three deliberate behaviour changes to expect from the differ, the two genuinely hard shapes (collection reconciliation, replication), and what will never be done for them (flattening). Ships in F5's docs pass. |
| StockPlusPlus sample | 3 profiles / 83 lines | 🟡 | **Effectively already migrated** — the Stage C inventory found every sample triple on a generated mapper or a repository override. The remaining profile maps are NOT triples: `CompanyBranchModel→CompanyBranchListDTO` and `Product→ProductCategoryListDTO` feed **Cosmos `ProjectTo`** in two controllers, and `Product→ProductModel` is replication. Those are **F3** and **E3** respectively, not this row. |
| ShiftIdentity.Data | 11 profiles / 352 lines | 🟡 | **Also effectively migrated.** Reading the profiles: 10 of the 11 are *purely* replication maps (`Entity→*Model`), already superseded by hand-written `ToXModel()` delegates and pinned by C2's goldens. Only `User.cs` still carries triple maps (`User→UserDTO`/`UserListDTO`) plus `UserDataDTO`, which is **F2**. The replication maps must stay until **E3** — they are the fallback that keeps un-migrated `Replicate` call sites working. |
| E2 Template's 12 replication sites | — | ✅ | All 12 now pass their `ToXModel()` delegate explicitly. **The plan's fix did not work as written:** `ReplicateAllAsync` lives in `ShiftIdentity.AzureFunctions`, which the API does not reference and should not — so instead of calling it, the calls keep their shape and gain delegates. Better as template content anyway: this is the file every new service is scaffolded from, so it now *demonstrates* passing a delegate rather than hiding the question behind a helper. |
| E3 Required replication delegate | — | ✅ | **Q9 answered: take the break.** All 9 overloads across both pipelines now require the delegate; all 4 AutoMapper fallbacks and the lazy `FallbackMapper` are gone. **AutoMapper left `ShiftEntity.CosmosDbReplication` entirely** — usings and `PackageReference` — which is F5's first action, arriving early as a consequence. `Utility.BuildStamp` now throws on an empty document id rather than writing a stamp that can never address the document again. |
| ~~ADP.Surveys / WarrantyClaims / ClaimableItems / Menus / Menu~~ | 37 triples, 16 `AfterMap` | ➖ | **Out of scope.** Consumer services — they migrate on their own schedule via E1 + the compat package. |

## Stage F — Delete

| Step | Status | Notes |
|------|--------|-------|
| F1 Delete the AutoMapper code path | ✅ | **Q10 answered "delete outright" — no compat package ships.** `AutoMapperShiftEntityMapper`, `DefaultAutoMapperProfile`, `AutoMapperExtensions`, `ShiftTaggingAutoMapperProfile`, `AddAutoMapper`, `AddEndpointDefaultMap` and the whole `services.AddAutoMapper(...)` composition block are gone. `ShiftRepository` resolves DI → registry → nothing. **`MappingMode` went with it** — it chose between AutoMapper and the registry, so with one side deleted its three values collapsed to one; startup validation is now unconditional. |
| F2 ShiftIdentity's ad-hoc `Map<T>` sites | ✅ | `Mappers/UserProjections.cs` (4 explicit projections) replaces every site; `IMapper` is out of `UserRepository`, `UserEndpoints`, `UserManagerEndpoints`. **All 11 profiles deleted as dead code** — 10 were replication maps already superseded at E3, `User.cs`'s triple maps by `UseGeneratedMapper`. Two findings: one call was a `UserInfoDTO`→`UserInfoDTO` identity map, and the write direction was writing `EmailVerified`/`PhoneVerified`/`IsDeleted`/audit/PK from the request body (see gap C-3). |
| F3 Project template detached | ✅ | Registrations removed from `API/Program.cs` and `Functions/Program.cs`; 3 profiles deleted; `ProductCategories.cs` now calls `ViewAsync` (its `opt.Items["lang"]` turned out to be read by **nothing**). The two `ProjectTo` controllers split: the SQL one uses `repository.MapToList`, the Cosmos one a hand-written `CompanyBranchProjections.ToListDTO` **verified against AutoMapper before deletion** — the check caught a real `""`-vs-`null` divergence. |
| F4 ADP.SyncAgent | ➖ | **Out of scope** — no ShiftEntity coupling, nothing blocked by it. Recorded so the release notes say "gone from the framework", not "gone". |
| F5 Package references + docs | ✅ | `ShiftEntity.Core.csproj` was the last one (replication went early at E3). `grep -rn "AutoMapper" --include=*.csproj` across all three repos now returns **nothing** — with no compat package there is no carve-out. Parity harness retired (`GoldenCapture` deleted, `ParityArms.Baseline` and `ArmKind.AutoMapperFallback` removed). `ShiftFrameworkDocs` rewritten; `auto-mapper-profiles.md` → `mappers.md` + nav. **The docs pass MISSED two pages**, because it grepped for the literal string `AutoMapper` and neither page contains it: `project-setup/data-project/repositories.md` (said `IMapper`, and taught a repository constructor that no longer compiles) and `project-setup/data-project/dependencies.md` (said "Automapper Profiles"). Both fixed in the follow-up audit below. |

---

## Open decisions

| # | Question | Status | Answer |
|---|----------|--------|--------|
| Q1 | Generator reaches package-mode consumers? | ✅ | **Yes.** Analyzer resolves, generator runs, mapper types are in the built assemblies. |
| Q2 | Nullable FK — clear or preserve? | ❓ | *(rec: clear + per-member opt-out)* |
| Q3 | Empty select DTO — `null` or `{Value:""}`? | ❓ | *(rec: make it global)* |
| Q4 | Entity auto-deep — default-on or opt-in? | 🟡 | A8 shipped on the recommendation (default-on + SHENGEN010). Confirm, or A8 needs revisiting. |
| ~~Q5~~ | ~~Is `Menu` retired?~~ | ➖ | Dropped 2026-08-22 — consumer scope, moot here. |
| ~~Q6~~ | ~~SyncAgent — delete or migrate?~~ | ➖ | Dropped 2026-08-22 — not a framework decision. See F4. |
| Q7 | Audit-field narrowing — note or advisory? | ✅ | **Neither — there is no narrowing.** Generated `MapToEntity` writes the audit + soft-delete members, as AutoMapper did; guards belong in the repository or an explicit `IgnoreEntity`. **Shipped 2026-08-22 for 5 of 6 — `ID` carved out** (its convention throws on the null every insert carries, and deep write would force an identity insert). Repository soft-delete guard shipped with it. Gap C-3 closed. |
| Q8 | Richer list payloads — accept? | ❓ | *(rec: accept, then measure)* |
| **Q9** | Ship the required-delegate compile break? | ✅ | **Yes — shipped 2026-08-24 in E3.** ADP and other consumers refactor later on their own schedule, pinned to the previous framework version until they do. |
| **Q10** | Shipped default: `AutoMapperFirst` forever, or a flip? | ✅ | **Answered 2026-08-25: delete it outright**, against the compat-package recommendation. `MappingMode` does not survive; un-migrated consumers (ADP, Menu) pin the previous framework version until they port. |

**Diagnostic ids:** shipped with Stage A — `SHENGEN007` unmapped list · `008` view members never written back
· `009` configuration cannot be baked · `010` deep write replaces tracked children. **`011` is reserved for
A11** (ambiguous case-insensitive match). Next free after that is `012`.

---

## Log

**2026-08-25** — **Adversarial audit after Stage F: three things the stage's "done" rows did not cover.**

**The framework's own public XML documentation still taught AutoMapper as the default.** About 16 `///` lines
— across `ShiftEntityEndpointAttributes`, `ShiftEntityEndpointDiscovery`, `ShiftRepositoryOptions`,
`ShiftTaggingServiceCollectionExtensions` and `Core/Extensions/IServiceCollectionExtensions` — described "the
default AutoMapper map(ping)" as what an endpoint or repository gets when nothing is configured. They ship
inside the NuGet packages and surface in IntelliSense, so they are consumer-facing documentation that F5's
`*.csproj` grep could not see. The accurate wording is the source-generated mapper resolved via
`ShiftEntityMapperRegistry`.

**A second docs repo was missed entirely.** `ShiftFrameworkDocumentation` (the Blazor docs site) is a
different repository from `ShiftFrameworkDocs` (mkdocs), and the Stage F docs pass covered only the latter. It
also held **the only actual build break the removal left behind**: `Docs.API/Program.cs` still called the
deleted `x.AddAutoMapper(...)`.

**Two mkdocs pages were missed by a case-sensitive grep** — `repositories.md` (says `IMapper`) and
`dependencies.md` (says "Automapper Profiles"); see F5 above. Grepping one spelling of the product name is not
a sweep.

**2026-08-25** — **Stage F: AutoMapper is gone from the framework.**

**Q10 was answered against the plan's own recommendation** — delete outright, no compat package — and that
reshaped the stage rather than just sizing it. F1 stopped being "ship a package" and became "delete a code
path". F2 and F3 had to land *first*: with no fallback, nothing catches a site that still expects one, so the
order the plan gave (F1 → F2 → F3) would have meant a broken tree in between.

**Three things this stage deleted were dead before it started, which is worth recording separately from the
work of deleting them.** All 11 ShiftIdentity profiles: ten were replication maps already superseded by
`IdentityReplicationMappingExtensions` at E3, and `User.cs`'s triple maps by `UseGeneratedMapper`. Two of the
three template profiles, for the same reason. And `UserEndpoints.cs:64`, which mapped `UserInfoDTO` to
`UserInfoDTO` — an identity map through AutoMapper, copying every member onto fresh instances to no purpose.
The Stage C inventory had already implied all of this (20 of 22 triples `Configured`); reading it as
"Stage F is mostly deletion" up front would have been correct.

**The one place the mapping was NOT already replaced was the Cosmos read path**, and it is the one that
justified the whole parity apparatus. `CosmosCompanyBranchController` projects from a replicated document, not
a SQL entity, so no repository and no generated mapper covers it — the profile was the only written
description of that mapping. The hand-written replacement was compared member-by-member against AutoMapper
*while AutoMapper still existed*, and diverged on two: AutoMapper's `long? -> string` convention yields `""`
for a null FK, while the profile's one explicit FK map (`CityId`) yields `null`. **The same DTO ships both
behaviours**, and no amount of reading the profile would have surfaced that — only running it did. Both are
reproduced exactly rather than tidied up; fixing the inconsistency is a deliberate API change, not a
side effect of dropping AutoMapper. `CosmosProjectionParityTests` froze the captured values as literals, so
the test outlived its oracle.

**`opt.Items["lang"]` needed no replacement.** The plan flagged it as a runtime-context feature `MappingContext`
has no equivalent for, and asked for a decision. Nothing anywhere reads that item — the Function was
demonstrating a feature no code consumed, while building a throwaway `MapperConfiguration` over the entire Data
assembly on every request.

**`MappingMode` did not survive, and should not have.** It existed to choose between AutoMapper and the
registry so services could move one at a time. With AutoMapper deleted, `AutoMapperFirst` names a behaviour
that no longer exists and `GeneratedFirst`/`GeneratedOnly` become the same thing. Leaving a knob with one
position would have been worse than removing it. Startup validation, which used to hard-fail only under
`GeneratedOnly` because the other modes had AutoMapper behind them, is now unconditional.

**One deliberate behaviour change, beyond the mechanical port** — flagged here because it is the opposite of
what Q7 decided for the *generated* mapper. The self-service profile PUT (`UpdateUserDataAsync`) used
`Map(dto, user)`, which wrote every name that lined up: `IsDeleted`, the audit fields, the primary key, and
`EmailVerified` / `PhoneVerified`. `ApplyProfileEdits` writes only the six fields a user may change about
themselves. Q7 said the *mapper* maps everything and the repository guards; this is not the mapper, it is a
single hand-written endpoint mapping, which is exactly the "unless ignored explicitly by the programmer" case.
🔒 Detail in the `.shift` copy (gap C-3).

**Verification.** ShiftEntity 480/480 · StockPlusPlus.Test 198/198 · ShiftIdentity solution, StockPlusPlus
API / Functions / Test / Web.Tests all build clean. `grep` for AutoMapper across all three repos returns only
comments. **Not done:** the template was not installed and `dotnet new shift` was not re-run across parameter
combinations (skipped at the user's request), and C1's differ second half remains outstanding — its window has
now closed with the oracle, so what it would have found on the un-migrated triples is no longer discoverable
by that route. Nothing depended on it, since the inventory found 0 `AutoMapperFallback` triples.

**Consumers.** ADP and Menu are hard-broken on upgrade and pin the previous framework version until they port.
That is the cost the compat package existed to avoid, accepted deliberately with Q10.

**2026-08-24** — **Stage E: E1 and E2 done, E3 shipped. AutoMapper is out of the replication package entirely.**

**The stage was much smaller than budgeted, for a good reason.** It was scoped as two service migrations;
reading the profiles showed both had already been migrated during Stage A — which the Stage C inventory had in
fact already told us (20 of 22 triples `Configured`). Of ShiftIdentity's 11 profiles, **10 are purely
replication maps**; only `User.cs` still carries triple maps. What looked like Stage E work was E3, F2 and F3
work wearing a Stage E label.

**E2 did not work the way the plan described.** The plan said the template needed no new code, just a call to
`ReplicateAllAsync` — but that helper lives in `ShiftIdentity.AzureFunctions`, which the API does not
reference and should not. Rather than drag an Azure Functions package into a web API, the 12 call sites keep
their shape and each gained its `ToXModel()` delegate. Better as template content anyway: this is the file
every new microservice is scaffolded from, so it now demonstrates passing a delegate instead of hiding the
question behind a helper.

**E3, on Q9 = take the break.** All 9 overloads require the delegate; the 4 fallbacks and the lazy
`FallbackMapper` are deleted. A delegate-free call site is now a compile error instead of a silent fall-through
to AutoMapper — which matters because every failure on that path is swallowed per row, surfacing as
permanently-dirty documents under a clean watermark rather than as an exception.

Consequence worth noting: with the fallbacks gone, **AutoMapper had no remaining use in
`ShiftEntity.CosmosDbReplication`**, so the usings and the `PackageReference` came out. That is Step F5's first
action arriving early as a by-product — and it takes AutoMapper 14's NU1903 advisory out of every consumer
build that references replication.

Also in E3: `Utility.BuildStamp` throws when a mapping produces a document with no `id`. It previously wrote a
stamp that could never address the document again, so change detection for that row broke permanently while
the upsert still succeeded and marked the row clean.

**Downstream impact, accepted deliberately:** 6 call sites in `ADP.ClaimableItems` and `ADP.WarrantyClaims` no
longer compile. Those teams pin the previous framework version until they port — a normal, reversible state,
where the alternative was a runtime throw inside a swallowed catch.

**2026-08-24** — **D4 reworked: version skew is now detected rather than declared.** The first cut stamped a
hand-maintained `abiVersion` into every generated mapper and compared it at startup. That was the wrong
instrument, for the reason any manual version number is: it fires on additive framework changes that break
nothing, and it stays silent exactly when it matters — the release where somebody changed a helper signature
and did not think to bump it. Step B8, which added a parameter to `MappingHelpers.ToForeignKey`, is precisely
that release.

Replaced with `VerifyBindings()`: JIT-prepare each registered mapper's methods, which resolves their call
targets, and report anything that no longer binds. Confirmed on a purpose-built probe — two versions of a
"framework" assembly, a consumer compiled against v1 carrying a one-argument call in its IL, a host loading
v2 — where `RuntimeHelpers.PrepareMethod` raises `MissingMethodException` at startup without invoking
anything, and the message names the missing method verbatim:

    Method not found: 'Int64 Fw.H.ToForeignKey(System.String)'

Nothing is emitted, nothing is versioned, and the check fires exactly when a call target is genuinely gone.
The error names the missing member and the package to rebuild, which the ABI number never could.

**2026-08-24** — **A11 shipped, and the customizations it made redundant are gone.** Case-insensitive member
matching landed with exact-first resolution, a `map.CaseSensitive()` opt-out and `SHENGEN011` for ambiguity.

That retires the last class of workaround from the convention sweep. Four `ForList` bridges existed ONLY
because matching was ordinal: `CompanyBranchListDTO.CompanyId`/`CityId`/`RegionId` ↔
`CompanyBranch.CompanyID`/`CityID`/`RegionID`, and `TeamListDTO.CompanyId` ↔ `Team.CompanyID`. All four are
removed. Regenerating and diffing shows the convention emits **character-for-character** what the deleted
lambdas produced — `CompanyId = e.CompanyID.HasValue ? e.CompanyID.Value.ToString() : null` — now matching
across the case difference, so there is no wire change. `SHENGEN007` is the backstop: had the convention not
covered them it would name them as unprojected columns, and it does not.

Worth stating because it nearly went the other way: these members are **LIST FILTER targets** (data-level
access, and the Team form's branch picker sending `$filter=CompanyId eq X`). If they stopped being projected,
EF would have nothing to bind the `Where` to and would inline the whole collection-bearing projection into the
predicate — untranslatable, and only at the moment a user filters. They still must be projected; the
convention is simply what projects them now. Anyone tempted to `IgnoreList` them should read that twice.

The earlier sweep recorded these as **KEEP** on the grounds that A11 was not implemented. That was true when
written and is not now — which is the hazard with "keep, because X is missing" notes generally: they are
correct until the day X lands, and nothing makes them re-read themselves.

**2026-08-24** — **Stage D complete.** The registry is now wired into repository resolution, and the mapping
layer validates at startup instead of at first request.

The change that matters is one line in `InitCommon`: the registry sits between DI and AutoMapper, gated on
`ShiftEntityOptions.MappingMode`. Before it, the registry was read by `UseGeneratedMapper()` and endpoint
discovery and by **nothing else** — so a generated mapper could exist, be correct, be registered, and the
repository would still use AutoMapper. The Stage C inventory had already found that live on
`Country / CountryDTO / CountryDTO`.

**Default stays `AutoMapperFirst`, so shipping Stage D changes nothing.** That is the whole safety property,
and it is pinned by a test asserting the registry is NOT consulted under the default — including for a host
that never configured `ShiftEntityOptions` at all, which must not start resolving generated mappers just
because the framework was upgraded underneath it.

Three things worth remembering:

1. **The activator is cached; the instance never is.** Generated mappers carry per-instance builder state that
   `AddConfiguration` mutates, so a shared singleton would leak one repository's customization into every
   other consumer of the same triple — presenting as intermittent mis-mapping, which is close to untraceable.
2. **D2's filter had to be deferred.** The mapping mode is only final after every
   `Configure<ShiftEntityOptions>` callback has run, and callers routinely configure options *after* calling
   `RegisterShiftRepositories`. So the "skip the AutoMapper default map for triples the registry covers" check
   lives inside the deferred AutoMapper factory, which runs when `IMapper` is first resolved.
3. **Conflicts are recorded, not thrown.** `ShiftEntityMapperRegistry.Register` runs inside a
   `[ModuleInitializer]`; throwing there surfaces as a `TypeInitializationException` with the real cause
   buried. D3 is what makes them readable.

One design note left deliberately: the registry is process-global with no reset, which production never needs
but tests must respect — each conflict test owns its own triple types, or they read each other's conflicts.

16 new tests. ShiftEntity.Tests 485/485, StockPlusPlus.Test 198/199 (1 skipped: the golden-capture tool).

**2026-08-23** — **Soft delete is now blocked on the write path, and allowed on the read path.** Two changes
that look opposed and are not.

*Repository (new, Step B11).* `ShiftRepository`'s upsert captures the stored `IsDeleted` before mapping and
restores it after, on **update** only. Deleting is a separate operation behind `Access.Delete`; an upsert needs
only `Access.Write`, so honouring the flag from a PUT body made the delete permission bypassable, and in the
other direction it was an undelete the framework has no API for. Insert is exempt — `AuditStamper` forces the
flag false there. Deliberately a repository policy, **not** a mapping rule: mappers keep mapping `IsDeleted`
like any other member, and the repository decides which of those writes it keeps. 4 tests in
`SoftDeleteOnUpdateTests`, including one asserting `DeleteAsync` still works.

*Reads (reverted).* A filter that dropped soft-deleted rows from mapped output was added and then **removed at
the owner's request**. Retiring a tag does not rewrite history: a tag already attached to an entity stays on it
and keeps being returned. Soft-deleting a tag only stops it being attached to anything NEW. Same for
deep-composed children. `TaggingTests.Product_DeletedTag_IsStillReturnedOnBothTheViewAndTheList` pins it.
**Step B10 is dropped, not deferred** — filtering deleted rows belongs to the repository and OData layer, and
AutoMapper never did it either, so this is also the parity behaviour.

**2026-08-23** — **Convention sweep: 4 customizations deleted, 2 comments corrected, 1 template arm collapsed.**
Now that the conventions cover more, workarounds written against the old gaps are dead weight — and worse, they
carry comments asserting limitations that no longer exist.

Removed, each proven by regenerating the mapper and diffing the emitted projection:

- `ShiftIdentity.Data/Repositories/UserRepository.cs` — `ForList` for `CompanyBranchID` and `CompanyID`.
- `ShiftIdentity.Data/Repositories/CompanyRepository.cs` — `ForList` for `ParentCompanyID`.
- `StockPlusPlus.Data/Repositories/ProductBrandRepository.cs` — a manual `Include(e => e.Tags)` the framework
  already adds for every `IShiftEntityTaggable` entity. With it gone both arms of the `#if (taggable)` split
  were identical, so the split collapsed too, and the item template's `taggable` description no longer promises
  an Include it does not emit.

The generated output for all three `ForList` removals is **character-for-character** what the deleted lambdas
produced (`X = e.X.HasValue ? e.X.Value.ToString() : null`), so there is no wire change. The build-time
backstop is `SHENGEN007`: had the convention not covered them, it would name them as unprojected.

**The rule that decides these is character-exact name equality, never the type shape.** `UserListDTO.CompanyID`
↔ `User.CompanyID` matches ordinally, so the convention handles it. `CompanyBranchListDTO.CompanyId` ↔
`CompanyBranch.CompanyID` differs by CASE, so its `ForList` is load-bearing and stays — along with `Team.cs`'s
`CompanyId`. Same type shape, opposite verdict. That stays true until Step A11 lands.

Two comments corrected because they asserted limitations the generator no longer has — exactly the rot this
sweep exists to remove: `InvoiceDTO.cs` claimed the list projection does not compose children automatically (it
does; `api/invoice-deep` builds three levels from a bare `UseGeneratedMapper()`) and referenced a
`ListLinesProjection` that does not exist; `StockPlusPlus.API/Program.cs`'s strategy index — the first thing a
newcomer reads — described Invoice as using a hand-written `InvoiceMapper` that does not exist.

Deliberately NOT removed, and worth knowing why: `CompanyRepository`'s `ForView(d => d.ParentCompany, …)` looks
like the same kind of leftover, but `((long?)null).ToString()` returns `""` while the convention's
`ToSelectDTO` returns `null` — a client-visible wire change on every root company, not a cleanup. Everything
else surveyed is genuine domain logic: M:N projections, flattening (declined by design), password-stripping,
hash-id encoding, and `IgnoreEntity` calls that are security policy rather than workarounds.

**2026-08-22** — **Q7 shipped: the generator now writes the audit and soft-delete columns.** Five members left
`EntityExcludedMembers` — `CreateDate`, `LastSaveDate`, `IsDeleted`, `CreatedByUserID`, `LastSavedByUserID` — so
the mapper maps them exactly as AutoMapper's unguarded `ReverseMap` always did. Per the decision: the mapper
maps, and deciding who may change a value is the repository's job or an explicit `IgnoreEntity`.

Three things worth keeping:

1. **`ID` was carved out, and it is not a matter of taste.** `EntityConvention` resolves DTO `string?` to entity
   `long` through `ToLong()`, which throws on the null every insert carries, and nothing in the framework catches
   `ShiftEntityMappingException` — so it would 500 every POST. Deep write is worse: pair `MapBack` maps into
   `new ChildEntity()`, so an existing child's key lands on a fresh row and SQL Server rejects the identity
   insert. That is strictly *less* robust than AutoMapper, which yields `0` here. Writing `ID` needs its own
   null-tolerant convention and an answer for deep children first. The most likely future mistake is someone
   "finishing the job" — `FrameworkAuditMemberWriteTests.TheKey_IsStillNotWrittenFromTheDto` fails the moment
   they do, before anything reaches a database.
2. **The repository now holds the soft-delete line**, which is where the decision said guards belong: on update
   the stored `IsDeleted` wins over the request body. Soft delete is gated on `Access.Delete` while an upsert
   needs only `Access.Write`, and there is no undelete API anywhere in the framework. This closed the same hole
   on the **AutoMapper** path, where it has been open in production all along — the generated mapper had been
   the *safe* one. Insert is exempt: `AuditStamper` forces `IsDeleted = false` there.
3. **`ViewHandledMembers` was deliberately NOT changed for symmetry.** Dropping `ID` from it while `ID` stays
   entity-excluded would fire SHENGEN008 on every generated mapper in every consuming repo, and `MapBaseFields`
   runs *after* the convention pass, so an existing `ForView` on an audit member would be silently clobbered.
   `CopyExcludedMembers` and the list-side filter are separate concerns and were left alone too. Coverage of the
   new writes comes from tests that run the mappers, not from a diagnostic that only fires on absence.

Also still pipeline-owned: `Tags` (owned by `TaggingPipeline` on both legs — writing it composes rows the
pipeline discards a line later, and `"tags": null` NREs on `Tags.Clear()`), plus `ReloadAfterSave`,
`AuditFieldsAreSet` and `IdempotencyKey`, none of which any DTO in the three repos declares.

Tests: 9 added (`FrameworkAuditMemberWriteTests` ×6, `SoftDeleteGuardTests` ×3); 2 source-text pins inverted
(`FrameworkMemberGatingTests`, `GeneratedDeepWriteTests`). ShiftEntity.Tests 462/462, StockPlusPlus.Test 175/175.

Logged, not fixed: `IsProtected` (`IShiftEntityProtectable`) has never been in the exclusion set, so it is
already convention-writable wherever a DTO exposes it. Separate gap.

**2026-08-22** — **Rescoped to the framework only.** ADP (`Surveys`, `WarrantyClaims`, `ClaimableItems`,
`Menus`, `SyncAgent`) and `Menu` removed as work; they remain in the gap register as *(downstream)* evidence.
Three consequences worth remembering:

1. Framework-owned mapping is 14 profiles / 435 lines with **zero `AfterMap` blocks** — all 16 of the hard
   collection-reconciliation blocks were downstream. Stage E went from five services to two (the sample, then
   ShiftIdentity).
2. Steps A5–A8 (already shipped) serve **no in-scope code**. They stay because only the framework can build
   them and no consumer can migrate without them.
3. Two new decisions fall out of the narrowing — **Q9** (ship the required-delegate compile break at 6
   consumer call sites we do not own?) and **Q10** (what is the shipped default, and does the AutoMapper
   fallback get deleted or *moved* into the compat package?). F1 stopped being an end-of-plan courtesy and
   became the deliverable the whole scope rests on.

**Also 2026-08-22** — **Step A11 added** (gap A-13): member matching is case-sensitive ordinal where
AutoMapper's was not. Proof it is a regression, not a limitation: the `CompanyBranch` profile deleted at
migration mapped `CompanyBranchListDTO.CompanyId`/`CityId`/`RegionId` from entity `CompanyID`/`CityID`/`RegionID`
with **no `ForMember`** — AutoMapper matched across case *and* converted `long?→string`. All three silently
stopped projecting on the flip; the repository now carries three hand-written `ForList` lines. Split out of
gap A-7, which had bundled it with "no flattening" under one disposition ("A5, message only") and so hid a
cheap fix behind a deliberate decline. Flattening stays declined. Two corrections found while confirming it:
there are **five** name-keyed dictionaries, not two, and the FK convention's hardcoded `"ID"` suffix is the
same defect. Implementation trap recorded in the step: ~20 emission sites interpolate the *lookup* name, so
relaxing the comparer without switching them to the matched symbol's name emits code that does not compile.

A sibling gap found in the same review — collection-kind mismatch silently dropped on write
(`IReadOnlyCollection<T>` DTO ↔ `List<T>` entity, live in `CompanyBranch.PublishTargets` and `Team.Tags`) —
turned out to be **already fixed** by Stage A's container-conversion work, and the per-member `ForEntity`
workarounds have been deleted. No step was added for it.

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

**2026-08-22 (later)** — **A11 done. Matching is case-insensitive by default, with an opt-out.**

The parity regression is closed: entity `CompanyID` (`long?`) now feeds DTO `CompanyId` (`string?`) with no
configuration, in all three directions. **469 tests pass** (6 new); ShiftEntity, ShiftIdentity.Data and
StockPlusPlus.Data all build with 0 errors and no new SHENGEN011 anywhere in either tree.

**The option** is `map.CaseSensitive()`, and only that:

| | spelling |
|---|---|
| attribute | *(none)* — see the note below |
| fluent | `map.CaseSensitive()` |
| default | `ShiftEntityMapperDefaults.CaseSensitiveMatching = false` |


**Fluent only.** An attribute (`[ShiftEntityMapperCaseSensitive]`, readable from the repository, the `[ShiftEntityMapper]` partial, the entity or the assembly) was built alongside it and then removed at the owner's request. One spelling beats four: easier to find, easier to reason about, and it keeps the setting with the rest of the per-mapper configuration. The default stays insensitive, so the opt-out is the only thing anyone has to write.

The setting propagates to CHILD mappers: a pair reached from a case-sensitive triple is generated case-sensitive too, rather than quietly relaxing the rule one level down. Pinned by `CaseSensitiveOptOut_ReachesChildMappers`.

**Resolution order**, as specified: exact ordinal first — always, and never beaten by a looser candidate — then
`OrdinalIgnoreCase`; two or more candidates with no exact match means the member is SKIPPED and `SHENGEN011`
names every candidate, as a warning, and the build succeeds. Exact-first is what makes the fallback safe: a
type carrying both `Id` and `ID` still binds each to its own exactly-named member, so only a member with no
exact counterpart can reach the ambiguous branch at all. A successful case-insensitive match is silent —
AutoMapper resolved these without comment, and warning on each would bury the real findings.

**Implementation notes worth keeping:**

- The five name-keyed lookups became one `MemberLookup`, built by GROUPING rather than `ToDictionary`. The old
  call throws on a duplicate key, which becomes reachable the moment names are compared without case, and an
  exception inside a source generator surfaces as a build failure with no usable message.
- The setting rides on `MapperDirectives` alongside `MaxDepth`. Note `MapperDirectives.Empty` is a single
  shared instance — a fresh one is created for a triple with no configuration, because writing the flag onto
  the shared one would leak the setting to every other triple.
- The FK convention is the same defect in another costume: its `EndsWith("ID")` test and its `Name + "ID"`
  lookups are now case-insensitive too, so an entity spelling it `CompanyId` gets the FK convention as well.
- **The trap the step predicted is real and cost two rounds.** Roughly twenty emission sites interpolated the
  REQUESTED name, which is correct only while the two spellings are identical. Missing two of them (the list
  scalar tail, and the general-conversion fallback) emitted `e.CompanyId` for an entity that spells it
  `CompanyID` — code that does not compile. Every source-side emission now reads the matched
  `IPropertySymbol.Name`; the target side keeps the DTO's. Only a behavioural test over a deliberately
  case-mismatched pair catches this, which is why all four required behaviours are pinned that way.

**ShiftIdentity — the three `ForList` scope-id lines are gone.** `CompanyBranchRepository` carried
`ForList(d => d.CompanyId, …)`, `CityId` and `RegionId` with a paragraph explaining that "BOTH the case AND the
type differ". Both halves are handled now — case by A11, `long? -> string` by the earlier conversion work — and
the generated projection is byte-for-byte what the hand-written lines produced
(`CompanyId = e.CompanyID.HasValue ? e.CompanyID.Value.ToString() : null`). The comment's *rationale* was kept:
those columns are LIST filter targets, and without a scalar to bind to EF inlines the collection-bearing
projection into the WHERE and cannot translate it. Anyone tempted to `IgnoreList` them should read it first.

**Recovery note.** Mid-step, an over-wide text replacement deleted ~370 lines of the generator (the conversion
engine, `TryGetElement`, `IsPairable`, `BuildViewBody`). It was recovered from the `Stage B complete` commit and
the A11 work re-applied on top; the full suite confirms nothing was lost. The lesson is narrow but real: slicing
a file between two anchors deletes whatever else happens to live between them.

**Mirror note (2026-08-22).** The `.shift` copy had drifted well behind this one and was re-synced from it.
The two passages that used to be deliberately fuller in the private mirror — Q7 and gap C-3 — no longer are:
Q7 is answered ("the mapper writes all properties; guard in the repository or with an explicit Ignore") and the
`IsDeleted` case is resolved by a repository policy, so there is no unshipped narrowing left to keep private.
The copies are now identical apart from the banner naming which one is primary. **This copy is primary.**
