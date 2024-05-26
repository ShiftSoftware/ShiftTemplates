
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model;

namespace StockPlusPlus.Data.Entities;

[TemporalShiftEntity]
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class ProductBrand : ShiftEntity<ProductBrand>,
    IEntityHasTeam<ProductBrand>, IEntityHasRegion<ProductBrand>, IEntityHasCompany<ProductBrand>, IEntityHasCompanyBranch<ProductBrand>
{
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Code { get; set; }

    public long? TeamID { get; set; }

    public long? RegionID { get; set; }
    public long? CompanyID { get; set; }
    public long? CompanyBranchID { get; set; }
}
