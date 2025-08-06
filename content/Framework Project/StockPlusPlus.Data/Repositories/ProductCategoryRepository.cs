using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Shared.DTOs.ProductCategory;

namespace StockPlusPlus.Data.Repositories;

public class ProductCategoryRepository : ShiftRepository<DB, Entities.ProductCategory, ProductCategoryListDTO, ProductCategoryDTO>
{
    public ProductCategoryRepository(DB db) : base(db, shiftRepositoryBuilder =>
    {
        shiftRepositoryBuilder.IncludeRelatedEntitiesWithFindAsync(x => x.Include(x => x.Products), x => x.Include(x => x.Photos));
    })
    {
    }
}