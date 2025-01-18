
using ShiftSoftware.ShiftEntity.Core;
#if (includeItemTemplateContent)
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model;
#endif

namespace StockPlusPlus.Data.Entities;

[TemporalShiftEntity]
#if (includeItemTemplateContent)
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
#endif
public class ProductBrand : ShiftEntity<ProductBrand>
#if (includeItemTemplateContent)
    ,IEntityHasTeam<ProductBrand>, IEntityHasRegion<ProductBrand>, IEntityHasCompany<ProductBrand>, IEntityHasCompanyBranch<ProductBrand>, IEntityHasIdempotencyKey<ProductBrand>, IEntityHasUniqueHash<ProductBrand>
#endif
{
#if (includeItemTemplateContent)
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Code { get; set; }

    public long? TeamID { get; set; }

    public long? RegionID { get; set; }
    public long? CompanyID { get; set; }
    public long? CompanyBranchID { get; set; }
    public Guid? IdempotencyKey { get; set; }

    public string? CalculateUniqueHash()
    {
        return $"{Name}|{Code}";
    }
#endif
}