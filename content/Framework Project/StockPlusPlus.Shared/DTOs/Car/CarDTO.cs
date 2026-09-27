using System.ComponentModel.DataAnnotations;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace StockPlusPlus.Shared.DTOs.Car;

public class CarDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }

    [Required]
    public string Make { get; set; } = default!;

    [Required]
    public string Model { get; set; } = default!;

    public int Year { get; set; }
}
