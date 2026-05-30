// Shared E2E helpers — mirrors the ADP Surveys harness (renderer/e2e/lib/util.ts).
// No test framework: each harness is a standalone tsx script using step()/assert().
import { execFileSync } from 'node:child_process';
import { createRequire } from 'node:module';

const require = createRequire(import.meta.url);

// ─── Endpoints / environment ────────────────────────────────────────────────
export const API = process.env.API_BASE ?? 'http://localhost:5069';
export const WEB = process.env.WEB_BASE ?? 'http://localhost:5186';
export const DB_SERVER = process.env.DB_SERVER ?? String.raw`localhost\sqlexpress`;
export const DB_NAME = process.env.DB_NAME ?? 'StockPlusPlusNET10';
export const USERNAME = process.env.E2E_USER ?? 'SuperUser';
export const PASSWORD = process.env.E2E_PASS ?? 'OneTwo';

export const HEADED = process.argv.includes('--headed') || process.env.HEADED === '1';
export const CHROME =
  process.env.CHROME_PATH ?? 'C:/Program Files/Google/Chrome/Application/chrome.exe';
export const SHOT_DIR = process.env.SHOT_DIR ?? 'C:/tmp/stockplusplus-e2e-shots';

// puppeteer-core: prefer the local devDependency; fall back to the shared
// screenshot-tool install other harnesses in this workspace use.
export function loadPuppeteer(): any {
  try {
    return require('puppeteer-core');
  } catch {
    return require('C:/tmp/screenshot-tool/node_modules/puppeteer-core');
  }
}

// ─── Logging / assertions ───────────────────────────────────────────────────
export function step(name: string, fn: () => Promise<void> | void) {
  return async () => {
    const t0 = Date.now();
    try {
      await fn();
      console.log(`  ✔ ${name}  (${Date.now() - t0}ms)`);
    } catch (e) {
      console.log(`  ✘ ${name}  (${Date.now() - t0}ms)`);
      throw e;
    }
  };
}

export function assert(cond: unknown, message: string): asserts cond {
  if (!cond) throw new Error(`Assertion failed: ${message}`);
}

export function assertEq<T>(actual: T, expected: T, message: string) {
  if (actual !== expected)
    throw new Error(
      `Assertion failed: ${message} — expected ${JSON.stringify(expected)}, got ${JSON.stringify(actual)}`,
    );
}

export const sleep = (ms: number) => new Promise((r) => setTimeout(r, ms));

// ─── HTTP (ShiftEntity API) ─────────────────────────────────────────────────
export async function api(
  method: string,
  path: string,
  init: { token?: string; body?: unknown } = {},
): Promise<Response> {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };
  if (init.token) headers['Authorization'] = `Bearer ${init.token}`;
  const url = path.startsWith('http') ? path : `${API}${path}`;
  return fetch(url, {
    method,
    headers,
    body: init.body === undefined ? undefined : JSON.stringify(init.body),
  });
}

export async function apiJson<T = unknown>(
  method: string,
  path: string,
  init: { token?: string; body?: unknown } = {},
): Promise<T> {
  const r = await api(method, path, init);
  const text = await r.text();
  if (!r.ok) throw new Error(`${method} ${path} → ${r.status}: ${text.slice(0, 800)}`);
  return text ? (JSON.parse(text) as T) : (undefined as T);
}

// Wait until the API + Web are both reachable (servers started separately).
export async function waitForApp(timeoutMs = 180_000) {
  const t0 = Date.now();
  const ok = async (url: string) => {
    try {
      const r = await fetch(url, { redirect: 'manual' as RequestRedirect });
      return r.status > 0;
    } catch {
      return false;
    }
  };
  while (Date.now() - t0 < timeoutMs) {
    if ((await ok(`${API}/api/Auth`)) || (await ok(API))) {
      if (await ok(WEB)) return;
    }
    await sleep(2000);
  }
  throw new Error(`API (${API}) and/or Web (${WEB}) not reachable within ${timeoutMs}ms`);
}

// ─── SQL (system sqlcmd, Windows integrated auth) ───────────────────────────
export function sql(query: string): string {
  try {
    return execFileSync(
      'sqlcmd',
      [
        '-S', DB_SERVER, '-E', '-d', DB_NAME,
        '-h', '-1', '-W', '-s', '\t',
        '-Q', `SET NOCOUNT ON; ${query}`,
      ],
      { encoding: 'utf8' },
    ).trim();
  } catch (e: any) {
    throw new Error(
      `sqlcmd failed: ${e.message ?? ''}\n${e.stdout?.toString?.() ?? ''}\n${e.stderr?.toString?.() ?? ''}`,
    );
  }
}

export function sqlScalar(query: string): string {
  return sql(query).trim();
}

// ─── Browser ────────────────────────────────────────────────────────────────
export async function launchBrowser() {
  const puppeteer = loadPuppeteer();
  return puppeteer.launch({
    executablePath: CHROME,
    headless: HEADED ? false : true,
    slowMo: HEADED ? Number(process.env.SLOWMO ?? 80) : 0, // visible pacing when headed
    args: ['--no-sandbox', '--disable-gpu', '--window-size=1400,1000'],
    defaultViewport: { width: 1300, height: 900 },
  });
}

