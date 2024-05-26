

using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Shared.Enums.Product;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace StockPlusPlus.Shared.DTOs.ProductCategory;

public class ProductCategoryDTO : ShiftEntityViewAndUpsertDTO, IHasBrandSelection<ProductCategoryDTO>
{
    [_ProductCategoryHashId]
    public override string? ID { get; set; }

    [Required]
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Code { get; set; }
    public List<ShiftFileDTO>? Photos { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [Range(1, int.MaxValue, ErrorMessage = "Required")]
    public TrackingMethod TrackingMethod { get; set; }


    [ShiftSoftware.ShiftEntity.Model.HashIds.BrandHashIdConverter]
    [Required]
    public ShiftEntitySelectDTO? Brand { get; set; }
}
