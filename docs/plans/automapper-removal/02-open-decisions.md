# AutoMapper Removal — Open Decisions

**Created:** 2026-08-19 · **Rescoped:** 2026-08-22 — framework only

> **Rescope note.** Q5 and Q6 were dropped when the plan narrowed to the framework — both were consumer-scope
> questions. Q9 and Q10 replace them, and both exist *because* of the narrowing. **Numbering is deliberately
> stable**: the other three docs reference these by number.

The judgment calls the team must make. Each changes what gets built. Recommendations are given, but none of
these has a default that is obviously right — that is why they are here rather than decided in
[`01-steps.md`](01-steps.md).

Record the answer inline (edit this file) and reflect it in [`STATUS.md`](STATUS.md).

---

## Q1 — Does the generator actually run in package-mode consumers?

**Status:** ✅ **answered 2026-08-21 — yes, it runs** · **Blocked:** everything (now unblocked)

The evidence contradicts itself. `shiftsoftware.shiftentity.efcore.nuspec:17` carries
`exclude="Build,Analyzers"` on the Core dependency, and `dotnet build -getItem:Analyzer` in
`ADP.ClaimableItems.Data` returned only SDK analyzers. Yet fresh `Generated_*.g.cs` files exist in ADP `obj/`
trees, which a later check confirmed directly.

This is not a preference — it is a fact to establish, and it decides whether ADP's migration is *"the registry
already has a mapper for every triple, flip a switch and audit"* or *"the analyzer has never run there."*

**Answer: yes.** All four ADP data projects were rebuilt from a deleted `obj/` against
`ShiftSoftware.ShiftEntity.EFCore` 2026.7.31.1 as a `PackageReference`. The generator ran in every one — it
emitted `SHENGEN004` during compilation, and its mapper types are in the built assemblies (37 total: 5
ClaimableItems / 18 Menus / 4 Surveys / 10 WarrantyClaims), the same set present before the clean.

**So ADP's migration is "the registry already has a mapper for every triple — flip a switch and audit."**
Estimates downstream can assume the generated mappers already exist and compile.

**Why the evidence looked contradictory.** Both halves of the contradiction were measurement error, not
disagreement about reality:

- `dotnet build -getItem:Analyzer` was queried through `-t:ResolvePackageAssets`, which runs *before* package
  analyzers are folded into `@(Analyzer)` — so it can only ever return the implicit SDK analyzers. Through
  `-t:ResolveReferences`, `ShiftSoftware.ShiftEntity.SourceGenerator.dll` is there.
- `exclude="Build,Analyzers"` on `shiftsoftware.shiftentity.efcore.nuspec:17` is simply what `dotnet pack`
  writes for a default `PackageReference`; it sits on *every* edge reaching Core and demonstrably does not
  stop analyzer resolution. A throwaway project referencing only `ShiftSoftware.ShiftEntity.EFCore` resolves
  the generator.

The fresh `Generated_*.g.cs` files in ADP `obj/` were real, but they are written only when
`EmitCompilerGeneratedFiles=true` — which ADP never sets. They were left behind by an inspection build during
the audit itself. **Corollary for anyone re-checking this: `obj/**/generated/` is not a valid signal.** It
stays empty on a normal build while the generator runs fine. See [`STATUS.md`](STATUS.md) for the full method.

---

## Q2 — Nullable FK: clear or preserve on a null `ShiftEntitySelectDTO`?

**Status:** ❓ unanswered · **Blocks:** Stage E divergence triage · **Recommendation: clear**

AutoMapper preserved the existing FK when the incoming select DTO was null. The generated mapper clears it.

The framing "this is data loss" is misleading: `ShiftAutocomplete` posts `null` when the user clears a
dropdown, so under AutoMapper **"clear this dropdown" was a silent no-op** — the user cleared the field, hit
save, and the old value came back. The new behavior fixes that. `SourceGeneratedMappingTests.cs:367` already
pins clear-on-null as a green test.