// Collect console.error + pageerror (minus favicon/static 404 noise).
export function captureErrors(page: any): () => string[] {
  const errors: string[] = [];
  page.on('pageerror', (e: Error) => errors.push(`pageerror: ${e.message}`));
  page.on('console', (msg: any) => {
    if (msg.type() !== 'error') return;
    const text = msg.text();
    if (text.toLowerCase().includes('favicon')) return;
    if (text.includes('404') && text.toLowerCase().includes('resource')) return;
    errors.push(`console.error: ${text}`);
  });
  return () => errors;
}

export async function shot(page: any, name: string) {
  try {
    const fs = require('node:fs');
    fs.mkdirSync(SHOT_DIR, { recursive: true });
    const path = `${SHOT_DIR}/${name}.png`;
    await page.screenshot({ path, fullPage: true });
    console.log(`    screenshot → ${path}`);
  } catch (e: any) {
    console.log(`    (screenshot failed: ${e.message})`);
  }
}

// ─── DOM text helpers (case-insensitive; avoid brittle MudBlazor classes) ────
export async function bodyText(page: any): Promise<string> {
  return page.evaluate(() => (document.body as HTMLElement).innerText || '');
}

export async function hasText(page: any, substr: string): Promise<boolean> {
  return page.evaluate(
    (s: string) => ((document.body as HTMLElement).innerText || '').toLowerCase().includes(s.toLowerCase()),
    substr,
  );
}

export async function waitForText(page: any, substr: string, timeout = 15_000) {
  await page.waitForFunction(
    (s: string) => ((document.body as HTMLElement).innerText || '').toLowerCase().includes(s.toLowerCase()),
    { timeout, polling: 300 },
    substr,
  );
}

// Click the first element matching `selector` whose visible text contains `text`.
export async function clickByText(page: any, selector: string, text: string): Promise<boolean> {
  return page.evaluate(
    (sel: string, t: string) => {
      const want = t.toLowerCase();
      const els = Array.from(document.querySelectorAll(sel));
      const el = els.find(
        (e) => (((e as HTMLElement).innerText || e.textContent || '').trim().toLowerCase().includes(want)),
      );
      if (el) {
        (el as HTMLElement).click();
        return true;
      }
      return false;
    },
    selector,
    text,
  );
}

export async function click(page: any, selector: string, timeout = 10_000) {
  await page.waitForSelector(selector, { timeout });
  await page.click(selector);
}

export async function goto(page: any, path: string) {
  const url = path.startsWith('http') ? path : `${WEB}${path}`;
  await page.goto(url, { waitUntil: 'networkidle2', timeout: 60_000 });
}

// Pull a JWT-looking value out of localStorage (key name is framework-defined).
// NOTE: keep evaluate() bodies free of named inner functions — esbuild/tsx
// rewrites them with a `__name(...)` helper that is undefined in the browser.
export async function extractToken(page: any): Promise<string | null> {
  return page.evaluate(() => {
    for (const k of Object.keys(localStorage)) {
      const v = localStorage.getItem(k) ?? '';
      if (v.split('.').length === 3 && v.length > 60) return v;
      try {
        const o = JSON.parse(v);
        const cands = [o && o.token, o && o.Token, o && o.accessToken, o && o.AccessToken];
        for (const c of cands)
          if (typeof c === 'string' && c.split('.').length === 3 && c.length > 60) return c;
      } catch {
        /* not json */
      }
    }
    return null;
  });
}

// Log in through the ShiftIdentity UI and return the captured bearer token.
// Uses keyboard typing + Tab (MudTextField commits its bind on blur) and Enter
// (MudForm submit doesn't fire on a programmatic button click).
export async function login(page: any): Promise<string> {
  await page.goto(`${WEB}/InvoiceList`, { waitUntil: 'networkidle2', timeout: 60_000 });
  await page.waitForSelector('input', { timeout: 30_000 });
  await sleep(800);

  await page.focus('input');
  await page.keyboard.type(USERNAME, { delay: 40 });
  await page.keyboard.press('Tab');
  await page.keyboard.type(PASSWORD, { delay: 40 });
  await sleep(150);
  await page.keyboard.press('Enter');

  // Wait for the token to land in the Web app's localStorage.
  await page.waitForFunction(
    () => {
      for (const k of Object.keys(localStorage)) {
        const v = localStorage.getItem(k) ?? '';
        if (v.split('.').length === 3 && v.length > 60) return true;
        try {
          const o = JSON.parse(v);
          const cands = [o && o.token, o && o.Token, o && o.accessToken, o && o.AccessToken];
          for (const c of cands)
            if (typeof c === 'string' && c.split('.').length === 3 && c.length > 60) return true;
        } catch {
          /* ignore */
        }
      }
      return false;
    },
    { timeout: 30_000, polling: 500 },
  );

  const token = await extractToken(page);
  assert(token, 'captured a bearer token from localStorage after login');
  return token!;
}
