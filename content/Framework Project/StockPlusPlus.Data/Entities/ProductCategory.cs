using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model;
using StockPlusPlus.Shared.Enums;

namespace StockPlusPlus.Data.Entities;

[TemporalShiftEntity]
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class ProductCategory : ShiftEntity<ProductCategory>,
    IEntityHasBrand<ProductCategory>, IEntityHasRegion<ProductCategory>, IEntityHasCompany<ProductCategory>, IEntityHasCompanyBranch<ProductCategory>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Code { get; set; }
    public string? Photos { get; set; }
    public TrackingMethod? TrackingMethod { get; set; }

    public long? BrandID { get; set; }
    public long? RegionID { get; set; }
    public long? CompanyID { get; set; }
    public long? CompanyBranchID { get; set; }
}