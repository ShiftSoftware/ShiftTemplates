using Mapster;
using ShiftSoftware.ShiftEntity.Core;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.ProductCategory;

namespace StockPlusPlus.Data.Mappers;

public class ProductCategoryMapsterMapper : IShiftEntityMapper<ProductCategory, ProductCategoryListDTO, ProductCategoryDTO>
{
    private static readonly TypeAdapterConfig _config = BuildConfig();

    private static TypeAdapterConfig BuildConfig()
    {
        var config = MapsterShiftEntityDefaults.CreateConfig();

        config.EntityToView<ProductCategory, ProductCategoryDTO>();
        config.ViewToEntity<ProductCategoryDTO, ProductCategory>();
        config.EntityToList<ProductCategory, ProductCategoryListDTO>();
        config.EntityCopy<ProductCategory>();

        return config;
    }

    public ProductCategoryDTO MapToView(ProductCategory entity)
    {
        return entity.Adapt<ProductCategoryDTO>(_config).MapBaseFields(entity);
    }

    public ProductCategory MapToEntity(ProductCategoryDTO dto, ProductCategory existing)
    {
        dto.Adapt(existing, _config);
        return existing;
    }

    public IQueryable<ProductCategoryListDTO> MapToList(IQueryable<ProductCategory> query)
    {
        return query.ProjectToType<ProductCategoryListDTO>(_config);
    }

    public void CopyEntity(ProductCategory source, ProductCategory target)
    {
        source.Adapt(target, _config);
    }
}