You cannot have both as a default.

**Recommendation:** keep clear-on-null, and add `map.PreserveForeignKeyOnNull(x => …)` as a per-member, baked
opt-out — so every nullable select member in scope becomes an explicit migration decision rather than a
silent one. Release-note it. The opt-out has to live in the **framework** even though most of the ~21 affected
members are downstream: a consumer cannot add a baked opt-out to a generator it does not own.

---

## Q3 — On the wire, is an empty `ShiftEntitySelectDTO` `null` or `{Value:""}`?

**Status:** ❓ unanswered · **Blocks:** Step B8 scope · **Recommendation: make it global**

The hash-id converter already collapses blank to `null`, so hash-id'd properties changed behavior long ago and
nobody noticed. The remaining ~10 non-hash-id'd properties still emit `{Value:""}`.

**Options:**
- **Make it global** — one converter registration, and the divergence disappears entirely.
- **Accept the split** — then audit those ~10 properties individually and pin each.

**Recommendation:** global. A single serialization rule is cheaper to hold in your head than a list of
exceptions, and it shrinks Step B8's residual cases.

---

## Q4 — Entity-side auto-deep: default-on with a diagnostic, or opt-in?

**Status:** ❓ unanswered · **Blocks:** Step A8 shape · **Recommendation: keep default-on, add the diagnostic**

This is a genuine choice between two silent failures:

- **Opt-in** re-opens the bug fixed on 2026-08-06 (`../mapping-abstraction-plan.md` §23): JSON-owned
  grandchildren read back fine and were silently emptied on save. That fix is pinned by
  `GeneratedDeepWriteTests`.
- **Default-on with no diagnostic** corrupts link tables: *(downstream)* the generated `MenuVariant` mapper
  composes four collections that `GeneralMappingProfile.cs:315-330` deliberately ignores and merges by
  business key, so saving either throws (FKs are forced to `Restrict`) or duplicates/orphans rows.

Note the asymmetry the framework-only scope creates here: the *risk* is a consumer's, the *default* is ours,
and the consumer cannot change it. That is an argument for the loud option, not the quiet one.

**Recommendation:** keep default-on and add the diagnostic (Step A8), because the failure it produces is
*loud at build time* while the opt-in failure is *silent at runtime*. But this is a real judgment call about
which risk the team would rather carry, so it should be decided deliberately rather than inherited.

---

## Q5 — ~~Is `Menu` retired in favour of `ADP.Menus`?~~ *(dropped — out of scope)*

**Status:** ➖ dropped 2026-08-22 · moot under framework-only scope

`Menu` is a consumer service. Whether it is alive changes nothing here — it migrates on its own schedule
either way, and this plan schedules no consumer migrations.

Kept for whoever picks up a consumer migration later, because the finding still holds: `Menu` and `ADP.Menus`
are **not** duplicates — 219 differing lines after normalization, and `Menu` is pinned four months behind on
the framework version, so migrating it needs a framework bump first.

---

## Q6 — ~~ADP.SyncAgent: delete or migrate?~~ *(dropped — out of scope)*

**Status:** ➖ dropped 2026-08-22 · not a framework decision

SyncAgent has no ShiftEntity coupling, so this plan neither blocks on it nor decides it. See Step F4, which
is kept as a record rather than as work.

The one thing this plan **does** decide: because SyncAgent still exists, the release notes say *"AutoMapper is
gone from the framework"*, never *"AutoMapper is gone"*.

---

## Q7 — Client-supplied `IsDeleted` / `CreateDate` / `EmailVerified` stop working. Release note, or security advisory?

**Status:** ✅ **answered 2026-08-22 — map them; this is not the mapper's concern** · **Blocks:** the Stage F release notes

