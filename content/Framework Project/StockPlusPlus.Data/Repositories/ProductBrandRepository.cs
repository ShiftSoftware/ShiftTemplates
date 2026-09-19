
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Mappers;
using StockPlusPlus.Shared.DTOs.ProductBrand;

namespace StockPlusPlus.Data.Repositories;

public class ProductBrandRepository : ShiftRepository<DB, Entities.ProductBrand, ProductBrandListDTO, ProductBrandDTO>
{
    // ProductBrand demonstrates the MAPPER CLASS door: Mappers/ProductBrandMapper.cs is an ordinary ShiftMapper
    // class (ShiftMapperBase) declaring only the pairs it customizes; a CreateMap there REPLACES the automatic
    // map for that pair, and the other pairs of the triple stay automatic. Nothing is plugged in here — the
    // repository resolves the host's mapper, which carries the class's maps and the automatic ones alike.
    // Tags need no repository plumbing: the framework auto-includes them for IShiftEntityTaggable entities,
    // which is why the taggable and non-taggable arms of this class used to be identical apart from an
    // Include that did nothing.
    public ProductBrandRepository(DB db) : base(db)
    {
    }
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