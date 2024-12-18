﻿

using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using StockPlusPlus.Shared.Enums;

namespace StockPlusPlus.Data.Entities;

[TemporalShiftEntity]
public class Product : ShiftEntity<Product>,
    IEntityHasDraft<Product>, 
    IEntityHasCountry<Product>,
    IEntityHasRegion<Product>, 
    IEntityHasCompany<Product>, 
    IEntityHasCompanyBranch<Product>, 
    IEntityHasIdempotencyKey<Product>,
    IEntityHasCity<Product>
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
    public long? RegionID { get; set; }
    public long? CompanyID { get; set; }
    public long? CompanyBranchID { get; set; }
    public Guid? IdempotencyKey { get; set; }
    public long? CityID { get; set; }
    public long? CountryID { get; set; }
}
