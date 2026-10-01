using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace StockPlusPlus.Shared.DTOs.SampleContact;

public abstract class SampleContactFieldsDTO : ShiftEntityViewAndUpsertDTO
{
    [Pii(PiiKind.Name)]
    public PiiFieldDTO? Name { get; set; }

    [Pii(PiiKind.Phone)]
    public PiiFieldDTO? Phone { get; set; }

    [Pii(PiiKind.Email)]
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
}
