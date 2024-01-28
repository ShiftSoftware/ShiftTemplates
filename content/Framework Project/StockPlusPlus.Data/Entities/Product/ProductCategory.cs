using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using StockPlusPlus.Shared.Enums.Product;

namespace StockPlusPlus.Data.Entities.Product;

[TemporalShiftEntity]
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class ProductCategory: ShiftEntity<ProductCategory>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Code { get; set; }
    public string? Photos { get; set; }
    public TrackingMethod? TrackingMethod { get; set; }
}
