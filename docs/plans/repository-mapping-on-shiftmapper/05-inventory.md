# Stage 0 — the frozen oracle

**Captured:** 2026-09-18, from `ShiftSoftware.ShiftEntity.SourceGenerator` as built at ShiftTemplates commit
`343460e` (framework 2026.09.17.1), through the sample's test host (`CustomWebApplicationFactory`, SQL Express,
`localhost\sqlexpress`).

This file is the record of what Stage 0 froze: every triple (0.1), the goldens that pin each one's four
directions (0.2), and the generator warnings the two data projects print today (0.3). Stage 2.8 diffs
ShiftMapper against §2, and Stage 3 migrates against §1 and §3.

---

## 1. Inventory — every triple, and how it maps today (0.1)

Enumerated by `TripleEnumerator.All()` (attribute endpoints via `ShiftEntityEndpointDiscovery`, plus every
`ShiftRepository<,,,>` subclass, over `StockPlusPlus.Data` and `ShiftIdentity.Data`) — never hand-listed. The
**Arm** column is what `ParityArms.GeneratedArm` resolved from the running host, i.e. the object the request
would map through. **Nested** is what the golden actually shows composed (an object or a list of objects inside
the view / list output — select DTOs and file DTOs are leaves, not nesting). **Customizations** are the fluent
calls read from the source.

### StockPlusPlus.Data (10 triples)

| Triple | Door | Arm | Nested (view / list) | Customizations / attributes |
|---|---|---|---|---|
| Country / CountryDTO / CountryDTO | attribute endpoint, built-in repository | `Generated_Country_CountryDTO_CountryDTO_…` | — | none |
| Country / CountryGeneratedDTO / CountryGeneratedDTO | attribute endpoint (`UseGeneratedMapper = true`), entity `IConfiguresShiftRepository` | `Generated_Country_CountryGeneratedDTO_…` | — | `UseGeneratedMapper(map => map.ForList(d => d.Name, …))` in `Country.ConfigureRepository` |
| Country / CountryMappedDTO / CountryMappedDTO | attribute endpoint `WithMapper<…, CountryMapper>` | `CountryMapper` (hand-written `IShiftEntityMapper`) | — | none — **unchanged by this plan** |
| Country / CountryRepoDTO / CountryRepoDTO | repository subclass `CountryRepository` | `Generated_Country_CountryRepoDTO_…` | — | `UseGeneratedMapper()` |
| Invoice / InvoiceDeepListDTO / InvoiceDeepDTO | attribute endpoint (`UseGeneratedMapper = true`), entity `IConfiguresShiftRepository` | `Generated_Invoice_InvoiceDeepListDTO_…` | view: `InvoiceLines[]`, `InvoiceLines[].Product`, `InvoiceLines[].Product.ProductBrand` / list: same three levels | `UseGeneratedMapper()`; `// [ShiftEntityMapperMaxDepth(2)]` demo comment |
| Invoice / InvoiceListDTO / InvoiceDTO | repository subclass `InvoiceRepository` | `Generated_Invoice_InvoiceListDTO_…` | view: `InvoiceLines[]` / list: `InvoiceLines[]`, `InvoiceLines[].Product` | `UseGeneratedMapper(map => map.ForListChildren(d => d.InvoiceLines, …, line => line.ForChild(l => l.Product, …)))` |
| Product / ProductListDTO / ProductDTO | repository subclass `ProductRepository` | `ProductRepository (method override)` | list: `Tags[]` (via `SelectWithTags`) | overrides all four methods — **unchanged by this plan** |
| ProductBrand / ProductBrandListDTO / ProductBrandDTO | repository subclass `ProductBrandRepository` → `UseMapper(new ProductBrandMapper())` | `ProductBrandMapper` (`[ShiftEntityMapper]` partial) | — | `[ShiftEntityMapper]`; `Configure`: `ForList(Code)`; `MapToEntity` takeover calling `MapToEntityGenerated` |
| ProductCategory / ProductCategoryListDTO / ProductCategoryDTO | repository subclass `ProductCategoryRepository` | `Generated_ProductCategory_…` | — (`Photos` file list and `Brand` select DTO are leaves) | `UseGeneratedMapper()` |
| Vehicle / VehicleListDTO / VehicleDTO | repository subclass `VehicleRepository` | `VehicleRepository (method override)` | — | overrides all four methods — **unchanged by this plan** |

### ShiftIdentity.Data (13 triples)

