
using ShiftSoftware.ShiftEntity.Model.Dtos;
#if (includeItemTemplateContent)
using System.ComponentModel.DataAnnotations;
#endif

namespace StockPlusPlus.Shared.DTOs.ProductBrand;

public class ProductBrandDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }
#if (includeItemTemplateContent)
    [Required]
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Code { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.TeamHashIdConverter]
    public ShiftEntitySelectDTO? Team { get; set; }
#endif
}