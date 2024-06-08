using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model;

namespace StockPlusPlus.Data.Entities;

[TemporalShiftEntity]
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class Country : ShiftEntity<Country>, IEntityHasIdempotencyKey<Country>
{
    public string Name { get; set; } = default!;
    public Guid? IdempotencyKey { get; set; }
}
