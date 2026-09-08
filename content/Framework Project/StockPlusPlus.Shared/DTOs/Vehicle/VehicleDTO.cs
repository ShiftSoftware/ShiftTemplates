using System.ComponentModel.DataAnnotations;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.HashIds;

namespace StockPlusPlus.Shared.DTOs.Vehicle;

public class VehicleDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }

    [Required]
    public string VIN { get; set; } = default!;

    [CompanyHashIdConverter]
    public ShiftEntitySelectDTO? Company { get; set; }

    [CompanyHashIdConverter]
    public ShiftEntitySelectDTO? IntermediaryCompany { get; set; }
}
