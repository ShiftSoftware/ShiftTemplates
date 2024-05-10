
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model;

namespace StockPlusPlus.Data.Entities.Product;

[TemporalShiftEntity]
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class ProductBrand : ShiftEntity<ProductBrand>, IEntityHasTeam<ProductBrand>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Code { get; set; }

    public long? TeamID { get; set; }
}
