// Attention feature — end-to-end browser tests (Iteration 6g).
// Drives the live Blazor WASM app + API with puppeteer-core. Seed → test → cleanup.
//   Run: npm run test:e2e   |   npm run test:e2e:headed
// Requires StockPlusPlus.API (:5069) and StockPlusPlus.Web (:5186) running, and
// the StockPlusPlusNET10 DB on localhost\sqlexpress (Windows auth).
import {
  launchBrowser, captureErrors, login, goto, click, clickByText, hasText, waitForText,
  shot, sleep, apiJson, sql, assert, assertEq, step, HEADED,
} from './lib/util.ts';
import { seedFixtures, cleanup, type Fixtures } from './lib/seed.ts';

let browser: any;
let fixtures: Fixtures | undefined;

// ─── DB verification helpers (indexed Invoice signals) ──────────────────────
// Invoice IDs are NOT hash-encoded (InvoiceDTO.ID carries no [HashId]), so the API
// id is the raw long and matches AttentionSignals.EntityId directly. (Client-supplied
// InvoiceNo is not honoured by the server, so we never correlate on it.)
function activeSignals(invoiceId: string, category?: string): number {
  const cat = category ? ` AND Category = '${category}'` : '';
  const out = sql(
    `SELECT COUNT(*) FROM AttentionSignals WHERE EntityType = 'Invoice' AND EntityId = ${invoiceId} AND ClearedAt IS NULL${cat};`,
  );
  return Number(out.trim() || '0');
}

function clearedSignals(invoiceId: string): number {
  const out = sql(
    `SELECT COUNT(*) FROM AttentionSignals WHERE EntityType = 'Invoice' AND EntityId = ${invoiceId} AND ClearedAt IS NOT NULL;`,
  );
  return Number(out.trim() || '0');
}

// Product uses JSON-shadow storage; the summary column on the Products row is the
// authoritative active-state flag (the ViewAndUpsert DTO doesn't expose it).
function productActive(name: string): boolean {
  return sql(`SELECT TOP 1 HasActiveAttention FROM Products WHERE Name = '${name}';`).trim() === '1';
}

// Optional case filter: `--case=3` or `--case=2,4,6`. Default = all 8.
const caseArg = process.argv.find((a) => a.startsWith('--case='));
const selected = caseArg
  ? new Set(caseArg.split('=')[1].split(',').map((s) => Number(s.trim())))
  : null;

const results: { name: string; ok: boolean; err?: string }[] = [];
async function run(name: string, fn: () => Promise<void>) {
  const n = parseInt(name, 10); // case number from the "N. …" prefix
  if (selected && !selected.has(n)) return;
  if (HEADED) {
    console.log(`\n▶  ${name}`);
    await sleep(1000); // pause so the case is easy to follow in the visible browser
  }
  try {
    await step(name, fn)();
    results.push({ name, ok: true });
  } catch (e: any) {
    results.push({ name, ok: false, err: e?.message ?? String(e) });
  }
  if (HEADED) await sleep(3000); // dwell on the end state before moving on
}

