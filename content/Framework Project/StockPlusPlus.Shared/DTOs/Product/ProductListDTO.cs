
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using ShiftSoftware.ShiftIdentity.Core.DTOs.City;
using StockPlusPlus.Shared.DTOs.ProductCategory;
using StockPlusPlus.Shared.Enums;
using System.Text.Json.Serialization;

namespace StockPlusPlus.Shared.DTOs.Product;

[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
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

    [CityHashIdConverter]
    public string? CityID { get; set; }
    public CityListDTO? City { get; set; }
    public string? CustomID
    {
        get
        {
            if (this.City is null)
                return null;

            return $"{this.ID}-{this.CityID}-{this.City.Name}";
        }
    }
}
