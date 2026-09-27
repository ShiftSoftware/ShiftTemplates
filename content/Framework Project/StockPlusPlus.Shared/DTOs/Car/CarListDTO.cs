using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace StockPlusPlus.Shared.DTOs.Car;

[ShiftEntityKeyAndName(nameof(ID), nameof(Make))]
public class CarListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }

    public string Make { get; set; } = default!;

    public string Model { get; set; } = default!;

    public int Year { get; set; }
}