| Triple | Door | Arm | Nested (view / list) | Customizations / attributes |
|---|---|---|---|---|
| AccessTree / AccessTreeListDTO / AccessTreeDTO | attribute endpoint (`UseGeneratedMapper = true`) | generated | — | none |
| App / AppDTO / AppDTO | attribute endpoint (`UseGeneratedMapper = true`) | generated | — | none |
| Brand / BrandListDTO / BrandDTO | attribute endpoint (`UseGeneratedMapper = true`) | generated | — | none |
| City / CityListDTO / CityDTO | attribute endpoint (`UseGeneratedMapper = true`), entity `IConfiguresShiftRepository` | generated | — | `ForList(Region, Country, CountryDisplayOrder, RegionDisplayOrder)` |
| Company / CompanyListDTO / CompanyDTO | repository subclass `CompanyRepository` | generated | view: `CustomFields` (dictionary) | `ForView(CustomFields, ParentCompany)`, `IgnoreEntity(CustomFields)`, `ForList(ParentCompanyName, Brands)` |
| CompanyBranch / CompanyBranchListDTO / CompanyBranchDTO | repository subclass `CompanyBranchRepository` | generated | view: `Phones[]`, `Emails[]`, `CustomFields` | `ForView(CustomFields, Departments, Services, Brands)`, `IgnoreEntity(CustomFields, Phone, ShortPhone)`, `ForList` ×11 (`Company`, `Region`, `City`, `CompanyTerminationDate`, four `*DisplayOrder`, `Brands`, `Departments`, `Services`) |
| CompanyCalendar / CompanyCalendarListDTO / CompanyCalendarDTO | attribute endpoint (`UseGeneratedMapper = true`), entity `IConfiguresShiftRepository` | generated | view: `ShiftGroups[]`, `ShiftGroups[].Shifts[]`, `WeekendGroups[]`, `WeekendGroups[].Weekends[]` (JSON-owned POCOs) | `ForView(Branches)`, `IgnoreEntity(Branches)`, `ForViewChildren(ShiftGroups, WeekendGroups)`, `ForEntityChildren(ShiftGroups, WeekendGroups)` |
| Country / CountryListDTO / CountryDTO | attribute endpoint (`UseGeneratedMapper = true`) | generated | — | none |
| Department / DepartmentListDTO / DepartmentDTO | attribute endpoint (`UseGeneratedMapper = true`) | generated | — | none |
| Region / RegionListDTO / RegionDTO | attribute endpoint (`UseGeneratedMapper = true`), entity `IConfiguresShiftRepository` | generated | — | `ForList(Country, CountryDisplayOrder)` |
| Service / ServiceListDTO / ServiceDTO | attribute endpoint (`UseGeneratedMapper = true`) | generated | — | none |
| Team / TeamListDTO / TeamDTO | attribute endpoint (`UseGeneratedMapper = true`), entity `IConfiguresShiftRepository` | generated | — | `ForView(Users, CompanyBranches)`, `ForList(Company)` |
| User / UserListDTO / UserDTO | repository subclass `UserRepository` | generated | — | `ForView(CompanyBranchID, TotpEnabled, AccessTrees)`, `IgnoreView(Password, RequireChangeAtNextLogin, SendVerification)`, `ForEntity(IntegrationId)`, `IgnoreEntity(Username, IsActive, Email, Phone, AccessTree)`, `ForList(CompanyBranch, TotpEnabled, LastSeen, AccessTrees)` |

Nested **pair** mappers the generator emitted for these (from the warnings and goldens): `InvoiceLine ↔
InvoiceLineDTO`, `InvoiceLine → InvoiceLineListDTO`, `Product → InvoiceLineProductListDTO`, and for the deep
endpoint `InvoiceLine ↔ InvoiceLineDeepDTO`, `Product ↔ ProductDeepDTO`, `ProductBrand ↔ ProductBrandDeepDTO`; ShiftIdentity's
`CompanyCalendarShiftGroup ↔ CompanyCalendarShiftGroupDTO`, `CompanyCalendarShift ↔ CompanyCalendarShiftItemDTO`,
`CompanyCalendarWeekendGroup ↔ CompanyCalendarWeekendGroupDTO` (JSON-owned, not tracked rows).

### Attribute and API usage to migrate (Stage 3) or delete (Stage 4)

