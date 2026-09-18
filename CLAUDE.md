# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What This Repository Is

ShiftTemplates is a dual-purpose repository: it contains a **dotnet new** project template (`shift`) and item template (`shiftentity`), and the template content itself doubles as a **sample project** (StockPlusPlus) used during Shift Framework development.

There are two solution files serving different purposes:
- **`ShiftTemplates.sln`** — for template packaging and the Builder tool
- **`content/Framework Project/StockPlusPlus.sln`** — the sample project / framework development

## Build & Run

```bash
# Build entire solution (from repo root)
dotnet build

# Build sample project
dotnet build "content/Framework Project/StockPlusPlus.sln"

# Run the API
dotnet run --project "content/Framework Project/StockPlusPlus.API"

# Apply EF Core migrations (from Package Manager Console or CLI)
dotnet ef database update --project "content/Framework Project/StockPlusPlus.Data" --startup-project "content/Framework Project/StockPlusPlus.API"
```

## Tests

```bash
# Run sample project tests (xUnit 3)
dotnet test "content/Framework Project/StockPlusPlus.Test"

# Run Blazor component tests (bunit)
dotnet test "content/Framework Project/StockPlusPlus.Web.Tests"
```

## Template Builder

The `ShiftTemplates.Builder` console app automates template development workflow:

```bash
# Full workflow: update versions, pack, install template, create test project
dotnet run --project ShiftTemplates.Builder

# Individual steps
dotnet run --project ShiftTemplates.Builder -- --update-template-versions --skip-template-install --skip-project
dotnet run --project ShiftTemplates.Builder -- --skip-project
```

The Builder: (1) reads `ShiftFrameworkGlobalSettings.props` and updates template.json with current versions, (2) packs and installs the template via `dotnet new install`, (3) creates a test project using `dotnet new shift`.

## Architecture

### Development Mode vs Package Mode

Controlled by `ShiftFrameworkGlobalSettings.props` at the repo root. When `shiftFrameworkDevelopmentMode` is defined and sibling framework repos are cloned with the correct folder structure, projects use **local project references** instead of NuGet packages. This file is excluded from the template output.

Required sibling repos for development mode: `ShiftEntity`, `ShiftBlazor`, `ShiftIdentity`, `TypeAuth`, `AzureFunctionsAspNetCoreAuthorization`, `ShiftFrameworkTestingTools`, `ShiftFrameworkLocalization`, `ShiftMapper`.

