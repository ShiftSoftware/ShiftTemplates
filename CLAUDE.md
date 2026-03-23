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

Required sibling repos for development mode: `ShiftEntity`, `ShiftBlazor`, `ShiftIdentity`, `TypeAuth`, `AzureFunctionsAspNetCoreAuthorization`, `ShiftFrameworkTestingTools`, `ShiftFrameworkLocalization`.

### Sample Project Structure (StockPlusPlus)

- **API** — ASP.NET Core Web API (.NET 10). Controllers, DbContext, identity integration, AutoMapper profiles
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

`ShiftFrameworkGlobalSettings.props` defines `ShiftFrameworkVersion`, `TypeAuthVersion`, and `AzureFunctionsAspNetCoreAuthorizationVersion`. The Builder's `UpdateTemplateVersions` reads these and writes them into `template.json` so newly created projects get correct package versions.

## CI/CD

Azure DevOps pipeline (`azure-pipeline.yml`) triggers on `release*` tags. It clones all framework sibling repos, builds everything, runs tests, then packs and publishes NuGet packages. Tag naming controls what gets published: `release-all`, `release-framework`, `release-typeauth`, `release-aspNetCore-authorization`.

## Active Work: Mapping Abstraction

We are actively decoupling `ShiftRepository` from AutoMapper by introducing `IShiftEntityMapper<TEntity, TListDTO, TViewDTO>` as a pluggable mapping abstraction. This is a cross-repo effort spanning ShiftEntity, ShiftTemplates, and ShiftIdentity.

**Planning doc with full context, status, and iteration tracking:** `../ShiftEntity/docs/mapping-abstraction-plan.md`

**IMPORTANT:** When making any mapping-related changes across ShiftEntity, ShiftTemplates, or ShiftIdentity, always update the planning doc to reflect what was done. This document is the single source of truth for the team — check it before starting work to see current status, and update it after completing work. Do not rely on memory or conversation context alone.

Key files in this repo:
- `content/Framework Project/StockPlusPlus.Data/Mappers/` — mapper implementations per strategy:
  - Manual: `ProductMapper.cs`, `ProductCategoryMapper.cs`, `InvoiceMapper.cs`
  - Mapperly: `ProductMapperlyMapper.cs`, `ProductCategoryMapperlyMapper.cs`, `InvoiceMapperlyMapper.cs`
- `content/Framework Project/StockPlusPlus.Data/Repositories/` — two-constructor pattern: DI picks `IShiftEntityMapper` when registered, falls back to AutoMapper
- `content/Framework Project/StockPlusPlus.API/Program.cs` — reads `MappingStrategy` from appsettings, conditionally registers mappers (`AutoMapper`, `Manual`, or `Mapperly`)
- `content/Framework Project/StockPlusPlus.Shared/Enums/MappingStrategy.cs` — enum for the toggle
- `content/Framework Project/StockPlusPlus.Test/Tests/ManualMappingTests.cs` — 8 integration tests validating `IShiftEntityMapper` path (pass with both Manual and Mapperly)
- `content/Framework Project/StockPlusPlus.Test/Tests/MappingPOC/` — POC test files comparing Manual, Mapperly, and Mapster approaches (not production code)

Key files in ShiftEntity (sibling repo):
- `ShiftEntity.Core/IShiftEntityMapper.cs` — the interface (4 methods: MapToView, MapToEntity, MapToList, CopyEntity)
- `ShiftEntity.Core/MappingHelpers.cs` — helpers to reduce manual mapping boilerplate (audit fields, FK ↔ ShiftEntitySelectDTO, ShallowCopyTo)
- `ShiftEntity.EFCore/AutoMapperShiftEntityMapper.cs` — wraps AutoMapper as an IShiftEntityMapper implementation
- `ShiftEntity.EFCore/ShiftRepository.cs` — unified on single `entityMapper` path, ReloadAfterSave handled inline

When working on mapping-related changes, always check the planning doc for current iteration status before starting.

## Key Conventions

- Conditional `#if` blocks throughout the sample project control what gets included in template output — be careful editing these as they affect both development mode and template generation
- `ImportAndReferenceAll.bat` / `RemoveAll.bat` toggle framework project references in the solution for development mode
- Connection strings and secrets are in `appsettings.Development.json` — SQL Server, Cosmos DB, Azure Storage, token RSA keys
