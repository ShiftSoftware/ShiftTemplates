using Riok.Mapperly.Abstractions;
using ShiftSoftware.ShiftEntity.Core;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.ProductCategory;

namespace StockPlusPlus.Data.Mappers;

[Mapper]
public partial class ProductCategoryMapperlyMapper : IShiftEntityMapper<ProductCategory, ProductCategoryListDTO, ProductCategoryDTO>
{
    // --- MapToView: Entity → DTO ---

    public ProductCategoryDTO MapToView(ProductCategory entity)
    {
        var dto = MapToViewGenerated(entity);

        dto.Photos = entity.Photos.ToShiftFiles();
        dto.Brand = entity.BrandID.ToSelectDTO();

        return dto.MapBaseFields(entity);
    }

    [MapperIgnoreSource(nameof(ProductCategory.Photos))]
    [MapperIgnoreSource(nameof(ProductCategory.BrandID))]
    [MapperIgnoreSource(nameof(ProductCategory.Products))]
    [MapperIgnoreSource(nameof(ProductCategory.ReloadAfterSave))]
    [MapperIgnoreSource(nameof(ProductCategory.AuditFieldsAreSet))]
    [MapperIgnoreSource(nameof(ProductCategory.RegionID))]
    [MapperIgnoreSource(nameof(ProductCategory.CompanyID))]
    [MapperIgnoreSource(nameof(ProductCategory.CompanyBranchID))]
    [MapperIgnoreSource(nameof(ProductCategory.IdempotencyKey))]
    [MapperIgnoreSource(nameof(ProductCategory.CreatedByUserID))]
    [MapperIgnoreSource(nameof(ProductCategory.LastSavedByUserID))]
    [MapperIgnoreTarget(nameof(ProductCategoryDTO.ID))]
    [MapperIgnoreTarget(nameof(ProductCategoryDTO.IsDeleted))]
    [MapperIgnoreTarget(nameof(ProductCategoryDTO.CreateDate))]
    [MapperIgnoreTarget(nameof(ProductCategoryDTO.LastSaveDate))]
    [MapperIgnoreTarget(nameof(ProductCategoryDTO.CreatedByUserID))]
    [MapperIgnoreTarget(nameof(ProductCategoryDTO.LastSavedByUserID))]
    [MapperIgnoreTarget(nameof(ProductCategoryDTO.Photos))]
    [MapperIgnoreTarget(nameof(ProductCategoryDTO.Brand))]
    private partial ProductCategoryDTO MapToViewGenerated(ProductCategory entity);

    // --- MapToEntity: DTO → Entity ---

    public ProductCategory MapToEntity(ProductCategoryDTO dto, ProductCategory existing)
    {
        MapToEntityGenerated(dto, existing);

        existing.Photos = dto.Photos.ToJsonString();
        existing.BrandID = dto.Brand.ToNullableForeignKey();

        return existing;
    }

    [MapperIgnoreTarget(nameof(ProductCategory.Photos))]
    [MapperIgnoreTarget(nameof(ProductCategory.BrandID))]
    [MapperIgnoreTarget(nameof(ProductCategory.Products))]
    [MapperIgnoreTarget(nameof(ProductCategory.ID))]
    [MapperIgnoreTarget(nameof(ProductCategory.CreateDate))]
    [MapperIgnoreTarget(nameof(ProductCategory.LastSaveDate))]
    [MapperIgnoreTarget(nameof(ProductCategory.IsDeleted))]
    [MapperIgnoreTarget(nameof(ProductCategory.ReloadAfterSave))]
    [MapperIgnoreTarget(nameof(ProductCategory.AuditFieldsAreSet))]
    [MapperIgnoreTarget(nameof(ProductCategory.RegionID))]
    [MapperIgnoreTarget(nameof(ProductCategory.CompanyID))]
    [MapperIgnoreTarget(nameof(ProductCategory.CompanyBranchID))]
    [MapperIgnoreTarget(nameof(ProductCategory.IdempotencyKey))]
    [MapperIgnoreTarget(nameof(ProductCategory.CreatedByUserID))]
    [MapperIgnoreTarget(nameof(ProductCategory.LastSavedByUserID))]
    [MapperIgnoreSource(nameof(ProductCategoryDTO.ID))]
    [MapperIgnoreSource(nameof(ProductCategoryDTO.IsDeleted))]
    [MapperIgnoreSource(nameof(ProductCategoryDTO.CreateDate))]
    [MapperIgnoreSource(nameof(ProductCategoryDTO.LastSaveDate))]
    [MapperIgnoreSource(nameof(ProductCategoryDTO.CreatedByUserID))]
    [MapperIgnoreSource(nameof(ProductCategoryDTO.LastSavedByUserID))]
    [MapperIgnoreSource(nameof(ProductCategoryDTO.Photos))]
    [MapperIgnoreSource(nameof(ProductCategoryDTO.Brand))]
    private partial void MapToEntityGenerated(ProductCategoryDTO dto, ProductCategory existing);

    // --- MapToList: IQueryable projection ---

    public IQueryable<ProductCategoryListDTO> MapToList(IQueryable<ProductCategory> query)
    {
        return query.Select(e => new ProductCategoryListDTO
        {
            ID = e.ID.ToString(),
            Name = e.Name,
            Description = e.Description,
            Code = e.Code,
            TrackingMethod = e.TrackingMethod,
            IsDeleted = e.IsDeleted,
            BrandID = e.BrandID.HasValue ? e.BrandID.Value.ToString() : null,
        });
    }

    // --- CopyEntity ---

    [MapperIgnoreTarget(nameof(ProductCategory.ReloadAfterSave))]
    [MapperIgnoreTarget(nameof(ProductCategory.AuditFieldsAreSet))]
    [MapperIgnoreTarget(nameof(ProductCategory.ID))]
    public partial void CopyEntity(ProductCategory source, ProductCategory target);
}
