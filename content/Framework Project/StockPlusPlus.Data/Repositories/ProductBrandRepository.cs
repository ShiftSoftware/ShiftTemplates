
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Shared.DTOs.ProductBrand;

namespace StockPlusPlus.Data.Repositories;

public class ProductBrandRepository : ShiftRepository<DB, Entities.ProductBrand, ProductBrandListDTO, ProductBrandDTO>
{
    // The four maps of this triple are declared automatically from the type arguments above; nothing about
    // mapping is written in a repository. Where a member needs more than convention, the customization goes in
    // the project's ONE mapper class — Mappers/StockPlusPlusMapper.cs, an ordinary ShiftMapperBase — as a
    // CreateMap for that pair, which REPLACES the automatic map for that pair while the other pairs of the
    // triple stay automatic (ProductBrand's list Code and write AfterMap are there). The repository resolves the
    // host's mapper, which carries the class's maps and the automatic ones alike; its only word about its maps
    // would be how deep they nest, o.Mapping(m => m.Nested(n)).
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