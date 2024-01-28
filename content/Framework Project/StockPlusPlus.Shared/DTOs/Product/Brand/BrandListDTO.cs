
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Text.Json.Serialization;

namespace StockPlusPlus.Shared.DTOs.Product.Brand;

[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class BrandListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Code { get; set; }
}
