
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.Entities.Product;
using StockPlusPlus.Shared.DTOs.Product.Brand;

namespace StockPlusPlus.Data.Repositories.Product;

public class BrandRepository : ShiftRepository<DB, Entities.Product.Brand, BrandListDTO, BrandDTO>
{
    public BrandRepository(DB db) : base(db)
    {
    }


    /// <summary>
    /// Implemented only to show that default methods can be overriden
    /// </summary>
    /// <param name="showDeletedRows"></param>
    /// <param name="queryable"></param>
    /// <returns></returns>
    public override IQueryable<BrandListDTO> OdataList(bool showDeletedRows = false, IQueryable<Brand>? queryable = null)
    {
        return base.OdataList(showDeletedRows, queryable);
    }


    /// <summary>
    /// Implemented only to show that default methods can be overriden
    /// </summary>
    /// <param name="entity"></param>
    /// <returns></returns>
    public override ValueTask<BrandDTO> ViewAsync(Brand entity)
    {
        //Do something here
        return base.ViewAsync(entity);
    }
}
