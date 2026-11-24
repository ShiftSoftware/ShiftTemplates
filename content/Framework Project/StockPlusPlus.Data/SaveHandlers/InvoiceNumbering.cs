using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;

namespace StockPlusPlus.Data.SaveHandlers;

/// <summary>
/// Before save, under a lock. Every new invoice gets the next number of its company, whoever saves it: the API, a job, a
/// seeder or a test. It is declared with the context (<c>DB.ConfigureSaveHandlers</c>), so no host can leave it out.
/// </summary>
/// <remarks>
/// This sample takes a before-save handler as far as it goes, on purpose: numbers without a gap, for saves that run at the
/// same time, for several invoices in one save and for several saves in one transaction. Most handlers need none of it.
/// One that checks its rows or fills in a value is a loop over <c>rows</c>, and nothing more.
/// </remarks>
public sealed class InvoiceNumbering : IBeforeSaveHandler<Invoice>
{
    public async ValueTask BeforeSaveAsync(PendingRows<Invoice> rows, CancellationToken cancellationToken)
    {
        var added = rows.Where(row => row.Change == RowChange.Added).Select(row => row.Entity);

        // In a fixed order, so that two saves that number several companies do not wait for each other.
        foreach (var company in added.GroupBy(invoice => invoice.CompanyID).OrderBy(company => company.Key))
        {
            // From here until it commits, one transaction at a time numbers the invoices of this company: reading the
            // last number, adding to it and the insert are one step for everybody else. The framework opens the
            // transaction, and takes the lock the way the database does it.
            await rows.TakeLockAsync($"InvoiceNo:{company.Key}", cancellationToken);

            // The last number is the table's, or the one this save gave out in an earlier round: those invoices are not
            // written yet.
            var kept = (typeof(InvoiceNumbering), company.Key);
            var last = Math.Max(
                await LastNumberAsync((DB)rows.Db, company.Key, cancellationToken),
                rows.Items.TryGetValue(kept, out var given) ? (long)given! : 0);

            foreach (var invoice in company)
                invoice.InvoiceNo = ++last;

            rows.Items[kept] = last;
        }
    }

    // A deleted invoice keeps its number. A table with many invoices wants an index on the company and the number, so that
    // this reads the company's own rows and no other.
    private static async Task<long> LastNumberAsync(DB db, long? companyId, CancellationToken cancellationToken) =>
        await db.Invoices.IgnoreQueryFilters().Where(invoice => invoice.CompanyID == companyId)
            .MaxAsync(invoice => (long?)invoice.InvoiceNo, cancellationToken) ?? 0;
}
