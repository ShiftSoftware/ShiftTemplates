
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Shared.DTOs.Product.Product;

namespace StockPlusPlus.Data.Repositories.Product;

public class ProductRepository : ShiftRepository<DB, Entities.Product.Product, ProductListDTO, ProductDTO>
{
    //The ProductCategory is intentionally not included to show that the ShiftAutoComplete can handle this asynchronously by making a get request to the oData endpoint.
    public ProductRepository(DB db) :
        base(db,
                x => x.IncludeRelatedEntitiesWithFindAsync(
                    y => y.Include(z => z.Brand),
                    y => y.Include(z => z.CountryOfOrigin)
                ))
    {
    }
}