| Spelling | Where |
|---|---|
| `[ShiftEntityMapper]` | sample `ProductBrandMapper` (1). Evidence only: `ADP.WarrantyClaims` `WarrantyClaimsObjectMappers.cs` (4 pair partials), `ADP.ClaimableItems` `ItemClaimCertificateRepository.cs` |
| `[ShiftEntityMapperIgnore]` | nowhere |
| `[ShiftEntityMapperMaxDepth]` | nowhere live — a commented demo on sample `Invoice` and a mention in `InvoiceDeepDTOs.cs` |
| `UseGeneratedMapper = true` (attribute property) | sample `Country` (2 endpoints), `Invoice` (1); ShiftIdentity `AccessTree`, `App`, `Brand`, `City`, `CompanyCalendar`, `Country`, `Department`, `Region`, `Service`, `Team` (10) |
| `UseGeneratedMapper()` | sample `CountryRepository`, `ProductCategoryRepository`, `Invoice.ConfigureRepository` |
| `UseGeneratedMapper(map => …)` | sample `InvoiceRepository`, `Country.ConfigureRepository`; ShiftIdentity `CompanyBranchRepository`, `CompanyRepository`, `UserRepository`, `City`, `CompanyCalendar`, `Region`, `Team`. Evidence only: 20 repositories in `ADP.*` / `Menu` |
| `UseMapper(new …)` | sample `ProductBrandRepository` (hand-plugged partial) |
| `IShiftObjectMapper` custom pairs | none in the framework-owned code (ADP.WarrantyClaims has 4) |

Both `[ShiftEntityMapperIgnore]` and a live `[ShiftEntityMapperMaxDepth]` have **zero** framework-owned uses,
so their deletion migrates nothing.

---

## 2. Goldens (0.2)

**Where:** `StockPlusPlus.Test/Tests/Parity/RepositoryMapping/Goldens/<Assembly>.<Entity>__<ListDTO>__<ViewDTO>.json`,
one file per triple, 23 files, 536 KB. Asserted by `StockPlusPlus.Test/Tests/RepositoryMappingParityTests.cs`
— six theories per triple (`View`, `Entity`, `List`, `Copy`, list `Shape`, fixture notes), 138 tests, ~2 s
once the host is up.

**One suite, both assemblies.** The plan said "and the same in `ShiftIdentity.Tests`"; not needed and not
done: the sample host registers ShiftIdentity's repositories and endpoints, so `ParityArms` resolves the
identity triples' real mappers from the same scope, with their `UseGeneratedMapper(map => …)` configuration
applied. A second suite would need a second host and would pin the same objects.

**How a golden is made** (`RepositoryMappingRun`):

| Section | What runs | Fixture |
|---|---|---|
| `View` | `MapToView(entity)` | entity graph, seed 1000, depth 3, two elements per collection at the top two levels |
| `Entity` | `MapToEntity(dto, existing)` → the `existing` entity | DTO graph seed 3000 depth 3; `existing` seed 5000 **depth 1** (only what the write leaves alone needs to be visible — a deep existing graph pinned 40 KB of untouched fixture per file) |
| `List` | `MapToList(new[] { entity }.AsQueryable()).ToList()` — LINQ-to-Objects | entity graph seed 1000 |
| `ListShape` | the projection's `Expression.ToString()` | — |
| `Copy` | `CopyEntity(source, target)` → the `target` entity | source seed 1000; target seed 5000 depth 1 |

The fixture builder (`DeterministicGraph`) fills every settable member from a running counter in ordinal
member-name order; a **string** member is filled with what its same-named counterpart on the other side can
parse (a number for a `long`, `"1197.5"` for a `decimal?`, JSON for a `List<ShiftFileDTO>`, an enum name for an
enum). Seeds keep the three graphs apart in the output: a 3000-series value in `Entity` is a member the write
overwrote, a 5000-series value is one it left alone.

**Serialization:** indented, `ReferenceHandler.IgnoreCycles`, no hash-id service in the options (every
hash-id converter short-circuits, IDs serialize raw — as the replication goldens are captured).

**Determinism:** captured, asserted on four separate process starts, and re-captured once more into a
scratch copy — `diff -rq` against the committed files found nothing, so the files are byte-identical run to run. No fixture notes on any triple — every member of every entity and DTO in both
assemblies could be built (no NetTopologySuite point, no abstract member, nothing skipped).

