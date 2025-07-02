using FluentValidation;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace StockPlusPlus.Shared.DTOs.Product;

public class InvoiceDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }
    public string ManualReference { get; set; } = default!;
    public DateTimeOffset? InvoiceDate { get; set; }
    public List<InvoiceLineDTO> InvoiceLines { get; set; } = new List<InvoiceLineDTO>();
}

public class InvoiceLineDTO
{
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