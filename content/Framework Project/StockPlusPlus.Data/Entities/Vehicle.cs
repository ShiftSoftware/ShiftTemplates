using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Flags;

namespace StockPlusPlus.Data.Entities;

/// <summary>
/// A two-leg data-level-access sample. A vehicle belongs to its owning company, but an intermediary company may
/// also need access while it handles the vehicle. The repository declares both columns as one Companies dimension.
/// </summary>
[TemporalShiftEntity]
[ShiftEntityKeyAndName(nameof(ID), nameof(VIN))]
[Index(nameof(CompanyID))]
[Index(nameof(IntermediaryCompanyID))]
public class Vehicle : ShiftEntity<Vehicle>, IEntityHasCompany<Vehicle>
{
    public string VIN { get; set; } = default!;

    public long? CompanyID { get; set; }

    public long? IntermediaryCompanyID { get; set; }
}
