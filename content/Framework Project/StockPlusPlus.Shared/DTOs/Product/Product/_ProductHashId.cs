
using ShiftSoftware.ShiftEntity.Model.HashIds;

namespace StockPlusPlus.Shared.DTOs.Product.Product;

public class _ProductHashId : JsonHashIdConverterAttribute<_ProductHashId>
{
    public _ProductHashId() : base(5) { }
}
