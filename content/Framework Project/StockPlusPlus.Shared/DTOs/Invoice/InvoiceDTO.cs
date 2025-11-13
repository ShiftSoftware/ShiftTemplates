using FluentValidation;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using StockPlusPlus.Shared.DTOs.Product;

namespace StockPlusPlus.Shared.DTOs.Invoice;

public class InvoiceDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }
    public string ManualReference { get; set; } = default!;
    public DateTimeOffset? InvoiceDate { get; set; }
    public long InvoiceNo { get; set; }
    [CompanyHashIdConverter]
    public string? CompanyID { get; set; }

    public List<InvoiceLineDTO> InvoiceLines { get; set; } = new List<InvoiceLineDTO>();
}

public class InvoiceListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
    public string ManualReference { get; set; } = default!;
    public DateTimeOffset? InvoiceDate { get; set; }
    public long InvoiceNo { get; set; }
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