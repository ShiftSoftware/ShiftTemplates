# StockPlusPlus.E2E — Attention feature browser tests

End-to-end browser tests for the **attention** feature (Iteration 6g), driven with
`puppeteer-core` + `tsx` against the live Blazor WASM app and API. Mirrors the ADP
Surveys e2e harness pattern: tiny `step()` / `assert()` helpers, `sql()` for DB
verification, and a **seed → test → cleanup** lifecycle (no test framework).

## Prerequisites

1. **SQL Server** with the `StockPlusPlusNET10` dev DB on `localhost\sqlexpress`
   (Windows integrated auth). The schema must be migrated (it is, if the app has run).
2. **API** running on `http://localhost:5069`. Cosmos isn't needed — start it with
   replication disabled so the emulator isn't required:
   ```
   dotnet run --project ../StockPlusPlus.API --launch-profile http -- --CosmosDb:Enabled=false
   ```
3. **Web (Blazor WASM)** running on `http://localhost:5186`:
   ```
   dotnet run --project ../StockPlusPlus.Web --launch-profile http
   ```
4. **Chrome** (puppeteer-core does not bundle a browser). Default path:
   `C:\Program Files\Google\Chrome\Application\chrome.exe` (override via `CHROME_PATH`).
5. `npm install` in this folder.

## Run

```
npm run test:e2e          # headless
npm run test:e2e:headed   # visible browser (debugging)
npm run typecheck         # tsc --noEmit
npm run probe             # one-off: validate login + dump live API shapes
```

Screenshots are written to `C:/tmp/stockplusplus-e2e-shots` (override via `SHOT_DIR`).
Credentials default to `SuperUser` / `OneTwo` (`E2E_USER` / `E2E_PASS` to override).

## Cases (all 8)

1. Form shows the attention banner for an entity with active signals.
2. Bell icon opens the signal-history dialog (timeline).
3. Edit + save reloads attention without reopening the form (6d fix).
4. Acknowledge clears the signal; the list reflects the cleared state (6d fix).
5. `ClearAttentionOnOpen` auto-acknowledges on form open (Product, JSON-shadow mode).
6. NeedsAttention page lists cross-entity active signals.
7. Re-saving the same trigger does not duplicate the signal (dedup, SQL-verified).
8. `FrameworkOverdueEvaluator` raises an Overdue/Warning signal (SQL-verified).

## Files

- `lib/util.ts` — endpoints/env, `step`/`assert`/`assertEq`, `api`/`apiJson`, `sql`,
  `launchBrowser`, UI helpers (`hasText`/`waitForText`/`clickByText`/`goto`), and `login`.
- `lib/seed.ts` — seeds overdue / missing-reference / dedup / reload invoices and a
  released-without-price product via the API; `cleanup()` reverses it.
- `e2e.ts` — the 8 cases. Runs every case (collecting pass/fail) then exits non-zero on any failure.
- `probe.ts` / `diag.ts` — diagnostics (login + live shapes; seed + DB dump). Not part of the suite.

## Gotchas learned (so the next session doesn't rediscover them)

- **Login**: navigate to a protected route → `/Identity/login` (same-origin Blazor page);
  type credentials with `keyboard.type` + Enter, then wait for `localStorage.token`
  (`{"Token":"<jwt>"}`). Programmatic localStorage seeding does not work.
- **tsx + `page.evaluate`**: never declare a *named* inner function inside an `evaluate`
  body — esbuild rewrites it with a `__name(...)` helper that is undefined in the browser.
  Inline the logic instead.
- **Invoice IDs are NOT hash-encoded** (`InvoiceDTO.ID` has no `[HashId]`), so the API id
  equals the raw `long` and matches `AttentionSignals.EntityId` directly. Products *are*
  hashed. Client-supplied `InvoiceNo` is not honoured by the server — don't correlate on it.
- **Editing a MudTextField**: commit the change by **blurring** (`activeElement.blur()`),
  not by pressing **Tab** — Tab moves focus into the adjacent date picker, whose modal
  overlay then blocks the Save button.
- **The Invoice form requires ≥3 lines** client-side (the API accepts fewer), so seed
  invoices with 3 lines or UI saves are blocked with "At least 3 items are required."
- **`ClearAttentionOnOpen`** clears on load, so the banner/footer is transient — assert the
  durable outcome (the entity's attention clears), not the fleeting footer text.
- The ViewAndUpsert DTO does **not** expose the attention summary columns; read product
  active-state from the `Products.HasActiveAttention` column (or the list DTO), not the form DTO.
