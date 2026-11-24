using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.Entities;

namespace StockPlusPlus.Data.SaveHandlers;

/// <summary>
/// After save. A category that was saved without a code gets one made from its ID, which only exists once the row is
/// written. The second write is in the same transaction as the first, so a category is never committed without a code.
/// </summary>
public sealed class ProductCategoryCode : IAfterSaveHandler<ProductCategory>
{
    public async ValueTask AfterSaveAsync(SavedRows<ProductCategory> rows, CancellationToken cancellationToken)
    {
        var coded = false;

        foreach (var row in rows)
        {
            if (row.Change != RowChange.Added || !string.IsNullOrWhiteSpace(row.Entity.Code))
                continue;

            row.Entity.Code = $"CAT-{row.Entity.ID:D5}";
            coded = true;
        }

        // A save made here joins the transaction of the save it follows. This handler is not called again for the
        // categories it is being called for.
        if (coded)
            await rows.Db.SaveChangesAsync(cancellationToken);
    }
}
