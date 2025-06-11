using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Shared.Enums;
using System.Text.Json.Serialization;

namespace StockPlusPlus.Shared.DTOs.ProductCategory;

[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class ProductCategoryListDTO : ShiftEntityListDTO, IHasBrandForeignColumn<ProductCategoryListDTO>
{
    [_ProductCategoryHashId]
    public override string? ID { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Code { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TrackingMethod? TrackingMethod { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    public string? BrandID { get; set; }

    //public List<Product.ProductListDTO> Products { get; set; } = new();
}
