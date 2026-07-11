
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Mappers;
using StockPlusPlus.Shared.DTOs.ProductBrand;
#if (taggable)
using Microsoft.EntityFrameworkCore;
#endif

namespace StockPlusPlus.Data.Repositories;

public class ProductBrandRepository : ShiftRepository<DB, Entities.ProductBrand, ProductBrandListDTO, ProductBrandDTO>
{
#if (taggable)
    // ProductBrand demonstrates SOURCE-GENERATED mapping: ProductBrandMapper is a [ShiftEntityMapper]
    // partial class whose methods are filled in by the ShiftEntity source generator.
    public ProductBrandRepository(DB db) : base(db, x =>
    {
        x.IncludeRelatedEntitiesWithFindAsync(q => q.Include(e => e.Tags));
        x.UseMapper(new ProductBrandMapper());
    })
    {
    }
#else
    // ProductBrand demonstrates SOURCE-GENERATED mapping: ProductBrandMapper is a [ShiftEntityMapper]
    // partial class whose methods are filled in by the ShiftEntity source generator.
    public ProductBrandRepository(DB db) : base(db, x => x.UseMapper(new ProductBrandMapper()))
    {
    }
#endif
#if (includeItemTemplateContent)
    /// <summary>
    /// Implemented only to show that default methods can be overriden
    /// </summary>
    /// <param name="queryable"></param>
    /// <returns></returns>
    public override ValueTask<IQueryable<ProductBrandListDTO>> OdataList(IQueryable<Entities.ProductBrand>? queryable = null)
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