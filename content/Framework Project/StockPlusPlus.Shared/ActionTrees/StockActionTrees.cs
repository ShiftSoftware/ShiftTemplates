
using ShiftSoftware.TypeAuth.Core;
using ShiftSoftware.TypeAuth.Core.Actions;

namespace StockPlusPlus.Shared.ActionTrees;

[ActionTree("Stock", "Stock")]
public class StockActionTrees
{
    public readonly static ReadWriteDeleteAction Brand = new("Brand");
    public readonly static ReadWriteDeleteAction ProductCategory = new("Product Category");
    public readonly static ReadWriteDeleteAction Product = new("Product");

    public readonly static ReadWriteDeleteAction Country = new("Country");

    [ActionTree("Data Level Access", "Data Level or Row-Level Access")]
    public class DataLevelAccess
    {
        public readonly static DynamicReadWriteDeleteAction Brand = new("Brand"); 
        public readonly static DynamicReadWriteDeleteAction ProductCategory = new("Product Category"); 
    }
}