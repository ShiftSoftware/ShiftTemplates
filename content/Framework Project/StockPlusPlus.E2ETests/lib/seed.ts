// Seeds attention-triggering fixtures via the authenticated API, and cleans them up.
// Trigger rules (from the framework + sample evaluators):
//   • FrameworkOverdueEvaluator  → DueDate <= now              → Overdue / Warning (indexed)
//   • InvoiceMissingReferenceEvaluator → ReleaseDate set + blank ManualReference → MissingReference / Info (indexed)
//   • Product.EvaluateAttention  → Price null + ReleaseDate set → ReleasedWithoutPrice / Warning (JSON-shadow)
import { api, apiJson, assert, sql } from './util.ts';

const iso = (d: Date) => d.toISOString();

export interface SeededInvoice {
  id: string;
  invoiceNo: number;
  ref: string;
}

export interface Fixtures {
  token: string;
  tag: string;
  lineProductId: string;
  categoryId: string;
  brandId: string;
  overdue: SeededInvoice; // DueDate past, has ref  → Overdue only
  missingRef: SeededInvoice; // ReleaseDate set, blank ref → MissingReference
  dedup: SeededInvoice; // same as missingRef; re-saved in the dedup test
  reload: SeededInvoice; // starts clean (has ref); ref cleared in the reload test
  product: { id: string; name: string };
}

function pickId(listResponse: any): string {
  const rows = listResponse.Value ?? listResponse.value ?? [];
  assert(rows.length > 0, 'list endpoint returned at least one row to reference');
  return rows[0].ID ?? rows[0].id;
}

function createdId(response: any): string {
  const e = response?.Entity ?? response?.entity ?? response;
  const id = e?.ID ?? e?.id;
  assert(typeof id === 'string' && id.length > 0, `create returned an ID (got ${JSON.stringify(response).slice(0, 200)})`);
  return id;
}

async function createInvoice(
  token: string,
  body: Record<string, unknown>,
): Promise<string> {
  const r = await apiJson<any>('POST', '/api/Invoice', { token, body });
  return createdId(r);
}

export async function seedFixtures(token: string): Promise<Fixtures> {
  const tag = `e2e-${Date.now()}`;
  const lineProductId = pickId(await apiJson('GET', '/api/Product?$top=1', { token }));
  const categoryId = pickId(await apiJson('GET', '/api/ProductCategory?$top=1', { token }));
  const brandId = pickId(await apiJson('GET', '/api/ProductBrand?$top=1', { token }));

  const now = new Date();
  const past = new Date(Date.now() - 5 * 864e5); // 5 days ago
  const baseNo = Math.floor(Date.now() / 1000) % 1_000_000_000;
  // The Invoice *form* enforces a client-side minimum of 3 lines (the server API
  // accepts fewer), so seed 3 lines — otherwise UI-driven saves are blocked.
  const lines = [1, 2, 3].map((n) => ({
    Product: { Value: lineProductId },
    Description: `${tag} line ${n}`,
    Price: n,
  }));

  // Overdue: DueDate in the past, ManualReference present, no ReleaseDate → Overdue (Warning) only.
  const overdueRef = `${tag}-OVD`;
  const overdueNo = baseNo;
  const overdueId = await createInvoice(token, {
    ManualReference: overdueRef,
    InvoiceDate: iso(now),
    DueDate: iso(past),
    InvoiceNo: overdueNo,
    InvoiceLines: lines,
  });

  // Missing reference: ReleaseDate set + blank ref → MissingReference (Info).
  const missNo = baseNo + 1;
  const missingRefId = await createInvoice(token, {
    ManualReference: '',
    InvoiceDate: iso(now),
    ReleaseDate: iso(past),
    InvoiceNo: missNo,
    InvoiceLines: lines,
  });

  // Dedup: same trigger as missing-ref; the test re-saves it and asserts no duplicate.
  const dedupNo = baseNo + 2;
  const dedupId = await createInvoice(token, {
    ManualReference: '',
    InvoiceDate: iso(now),
    ReleaseDate: iso(past),
    InvoiceNo: dedupNo,
    InvoiceLines: lines,
  });

  // Reload: has a ref + ReleaseDate → starts with NO active signal. The reload test clears
  // the ref and saves, which raises MissingReference and must surface without reopening.
  const reloadRef = `${tag}-REL`;
  const reloadNo = baseNo + 3;
  const reloadId = await createInvoice(token, {
    ManualReference: reloadRef,
    InvoiceDate: iso(now),
    ReleaseDate: iso(past),
    InvoiceNo: reloadNo,
    InvoiceLines: lines,
  });

  // Product: no price + ReleaseDate → ReleasedWithoutPrice (Warning), JSON-shadow storage.
  const productName = `${tag}-NoPrice`;
  const productId = createdId(
    await apiJson<any>('POST', '/api/Product', {
      token,
      body: {
        Name: productName,
        TrackingMethod: 'Serial',
        ProductCategory: { Value: categoryId },
        ProductBrand: { Value: brandId },
        Price: null,
        ReleaseDate: iso(past),
      },
    }),
  );

  return {
    token,
    tag,
    lineProductId,
    categoryId,
    brandId,
    overdue: { id: overdueId, invoiceNo: overdueNo, ref: overdueRef },
    missingRef: { id: missingRefId, invoiceNo: missNo, ref: '' },
    dedup: { id: dedupId, invoiceNo: dedupNo, ref: '' },
    reload: { id: reloadId, invoiceNo: reloadNo, ref: reloadRef },
    product: { id: productId, name: productName },
  };
}

export async function cleanup(f: Fixtures) {
  const del = async (path: string) => {
    try {
      await api('DELETE', path, { token: f.token });
    } catch {
      /* best effort */
    }
  };
  await del(`/api/Invoice/${f.overdue.id}`);
  await del(`/api/Invoice/${f.missingRef.id}`);
  await del(`/api/Invoice/${f.dedup.id}`);
  await del(`/api/Invoice/${f.reload.id}`);
  await del(`/api/Product/${f.product.id}`);
  // Hard-remove the indexed signal rows for the seeded invoices so reruns stay clean.
  const ids = [f.overdue.id, f.missingRef.id, f.dedup.id, f.reload.id];
  try {
    sql(`DELETE FROM AttentionSignals WHERE EntityType = 'Invoice' AND EntityId IN (${ids.join(', ')});`);
  } catch {
    /* best effort */
  }
}
