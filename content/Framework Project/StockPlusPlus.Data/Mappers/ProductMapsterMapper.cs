using Mapster;
using ShiftSoftware.ShiftEntity.Core;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Product;

namespace StockPlusPlus.Data.Mappers;

public class ProductMapsterMapper : IShiftEntityMapper<Product, ProductListDTO, ProductDTO>
{
    private static readonly TypeAdapterConfig _config = BuildConfig();

    private static TypeAdapterConfig BuildConfig()
    {
        var config = MapsterShiftEntityDefaults.CreateConfig();

        config.EntityToView<Product, ProductDTO>();

        config.ViewToEntity<ProductDTO, Product>()
            .Map(d => d.IsDraft, s => s.IsDraft ?? false);

        // Only "Category" needs explicit mapping (name mismatch with ProductCategory.Name).
        // Everything else — ID, ProductBrand, ProductCategoryID, etc. — handled by convention.
        config.EntityToList<Product, ProductListDTO>()
            //.Map(d => d.ProductBrand, s => s.ProductBrand != null ? s.ProductBrand : null)
            .Map(d => d.Category, s => s.ProductCategory != null ? s.ProductCategory.Name : null);

        config.EntityCopy<Product>();

        return config;
    }

    public ProductDTO MapToView(Product entity)
    {
        return entity.Adapt<ProductDTO>(_config).MapBaseFields(entity);
    }

    public Product MapToEntity(ProductDTO dto, Product existing)
    {
        dto.Adapt(existing, _config);
        return existing;
    }

    public IQueryable<ProductListDTO> MapToList(IQueryable<Product> query)
    {
        return query.ProjectToType<ProductListDTO>(_config);
    }

    public void CopyEntity(Product source, Product target)
    {
        source.Adapt(target, _config);
    }
}
