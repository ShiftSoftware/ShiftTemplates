
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Shared.DTOs.ProductCategory;
using StockPlusPlus.Shared.Enums;
using System.Text.Json.Serialization;

namespace StockPlusPlus.Shared.DTOs.Product;

public class ProductListDTO : ShiftEntityListDTO, IHasDraftColumn<ProductListDTO>
{
    [_ProductHashId]
    public override string? ID { get; set; }
    public string Name { get; set; } = default!;

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string? ProductBrand { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string? Category { get; set; }

    [_ProductCategoryHashId]
    public string? ProductCategoryID { get; set; }

    public string? ProductBrandID { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TrackingMethod TrackingMethod { get; set; }
    public DateTimeOffset ReleaseDate { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
    public bool IsDraft { get; set; }
}