async function main() {
  browser = await launchBrowser();
  const page = await browser.newPage();
  const errors = captureErrors(page);

  console.log('Logging in via the ShiftIdentity UI…');
  const token = await login(page);
  console.log('  token captured.');

  console.log('Seeding attention fixtures via the API…');
  fixtures = await seedFixtures(token);
  const f = fixtures;
  console.log(
    `  overdue #${f.overdue.invoiceNo}  missingRef #${f.missingRef.invoiceNo}  ` +
      `dedup #${f.dedup.invoiceNo}  reload #${f.reload.invoiceNo}  product "${f.product.name}"\n`,
  );

  // 1 — Banner display
  await run('1. form shows the attention banner for an entity with active signals', async () => {
    await goto(page, `/InvoiceForm/${f.overdue.id}`);
    await waitForText(page, 'needs attention', 25_000);
    assert(await hasText(page, 'Due date'), 'overdue reason ("Due date … has passed") rendered in the banner');
    await shot(page, '1-banner');
  });

  // 2 — Bell icon → history dialog
  await run('2. bell icon opens the signal-history dialog with the timeline', async () => {
    await goto(page, `/InvoiceForm/${f.overdue.id}`);
    await waitForText(page, 'needs attention', 25_000);
    await click(page, '[aria-label="Signal history"]');
    await page.waitForSelector('.mud-dialog', { timeout: 10_000 });
    await waitForText(page, 'Raised:', 8_000);
    assert(await hasText(page, 'Overdue'), 'history dialog lists the Overdue signal');
    await shot(page, '2-history');
    if (!(await clickByText(page, '.mud-dialog button', 'Close'))) await page.keyboard.press('Escape');
    await sleep(600);
  });

  // 3 — Save → attention reload (6d fix): clearing the ref raises MissingReference on save
  await run('3. editing + saving reloads attention without reopening (6d fix)', async () => {
    await goto(page, `/InvoiceForm/${f.reload.id}`);
    await sleep(1800);
    await shot(page, '3a-open');
    assert(!(await hasText(page, 'needs attention')), 'reload invoice starts with no active attention');
    if (await page.$('[aria-label="Edit"]')) {
      await click(page, '[aria-label="Edit"]');
      await sleep(800);
    }
    const focused = await page.evaluate((ref: string) => {
      const inp = Array.from(document.querySelectorAll('input')).find(
        (i) => (i as HTMLInputElement).value === ref,
      ) as HTMLInputElement | undefined;
      if (!inp) return false;
      inp.focus();
      inp.select();
      return true;
    }, f.reload.ref);
    assert(focused, 'found and focused the ManualReference input');
    await page.keyboard.press('Backspace');
    // Commit the MudTextField by blurring it — do NOT Tab: Tab focuses the adjacent
    // date picker, whose modal overlay then blocks the Save button.
    await page.evaluate(() => (document.activeElement as HTMLElement | null)?.blur());
    await sleep(600);
    await shot(page, '3b-cleared');
    if (!(await clickByText(page, 'button', 'Save'))) await page.keyboard.press('Enter');
    await sleep(3000);
    await shot(page, '3c-saved');
    // Source of truth: saving must have raised the MissingReference signal.
    assert(activeSignals(f.reload.id, 'MissingReference') >= 1, 'save persisted and raised MissingReference in DB');
    // 6d: the banner must reflect the newly-raised signal without reopening the form.
    assert(await hasText(page, 'needs attention'), 'banner reloaded to show the new signal after save (6d)');
  });

  // 4 — Acknowledge → list refresh (6d fix)
  await run('4. acknowledge clears the signal; list reflects cleared state', async () => {
    await goto(page, `/InvoiceForm/${f.missingRef.id}`);
    await waitForText(page, 'needs attention', 20_000);
    assert(await clickByText(page, 'button', 'Acknowledge'), 'Acknowledge button present and clicked');
    await sleep(1800);
    assert(clearedSignals(f.missingRef.id) >= 1, 'signal marked cleared in DB after acknowledge');
    assertEq(activeSignals(f.missingRef.id), 0, 'no active signals remain after acknowledge');
    await goto(page, '/InvoiceList');
    await sleep(2500);
    await shot(page, '4-list-after-ack');
  });

  // 5 — ClearAttentionOnOpen (product form auto-acknowledge)
  await run('5. ClearAttentionOnOpen shows the banner on open, then clears it server-side', async () => {
    assert(productActive(f.product.name), 'product starts with active attention (released without price)');
    await goto(page, `/ProductForm/${f.product.id}`);
    // Regression guard: the banner must be VISIBLE on open (clearing no longer empties it),
    // showing the reason + the auto-acknowledged footer.
    await waitForText(page, 'needs attention', 20_000);
    assert(await hasText(page, 'without a price'), 'banner shows the released-without-price reason');
    assert(await hasText(page, 'Automatically acknowledged on open'), 'banner shows the auto-acknowledged footer');
    await shot(page, '5-clear-on-open');
    // …and the persisted signal is cleared server-side so the next open won't nag.
    let cleared = false;
    for (let i = 0; i < 15; i++) {
      if (!productActive(f.product.name)) {
        cleared = true;
        break;
      }
      await sleep(1000);
    }
    assert(cleared, 'product attention cleared server-side after open (ClearAttentionOnOpen)');
  });

  // 6 — NeedsAttentionList cross-entity page
  await run('6. NeedsAttention page lists cross-entity active signals', async () => {
    await goto(page, '/NeedsAttentionPage');
    await waitForText(page, 'Overdue', 20_000);
    assert(await hasText(page, 'Invoice'), 'NeedsAttention table shows Invoice rows');
    await shot(page, '6-needs-attention');
  });

  // 7 — Signal dedup on re-save (SQL)
  await run('7. re-saving the same trigger does not duplicate the signal (dedup)', async () => {
    assertEq(activeSignals(f.dedup.id, 'MissingReference'), 1, 'exactly one MissingReference signal after create');
    const full: any = await apiJson('GET', `/api/Invoice/${f.dedup.id}`, { token });
    const entity = full?.Entity ?? full?.entity ?? full;
    await apiJson('PUT', `/api/Invoice/${f.dedup.id}`, { token, body: entity });
    await sleep(600);
    assertEq(activeSignals(f.dedup.id, 'MissingReference'), 1, 'still one signal after re-save (deduped)');
  });

  // 8 — FrameworkOverdueEvaluator raised an Overdue/Warning signal
  await run('8. FrameworkOverdueEvaluator raised an Overdue (Warning) signal', async () => {
    const row = sql(
      `SELECT TOP 1 Severity FROM AttentionSignals WHERE EntityType = 'Invoice' AND EntityId = ${f.overdue.id} ` +
        `AND Category = 'Overdue' AND Source = 'FrameworkOverdueEvaluator' AND ClearedAt IS NULL;`,
    );
    assert(row.trim().length > 0, 'an active Overdue signal from FrameworkOverdueEvaluator exists');
    assertEq(Number(row.trim()), 2, 'overdue severity is Warning (2)');
  });

  // 9 — Saving must NOT auto-clear: ClearAttentionOnOpen is open-only (user-reported fix).
  await run('9. saving a price-less product re-raises a signal that is NOT auto-cleared', async () => {
    const newName = `${f.product.name}-RS`;
    await goto(page, `/ProductForm/${f.product.id}`);
    await sleep(2500); // let ClearAttentionOnOpen settle (clears any signal present at open)
    if (await page.$('[aria-label="Edit"]')) {
      await click(page, '[aria-label="Edit"]');
      await sleep(800);
    }
    const found = await page.evaluate((name: string) => {
      const i = Array.from(document.querySelectorAll('input')).find(
        (x) => (x as HTMLInputElement).value === name,
      ) as HTMLInputElement | undefined;
      if (!i) return false;
      i.focus();
      i.select();
      return true;
    }, f.product.name);
    assert(found, 'found the product Name field to edit');
    await page.keyboard.type(newName); // price stays null → ReleasedWithoutPrice re-raises on save
    await page.evaluate(() => (document.activeElement as HTMLElement | null)?.blur());
    await sleep(400);
    if (!(await clickByText(page, 'button', 'Save'))) await page.keyboard.press('Enter');
    await sleep(3500);
    await shot(page, '9-after-save');
    // With the open-only fix, the save-raised signal stays active + visible (not auto-cleared).
    assert(productActive(newName), 'save-raised signal is NOT auto-cleared (ClearAttentionOnOpen is open-only)');
    assert(await hasText(page, 'needs attention'), 'banner shows the re-raised signal after save');
  });

  const errs = errors();
  if (errs.length) console.log('\nConsole/page errors observed:\n' + errs.join('\n'));

  const passed = results.filter((r) => r.ok).length;
  console.log(`\n──────────── ${passed}/${results.length} cases passed ────────────`);
  for (const r of results) console.log(`  ${r.ok ? '✔' : '✘'} ${r.name}${r.err ? `\n      ${r.err}` : ''}`);
  if (passed !== results.length) throw new Error(`${results.length - passed} case(s) failed`);
}

main()
  .then(async () => {
    if (fixtures) await cleanup(fixtures);
    await browser?.close();
    console.log('\n✅ All attention E2E cases passed.');
  })
  .catch(async (err) => {
    console.error('\n❌ E2E FAILED:', err?.message ?? err);
    try {
      if (fixtures) await cleanup(fixtures);
    } catch {
      /* ignore */
    }
    try {
      await browser?.close();
    } catch {
      /* ignore */
    }
    process.exit(1);
  });
