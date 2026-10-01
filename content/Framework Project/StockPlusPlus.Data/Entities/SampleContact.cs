using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.SampleContact;

namespace StockPlusPlus.Data.Entities;

// Synthetic contacts demonstrate opt-in on independent list and detail DTOs.
[ShiftEntityKeyAndName(nameof(ID), nameof(Label))]
[ShiftEntitySecureEndpoint<SampleContactListDTO, SampleContactDTO, StockPlusPlusActionTree>(
    "api/sample-contact", nameof(StockPlusPlusActionTree.SampleContacts))]
public class SampleContact : ShiftEntity<SampleContact>
{
    public string Label { get; set; } = default!;
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Identifier { get; set; }
}
