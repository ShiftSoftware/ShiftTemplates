using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace StockPlusPlus.Shared.DTOs.SampleContact;

public class SampleContactListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
    public string Label { get; set; } = default!;

    [Pii(PiiKind.Name)]
    public PiiFieldDTO? Name { get; set; }

    [Pii(PiiKind.Phone)]
    public PiiFieldDTO? Phone { get; set; }
}
