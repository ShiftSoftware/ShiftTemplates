
using ShiftSoftware.ShiftEntity.Model.HashIds;

namespace StockPlusPlus.Shared.DTOs.ProductCategory;

public class _ProductCategoryHashId : JsonHashIdConverterAttribute<_ProductCategoryHashId>
{
    public _ProductCategoryHashId() : base(5) { }
}
