
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Shared.DTOs.ProductBrand;

namespace StockPlusPlus.Data.Repositories;

public class ProductBrandRepository : ShiftRepository<DB, Entities.ProductBrand, ProductBrandListDTO, ProductBrandDTO>
{
    public ProductBrandRepository(DB db) : base(db)
    {
    }
#if (includeItemTemplateContent)
    /// <summary>
    /// Implemented only to show that default methods can be overriden
    /// </summary>
    /// <param name="queryable"></param>
    /// <returns></returns>
    public override IQueryable<ProductBrandListDTO> OdataList(IQueryable<Entities.ProductBrand>? queryable = null)
    {
        return base.OdataList(queryable);
    }


    /// <summary>
    /// Implemented only to show that default methods can be overriden
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public override ValueTask<ProductBrandDTO> ViewAsync(Entities.ProductBrand entity)
    {
        //Do something here
        return base.ViewAsync(entity);
    }
#endif
}