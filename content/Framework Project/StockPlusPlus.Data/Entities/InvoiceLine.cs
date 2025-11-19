using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;

namespace StockPlusPlus.Data.Entities;

public class InvoiceLine : ShiftEntity<InvoiceLine>
{
    public string Description { get; set; } = default!;
    public decimal Price { get; set; }
    public long ProductID { get; set; }
    public long InvoiceID { get; set; }
    public virtual Product Product { get; set; } = default!;
    public virtual Invoice Invoice { get; set; } = default!;
}
