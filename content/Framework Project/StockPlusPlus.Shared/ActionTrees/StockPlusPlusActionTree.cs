
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace StockPlusPlus.Shared.ActionTrees;

[ActionTree("StockPlusPlus Actions", "StockPlusPlus Actions")]
public class StockPlusPlusActionTree
{
#if (includeSampleApp)
    public readonly static ReadWriteDeleteAction ProductBrand = new("Brand");
    public readonly static ReadWriteDeleteAction ProductCategory = new("Product Category");
    public readonly static ReadWriteDeleteAction Product = new("Product");
    public readonly static ReadWriteDeleteAction Invoice = new("Invoice");

    public readonly static ReadWriteDeleteAction Country = new("Country");

    public readonly static DecimalAction MaxTop = new("Max Top", null, 5, int.MaxValue);

    [ActionTree("Data Level Access", "Data Level or Row-Level Access")]
    public class DataLevelAccess
    {
        public readonly static DynamicReadWriteDeleteAction ProductBrand = new("Brand");
        public readonly static DynamicReadWriteDeleteAction ProductCategory = new("Product Category");
    }
#endif
}