`ShiftMapper` (https://github.com/ShiftSoftware/ShiftMapper) is referenced by `StockPlusPlus.Data` — the runtime library as a normal project reference and `ShiftMapper.Generator` as an analyzer (the source-generator half of the single `ShiftSoftware.ShiftMapper` package; the package also carries `ShiftMapper.CodeFixes`, which is IDE-only and is not wired in dev mode, matching ShiftMapper's own sample) — and, since 2026-09-15, by two framework projects: `ShiftEntity.CosmosDbReplication` (library only — it is the mapping fallback for Cosmos replication, see below) and `ShiftIdentity.Data` (library + generator, which carries the identity replication maps as a profile). Every sibling that references the runtime library uses the TypeAuth pattern: `ProjectReference` when the checkout exists, `PackageReference` at `$(ShiftMapperVersion)` otherwise. ShiftMapper pins `Microsoft.Extensions.DependencyInjection.Abstractions` 10.0.11, so any project that references that package directly AND reaches ShiftMapper must sit at 10.0.11 or restore fails with NU1605. **The pipeline clones it anonymously like every other sibling, so the GitHub repository must be public before a `release*` tag is built** — the clone is unconditional because the sample build needs the checkout (the package is only ever published from this pipeline). And because the framework packages now depend on it, the first release after this must be `release-all` (or `release-shiftmapper` first), never a lone `release-framework`.

**Cosmos replication maps through ShiftMapper.** The mapping delegate on `Replicate` / `UpdateReference` / `UpdatePropertyReference` (both pipelines in `ShiftEntity.CosmosDbReplication`) is optional; a call site that omits it maps through the host's registered `IMapper` (ShiftMapper's run-time door — renamed from `IShiftMapper` on 2026-09-17), resolved by `ReplicationMapper.cs` ONCE per registered action and ABOVE the row loop, with the pair checked via `CanMap` — a missing mapper or pair throws out of the run rather than surfacing as permanently-dirty rows under a clean watermark (which is how the old AutoMapper fallback failed; never move the resolution inside the per-row catch). **ShiftMapper's model (since its 2026-09-17 commits `125abd4` + `1514014`):** a mapper class (`ShiftMapperBase`) is ONLY a place to write `CreateMap`s — nothing is generated onto it, it need not be `partial`, nothing injects it. The generator writes ONE generated class per assembly holding every map from every mapper class in the project AND every mapper class the referenced packages declare; the typed methods (`mapper.Map<BrandDto>(x)`, `MapToBrandDto`, `ProjectTo`) are extension methods on `Mapper`, the one object application code injects; `IMapper` (5 members, `CanMap` included) is the door for library code that cannot name the host's types, and `Mapper` implements it explicitly. `services.AddShiftMapper()` registers the CALLING ASSEMBLY's generated mapper (nothing is named) plus `Mapper`/`IMapper` over every generated mapper any call registered, dispatching each pair to the first that declares it, the application's own first; a repeat call from the same assembly is a no-op in the registry. `o.AddMapper<T>()` only matters under `MapperDiscovery.LocalAndRegistered`/`Registered` and is SM0046 under the default `All`; `CompositeShiftMapper` and public `IncludeMapper` are gone. ShiftIdentity's 19 replication pairs are declared in `ShiftIdentity.Data/Replication/ShiftIdentityReplicationMapper.cs` (a plain `ShiftMapperBase`; rules would go in a `ShiftMapperConversions` pack, of which identity has none); `SetUpAllIdentityReplications` (Dashboard.AspNetCore), `ReplicateAllAsync` (AzureFunctions) and the template's `UtilityController.ReplicateAll` pass no delegates. **The mapper is registered by the identity registrations themselves** — `AddShiftIdentityDashboard<TDbContext>()` (API side) and the Functions worker's `AddShiftIdentity(issuer, key)` (catch-up side) both call `AddShiftIdentityReplicationMapper()` — so the template hosts carry no line for it; it cannot ride on `SetUpAllIdentityReplications` because the trigger's options callback runs per scope over an already-built container. That call is just `services.AddShiftMapper(o => o.Lifetime = lifetime)` made FROM ShiftIdentity.Data, so it registers that assembly's generated mapper; keep it an inline lambda, not the generic short form, because `Assembly.GetCallingAssembly()` is not inlining-safe across the identity registrations. Its idempotency is ShiftMapper's own (no guard of its own any more). A host with a generator of its own writes NOTHING to get the pairs: they are in its generated mapper because it references the package, and `Mapper` answers from the host's first; re-declaring one of the 19 pairs in a second mapper class is refused (SM0042). A host that wires identity replication without either identity registration calls `AddShiftIdentityReplicationMapper()` itself. `StockPlusPlus.Data` carries a generated mapper of its own for the same reason (it references ShiftIdentity.Data with the generator attached) but never registers it — the sample makes no `AddShiftMapper` call — so at run time the door is ShiftIdentity.Data's. The documents are pinned by the 24 frozen goldens in `StockPlusPlus.Test/Tests/ReplicationMappingParityTests.cs` (mapping through `Mapper.Create(typeof(ShiftIdentityReplicationMapper).Assembly)` held as `IMapper` — the package's own generated mapper through the run-time door) plus `IdentityReplicationMapperRegistrationTests.cs` (which asserts the container holds the generated type, one `Mapper` and one `IMapper`, never the mapper class, and cross-checks the pairs against the `ShiftMapperDeclaredMap` metadata by `DeclaredBy`); the mapper's transcription rules (ignore `ID` on every map, ternaries for navigation reads, `"0"` for a key behind a null navigation, `BranchID` ignored on the merge maps) are documented in the mapper itself. The repository triple (`IShiftEntityMapper`, the ShiftEntity generator) does NOT use ShiftMapper — do not widen its use there without being asked.

### Sample Project Structure (StockPlusPlus)

- **API** — ASP.NET Core Web API (.NET 10). Controllers, DbContext, identity integration
- **Data** — EF Core entities, repositories, migrations. Uses ShiftEntity.EFCore patterns
- **Shared** — DTOs, enums, TypeAuth action trees (permission definitions)
- **Web** — Blazor WebAssembly frontend using ShiftBlazor components
- **Functions** — Azure Functions (optional in template). Timer triggers, HTTP functions
- **Test** — xUnit integration tests
- **Web.Tests** — bunit Blazor component tests

### Template System

Template config lives in `content/Framework Project/.template.config/template.json`:
- **sourceName**: `StockPlusPlus` — replaced with user's project name during `dotnet new shift`
- Key parameters: `includeSampleApp` (bool), `shiftIdentityHostingType` (Internal/External), `addFunctions` (bool), `addTest` (bool)
- Conditional compilation via `#if` directives tied to template symbols (e.g., `includeSampleApp`, `internalShiftIdentityHosting`)
- The item template at `content/ShiftEntity/` scaffolds a full entity (DTO, entity class, repository, controller, Blazor page)

### Framework Version Management

`ShiftFrameworkGlobalSettings.props` defines `ShiftFrameworkVersion`, `TypeAuthVersion`, `AzureFunctionsAspNetCoreAuthorizationVersion` and `ShiftMapperVersion`. The Builder's `UpdateTemplateVersions` reads these and writes them into `template.json` so newly created projects get correct package versions. The last three are standalone: each sibling repo imports this props file — from the `ShiftTemplates` folder and from `s`, the pipeline's checkout directory, with the path relative to the importing file (`..\..\` from a csproj one level below the repo root, as in TypeAuth; `..\` from `ShiftMapper`'s repo-root `Directory.Build.props`) — and sets its `<Version>` from its own entry, so those packages move independently of the framework version. `ShiftMapper` keeps a fallback number in that props file for standalone clones; the published number is always the one here.

## CI/CD

Azure DevOps pipeline (`azure-pipeline.yml`) triggers on `release*` tags. It clones all framework sibling repos, builds everything, runs tests, then packs and publishes NuGet packages. **Test projects come in two tiers.** A project the pipeline runs by default finishes in seconds and needs nothing outside the process: no SQL Server, no Cosmos emulator, no real-clock waits. Anything slower lives in a separate `*.LongRunning` test project, or is a whole project of that kind such as `ShiftMapper.Generator.Tests`, and runs on the pipeline only when the tag name contains `with-long-running-tests`, for example `release-framework-with-long-running-tests` or `release-all-with-long-running-tests`; locally, run such a project on purpose from Test Explorer. On `release-all` and `release-framework` tags only, before anything is packed, `.pipelines/identity-checks.steps.yml` builds the fast identity test projects once in Release and runs two gates with `--no-build`: `identity-policy` (all of `ShiftIdentity.Tests`: database-free policy and token tests, plus the shared fixtures that `ShiftIdentity.Tests.LongRunning`, `StockPlusPlus.Test` and the sample API's development hosts build on; no SQL connection is passed, so a SQL-backed test that lands there fails) and `identity-ui` (all of `ShiftIdentity.Blazor.Tests`, bunit). The other tags publish no identity package and skip the whole block at template-expansion time. With the long-running tag the template also starts a disposable SQL Server container and runs `identity-long-running` (all of `ShiftIdentity.Tests.LongRunning`: every SQL-backed identity test and the real-clock response-floor evidence, about six minutes on a hosted agent) and `identity-consumer-contract` (`Category=IdentityConsumer` in `StockPlusPlus.Test`, SQL-backed). The `Category` traits (`Policy`, `Sql`, `Http`, `Ui`) stay on the identity classes as descriptions; only `IdentityConsumer` is still used as a filter. The ShiftMapper generator suite (484 Roslyn compilations) is likewise run only with the long-running tag; ShiftMapper's own GitHub workflow runs it on every push. Tag naming controls what gets published: `release-all`, `release-framework`, `release-typeauth`, `release-aspNetCore-authorization`, `release-shiftmapper`. `release-all` publishes every group; the specific tags publish only their own. The ShiftMapper group runs `ShiftMapper.Tests` (and `ShiftMapper.Generator.Tests` only with the long-running tag), packs `ShiftSoftware.ShiftMapper` (one package carrying the library and the generator) into its own staging folder, verifies both halves are inside the `.nupkg`, and pushes with `-SkipDuplicate` so a `release-all` after a `release-shiftmapper` of the same version does not fail.

## Active Work: Mapping Abstraction

`ShiftRepository` maps through `IShiftEntityMapper<TEntity, TListDTO, TViewDTO>`. This replaced AutoMapper, which has now been removed from the framework entirely (ShiftEntity, ShiftIdentity and this repo). It survives only in consumer repos that have not migrated — ADP, Menu and ADP.SyncAgent — so say "removed from the framework", not "gone".

**Planning doc with full context, status, and iteration tracking:** `.shift/repos/shift-entity/mapping-abstraction-plan.md` (in the `.shift` repo).

**IMPORTANT:** When making any mapping-related changes across ShiftEntity, ShiftTemplates, or ShiftIdentity, always update the planning doc to reflect what was done. This document is the single source of truth for the team — check it before starting work to see current status, and update it after completing work. Do not rely on memory or conversation context alone.

### AutoMapper removal — plan lives in this repo

The removal of AutoMapper (completed 2026-08-25 — an explicit or generated mapper is now required, with no fallback) is tracked in its own plan, and that plan lives **here, in this repo**: [`docs/plans/automapper-removal/`](docs/plans/automapper-removal/). Start at [`STATUS.md`](docs/plans/automapper-removal/STATUS.md) for what has landed and what is next.

**Depend on the in-repo plan first.** A mirror is kept in `.shift/repos/shift-entity/automapper-removal/` for cross-repo visibility, but this copy is the one to read and work from.

### Next: repository mapping generated by ShiftMapper — plan lives in this repo

**Proposed 2026-09-18, nothing built yet.** The next mapping step is to retire `ShiftEntity.SourceGenerator` and have ShiftMapper declare and generate the four repository maps (nested children included) automatically from `ShiftRepository<,,,>` type arguments and the endpoint attributes — no mapping attributes for the programmer (the three `ShiftEntityMapper*` attributes and `UseGeneratedMapper = true` are deleted), configuration either in the repository as `o.Mapping(m => m.List.ForMember(...))` in ShiftMapper's vocabulary or in an ordinary `ShiftMapperBase` class that overrides the repository for the pair it declares, and the maps usable anywhere through `Mapper`/`IMapper`. Plan: [`docs/plans/repository-mapping-on-shiftmapper/`](docs/plans/repository-mapping-on-shiftmapper/) — start at `README.md`, track in `STATUS.md`. **Stage 0 is done (2026-09-18):** `StockPlusPlus.Test/Tests/RepositoryMappingParityTests.cs` pins all 23 triples' four directions against frozen goldens in `Tests/Parity/RepositoryMapping/Goldens/` captured from the old generator — never regenerate them casually (`SHIFT_TEST_CAPTURE_REPOSITORY_MAPPING_GOLDENS=1` is the switch, and after Stage 3 it captures the implementation under test); the inventory and SHENGEN baseline are in `05-inventory.md`. It has no `.shift` mirror; the `.shift` mapping plan only points here. Until its Stage 3 lands, everything in "Active Work: Mapping Abstraction" still describes the code as it is.

**When any AutoMapper-removal work lands — in this repo or in ShiftEntity, ShiftIdentity or ADP — update [`docs/plans/automapper-removal/STATUS.md`](docs/plans/automapper-removal/STATUS.md) here, and mirror the same edit into the `.shift` copy.** Keep both in step; never update only one.

Two passages (Q7 and gap C-3) are deliberately fuller in the `.shift` mirror than in this public copy: they carry the reproduction detail behind gap C-3. **That narrowing shipped on 2026-08-25 with Stage F2**, so "keep it out until it does" no longer applies as written — consumers upgrade on their own schedule, so the reproduction stays private regardless. The deliberate exception is exactly two files, `00-gap-register.md` and `02-open-decisions.md`: never sync those between the two locations. `STATUS.md` and everything else in the folder are kept **byte-identical**.

Sample mapping strategies (one per entity — the repository picks via `ShiftRepositoryOptions`). A repository that configures nothing resolves in this order, with no further fallback: a mapper set on the options (`UseMapper`/`UseGeneratedMapper`) → an `IShiftEntityMapper<E,L,V>` registered in DI → the source-generated mapper from `ShiftEntityMapperRegistry` → nothing, and then the mapping methods throw. A triple nothing covers is a **startup** error (`ShiftEntityMapperValidation`, called unconditionally), not a request-time surprise. See the planning doc for the full inventory and rationale:
- **Product** — overrides `MapToView`/`MapToEntity`/`MapToList` in `Repositories/ProductRepository.cs`.
- **Invoice** — SOURCE-GENERATED with DEEP child mapping: `Repositories/InvoiceRepository.cs` calls `UseGeneratedMapper(map => map.ForEntityChildren(x => x.InvoiceLines, d => d.InvoiceLines))`; `MapToView` auto-composes `InvoiceLines` via the generated pair mapper.
- **ProductCategory** — SOURCE-GENERATED (auto): `UseGeneratedMapper()`; covers the FK↔`ShiftEntitySelectDTO` relationship + `List<ShiftFileDTO>`↔JSON file conventions.
- **ProductBrand** — `[ShiftEntityMapper]` partial class (`Mappers/ProductBrandMapper.cs`) filled by the generator, with a `Configure` hook customizing one property; plugged via `UseMapper(new ProductBrandMapper())`.
- **Country** — zero-code SOURCE-GENERATED: `Repositories/CountryRepository.cs` (`UseGeneratedMapper()`) and the `api/country-generated` endpoint (`UseGeneratedMapper = true`). `api/countrymapped` uses the hand-written `Mappers/CountryMapper.cs` (manual `IShiftEntityMapper`).
- Source generator lives in the sibling `ShiftEntity.SourceGenerator` project (ships as an analyzer inside the `ShiftSoftware.ShiftEntity` package).
- Tests: `Tests/ManualMappingTests.cs` (Product/ProductCategory/Invoice end-to-end), `Tests/SourceGeneratedMappingTests.cs` (which also holds the `AutoDiscovered*` and `MapperCustomizationTests` classes), `Tests/DeepMappingTests.cs`, `Tests/DeepListMappingTests.cs`; `Tests/MappingPOC/` holds POC comparison files (Manual/Mapperly/Mapster, kept only as a one-off comparison — none of them is a supported framework mapping strategy).

`[ShiftEntityMapperMaxDepth(n)]` caps automatic deep composition; `[ShiftEntityMapperIgnore]` drops a member.

**Conversions are convention-based and symmetric** — whatever the read direction converts, the write direction converts back. Scalars: every numeric type, `bool`, `DateTime`, `DateTimeOffset`, `TimeSpan`, `DateOnly`, `TimeOnly`, `Guid` and enums ↔ `string`; enums ↔ numbers; numeric ↔ numeric. Text is parsed and formatted with the **invariant** culture (a DTO value crosses machines and locales), and malformed input throws `ShiftEntityMappingException` naming the member rather than writing a default. Collections convert element type and container type independently — `List`/`ICollection`/`IEnumerable`/`IReadOnly*`/`HashSet`/arrays in any pairing, for simple and complex elements alike. The LIST direction is deliberately restricted to what EF can turn into SQL (casts and `ToString()`, never a helper call); text parsing is reported by `SHENGEN007` instead, so a projection never fails at query time.

**Soft delete is NOT the mapper's concern.** It is enforced by the repository and the OData layer — `ApplyDefaultSoftDeleteFilter` in `ShiftEntity.Web/Extensions/IQueryableExtensions.cs` and `ShiftEntityCrudHandler`. Both cover ROOT rows only, so children composed deep inside a payload are not filtered by anything — and that is the INTENDED behaviour, not a gap: Step B10 of the removal plan was DROPPED on 2026-08-23 for exactly that reason, and `Tests/TaggingTests.cs::Product_DeletedTag_IsStillReturnedOnBothTheViewAndTheList` pins it so it does not come back. If root-level filtering ever needs extending, it belongs in the repository/EF layer, not in generated mapping code.

Attribute-driven endpoints can also take a mapper (iteration §20 in the planning doc). Alongside `[ShiftEntityEndpoint<…>]` / `[ShiftEntityEndpoint<…, TRepository>]`, there are `[ShiftEntityEndpointWithMapper<…, TMapper>]` / `[ShiftEntitySecureEndpointWithMapper<…, TActionTree, TMapper>]` where `TMapper : class, IShiftEntityMapper` (a non-generic marker; the exact `(entity,list,view)` triple is validated at discovery). A mapper keeps the built-in repository but replaces the generated mapping. Sample: `StockPlusPlus.Data/Entities/Country.cs` exposes `api/country` (source-generated) and `api/countrymapped` (`Mappers/CountryMapper.cs` + the distinct `Shared/DTOs/CountryMappedDTO.cs` — a distinct DTO is REQUIRED because the mapper is keyed by DTO type). UI to test it: `StockPlusPlus.Web/Pages/Country/CountryMappedList.razor` + `CountryMappedForm.razor` (EntitySet/Endpoint `CountryMapped`), linked from `Shared/NavMenu.razor` as "Countries (Mapped)". Tests: `Tests/AttributeEndpointMapperDiscoveryTests.cs` (DB-independent) + `Tests/AttributeEndpointTests.cs`.

Key files in ShiftEntity (sibling repo):
- `ShiftEntity.Core/IShiftEntityMapper.cs` — the interface (4 methods: MapToView, MapToEntity, MapToList, CopyEntity)
- `ShiftEntity.Core/MappingHelpers.cs` — helpers to reduce manual mapping boilerplate (audit fields, FK ↔ ShiftEntitySelectDTO, ShallowCopyTo)
- `ShiftEntity.EFCore/ShiftRepository.cs` — unified on a single mapper field (`innerMapper`, resolved in `InitCommon`), ReloadAfterSave handled inline

### Build-time baked mapping (2026-07-14)

The source-generated mappers now decide custom-vs-convention-vs-ignore at BUILD time — no per-property runtime branch. The generator statically SEARCHES the fluent config (a mapper's `Configure`, a repo's `UseGeneratedMapper(map=>…)`, nested `configureChild`) keyed by the `ShiftMapperBuilder<E,L,V>` type args, and per member emits either a reference to the runtime delegate (`InvokeView/InvokeEntity/InvokeCopy` — value stays runtime, only the decision is baked), the convention directly, or nothing (Ignore). New capabilities across all four methods:
- **Ignore** — fluent `map.Ignore(…)`/`IgnoreView/Entity/List/Copy(…)` or `[ShiftEntityMapperIgnore]` on any side; the member is OMITTED from generated code (not a runtime skip), complex subtrees pruned.
- **Automatic deep mapping** — child objects/collections compose automatically up to a **max depth** (default 10; per-repo via `[ShiftEntityMapperMaxDepth(n)]` or `map.MaxDepth(n)`). Explicit `ForXxxChild(ren)` still composes beyond the cap (and, when present, makes auto-deep step aside for that member). Done for **View** (compose child DTO), **Entity** (replace-with-new), and **List** (inline correlated SQL member-init, grandchildren included). All three directions compose the **same** set of members — a child the view reads is a child the entity writes — and "child" means any pairable class, not just a `ShiftEntity` navigation (plain JSON-column POCOs and owned types included). That symmetry was broken until 2026-08-06: the entity side only composed `ShiftEntity`-derived navigations, so JSON-owned grandchildren read back fine and were silently emptied on save (see `.shift/repos/shift-entity/mapping-abstraction-plan.md` §23). Pinned by `ShiftEntity.Tests/Mapping/GeneratedDeepWriteTests.cs`.
- Runtime-only `AddConfiguration` on an already-generated mapper is no longer baked (generator runs on production assemblies only, not the test assembly) — express customization statically. **Conditional registration is a build error** (`SHENGEN005`): a config call inside an `if/switch/loop/?:/&&/||/??` fails the build — register unconditionally and put the condition inside the value delegate. (There is deliberately NO runtime opt-out attribute.)

New framework files: `ShiftEntity.Core/ShiftEntityMapperConfigAttributes.cs` (MaxDepth/Ignore attrs), `ShiftEntity.Core/ShiftMapperBuilder.cs` (Ignore/MaxDepth/Invoke*). The baked features are currently verified manually on Invoice + by generated-output inspection (a dedicated DB-free demo triple was tried then removed — the existing Invoice mapper can't host these tests since it's an auto mapper with explicitly-configured children). Deep composition deliberately stops short of `CopyEntity`: that one is a shallow, same-type entity→entity copy, so there is no `ForCopyChild(ren)` — per-member copy customization is `ForCopy`/`IgnoreCopy`. `SHENGEN007` has since shipped (Stage A, 2026-08-21).

### Generator diagnostics (SHENGEN00x)

All live in `ShiftEntity.SourceGenerator/ShiftEntityMapperGenerator.cs`. **Errors** — 001 (mapper class not partial), 002 (no mapper interface), 005 (conditional mapper configuration), 006 (entity repository configuration suppressed), 009 (mapper configuration cannot be baked). **Warnings** — 003 (deep-mapping cycle, member skipped), 004 (unmapped view members), 007 (unmapped list members), 008 (view members never written back), 010 (deep write replaces tracked child rows), 011 (ambiguous case-insensitive member match, member skipped). The split is deliberate: errors mark something that can't be expressed at build time or would run silently wrong; warnings mark something merely skipped.

007/008/009/010 landed with Stage A of the AutoMapper removal (2026-08-21) — see [`docs/plans/automapper-removal/STATUS.md`](docs/plans/automapper-removal/STATUS.md). 007 prints a paste-ready `map.ForList(...)` line, filling in the flattened path when the member name resolves to one; deliberate flattening is NOT implemented, and the synthesized line is what makes that affordable. 008 reports the read/write ASYMMETRY (DTO members `MapToView` reads and `MapToEntity` never writes) rather than mirroring 004, which would warn on every internal column. 009 pairs with `ShiftMapperBuilder.VerifyBaked`, which throws at first use for configuration applied from another assembly — the case no compilation-local analysis can see. 010 fires when automatic deep write would replace tracked child rows that carry a required FK back to the parent; `map.AfterEntity(...)` and the `existing`-aware `map.ForEntity(...)` overload are the escape hatches.

Generator tests: `ShiftEntity.Tests/Mapping/`. `MapperGeneratorHarness.cs` compiles a scaffold with the generator, loads the assembly, and runs the generated mappers as real objects — assert on the resulting object, not on emitted source text.

**SHENGEN006** (error as of 2026-07-15) fires when an entity declares `IConfiguresShiftRepository<E, L, V>` while a repository for the SAME triple passes an options builder to its base constructor — the builder means the repository configures itself and takes over, so the entity's `ConfigureRepository` never runs and nothing fails at runtime. Fix: move the config into the repository's builder, or drop the builder. Detection (`PassesOptionsBuilder`) only fires on a certainty — it requires a DIRECT `ShiftRepository<,,,>` base and needs EVERY constructor reaching base to pass a non-null builder, so an intermediate base class or a single builder-less constructor stays silent rather than break a build on a guess. There is no opt-out attribute.

Tests: `ShiftEntity.Tests/Mapping/GeneratorDiagnosticTests.cs` — drives the generator in-process over a hand-built compilation (the generator is a plain ProjectReference in that project, deliberately NOT an analyzer). The "silent" cases are as much the contract as the firing one.

When working on mapping-related changes, always check the planning doc for current iteration status before starting.

## Tagging

Cross-cutting tag system. Opt-in per entity via `IShiftEntityTaggable` / `IShiftEntityTaggableDTO`. Per-microservice vocabulary — each service owns its `Tag` table, its action-tree node, its endpoints.

**Planning doc:** `.shift/repos/shift-entity/tagging-plan.md` — full status, decisions, file inventory, programmer cheat sheet.

Key files in this repo:
- `content/Framework Project/StockPlusPlus.Shared/ActionTrees/StockPlusPlusActionTree.cs` — `Tags = new("Tags")` ReadWriteDeleteAction node
- `content/Framework Project/StockPlusPlus.Data/Entities/Product.cs` — sample taggable entity (`IShiftEntityTaggable`)
- `content/Framework Project/StockPlusPlus.Shared/DTOs/Product/ProductDTO.cs` — sample taggable view DTO (`IShiftEntityTaggableDTO`); `StockPlusPlus.Web/Pages/Product/ProductForm.razor` places just `<ShiftTagPicker @bind-Value="TheItem.Tags" />` — add(+) and view-on-double-click work by default (picker defaults QuickAdd to the framework `ShiftTagForm` component)
- `content/Framework Project/StockPlusPlus.Shared/DTOs/Product/ProductListDTO.cs` — sample taggable list DTO (`IShiftEntityTaggableDTO` + `Tags` projected in `MapToList`); `StockPlusPlus.Web/Pages/Product/ProductList.razor` places `<ShiftTagColumn />` to show them
- `content/Framework Project/StockPlusPlus.Data/Mappers/` (`CountryMapper.cs`, `ProductBrandMapper.cs`) and Product's mapping overrides in `Repositories/ProductRepository.cs` — mapping code does NOT handle Tags; the framework auto-includes + auto-maps on read, `SelectWithTags` carries them into the list projection, and the pipeline attaches on write.
- `content/Framework Project/StockPlusPlus.Data/Repositories/ProductRepository.cs` — no manual `.Include(x => x.Tags)`; the framework auto-includes Tags for `IShiftEntityTaggable` entities
- `content/Framework Project/StockPlusPlus.API/Program.cs` — `AddShiftTagging<DB>(StockPlusPlusActionTree.Tags)` (or `AddShiftTagging<DB>()` for anonymous endpoints) + `MapShiftTaggingEndpoints<DB>()`
- `content/Framework Project/StockPlusPlus.Data/Migrations/20260604115251_Tagging.cs` — Tags + ProductTags tables
- `content/Framework Project/StockPlusPlus.Web/Program.cs` — calls `AddShiftBlazorTagging(o => { o.BaseUrlKey = "StockPluPlus"; o.TypeAuthAction = StockPlusPlusActionTree.Tags; })` for shared tag-component config.
- `content/Framework Project/StockPlusPlus.Web/Pages/Tags/TagList.razor` (`@page "/tags"`) hosts `<ShiftTagList />`; `Pages/Tags/TagForm.razor` (`@page "/tags/{Key?}"`) hosts `<ShiftTagForm Key="@Key" />` — app-owned pages around the framework components
- `content/Framework Project/StockPlusPlus.Test/Tests/TaggingTests.cs` — 10 integration tests (incl. list-projection + view-after-save)
- `content/ShiftEntity/.template.config/template.json` — `taggable` item-template flag (`dotnet new shiftentity --taggable`)

Key files in sibling repos:
- `ShiftEntity.Core/Tagging/` — `Tag` entity + `IShiftEntityTaggable` + `ShiftTagTableAttribute` + `TagProjection` (read-side Tag→TagDTO) + `TaggableProjectionExtensions` (`SelectWithTags`)
- `ShiftEntity.Model/Dtos/Tagging/` — `TagDTO`, `TagListDTO`, `IShiftEntityTaggableDTO`
- `ShiftEntity.EFCore/Tagging/` — `AddShiftTagging<TDbContext>()` registration (action → secured endpoints; no action → anonymous), `ShiftTagRepository`, `TaggingPipeline` (upsert-on-save: attaches existing tags only — unknown ignored, no implicit create), `ShiftTagMapper` (the `IShiftEntityMapper<Tag, TagListDTO, TagDTO>` that `AddShiftTagging` registers via `TryAddScoped`)
- `ShiftEntity.EFCore/ShiftRepository.cs` — auto-includes Tags on find (`BaseFindAsync`) and auto-maps them onto the DTO (`ViewAsync`) for taggable entities
- `ShiftEntity.EFCore/Extensions/ModelBuilderExtensions.cs` — `ConfigureTagging` auto-wires M:N for every `IShiftEntityTaggable` entity
- `ShiftEntity.Web/Tagging/TaggingEndpoints.cs` — `MapShiftTaggingEndpoints<DB>()`
- `ShiftBlazor/Components/Tagging/` — `ShiftTagPicker` + `ShiftTagDisplay`
- `ShiftBlazor/Components/Tagging/ShiftTagList.razor` + `ShiftTagForm.razor` — tag management **components** (not routed pages); the programmer hosts them in their own `@page` pages. `ShiftTagList` opens `ShiftTagForm` as the row dialog.
- `ShiftBlazor/Tagging/` — `ShiftBlazorTaggingOptions` + `AddShiftBlazorTagging` registration extension (config only — the tag UI is components, so no router/assembly wiring)
- `ShiftBlazor/Components/Tagging/ShiftTagPicker.razor` — drop-in form picker; binds `List<TagDTO>`, forwards the full `ShiftAutocomplete` config via `CaptureUnmatchedValues`. Existing tags only (no free-typed create); "+" add button + double-click-view default to the framework `ShiftTagForm` component (`QuickAddComponentType`/`QuickAddParameterName` overridable, or null to hide). Placed explicitly in forms (no auto-render).
- `ShiftBlazor/Components/ShiftList/ShiftTagColumn.razor` — drop-in `<ShiftTagColumn />` grid column the programmer places inside `<ShiftList>` (list DTO must be `IShiftEntityTaggableDTO`); renders read-only tag chips. Not used → render tags your own way.
- `ShiftBlazor/Components/Tagging/ShiftTagFilter.razor` — drop-in `<ShiftTagFilter />` (non-generic) placed in a `<ShiftList>` with `EnableFilterPanel`; adds a multi-select Tags filter to the filter panel. Thin preset over `ForeignFilter` + `CollectionPrefix=["Tags"]` (the framework's M:N filter pattern). Sample: `ProductList.razor`.

When making tagging-related changes across any of these repos, update the planning doc.

## Key Conventions

- Conditional `#if` blocks throughout the sample project control what gets included in template output — be careful editing these as they affect both development mode and template generation
- `ImportAndReferenceAll.bat` / `RemoveAll.bat` toggle framework project references in the solution for development mode
- Connection strings and secrets are in `appsettings.Development.json` — SQL Server, Cosmos DB, Azure Storage, token RSA keys
