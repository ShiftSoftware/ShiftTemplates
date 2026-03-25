using Riok.Mapperly.Abstractions;
using ShiftSoftware.ShiftEntity.Core;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Product;

namespace StockPlusPlus.Data.Mappers;

[Mapper]
public partial class ProductMapperlyMapper : IShiftEntityMapper<Product, ProductListDTO, ProductDTO>
{
    // --- MapToView: Entity → DTO ---
    // Mapperly generates scalar property mapping. We handle FK→SelectDTO and base fields manually
    // because SelectDTO.Text requires the navigation property (e.g., ProductBrand?.Name).

    public ProductDTO MapToView(Product entity)
    {
        var dto = MapToViewGenerated(entity);

        dto.ProductCategory = entity.ProductCategoryID.ToSelectDTO(entity.ProductCategory?.Name);
        dto.ProductBrand = entity.ProductBrandID.ToSelectDTO(entity.ProductBrand?.Name);
        dto.CountryOfOrigin = entity.CountryOfOriginID.ToSelectDTO(entity.CountryOfOrigin?.Name);

        return dto.MapBaseFields(entity);
    }

    [MapperIgnoreSource(nameof(Product.ProductCategory))]
    [MapperIgnoreSource(nameof(Product.ProductBrand))]
    [MapperIgnoreSource(nameof(Product.CountryOfOrigin))]
    [MapperIgnoreSource(nameof(Product.ProductCategoryID))]
    [MapperIgnoreSource(nameof(Product.ProductBrandID))]
    [MapperIgnoreSource(nameof(Product.CountryOfOriginID))]
    [MapperIgnoreSource(nameof(Product.ReloadAfterSave))]
    [MapperIgnoreSource(nameof(Product.AuditFieldsAreSet))]
    [MapperIgnoreSource(nameof(Product.RegionID))]
    [MapperIgnoreSource(nameof(Product.CompanyID))]
    [MapperIgnoreSource(nameof(Product.CompanyBranchID))]
    [MapperIgnoreSource(nameof(Product.IdempotencyKey))]
    [MapperIgnoreSource(nameof(Product.CityID))]
    [MapperIgnoreSource(nameof(Product.CountryID))]
    [MapperIgnoreSource(nameof(Product.CreatedByUserID))]
    [MapperIgnoreSource(nameof(Product.LastSavedByUserID))]
    [MapperIgnoreTarget(nameof(ProductDTO.ID))]
    [MapperIgnoreTarget(nameof(ProductDTO.IsDeleted))]
    [MapperIgnoreTarget(nameof(ProductDTO.CreateDate))]
    [MapperIgnoreTarget(nameof(ProductDTO.LastSaveDate))]
    [MapperIgnoreTarget(nameof(ProductDTO.CreatedByUserID))]
    [MapperIgnoreTarget(nameof(ProductDTO.LastSavedByUserID))]
    [MapperIgnoreTarget(nameof(ProductDTO.ProductCategory))]
    [MapperIgnoreTarget(nameof(ProductDTO.ProductBrand))]
    [MapperIgnoreTarget(nameof(ProductDTO.CountryOfOrigin))]
    private partial ProductDTO MapToViewGenerated(Product entity);

    // --- MapToEntity: DTO → Entity ---

    public Product MapToEntity(ProductDTO dto, Product existing)
    {
        MapToEntityGenerated(dto, existing);

        existing.ProductCategoryID = dto.ProductCategory.ToForeignKey();
        existing.ProductBrandID = dto.ProductBrand.ToForeignKey();
        existing.CountryOfOriginID = dto.CountryOfOrigin.ToNullableForeignKey();

        return existing;
    }

    [MapperIgnoreTarget(nameof(Product.ProductCategory))]
    [MapperIgnoreTarget(nameof(Product.ProductBrand))]
    [MapperIgnoreTarget(nameof(Product.CountryOfOrigin))]
    [MapperIgnoreTarget(nameof(Product.ProductCategoryID))]
    [MapperIgnoreTarget(nameof(Product.ProductBrandID))]
    [MapperIgnoreTarget(nameof(Product.CountryOfOriginID))]
    [MapperIgnoreTarget(nameof(Product.ID))]
    [MapperIgnoreTarget(nameof(Product.CreateDate))]
    [MapperIgnoreTarget(nameof(Product.LastSaveDate))]
    [MapperIgnoreTarget(nameof(Product.IsDeleted))]
    [MapperIgnoreTarget(nameof(Product.ReloadAfterSave))]
    [MapperIgnoreTarget(nameof(Product.AuditFieldsAreSet))]
    [MapperIgnoreTarget(nameof(Product.RegionID))]
    [MapperIgnoreTarget(nameof(Product.CompanyID))]
    [MapperIgnoreTarget(nameof(Product.CompanyBranchID))]
    [MapperIgnoreTarget(nameof(Product.IdempotencyKey))]
    [MapperIgnoreTarget(nameof(Product.CityID))]
    [MapperIgnoreTarget(nameof(Product.CountryID))]
    [MapperIgnoreTarget(nameof(Product.CreatedByUserID))]
    [MapperIgnoreTarget(nameof(Product.LastSavedByUserID))]
    [MapperIgnoreSource(nameof(ProductDTO.ID))]
    [MapperIgnoreSource(nameof(ProductDTO.IsDeleted))]
    [MapperIgnoreSource(nameof(ProductDTO.CreateDate))]
    [MapperIgnoreSource(nameof(ProductDTO.LastSaveDate))]
    [MapperIgnoreSource(nameof(ProductDTO.CreatedByUserID))]
    [MapperIgnoreSource(nameof(ProductDTO.LastSavedByUserID))]
    [MapperIgnoreSource(nameof(ProductDTO.ProductCategory))]
    [MapperIgnoreSource(nameof(ProductDTO.ProductBrand))]
    [MapperIgnoreSource(nameof(ProductDTO.CountryOfOrigin))]
    private partial void MapToEntityGenerated(ProductDTO dto, Product existing);

    // --- MapToList: IQueryable projection ---
    // Manual Select() because Mapperly can't project navigation properties to strings in SQL.

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
            ProductBrandName = p.ProductBrand != null ? p.ProductBrand.Name : null,
            Category = p.ProductCategory != null ? p.ProductCategory.Name : null,
            ProductCategoryID = p.ProductCategoryID.ToString(),
            ProductBrandID = p.ProductBrandID.ToString(),
            CityID = p.CityID.HasValue ? p.CityID.Value.ToString() : null,
        });
    }

    // --- CopyEntity ---

    [MapperIgnoreTarget(nameof(Product.ReloadAfterSave))]
    [MapperIgnoreTarget(nameof(Product.AuditFieldsAreSet))]
    [MapperIgnoreTarget(nameof(Product.ID))]
    public partial void CopyEntity(Product source, Product target);
}
