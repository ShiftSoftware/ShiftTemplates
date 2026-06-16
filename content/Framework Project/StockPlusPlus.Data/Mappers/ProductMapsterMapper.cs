using Mapster;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Tagging;
using ShiftSoftware.ShiftEntity.Model.Dtos.Tagging;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Product;

namespace StockPlusPlus.Data.Mappers;

public class ProductMapsterMapper : IShiftEntityMapper<Product, ProductListDTO, ProductDTO>
{
    private static readonly TypeAdapterConfig _config = BuildConfig();

    private static TypeAdapterConfig BuildConfig()
    {
        var config = MapsterShiftEntityDefaults.CreateConfig();

        // Tag → TagDTO projection — used by the LIST (EntityToList projects the Tags collection).
        config.NewConfig<Tag, TagDTO>()
            .Map(d => d.ID, s => s.ID.ToString());

        // For the single-entity VIEW, the framework auto-includes + auto-maps Tags, so ignore
        // them here (and on write/copy) to leave the navigation to the framework.
        config.EntityToView<Product, ProductDTO>()
            .Ignore(d => d.Tags);

        config.ViewToEntity<ProductDTO, Product>()
            .Ignore(d => d.HasActiveAttention, d => d.HighestSeverity!, d => d.ActiveSignalCount)
            .Ignore(d => d.Tags)
            .Map(d => d.IsDraft, s => s.IsDraft ?? false);

        // Only "Category" needs explicit mapping (name mismatch with ProductCategory.Name).
        // Tags are projected by convention via the Tag → TagDTO config above.
        config.EntityToList<Product, ProductListDTO>()
            //.Map(d => d.ProductBrand, s => s.ProductBrand != null ? s.ProductBrand : null)
            .Map(d => d.Category, s => s.ProductCategory != null ? s.ProductCategory.Name : null);

        config.EntityCopy<Product>()
            .Ignore(d => d.Tags);

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
