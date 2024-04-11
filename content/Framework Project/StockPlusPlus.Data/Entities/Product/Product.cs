

using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model.Replication;
using StockPlusPlus.Data.ReplicationModels;
using StockPlusPlus.Shared.Enums.Product;

namespace StockPlusPlus.Data.Entities.Product;

[TemporalShiftEntity]
public class Product : ShiftEntity<Product>, IEntityHasDraft<Product>
{
    public string Name { get; set; } = default!;

    public TrackingMethod TrackingMethod { get; set; }

    public long ProductCategoryID { get; set; }

    public long ProductBrandID { get; set; }
    public long? CountryOfOriginID { get; set; }

    public virtual ProductCategory? ProductCategory { get; set; }

    public virtual ProductBrand? ProductBrand { get; set; }
    public virtual Country? CountryOfOrigin { get; set; }

    public DateTimeOffset? ReleaseDate { get; set; }
    public bool IsDraft { get; set; }
}
