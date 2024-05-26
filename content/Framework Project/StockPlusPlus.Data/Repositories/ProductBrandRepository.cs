
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.ProductBrand;

namespace StockPlusPlus.Data.Repositories;

public class ProductBrandRepository : ShiftRepository<DB, Entities.ProductBrand, ProductBrandListDTO, ProductBrandDTO>
{
    public ProductBrandRepository(DB db) : base(db)
    {
    }


    /// <summary>
    /// Implemented only to show that default methods can be overriden
    /// </summary>
    /// <param name="showDeletedRows"></param>
    /// <param name="queryable"></param>
    /// <returns></returns>
    public override IQueryable<ProductBrandListDTO> OdataList(bool showDeletedRows = false, IQueryable<ProductBrand>? queryable = null)
    {
        return base.OdataList(showDeletedRows, queryable);
    }


    /// <summary>
    /// Implemented only to show that default methods can be overriden
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public override ValueTask<ProductBrandDTO> ViewAsync(ProductBrand entity)
    {
        //Do something here
        return base.ViewAsync(entity);
    }
}
