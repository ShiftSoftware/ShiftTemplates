
using ShiftSoftware.ShiftEntity.Model.Dtos;
#if (includeItemTemplateContent)
using System.ComponentModel.DataAnnotations;
#endif
#if (taggable)
using ShiftSoftware.ShiftEntity.Model.Dtos.Tagging;
#endif

namespace StockPlusPlus.Shared.DTOs.ProductBrand;

public class ProductBrandDTO : ShiftEntityViewAndUpsertDTO
#if (taggable)
    , IShiftEntityTaggableDTO
#endif
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
#if (taggable)
    public List<TagDTO> Tags { get; set; } = new();
#endif
}