using ShiftSoftware.ShiftEntity.Core;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.ProductBrand;

namespace StockPlusPlus.Data.Mappers;

/// <summary>
/// ProductBrand demonstrates SOURCE-GENERATED mapping: declare this partial class and the
/// ShiftEntity source generator fills in the four IShiftEntityMapper methods by convention
/// (scalars by name, FK ↔ ShiftEntitySelectDTO, audit fields via MapBaseFields, inline
/// SQL-translatable list projection, generated property-by-property CopyEntity). Implement
/// any of the methods here to take it over — the generator skips user-implemented methods.
/// </summary>
[ShiftEntityMapper]
public partial class ProductBrandMapper : IShiftEntityMapper<ProductBrand, ProductBrandListDTO, ProductBrandDTO>
{
}
