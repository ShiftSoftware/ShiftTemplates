using FluentValidation;
using ShiftSoftware.ShiftEntity.Core.Attention;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using StockPlusPlus.Shared.DTOs.Product;

namespace StockPlusPlus.Shared.DTOs.Invoice;

public class InvoiceDTO : ShiftEntityViewAndUpsertDTO, IHasAttentionSignals
{
    public override string? ID { get; set; }
    public string ManualReference { get; set; } = default!;
    public DateTimeOffset? InvoiceDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public DateTimeOffset? ReleaseDate { get; set; }
    public long InvoiceNo { get; set; }
    [CompanyHashIdConverter]
    public string? CompanyID { get; set; }

    public List<InvoiceLineDTO> InvoiceLines { get; set; } = new List<InvoiceLineDTO>();
}

public class InvoiceListDTO : ShiftEntityListDTO, IHasAttentionSummary
{
    public override string? ID { get; set; }
    public string ManualReference { get; set; } = default!;
    public DateTimeOffset? InvoiceDate { get; set; }
    public long InvoiceNo { get; set; }
    public bool HasActiveAttention { get; set; }
    public AttentionSeverity? HighestSeverity { get; set; }
    public int ActiveSignalCount { get; set; }

    // DEEP LIST mapping: unlike MapToView, the list projection does NOT compose children automatically.
    // It is populated explicitly and directionally by InvoiceRepository's ForList (list → lines → product).
    // See InvoiceRepository.ListLinesProjection.
    public List<InvoiceLineListDTO> InvoiceLines { get; set; } = new List<InvoiceLineListDTO>();
}

// The nested line shape carried inside the invoice LIST row. Its Product is a CUSTOM complex DTO
// (NOT a ShiftEntitySelectDTO). The generated list projection composes it recursively — list → lines →
// product — purely from the ForListChildren call in InvoiceRepository (no partial, no attribute).
public class InvoiceLineListDTO
{
    public string? ID { get; set; }
    public string Description { get; set; } = default!;
    public decimal Price { get; set; }

    public InvoiceLineProductListDTO Product { get; set; } = default!;
}

// A CUSTOM product DTO (not ShiftEntitySelectDTO) on the invoice LIST line: id + name + one more of the
// product's own columns. Composed from the InvoiceLine.Product navigation by the generated list projection,
// SQL-translatable: e.Product == null ? null : new InvoiceLineProductListDTO { ID = ..., Name = ..., ... }.
public class InvoiceLineProductListDTO
{
    [_ProductHashId]
    public string? ID { get; set; }
    public string Name { get; set; } = default!;
    public int? Price { get; set; }
}

public class InvoiceLineDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }
    [_ProductHashId]
    public ShiftEntitySelectDTO Product { get; set; } = default!;

    public string Description { get; set; } = default!;

    public decimal Price { get; set; }
}

public class InvoiceValidator : AbstractValidator<InvoiceDTO>
{
    public InvoiceValidator()
    {
        RuleFor(x => x.InvoiceDate)
            .NotNull();

        RuleFor(x => x.InvoiceLines)
            .NotEmpty();

        RuleForEach(x => x.InvoiceLines)
            .ChildRules(x =>
            {
                x.RuleFor(x => x.Product)
                .NotEmpty();

                x.RuleFor(x => x.Description)
                .NotEmpty();

                x.RuleFor(x => x.Price)
                .GreaterThan(0);
            });
    }
}