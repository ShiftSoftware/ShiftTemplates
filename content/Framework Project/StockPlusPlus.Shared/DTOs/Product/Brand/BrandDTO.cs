﻿
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace StockPlusPlus.Shared.DTOs.Product.Brand;

public class BrandDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }

    [Required]
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Code { get; set; }
}
