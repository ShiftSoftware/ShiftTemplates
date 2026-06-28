
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Product;
using ShiftSoftware.ShiftEntity.EFCore.Tagging;

namespace StockPlusPlus.Data.Repositories;

public class ProductRepository : ShiftRepository<DB, Entities.Product, ProductListDTO, ProductDTO>
{
    public bool IncludeProductCategoryOnGetIquery { get; set; }

    // Tags are auto-included by the framework for IShiftEntityTaggable entities — no need to
    // list them here.
    private static readonly Action<ShiftRepositoryOptions<Product, ProductListDTO, ProductDTO>> IncludeOptions =
        x => x.IncludeRelatedEntitiesWithFindAsync(
            y => y.Include(z => z.ProductBrand),
            y => y.Include(z => z.CountryOfOrigin)
        );

    // Product demonstrates the OVERRIDE mapping strategy: the repository overrides the mapping
    // methods directly (no plugged mapper). The AutoMapper default still backs CopyEntity.
    //The ProductCategory is intentionally not included to show that the ShiftAutoComplete can handle this asynchronously by making a get request to the oData endpoint.
    public ProductRepository(DB db) :
        base(db, IncludeOptions)
    {
    }

    public override ProductDTO MapToView(Product entity)
    {
        return new ProductDTO
        {
            Name = entity.Name,
            TrackingMethod = entity.TrackingMethod,
            Price = entity.Price,
            ReleaseDate = entity.ReleaseDate,
            IsDraft = entity.IsDraft,

            ProductCategory = entity.ProductCategoryID.ToSelectDTO(entity.ProductCategory?.Name),
            ProductBrand = entity.ProductBrandID.ToSelectDTO(entity.ProductBrand?.Name),
            CountryOfOrigin = entity.CountryOfOriginID.ToSelectDTO(entity.CountryOfOrigin?.Name),
        }.MapBaseFields(entity);
    }

    public override Product MapToEntity(ProductDTO dto, Product existing)
    {
        existing.Name = dto.Name;
        existing.TrackingMethod = dto.TrackingMethod;
        existing.Price = dto.Price;
        existing.ReleaseDate = dto.ReleaseDate;
        existing.IsDraft = dto.IsDraft ?? false;

        existing.ProductCategoryID = dto.ProductCategory.ToForeignKey();
        existing.ProductBrandID = dto.ProductBrand.ToForeignKey();
        existing.CountryOfOriginID = dto.CountryOfOrigin.ToNullableForeignKey();

        return existing;
    }

    public override IQueryable<ProductListDTO> MapToList(IQueryable<Product> queryable)
    {
        return queryable.SelectWithTags(p => new ProductListDTO
        {
            ID = p.ID.ToString(),
            Name = p.Name,
            Price = p.Price,
            TrackingMethod = p.TrackingMethod,
            ReleaseDate = p.ReleaseDate,
            LastSaveDate = p.LastSaveDate,
            IsDraft = p.IsDraft,
            IsDeleted = p.IsDeleted,
            ProductBrandName = p.ProductBrand != null ? p.ProductBrand.Name : null,
            Category = p.ProductCategory != null ? p.ProductCategory.Name : null,
            ProductCategoryID = p.ProductCategoryID.ToString(),
            ProductBrandID = p.ProductBrandID.ToString(),
            CityID = p.CityID.HasValue ? p.CityID.Value.ToString() : null,
            HasActiveAttention = p.HasActiveAttention,
            HighestSeverity = (int?)p.HighestSeverity,
        });
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
