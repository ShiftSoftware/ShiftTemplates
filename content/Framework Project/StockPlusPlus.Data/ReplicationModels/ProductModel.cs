using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Shared.Enums;

namespace StockPlusPlus.Data.ReplicationModels;

public class ProductModel : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }

    public string Name { get; set; } = default!;

    [JsonConverter(typeof(StringEnumConverter))]
    public TrackingMethod TrackingMethod { get; set; }

    public long ProductCategoryID { get; set; }

    public long BrandID { get; set; }
}
