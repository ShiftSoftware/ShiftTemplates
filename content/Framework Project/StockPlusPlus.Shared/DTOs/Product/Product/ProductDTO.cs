
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Shared.DTOs.Product.ProductCategory;
using StockPlusPlus.Shared.Enums.Product;
using System.Text.Json.Serialization;

namespace StockPlusPlus.Shared.DTOs.Product.Product;

public class ProductDTO : ShiftEntityViewAndUpsertDTO
{
    [_ProductHashId]
    public override string? ID { get; set; }
    public string Name { get; set; } = default!;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TrackingMethod TrackingMethod { get; set; }

    [_ProductCategoryHashId]
    public ShiftEntitySelectDTO ProductCategory { get; set; } = default!;

    public ShiftEntitySelectDTO ProductBrand { get; set; } = default!;
    public DateTimeOffset? ReleaseDate { get; set; }

    public ShiftEntitySelectDTO? CountryOfOrigin { get; set; } = default!;
}
