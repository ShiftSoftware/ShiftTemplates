using System.ComponentModel.DataAnnotations;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Core.Phones;

namespace StockPlusPlus.Shared.DTOs.SampleContact;

public class SampleContactPhoneDTO : ShiftEntityDTOBase
{
    public override string? ID { get; set; }
    public string Label { get; set; } = "Phone";
    [Pii(PiiKind.Phone)]
    [Required(ErrorMessage = "Additional phone is required.")]
    [StringLength(40, ErrorMessage = "Additional phone must be at most 40 characters.")]
    [ValidPhoneNumber]
    public PiiFieldDTO? Number { get; set; }
}
