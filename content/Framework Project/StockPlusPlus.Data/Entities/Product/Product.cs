

using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Replication;
using StockPlusPlus.Data.ReplicationModels;
using StockPlusPlus.Shared.Enums.Product;

namespace StockPlusPlus.Data.Entities.Product;

[TemporalShiftEntity]
public class Product : ShiftEntity<Product>
{
    public string Name { get; set; } = default!;

    public TrackingMethod TrackingMethod { get; set; }

    public long ProductCategoryID { get; set; }

    public long BrandID { get; set; }
    public long? CountryOfOriginID { get; set; }

    public virtual ProductCategory? ProductCategory { get; set; }

    public virtual Brand? Brand { get; set; }
    public virtual Country? CountryOfOrigin { get; set; }

    public DateTimeOffset? ReleaseDate { get; set; }
}
