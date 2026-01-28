
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Product;

namespace StockPlusPlus.Data.Repositories;

public class ProductRepository : ShiftRepository<DB, Entities.Product, ProductListDTO, ProductDTO>
{
    public bool IncludeProductCategoryOnGetIquery { get; set; }

    //The ProductCategory is intentionally not included to show that the ShiftAutoComplete can handle this asynchronously by making a get request to the oData endpoint.
    public ProductRepository(DB db) :
        base(db,
                x => x.IncludeRelatedEntitiesWithFindAsync(
                    y => y.Include(z => z.ProductBrand),
                    y => y.Include(z => z.CountryOfOrigin)
                ))
    {
    }

    public override async ValueTask<IQueryable<Product>> GetIQueryable(DateTimeOffset? asOf, List<string>? includes, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {
        var q = await base.GetIQueryable(asOf, includes, disableDefaultDataLevelAccess, disableGlobalFilters);

        if (this.IncludeProductCategoryOnGetIquery)
            q = q.Include(x => x.ProductCategory);

        return q;
    }

    public async Task BulkDeleteAsync(List<Product> products)
    {
        foreach (var product in products)
        {
            product.IsDeleted = true;
            product.ProductCategory!.IsDeleted = true;
        }

        await this.SaveChangesAsync();
    }
}