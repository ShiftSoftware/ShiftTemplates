
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.Text.Json.Serialization;

namespace StockPlusPlus.Shared.DTOs.Product.ProductBrand;

[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class ProductBrandListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Code { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.UserHashIdConverter]
    public string? CreatedByUserID { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.UserGroupHashIdConverter]
    public string? UserGroupID { get; set; }
}
