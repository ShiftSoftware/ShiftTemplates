using ShiftSoftware.ShiftEntity.Core;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.ProductCategory;
using static ShiftSoftware.ShiftEntity.Core.MappingHelpers;

namespace StockPlusPlus.Data.Mappers;

public class ProductCategoryMapper : IShiftEntityMapper<ProductCategory, ProductCategoryListDTO, ProductCategoryDTO>
{
    public ProductCategoryDTO MapToView(ProductCategory entity)
    {
        return entity.MapBaseFieldsToView(new ProductCategoryDTO
        {
            Name = entity.Name,
            Description = entity.Description,
            Code = entity.Code,
            TrackingMethod = entity.TrackingMethod ?? 0,

            // JSON string → List<ShiftFileDTO>
            Photos = entity.Photos.ToShiftFiles(),

            // FK → ShiftEntitySelectDTO
            Brand = ToSelectDTO(entity.BrandID),
        });
    }

    public ProductCategory MapToEntity(ProductCategoryDTO dto, ProductCategory existing)
    {
        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.Code = dto.Code;
        existing.TrackingMethod = dto.TrackingMethod;

        // List<ShiftFileDTO> → JSON string
        existing.Photos = dto.Photos.ToJsonString();

        // ShiftEntitySelectDTO → FK
        existing.BrandID = dto.Brand.ToNullableForeignKey();

        return existing;
    }

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

    public void CopyEntity(ProductCategory source, ProductCategory target)
    {
        source.ShallowCopyTo(target);
    }
}
