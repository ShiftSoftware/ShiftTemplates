
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.Dtos.Tagging;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using ShiftSoftware.ShiftIdentity.Core.DTOs.City;
using StockPlusPlus.Shared.DTOs.ProductCategory;
using StockPlusPlus.Shared.Enums;
using System.Text.Json.Serialization;

namespace StockPlusPlus.Shared.DTOs.Product;

[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class ProductListDTO : ShiftEntityListDTO, IHasDraftColumn<ProductListDTO>, IHasAttentionSummary, IShiftEntityTaggableDTO
{
    [_ProductHashId]
    public override string? ID { get; set; }
    public string Name { get; set; } = default!;

    public int? Price { get; set; }
    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string? ProductBrandName { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string? Category { get; set; }

    [_ProductCategoryHashId]
    public string? ProductCategoryID { get; set; }

    public string? ProductBrandID { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TrackingMethod TrackingMethod { get; set; }
    public DateTimeOffset? ReleaseDate { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
    public bool IsDraft { get; set; }

    [CityHashIdConverter]
    public string? CityID { get; set; }
    public CityListDTO? City { get; set; }
    public bool HasActiveAttention { get; set; }
    public int? HighestSeverity { get; set; }
    public int ActiveSignalCount { get; set; }

    // Implementing IShiftEntityTaggableDTO makes ShiftList auto-render a read-only Tags column.
    // Populated by the mapper's MapToList projection (list responses are projections, so —
    // unlike the single-entity view — tags must be projected here, not auto-mapped post-hoc).
    public List<TagDTO> Tags { get; set; } = new();

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
