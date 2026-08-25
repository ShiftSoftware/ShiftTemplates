# Migrating a service off AutoMapper

This is Step E1 of the AutoMapper-removal plan, written to be read on its own. If you own a service that uses
Shift Framework and you have been told AutoMapper is going away, this is the whole procedure.

You will run it without the person who wrote it in the room, so it says *why* at each step, not just what.

---

## Before you start

**AutoMapper has been removed from the framework** (2026-08-25). There is no compatibility package and no
fallback: a triple with no mapper now fails at **startup**, listing everything that is missing.

**So the order matters.** Stay pinned to your current framework version — the last one that still carries
AutoMapper — and do this whole procedure there, where you can still compare against the old behaviour. Upgrade
only at the end, as step 6. Migrating and upgrading at the same time means debugging both at once with no
oracle for either.

What you need first:

- Your current framework version pinned, and your build green on it.
- Your test suite green.
- Ten minutes to read this.

> **If you are reading this because your build just broke on an upgrade:** roll the framework version back
> first. Nothing here is easier to do from a broken tree.

---

## The recipe

### 1. Ask each repository for its generated mapper

Still on your pinned version, so AutoMapper is present and every step below is reversible.

```csharp
public ProductRepository(DB db) : base(db, o => o.UseGeneratedMapper())
{
}
```

Do this one repository at a time. Everything you have not converted yet still runs on AutoMapper, so the tree
stays green throughout and each conversion is reviewable on its own.

For an entity with no repository class, the generated mapper is picked up automatically once you are on the
new framework version — nothing to write.

### 2. Build, and clear every `SHENGEN` warning

Expect a lot of them the first time. **That is the deliverable, not a problem.** Each one is a mapping decision
that was previously being made silently, and the point of this step is that you now make it deliberately.

| diagnostic | what it means | what to do |
|---|---|---|
| `SHENGEN004` | a view DTO member nothing maps | `ForView`, or `IgnoreView` if it is genuinely write-only |
| `SHENGEN007` | a list DTO member nothing projects — **the column comes back empty** | paste the `ForList(...)` line the message prints for you |
| `SHENGEN008` | a member `MapToView` reads and `MapToEntity` never writes back — **displays fine, silently fails to save** | `ForEntity`, or `IgnoreEntity` if it is read-only by design |
| `SHENGEN010` | automatic deep write would REPLACE tracked child rows | `map.AfterEntity(...)`, or the `existing`-aware `map.ForEntity(...)` |
| `SHENGEN011` | two members match one name case-insensitively | rename one, or map it explicitly |

Do not bulk-suppress these. `SHENGEN008` in particular is the failure this whole effort exists to prevent: a
field that displays correctly and never saves, with a green build and an HTTP 200.

### 3. Run the parity differ

Compare what the generated mapper produces against what AutoMapper produced, per member, for every triple.
Resolve **every** divergence explicitly: either fix the mapper, or record it as a reviewed
`KnownDivergence` with a reason.

**Do this while AutoMapper is still installed.** Once it is gone there is no oracle, permanently — you can
never again ask "what did this used to return?"

The divergences you should expect, because they are behaviour changes the framework made deliberately:

- **Nullable foreign keys are now cleared, not preserved.** Under AutoMapper, clearing a dropdown and saving was a silent no-op — the old value came back. That was a bug; it is fixed. Check nothing depended on it.
- **List payloads get richer.** Generated projections populate `ShiftEntitySelectDTO.Text`, which `ProjectTo` left empty. Better data, one LEFT JOIN per member. Measure your widest grids; `IgnoreList` per member where the join cost is real.
- **Malformed input now throws instead of silently writing a default.** A non-numeric foreign key is a 400 naming the field rather than a `0` written to your database.

### 4. Run your own test suite

Not the framework's. Yours.

### 5. Delete your AutoMapper profiles

Still on the pinned version. Deleting them here, before the upgrade, is the point: if removing a profile
changes behaviour, the differ in step 3 missed something, and you can still find out what.

Delete `AddAutoMapper(...)` and the `AutoMapper` package reference in the same pass.

### 6. Upgrade the framework

Only now. Startup validation fails the app if any triple has no mapper, listing all of them at once — so you
find out at boot with a complete list, rather than per-request in production.

If it fails, the list tells you exactly which triples you missed; each one needs a `UseGeneratedMapper()`, a
`UseMapper(...)`, or overridden mapping methods.

---

## The two shapes that are actually hard

Everything else in this migration is mechanical. These two are not.

### Collection reconciliation (the `AfterMap` shape)

If you have `AfterMap` blocks that reconcile a child collection against tracked rows — matching by business
key, reviving soft-deleted rows, preserving IDs — those do **not** port to a convention. Automatic deep write
is replace-with-new, which for tracked children with required foreign keys either throws or orphans rows.
`SHENGEN010` warns you.

Port them to `map.AfterEntity((dto, entity, ctx) => { ... })`, which runs after the generated body and can see
both sides, or use the `existing`-aware `map.ForEntity(...)` overload. The blocks usually transfer near-verbatim.

### Replication mappings (Cosmos)

Replication mappings do not live in the `(entity, list, view)` triple and never will — they are N documents per
entity, merged onto existing documents. Port them as plain static methods:

```csharp
public static BrandModel ToBrandModel(this Brand src) => new BrandModel { ... };
```

then pass them explicitly:

```csharp
.Replicate<BrandModel>(containerName, x => x.ToBrandModel())
```

**Two transcription traps.** Get these wrong and the failure is invisible, because replication swallows per-row
errors and still stamps a clean watermark:

1. **Null-navigation propagation.** AutoMapper silently yielded null when it walked through a null navigation. A hand-written port dereferences and throws — inside a swallowed `catch`. Use `?.` and `?? default` throughout.
2. **`default(long)` became `"0"`, not `""`.** If a document's id came from a null navigation, AutoMapper wrote `"0"`. Preserve that, or you change live document content in a partitioned store.

Capture golden JSON snapshots of the current AutoMapper output **before** you port anything, and diff against
them. These are documents in a partitioned store: a wrong write stamps a clean watermark and never retries.

---

## Things that will not be done for you

- **Flattening is not a convention** and will not become one. `src.City.Region.Country.DisplayOrder → CountryDisplayOrder` needs an explicit `ForList`. `SHENGEN007` prints the exact line to paste.
- **`Ignore*` calls are not workarounds.** If you find `IgnoreEntity(e => e.CustomFields)` in a repository, it is probably load-bearing — often the only thing stopping a request body from overwriting server-owned state. Read before deleting.
- **Audit and soft-delete columns are mapper payload.** Generated `MapToEntity` writes `CreateDate`, `LastSaveDate`, `IsDeleted`, `CreatedByUserID`, `LastSavedByUserID`, matching AutoMapper. The repository refuses `IsDeleted` on update — deleting stays behind its own permission — but the rest are yours to guard per entity if you need to.

---

## If something goes wrong

**Before step 6:** revert the repository you just converted. That is why the conversion is per-repository and
why the upgrade is last — every step up to the upgrade is a one-line revert, with AutoMapper still installed
and still able to answer "what did this used to return?"

**After step 6:** roll the framework version back. That restores the oracle, at which point you are back in
the reversible part of the procedure.

There is deliberately no mode switch and no compatibility shim to fall back on. An earlier draft of this plan
shipped both; they were removed because a silent swap of a hand-tuned profile for convention output is the one
change that cannot be reviewed after the fact — and a fallback that quietly catches the triples you forgot is
exactly that swap, just later and with nobody watching.
