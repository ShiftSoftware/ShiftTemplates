# Open decisions

The judgment calls this plan cannot make on its own. Each has a recommendation; a step that depends on one
says so. Record the answer here and in [`STATUS.md`](STATUS.md) when it is taken.

Already settled in discussion (2026-09-18), restated so nobody re-opens them:
- **No attributes for the programmer.** The three ShiftEntity mapping attributes and `UseGeneratedMapper = true`
  are deleted and nothing replaces them with another attribute; the framework's rules are code in a pack.
- **Configuration in the repository stays**, in ShiftMapper's vocabulary (`o.Mapping(m => …)`).
- **A mapper class overrides the repository** for the pair it declares.
- **The maps are usable anywhere**, not only in the repository.

---

## Q1 — One map per pair per assembly

**The question.** Today each repository has its own mapper instance, so two repositories over the same
`(entity, list, view)` triple can shape the same DTO differently. ShiftMapper has one map per pair per
assembly. The repository can still configure it (`o.Mapping`), but only one repository per pair may.

**What it changes.** Two repositories that want different shapes need different DTOs — which is already the
framework's rule for `WithMapper` endpoints and how the sample is built. Nothing in the sample or ShiftIdentity
relies on the per-repository copy; the ADP repositories should be checked in Stage 0.1.

**Recommendation: accept.** One place to look per pair, and a child customized once instead of per parent.
Needed by: M1, M3.

## Q2 — Two repositories configuring one pair is a build error

**The question.** When two closing types both call `o.Mapping(...)` for the same pair with different
content, which one wins? Silently picking by construction order is exactly the coin-flip the old registry
guarded against.

**Recommendation: error (SM0050)**, naming both. Fix by moving the configuration to a mapper class (one
place) or by giving one endpoint a distinct DTO. Needed by: M3.

## Q3 — The mapper may construct the repository from DI

**The question.** A repository's `o.Mapping(...)` expressions live in the repository's customization store,
filled when the repository is constructed. If a service maps `Invoice → InvoiceListDTO` through `Mapper`
before any `InvoiceRepository` exists in the scope, the generated map has to get the expressions from
somewhere: ShiftMapper resolves the closing type from DI — as it constructs a mapper class from DI today.
That means constructing a repository (and its `DbContext`, scoped, already there) from inside the mapper.

