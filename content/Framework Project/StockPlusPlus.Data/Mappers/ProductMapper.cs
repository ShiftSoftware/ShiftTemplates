using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Product;

namespace StockPlusPlus.Data.Mappers;

/// <summary>
/// Manual IShiftEntityMapper implementation for Product.
/// Demonstrates explicit mapping for an entity with:
///   - ShiftEntitySelectDTO FK conventions (ProductCategory, ProductBrand, CountryOfOrigin)
///   - Navigation properties via Includes (ProductBrand, CountryOfOrigin)
///   - Enum properties (TrackingMethod)
///   - Nullable FK (CountryOfOriginID)
///   - Separate List vs View DTO shapes
/// </summary>
public class ProductMapper : IShiftEntityMapper<Product, ProductListDTO, ProductDTO>
{
    public ProductDTO MapToView(Product entity)
    {
        return new ProductDTO
        {
            ID = entity.ID.ToString(),
            Name = entity.Name,
            TrackingMethod = entity.TrackingMethod,
            Price = entity.Price,
            ReleaseDate = entity.ReleaseDate,
            IsDraft = entity.IsDraft,
            IsDeleted = entity.IsDeleted,
            CreateDate = entity.CreateDate,
            LastSaveDate = entity.LastSaveDate,
            CreatedByUserID = entity.CreatedByUserID?.ToString(),
            LastSavedByUserID = entity.LastSavedByUserID?.ToString(),

            // FK → ShiftEntitySelectDTO (explicit, no reflection)
            ProductCategory = new ShiftEntitySelectDTO
            {
                Value = entity.ProductCategoryID.ToString(),
                Text = entity.ProductCategory?.Name
            },
            ProductBrand = new ShiftEntitySelectDTO
            {
                Value = entity.ProductBrandID.ToString(),
                Text = entity.ProductBrand?.Name
            },
            CountryOfOrigin = entity.CountryOfOriginID.HasValue
                ? new ShiftEntitySelectDTO
                {
                    Value = entity.CountryOfOriginID.Value.ToString(),
                    Text = entity.CountryOfOrigin?.Name
                }
                : null,
        };
    }

    public Product MapToEntity(ProductDTO dto, Product existing)
    {
        existing.Name = dto.Name;
        existing.TrackingMethod = dto.TrackingMethod;
        existing.Price = dto.Price;
        existing.ReleaseDate = dto.ReleaseDate;
        existing.IsDraft = dto.IsDraft ?? false;

        // ShiftEntitySelectDTO → FK (explicit, no reflection)
        existing.ProductCategoryID = long.Parse(dto.ProductCategory.Value);
        existing.ProductBrandID = long.Parse(dto.ProductBrand.Value);
        existing.CountryOfOriginID = dto.CountryOfOrigin != null && !string.IsNullOrWhiteSpace(dto.CountryOfOrigin.Value)
            ? long.Parse(dto.CountryOfOrigin.Value)
            : null;

        // Navigation properties are NOT touched — no risk of overwriting with null
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
        target.Name = source.Name;
        target.TrackingMethod = source.TrackingMethod;
        target.Price = source.Price;
        target.ReleaseDate = source.ReleaseDate;
        target.IsDraft = source.IsDraft;
        target.ProductCategoryID = source.ProductCategoryID;
        target.ProductBrandID = source.ProductBrandID;
        target.CountryOfOriginID = source.CountryOfOriginID;
        target.ProductCategory = source.ProductCategory;
        target.ProductBrand = source.ProductBrand;
        target.CountryOfOrigin = source.CountryOfOrigin;
        target.RegionID = source.RegionID;
        target.CompanyID = source.CompanyID;
        target.CompanyBranchID = source.CompanyBranchID;
        target.CountryID = source.CountryID;
        target.CityID = source.CityID;
        target.CreateDate = source.CreateDate;
        target.LastSaveDate = source.LastSaveDate;
        target.CreatedByUserID = source.CreatedByUserID;
        target.LastSavedByUserID = source.LastSavedByUserID;
        target.IsDeleted = source.IsDeleted;
        // ReloadAfterSave is intentionally NOT copied
    }
}
