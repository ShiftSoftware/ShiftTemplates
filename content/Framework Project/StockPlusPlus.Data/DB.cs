
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftIdentity.Data;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Entities.Product;

namespace StockPlusPlus.Data;

public class DB : ShiftIdentityDbContext
{
    public DB(DbContextOptions option) : base(option)
    {
    }

    public DbSet<Brand> Brands { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }
    public DbSet<Product> Products { get; set; }
    public DbSet<Country> Countries { get; set; }
}
