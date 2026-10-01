using ShiftSoftware.ShiftEntity.Core;

namespace StockPlusPlus.Data.Entities;

// Fake additional phone records demonstrate protected collection saves.
public class SampleContactPhone : ShiftEntity<SampleContactPhone>
{
    public long SampleContactID { get; set; }
    public SampleContact SampleContact { get; set; } = default!;
    public string? Number { get; set; }
    public string Label { get; set; } = "Phone";
}
