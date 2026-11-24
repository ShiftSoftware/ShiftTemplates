using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;

namespace StockPlusPlus.Data.SaveHandlers;

/// <summary>
/// Before commit. No invoice line is committed for a product that is still a draft. The rule is about what the
/// transaction leaves behind and not about one save: a product may be published by a later save of the same
/// transaction, so it is judged once, when everything is written.
/// </summary>
/// <remarks>
/// It is here to show the stage. A rule that each save can answer for on its own is simpler before the save.
/// </remarks>
public sealed class NoDraftProductsOnInvoices : IBeforeCommitHandler<InvoiceLine>
{
    public async ValueTask BeforeCommitAsync(SavedRows<InvoiceLine> rows, CancellationToken cancellationToken)
    {
        // One row per line, however many saves of the transaction wrote it. A line that is deleted, or marked as deleted,
        // breaks no rule.
        var products = rows.Where(row => row.Change != RowChange.Deleted && !row.Entity.IsDeleted)
            .Select(row => row.Entity.ProductID)
            .Distinct()
            .ToList();

        if (products.Count == 0)
            return;

        // A line has the ID of its product and not the product, so whether that is a draft is read here. The read is
        // inside the transaction, so it sees what the transaction wrote.
        var drafts = await ((DB)rows.Db).Products
            .Where(product => products.Contains(product.ID) && product.IsDraft)
            .Select(product => product.Name)
            .Distinct()
            .OrderBy(name => name)
            .ToListAsync(cancellationToken);

        // The exception leaves the call that commits, and nothing is committed.
        if (drafts.Count > 0)
            throw new ShiftEntityException(new Message("Draft product", $"An invoice cannot have a line for a draft product: {string.Join(", ", drafts)}."));
    }
}