> **DECISION.** The generated `MapToEntity` **writes** the framework members, matching AutoMapper's unguarded
> `ReverseMap()`. Rationale, from the owner: *"the mapping should write all properties, not ignore any
> framework-related one unless it is ignored explicitly by the programmer — this should be handled either in
> the repository or by an explicit Ignore. It is not in the scope of the mapper."*
>
> So there is **no narrowing to communicate** and no advisory: behaviour is unchanged from AutoMapper. What
> changes is where the guard belongs — the repository or an explicit `IgnoreEntity`, not the generator.
> The question below is kept for the record of what was weighed.
>
> **Consequences that must be handled elsewhere, verified against `AuditStamper.StampAuditFields`:** on
> INSERT the stamper fills `CreateDate`/`CreatedByUserID`/`LastSaveDate`/`LastSavedByUserID` only where unset
> and forces `IsDeleted = false`, so a supplied value wins; on UPDATE it always overwrites
> `LastSaveDate`/`LastSavedByUserID` and touches nothing else — so `IsDeleted`, `CreateDate` and
> `CreatedByUserID` are whatever the mapper wrote. `AuditFieldsAreSet` is on no DTO, so the stamper's own
> guard cannot be set from the wire.
>
> **Action this creates:** entities whose upsert endpoint is reachable by a non-admin need either an
> `IgnoreEntity` for those members or a repository-side guard. Track it per entity, not per mapper.
>
> **RESOLVED for `IsDeleted`, 2026-08-23 — a repository policy, framework-wide.** `ShiftRepository`'s upsert
> captures the stored `IsDeleted` before mapping and restores it after, on **update** only. Deleting is a
> separate operation behind `Access.Delete`; an upsert needs only `Access.Write`, so honouring the flag from a
> PUT body would make the delete permission bypassable — and in the other direction it would be an undelete,
> which the framework exposes no API for. Insert is exempt: `AuditStamper` forces `IsDeleted = false` there.
>
> This is deliberately **not** a mapping rule. Mappers keep mapping `IsDeleted` like any other member; the
> repository decides which of those writes it keeps. Pinned by `ShiftEntity.Tests/Auditing/SoftDeleteOnUpdateTests.cs`
> (4 tests, including one that `DeleteAsync` still works — the guard covers upserts only).
>
> `CreateDate` / `CreatedByUserID` remain client-writable by design: `AuditStamper`'s documented rule is that a
> manually-assigned value wins, and provenance is not an access-control boundary.
>
> **SHIPPED 2026-08-22 — five of six members, plus the guard.** `CreateDate`, `LastSaveDate`, `IsDeleted`,
> `CreatedByUserID` and `LastSavedByUserID` left `EntityExcludedMembers`. **`ID` did not**, and that is not a
> policy exception — `EntityConvention` resolves entity `long ID` from DTO `string? ID` through `ToLong()`,
> which **throws on the null every insert carries**, and `ShiftEntityCrudHandler` catches only
> `ShiftEntityException`, so it would escape as a 500 on **every POST**. Deep write is worse: pair `MapBack`
> maps into `new ChildEntity()`, so an existing child's key would be pushed onto a fresh row and SQL Server
> would reject the explicit identity insert. That is strictly *less* robust than AutoMapper, which yields `0`
> here — so writing `ID` needs its own null-tolerant convention and an answer for deep children first. Pinned
> by `FrameworkAuditMemberWriteTests.TheKey_IsStillNotWrittenFromTheDto`.
>
> Also still pipeline-owned, and unreachable in practice — **no DTO in any of the three repos declares any of
> them**: `ReloadAfterSave`, `AuditFieldsAreSet`, `IdempotencyKey`. And `Tags`, which `TaggingPipeline` owns on
> both legs: writing it composes rows the pipeline discards a line later, and a `"tags": null` payload NREs on
> `entity.Tags.Clear()`.
>
> **The repository guard shipped with it** (`ShiftRepository`'s upsert): on **update** the tracked row's own
> `IsDeleted` wins over the request body, because soft delete is gated on `Access.Delete` while an upsert needs
> only `Access.Write`, and the framework has no undelete API. Insert is exempt — `AuditStamper` forces
> `IsDeleted = false` there. This closes the same hole on the **AutoMapper** path, which has been open in
> production all along. `CreateDate`/`CreatedByUserID` are deliberately left client-writable: `AuditStamper`'s
> documented rule is that a manually-assigned value wins, and provenance is not an access-control boundary.
>

---

## Q8 — Do you accept richer list payloads?

**Status:** ❓ unanswered · **Blocks:** Stage E divergence triage · **Recommendation: accept, then measure**

Generated list projections populate `ShiftEntitySelectDTO`s — including `Text` — that `ProjectTo` left empty,
because it never ran AutoMapper's `AfterMap`. Better data; one LEFT JOIN per member.

**Recommendation:** accept globally rather than suppressing list-side select bindings to preserve what was
effectively a `ProjectTo` bug. Then measure on the widest grids and apply `IgnoreList` per member where the
join cost is real.

**Open item:** audit any remaining list DTO carrying a select DTO member. The three known cases
(`CompanyListDTO`, `CompanyBranchListDTO`, `UserListDTO`) are all in scope and already pin them with explicit
`ForList`. *(downstream: `ReplacementItemListDTO` in `ADP.Menus`/`Menu` is unchecked — note it in the
migration guide, do not audit it here.)*

---

## Q9 — Do we ship the required-delegate compile break while consumers are un-migrated?

**Status:** ✅ **answered 2026-08-22 — yes, but at E3, not before** · **Blocks:** Step E3

> **DECISION.** Take the compile break — **when Step E3 is reached**, as part of removing AutoMapper from
> replication. Do **not** make the delegate required earlier as a standalone change. The `= null` defaults and
> the four AutoMapper fallback sites stay exactly as they are until then; B4 made the resolve lazy and that is
> all it was meant to do.
>
> The pre-announcement obligation stands: the six downstream call sites
> (`ADP.ClaimableItems` ×5, `ADP.WarrantyClaims` ×1) must hear about it before the release that carries it.

Step E3 drops the `= null` default on the replication mapping delegate. In scope this is free: the template's
12 sites are ported in E2. Out of scope it is a **compile break** at 6 call sites (`ADP.ClaimableItems` ×5,
`ADP.WarrantyClaims` ×1) that this plan does not fix.

The alternative is keeping the `= null` overloads and throwing at the fallback instead. That converts a build
error into a runtime throw **inside a swallowed per-row `catch`** — permanently-dirty rows under a clean
watermark, which is the single failure shape this plan works hardest to eliminate everywhere else.

**Recommendation:** take the compile break. Say plainly in the release notes that a consumer not ready to
port stays on the previous framework version — a pinned version is a normal, reversible state; a
half-replicated Cosmos partition is not.

**This is the one place framework-only scope creates work for someone else.** Decide it deliberately, and
tell the consumer teams *before* the release rather than with it.

---

## Q10 — What ships as the default: `AutoMapperFirst` forever, or a flip?

**Status:** ❓ unanswered · **Blocks:** D1, F1, F5 · **Recommendation: `AutoMapperFirst` until F5, then it stops existing**

`MappingMode` (D1) exists so each service can move on its own schedule. With every consumer out of scope, the
live question becomes: what does the **framework** ship as the default, and for how long?

- **Keep `AutoMapperFirst` and never delete the fallback** — then the removal never actually happens; the plan produces better diagnostics and nothing else.
- **Flip the default to `GeneratedFirst` while consumers are un-migrated** — exactly the silent profile-for-convention swap D1 exists to prevent. Measured on the first triple examined, the generated `WarrantyClaim` mapper produced three regressions with zero warnings.

**Recommendation:** the default stays `AutoMapperFirst` through every release up to F5. At F5 the fallback
*moves* into the compat package rather than being deleted outright, so the shipped chain becomes
registry → compat seam (only if you installed it) → throw. A consumer opts in with one package and one line,
and nobody's mapper silently changes underneath them. `MappingMode` survives afterwards as the per-service
opt-in it was designed to be.