**Capture switch:** `SHIFT_TEST_CAPTURE_REPOSITORY_MAPPING_GOLDENS=1` rewrites every file from whatever
mapper the host resolves. After Stage 3 that is ShiftMapper — i.e. the implementation under test — so from
then on the switch is only for a change that is MEANT, recorded in [`02-open-decisions.md`](02-open-decisions.md).

**What the goldens visibly pin** (spot-checked, worth knowing when a diff appears in 2.8):

- three-level composition on `api/invoice-deep`, in the view and inside one list projection;
- `Photos` JSON ↔ `List<ShiftFileDTO>` on ProductCategory, both ways;
- `SelectWithTags` in Product's list shape and `Tags[]` in its list result;
- Invoice's deep write: `InvoiceLines` replaced with 3000-series rows; `CompanyID` overwritten; `RegionID`,
  `CityID`, `CountryID`, `IdempotencyKey` left at their 5000-series values (never written from a request);
- CompanyBranch's repository configuration: `Departments`/`Services`/`Brands` as select-DTO lists,
  `Latitude` read from a string column into `decimal?` (`1197.5`), `CustomFields` with the password `Value`
  blanked, `Phone`/`ShortPhone` left alone on write;
- `IsDeleted`, `CreateDate`, `LastSaveDate`, `CreatedByUserID`, `LastSavedByUserID` mapped in every direction
  (Q7 of the AutoMapper removal), `ID` never written.

---

## 3. SHENGEN baseline (0.3)

`dotnet build --no-incremental` of `StockPlusPlus.Data` (which rebuilds `ShiftIdentity.Data`) and of
`ShiftIdentity.Data`, 2026-09-18. **11 distinct warnings; no errors.** Each row says what it must be after
ShiftMapper takes over, per [`03-coverage.md`](03-coverage.md); Stage 2.8 checks the list.

| # | Warning | Triple / pair | Members | Must become |
|---|---|---|---|---|
| 1 | SHENGEN004 unmapped view members | Company / CompanyListDTO / CompanyDTO | `AlternativeExternalId` | SM0001 on `Company → CompanyDTO` |
| 2 | SHENGEN004 | pair CompanyCalendarShift → CompanyCalendarShiftItemDTO | `StartTime`, `EndTime` | SM0001 on the nested pair (or a conversion, if the types now convert — check) |
| 3 | SHENGEN007 unmapped list members | pair CompanyCalendarShiftGroup → CompanyCalendarShiftGroupDTO | `Departments`, `Brands`, `Shifts.StartTime`, `Shifts.EndTime` | SM0001 on the nested pair's list use |
| 4 | SHENGEN007 | pair CompanyCalendarShift → CompanyCalendarShiftItemDTO | `StartTime`, `EndTime` | SM0001 |
| 5 | SHENGEN007 | pair CompanyCalendarWeekendGroup → CompanyCalendarWeekendGroupDTO | `Departments`, `Brands` | SM0001 |
| 6 | SHENGEN007 | Product / ProductListDTO / ProductDTO | `ProductBrandName`, `Category`, `City` | **noise today**: `ProductRepository` overrides `MapToList`, the generated mapper is never used. Under ShiftMapper the implicit map still exists and would still warn (SM0001) — Stage 3.2 decides whether the override stays or becomes `o.Mapping` |
| 7 | SHENGEN008 view reads, entity never writes | CompanyBranch / … / CompanyBranchDTO | `CompanyID`, `Departments`, `Services`, `Brands` | SM0006 on the reverse map (Info) — the M:N members are reconciled in the entity's upsert hook, by design |
| 8 | SHENGEN008 | Team / … / TeamDTO | `CompanyBranches`, `Users` | SM0006 |
| 9 | SHENGEN008 | User / … / UserDTO | `CompanyBranchID`, `TotpEnabled`, `AccessTrees` | SM0006 |
| 10 | SHENGEN010 deep write replaces tracked child rows | Invoice / InvoiceDeepListDTO / InvoiceDeepDTO | `InvoiceLines` | SM0049 Info (M6) |
| 11 | SHENGEN010 | Invoice / InvoiceListDTO / InvoiceDTO | `InvoiceLines` | SM0049 Info (M6) — `InvoiceRepository.UpsertAsync` deletes-and-recreates, which is why it is not fixed |

Not present in the baseline, and therefore nothing to carry: SHENGEN003 (no cycle in any DTO graph), SHENGEN005/009
(no conditional configuration), SHENGEN006 (no entity+builder clash), SHENGEN011 (no case-ambiguous member).
