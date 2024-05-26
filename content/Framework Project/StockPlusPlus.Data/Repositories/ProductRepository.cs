
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Shared.DTOs.Product;

namespace StockPlusPlus.Data.Repositories;

public class ProductRepository : ShiftRepository<DB, Entities.Product, ProductListDTO, ProductDTO>
{
    //The ProductCategory is intentionally not included to show that the ShiftAutoComplete can handle this asynchronously by making a get request to the oData endpoint.
    public ProductRepository(DB db) :
        base(db,
                x => x.IncludeRelatedEntitiesWithFindAsync(
                    y => y.Include(z => z.ProductBrand),
                    y => y.Include(z => z.CountryOfOrigin)
                ))
    {
    }
}