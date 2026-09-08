using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.HashIds;

namespace StockPlusPlus.Shared.DTOs.Vehicle;

[ShiftEntityKeyAndName(nameof(ID), nameof(VIN))]
public class VehicleListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }

    public string VIN { get; set; } = default!;

    [CompanyHashIdConverter]
    public string? CompanyID { get; set; }

    [CompanyHashIdConverter]
    public string? IntermediaryCompanyID { get; set; }
}
