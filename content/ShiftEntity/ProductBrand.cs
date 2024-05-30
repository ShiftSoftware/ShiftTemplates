
using Microsoft.EntityFrameworkCore;
using StockPlusPlus.Data.Entities;

namespace StockPlusPlus.Data.DbContext;

public partial class DB
{
    public DbSet<ProductBrand> ProductBrand { get; set; }
}