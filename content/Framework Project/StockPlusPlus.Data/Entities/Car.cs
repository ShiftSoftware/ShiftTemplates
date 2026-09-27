using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model;

namespace StockPlusPlus.Data.Entities;

/// <summary>
/// A plain catalog entity gated by its own TypeAuth action (<c>StockPlusPlusActionTree.Car</c>): list and view
/// need Read, insert and update need Write, delete needs Delete — enforced by the API, not just the UI.
/// </summary>
[TemporalShiftEntity]
[ShiftEntityKeyAndName(nameof(ID), nameof(Make))]
public class Car : ShiftEntity<Car>
{
    public string Make { get; set; } = default!;

    public string Model { get; set; } = default!;

    public int Year { get; set; }
}
