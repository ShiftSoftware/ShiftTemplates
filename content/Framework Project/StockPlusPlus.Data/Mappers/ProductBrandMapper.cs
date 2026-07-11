using ShiftSoftware.ShiftEntity.Core;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.ProductBrand;

namespace StockPlusPlus.Data.Mappers;

/// <summary>
/// ProductBrand demonstrates the [ShiftEntityMapper] PARTIAL-CLASS form of source generation (the
/// customization path): declaring this class makes the generator fill the four IShiftEntityMapper
/// methods into it — instead of emitting an auto-named mapper for the triple — and register THIS class
/// in the registry. Implement any of the four methods (or add [MapperIgnore]-style tweaks in future
/// iterations) to customize; the generator skips user-implemented methods. Entities with nothing custom
/// need no class at all — see Country (api/country-generated + CountryRepository).
/// </summary>
[ShiftEntityMapper]
public partial class ProductBrandMapper : IShiftEntityMapper<ProductBrand, ProductBrandListDTO, ProductBrandDTO>
{
    // Per-property customization hook: registering a member automatically suppresses the generated
    // convention for it (everything else stays generated). ForView/ForEntity/ForCopy take plain
    // lambdas (optionally with an IServiceProvider); ForList takes an expression over the entity,
    // composed into the single SQL projection.
    partial void Configure(ShiftMapperBuilder<ProductBrand, ProductBrandListDTO, ProductBrandDTO> map)
    {
        map.ForList(d => d.Code, entity => entity.Code ?? "(No Code)");
    }
}
