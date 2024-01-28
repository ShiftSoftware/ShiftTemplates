
using System.ComponentModel;

namespace StockPlusPlus.Shared.Enums.Product;

public enum TrackingMethod
{
    [Description("Serial")]
    Serial = 1,

    [Description("Batch/LOT")]
    Batch_LOT = 2,

    [Description("Weight/Volume")]
    Weight_Volume = 3,

    [Description("Kitting/Assembly")]
    Kitting_Assembly = 4,

    [Description("No Tracking")]
    NoTracking = 5,
}
