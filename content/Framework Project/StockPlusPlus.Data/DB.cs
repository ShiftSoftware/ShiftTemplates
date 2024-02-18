
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftIdentity.Data;
#if (includeSampleApp)
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Entities.Product;
#endif

namespace StockPlusPlus.Data;

public class DB : ShiftIdentityDbContext
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
