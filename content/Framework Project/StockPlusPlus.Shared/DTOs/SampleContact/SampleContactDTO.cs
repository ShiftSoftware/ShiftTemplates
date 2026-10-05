using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Core.Phones;
using System.ComponentModel.DataAnnotations;

namespace StockPlusPlus.Shared.DTOs.SampleContact;

public abstract class SampleContactFieldsDTO : ShiftEntityViewAndUpsertDTO
{
    [Pii(PiiKind.Name)]
    public PiiFieldDTO? Name { get; set; }

    [Pii(PiiKind.Phone)]
    [Required(ErrorMessage = "Phone is required.")]
    [StringLength(40, ErrorMessage = "Phone must be at most 40 characters.")]
    [ValidPhoneNumber]
    public PiiFieldDTO? Phone { get; set; }

    [Pii(PiiKind.Email)]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    public PiiFieldDTO? Email { get; set; }

    [Pii(PiiKind.Address)]
    public PiiFieldDTO? Address { get; set; }

    [Pii(PiiKind.Identifier, Revealable = false)]
    public PiiFieldDTO? Identifier { get; set; }
}

public class SampleContactDTO : SampleContactFieldsDTO
{
    public override string? ID { get; set; }
    public string Label { get; set; } = default!;
    public List<SampleContactPhoneDTO> Phones { get; set; } = new();
}
