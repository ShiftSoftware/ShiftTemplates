// Diagnostic: seed fixtures, then dump DB + API state WITHOUT cleanup.
import { launchBrowser, login, sql, apiJson } from './lib/util.ts';
import { seedFixtures } from './lib/seed.ts';

const browser = await launchBrowser();
try {
  const page = await browser.newPage();
  const token = await login(page);
  const f = await seedFixtures(token);
  const nos = `${f.overdue.invoiceNo},${f.missingRef.invoiceNo},${f.dedup.invoiceNo},${f.reload.invoiceNo}`;
  console.log('\nfixtures:', JSON.stringify({ overdue: f.overdue, missingRef: f.missingRef, dedup: f.dedup, reload: f.reload, product: f.product }, null, 2));

  console.log('\n-- Invoices (by InvoiceNo) --');
  console.log(sql(`SELECT i.ID, i.InvoiceNo, i.ManualReference, i.HasActiveAttention, i.HighestSeverity, i.ActiveSignalCount FROM Invoices i WHERE i.InvoiceNo IN (${nos});`));

  console.log('\n-- AttentionSignals JOINed to those Invoices --');
  console.log(sql(`SELECT i.InvoiceNo, s.EntityType, s.EntityId, s.Category, s.Source, s.Severity, IIF(s.ClearedAt IS NULL,'active','cleared') FROM AttentionSignals s JOIN Invoices i ON i.ID = s.EntityId WHERE i.InvoiceNo IN (${nos});`));

  console.log('\n-- Top 12 AttentionSignals overall (most recent) --');
  console.log(sql(`SELECT TOP 12 s.ID, s.EntityType, s.EntityId, s.Category, s.Source, IIF(s.ClearedAt IS NULL,'active','cleared') FROM AttentionSignals s ORDER BY s.ID DESC;`));

  console.log('\n-- Product via SQL (by name) --');
  console.log(sql(`SELECT TOP 3 ID, Name, Price, ReleaseDate, HasActiveAttention, HighestSeverity, ISNULL(LEN(AttentionSignalsJson),-1) AS JsonLen FROM Products WHERE Name = '${f.product.name}';`));

  console.log('\n-- Product via API --');
  const p: any = await apiJson('GET', `/api/Product/${f.product.id}`, { token });
  console.log(JSON.stringify(p, null, 2).slice(0, 700));

  console.log('\n-- per-entity attention endpoint (overdue invoice) --');
  const att: any = await apiJson('GET', `/api/Invoice/${f.overdue.id}/attention`, { token }).catch((e: any) => ({ error: e.message }));
  console.log(JSON.stringify(att, null, 2).slice(0, 700));

  console.log('\n(no cleanup — data left in place; rerun diag or clean by InvoiceNo)');
} finally {
  await browser.close();
}
