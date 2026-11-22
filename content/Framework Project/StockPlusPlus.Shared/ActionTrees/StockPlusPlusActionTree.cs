
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
    public readonly static ReadWriteDeleteAction Vehicle = new("Vehicle");
    public readonly static ReadWriteDeleteAction SampleContacts = new("Sample Contacts");

    public readonly static ReadWriteDeleteAction Country = new("Country");
    public readonly static ReadWriteDeleteAction Tags = new("Tags");

    // A text action: the warehouse codes a user works in, comma-separated ("*" is every warehouse). A user's trees are
    // combined as a union, and the reverse lookup finds whoever holds every code searched for.
    public readonly static TextAction Warehouses = new("Warehouses", TextCombine.UnionList(","), "Warehouse codes, comma-separated (* for all)", maximumAccess: "*");

    [ActionTree("Data Level Access", "Data Level or Row-Level Access")]
    public class DataLevelAccess
    {
        public readonly static DynamicReadWriteDeleteAction ProductBrand = new("Brand");
        public readonly static DynamicReadWriteDeleteAction ProductCategory = new("Product Category");
    }
#endif
}