**Recommendation: accept.** It is the same rule ShiftMapper already has for mapper classes ("built from the
container the first time anything is mapped"), and it is what makes "the maps work anywhere" true for a
configured map. The alternative — "a configured map only works inside its repository" — is a trap. Needed
by: M3.

## Q4 — Flattening on implicit maps: off

**The question.** ShiftMapper fills `OrderDto.CustomerName` from `Order.Customer.Name` by default (audited by
SM0020 Info). The old generator refuses to flatten and prints a paste-ready `ForList` line instead; the
mapping-abstraction plan lists "implicit flattening can expose data unintentionally" as a motivation.

**Recommendation: the marker sets `Flattening = false`**, so a repository never silently reaches two levels
into an entity. A programmer who wants flattening turns it on for a pair in a mapper class:
`CreateMap<Order, OrderListDTO>(o => o.Flattening = true)`. The select-DTO convention is unaffected — it is
a convention, not flattening. Needed by: M1.

## Q5 — Null collections: accept ShiftMapper's default (null → empty)

**The question.** The old generator emits `dto.Lines = e.Lines == null ? null : …` — null stays null.
ShiftMapper's default turns a null source collection into an empty destination collection
(`AllowNullCollections = false`), which is AutoMapper's behaviour and what the framework had before the
generator.

**Recommendation: accept the default.** It removes a null check from every consumer and matches the
behaviour the DTOs were originally written against. The 0.2 goldens that pin `null` are updated in Stage 2.8
with this decision noted beside them.

## Q6 — SHENGEN006 moves to a small analyzer

**The question.** "The entity's `IConfiguresShiftRepository` will not run because the repository passes a
builder" is the one old diagnostic that is not about mapping. It dies with the generator unless it is
re-homed.

**Recommendation: re-home it** as a plain `DiagnosticAnalyzer` in the (currently empty)
`ShiftEntity.Analyzers` project, packaged as an analyzer inside `ShiftSoftware.ShiftEntity`. It is ~40
lines of logic and it guards a silent failure that is otherwise found in production. Needed by: Stage 4.2.

## Q7 — One release where both spellings work

**The question.** `ADP.*` and `Menu` have 20 repositories on `UseGeneratedMapper(map => …)` (~250 fluent
calls) and two files with `[ShiftEntityMapper]`. Deleting the old generator in the same release that
introduces the new path breaks them with no landing pad.

**Recommendation:** Stage 3 ships in one framework release (ShiftMapper ahead of the registry; the old
generator still present and working); Stage 4 ships in the **next** one. The release notes carry
[`04-migration-guide.md`](04-migration-guide.md). Consumers migrate on their own schedule within that window;
a consumer that has not migrated when Stage 4 ships stays on the previous framework version.

**In effect since 2026-09-19 (Stage 3).** `UseGeneratedMapper(...)` and the attribute property are `[Obsolete]`
with the guide's path in the message; a project still on them builds with warnings and maps exactly as before,
because the registry answers any triple ShiftMapper does not declare — which is every triple whose repository
says `UseGeneratedMapper`, since that call marks the options as configured ahead of any resolution.

## Q8 — Who registers the generated mapper

**The question.** ShiftMapper's model is that the *application* calls `builder.Services.AddShiftMapper()`,
which registers the calling assembly's generated mapper — and that mapper carries every referenced project's
and package's maps. The template can add that line. But "the programmer writes nothing" was the ask, and a
host that forgets the line only finds out at startup.

**Recommendation: both.** `RegisterShiftRepositories(assemblies)` registers the generated mapper of every
assembly it scans (ShiftMapper gains a small public `AddShiftMapper(Assembly)` overload built on the
existing `Mapper.GeneratedIn(assembly)`), so a host that scans its data assembly is covered with no extra
line. The template still adds `builder.Services.AddShiftMapper();` in the API, because that is the place a
host's own rules and mapper classes go, and it is one line. Startup validation names the line when a triple
is uncovered. Needed by: Step 2.7.

## Q9 — The signal on ShiftEntity's base class: attribute, or a generic mapper class

**The question.** ShiftMapper's generator has to be told "a class deriving from `ShiftRepository<,,,>`
declares maps between its type arguments". The plan uses a marker attribute *inside ShiftEntity* on the base
class — the programmer never sees it. The alternative spelling is a generic `ShiftMapperBase` class inside
ShiftEntity (`ShiftRepositoryMaps<TEntity, TList, TView>` with the four `CreateMap`s in its constructor)
that ShiftMapper closes once per repository — but ShiftMapper would still need to be told which base class to
close it for, so the same signal exists, only spelled as a type reference.

A third option was considered and rejected: a thin ShiftEntity generator writing ShiftMapper's declaration
metadata into `Data.dll` so the API's ShiftMapper generator reads it like a package (the "Contoso way").
It works only one project away — a repository living in the host project silently gets no maps — reports a
repository's warnings in the API's build with no line to click, and couples ShiftEntity to ShiftMapper's
metadata format byte-exact.

**Recommendation: the marker attribute inside ShiftEntity.** Smallest thing for ShiftMapper to read,
invisible to the programmer, and the generic-mapper-class spelling can be added later as sugar if it reads
better. Needed by: M1.

## Q10 — `CopyEntity` no longer copies `Tags` (nor `IdempotencyKey`)

**Decided 2026-09-19 (Stage 2.8): accepted, for both.** The diff showed exactly one difference on every
un-customized triple: `Copy.IdempotencyKey`, because the pack ignores it as a destination on every map and the
old shallow copy carried it. The copy refreshes a tracked row from a fresh load of the SAME row, whose key is the
same; nothing depends on the copy. No narrowing added. To be clear about the key itself (asked 2026-09-19): it is
client-generated — the UI sends it so a retried POST creates one row — but it travels in the `Idempotency-Key`
HEADER and the repository's upsert stamps it onto the entity after mapping; the mapper never wrote it, under the
old generator or now, and the duplicate-key protection is untouched.

**The question.** `CopyEntity` (entity → entity, used by `ReloadAfterSave`) today copies everything except
`ID`, `ReloadAfterSave`, `AuditFieldsAreSet` — so it copies `Tags`. With `Tags` ignored as a destination by
the pack rule (Step 2.2) it will not.

**Recommendation: accept, pending the golden diff.** The copy refreshes a tracked row from a fresh load
after the save pipeline has already attached the tags; copying them again is redundant. If Stage 2.8 shows a
functional difference, the `IgnoreMember` rule gains a "when the source is X" narrowing.

## Q11 — A blank select DTO on a NULLABLE foreign key clears it; on a required one it is a 400

**Decided 2026-09-19 (Stage 2.2): kept exactly.** ShiftMapper's `ParseOrNull<long>` already turns blank into
null; its `Parse<long>` turns blank into **0**, which the pack overrides with `string? → long` throwing the
same `ShiftEntityException` (`Model Validation Error`, `For` = the select) that `ToForeignKey` throws. Naming
the field needed a conversion that knows what it is converting — ShiftMapper gained the
`Func<TSource, string, TDestination>` form of `CreateConversion` for it. Non-numeric text on any other scalar
(an `int`, a date) is now ALSO a 400 naming the field, translated by the adapter from
`ShiftMapperConversionException`; before it was an uncaught exception.

**The question.** Today `ToNullableForeignKey` returns `null` for a blank `Value` (clearing the FK is
legitimate) and `ToForeignKey` throws a **400** naming the field. ShiftMapper's derived write parses
`dto.Brand.Value` with its own converter; what it does with `""` on `long?` and on `long` must be verified.

**Recommendation: keep today's behaviour exactly.** If ShiftMapper's default differs, the pack registers the
two conversions (`string? → long?` treating blank as null; `string → long` throwing) — a registered pair
beats the built-in table, and the repository's exception translation (Step 2.6) turns the throw into the
same 400. Verified in Stage 2.8.

## Q13 — `Tag → TagDTO` maps every member

**Decided 2026-09-19 (Stage 2.8): accepted.** `TagProjection.ToDto` filled five members (`ID`, `Name`,
`Color`, `Description`, `IntegrationID`); the framework map `Tag → TagDTO` fills the DTO's base members too
(`CreateDate`, `LastSaveDate`, `CreatedByUserID`, `LastSavedByUserID`, `IsDeleted`). Visible only on
`Product`'s list golden, whose repository overrides `MapToList` anyway. Harmless on a DTO, and the map is the
ordinary one every service gets.

## Q14 — Dictionary-valued nesting

**Decided 2026-09-19 (Stage 3): the ShiftMapper feature.** A `Dictionary<K, Entity>` fills a
`Dictionary<K, Dto>` with the keys carried across and each value through the pair's map (which has to exist —
SM0011 otherwise), in memory only: the projection leaves the member out and the map says so (SM0030, as for a
conversion without a query form). Identity's `CustomFields` turned out NOT to be the plain case — its read side
strips password values — so it keeps a `ForMember` on the view maps and an `Ignore` on the write maps, exactly
as the old configuration did; the feature serves the general case.

## Q15 — One DTO type as both list and view is one map

**Decided 2026-09-19 (Stage 3.6): accepted.** The old generator had four methods, so a `ForList` on a triple
whose list DTO and view DTO were the SAME type changed the list only. ShiftMapper maps PAIRS, and
`(Country, CountryGeneratedDTO)` is one pair whether it is read as a list or a view — so a customization on
`m.List` is on `m.View` too, and the surface's two handles are the same map. Visible on one sample triple
(`api/country-generated`, whose golden now shows the suffix on the view as well); no identity triple is
affected. The framework's recommendation stands on its own merits: a list and a view that must differ get two
DTO types, as every other triple in the sample has.

## Q12 — Depth default stays 10; a cycle is Info, not error

Not really open — restating today's behaviour so nobody re-litigates it in M2. `Nested = 10` on the
framework's markers; `m.Nested(n)` per repository or entity; a DTO cycle stops at the closing member with an
Info (SM0048), because the framework asked for nesting and cycles in DTO graphs are ordinary.
