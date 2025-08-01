﻿
using Microsoft.EntityFrameworkCore;
#if (internalShiftIdentityHosting)
using ShiftSoftware.ShiftIdentity.Data;
#elif (externalShiftIdentityHosting)
using ShiftSoftware.ShiftEntity.EFCore;
#endif

#if (includeSampleApp)
using StockPlusPlus.Data.Entities;
#endif

namespace StockPlusPlus.Data.DbContext;

#if (internalShiftIdentityHosting)
public partial class DB : ShiftIdentityDbContext
#elif (externalShiftIdentityHosting)
public partial class DB : ShiftDbContext
#endif
{
    public DB(DbContextOptions<DB> option) : base(option)
    {
    }

#if (includeSampleApp)
    public DbSet<ProductBrand> Brands { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Country> Countries { get; set; }
#endif
}
