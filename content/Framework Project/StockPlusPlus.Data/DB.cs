
using Microsoft.EntityFrameworkCore;
#if (internalShiftIdentityHosting)
using ShiftSoftware.ShiftIdentity.Data;
#elif (externalShiftIdentityHosting)
using ShiftSoftware.ShiftEntity.EFCore;
#endif

#if (includeSampleApp)
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Entities.Product;
#endif

namespace StockPlusPlus.Data;

#if (internalShiftIdentityHosting)
public class DB : ShiftIdentityDbContext
#elif (externalShiftIdentityHosting)
public class DB : ShiftDbContext
#endif
{
    public DB(DbContextOptions option) : base(option)
    {
    }

#if (includeSampleApp)
    public DbSet<Brand> Brands { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Country> Countries { get; set; }
#endif
}
