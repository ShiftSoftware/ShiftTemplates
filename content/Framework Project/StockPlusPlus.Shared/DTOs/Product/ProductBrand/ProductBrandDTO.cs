﻿
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations;

namespace StockPlusPlus.Shared.DTOs.Product.ProductBrand;

public class ProductBrandDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }

    [Required]
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Code { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.TeamHashIdConverter]
    public ShiftEntitySelectDTO? Team { get; set; }
}
