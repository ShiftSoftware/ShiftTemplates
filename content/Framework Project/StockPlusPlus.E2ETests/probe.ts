// Diagnostic probe — validates login + token capture and dumps live API shapes.
// Run: npm run probe  (headless)  |  npm run probe:headed
import {
  WEB, API, USERNAME, PASSWORD, launchBrowser, captureErrors, extractToken, sleep, shot, api,
} from './lib/util.ts';

async function dump(label: string, fn: () => Promise<unknown>) {
  try {
    const r = await fn();
    console.log(`\n### ${label}\n` + JSON.stringify(r, null, 2).slice(0, 2500));
  } catch (e: any) {
    console.log(`\n### ${label}  — ERROR: ${e.message}`);
  }
}

const browser = await launchBrowser();
const page = await browser.newPage();
const errors = captureErrors(page);

try {
  console.log(`Navigating to ${WEB}/InvoiceList ...`);
  await page.goto(`${WEB}/InvoiceList`, { waitUntil: 'networkidle2', timeout: 60_000 });
  await sleep(1500);
  console.log('URL after load:', page.url());
  await shot(page, 'probe-1-login');

  // Dump every input so we know the login form shape.
  const inputs = await page.$$eval('input', (els: any[]) =>
    els.map((e) => ({ type: e.type, name: e.name, id: e.id, placeholder: e.placeholder, label: e.getAttribute('aria-label') })),
  );
  console.log('\n### inputs on page\n' + JSON.stringify(inputs, null, 2));
  const buttons = await page.$$eval('button', (els: any[]) =>
    els.map((e) => (e.textContent || '').trim()).filter(Boolean).slice(0, 12),
  );
  console.log('\n### buttons on page\n' + JSON.stringify(buttons, null, 2));

  // Attempt the documented login: type + Tab + Enter.
  console.log('\nAttempting login as', USERNAME);
  await page.focus('input');
  await page.keyboard.type(USERNAME, { delay: 40 });
  await page.keyboard.press('Tab');
  await page.keyboard.type(PASSWORD, { delay: 40 });
  await sleep(200);
  await page.keyboard.press('Enter');
  await sleep(4000);
  console.log('URL after login attempt:', page.url());
  await shot(page, 'probe-2-after-login');

  const ls = await page.evaluate(() =>
    Object.keys(localStorage).map((k) => ({ key: k, len: (localStorage.getItem(k) || '').length, sample: (localStorage.getItem(k) || '').slice(0, 60) })),
  );
  console.log('\n### localStorage keys\n' + JSON.stringify(ls, null, 2));

  const token = await extractToken(page);
  console.log('\n### token captured:', token ? token.slice(0, 30) + '…' : 'NONE');

  if (token) {
    await dump('GET /api/ProductBrand?$top=3', () => api('GET', '/api/ProductBrand?$top=3', { token }).then((r) => r.json()));
    await dump('GET /api/ProductCategory?$top=3', () => api('GET', '/api/ProductCategory?$top=3', { token }).then((r) => r.json()));
    await dump('GET /api/Product?$top=2', () => api('GET', '/api/Product?$top=2', { token }).then((r) => r.json()));
    await dump('GET /api/Invoice?$top=2', () => api('GET', '/api/Invoice?$top=2', { token }).then((r) => r.json()));
    await dump('GET /api/attention/active', () => api('GET', '/api/attention/active', { token }).then((r) => r.json()));
  }

  console.log('\n### console/page errors\n' + JSON.stringify(errors(), null, 2));
} finally {
  await browser.close();
}
