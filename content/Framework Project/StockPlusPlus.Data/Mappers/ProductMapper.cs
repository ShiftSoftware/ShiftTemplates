using ShiftSoftware.ShiftEntity.Core;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Product;
using static ShiftSoftware.ShiftEntity.Core.MappingHelpers;

namespace StockPlusPlus.Data.Mappers;

public class ProductMapper : IShiftEntityMapper<Product, ProductListDTO, ProductDTO>
{
    public ProductDTO MapToView(Product entity)
    {
        return entity.MapBaseFieldsToView(new ProductDTO
        {
            Name = entity.Name,
            TrackingMethod = entity.TrackingMethod,
            Price = entity.Price,
            ReleaseDate = entity.ReleaseDate,
            IsDraft = entity.IsDraft,

            ProductCategory = ToSelectDTO(entity.ProductCategoryID, entity.ProductCategory?.Name),
            ProductBrand = ToSelectDTO(entity.ProductBrandID, entity.ProductBrand?.Name),
            CountryOfOrigin = ToSelectDTO(entity.CountryOfOriginID, entity.CountryOfOrigin?.Name),
        });
    }

    public Product MapToEntity(ProductDTO dto, Product existing)
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

    public IQueryable<ProductListDTO> MapToList(IQueryable<Product> query)
    {
        return query.Select(p => new ProductListDTO
        {
            ID = p.ID.ToString(),
            Name = p.Name,
            Price = p.Price,
            TrackingMethod = p.TrackingMethod,
            ReleaseDate = p.ReleaseDate,
            LastSaveDate = p.LastSaveDate,
            IsDraft = p.IsDraft,
            IsDeleted = p.IsDeleted,
            ProductBrand = p.ProductBrand != null ? p.ProductBrand.Name : null,
            Category = p.ProductCategory != null ? p.ProductCategory.Name : null,
            ProductCategoryID = p.ProductCategoryID.ToString(),
            ProductBrandID = p.ProductBrandID.ToString(),
            CityID = p.CityID.HasValue ? p.CityID.Value.ToString() : null,
        });
    }

    public void CopyEntity(Product source, Product target)
    {
        source.ShallowCopyTo(target);
    }
}
