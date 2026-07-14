using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace StockPlusPlus.Shared.DTOs.Invoice;

// A DEEP read-only demo triple for "api/invoice-deep": Invoice → InvoiceLines → Product → ProductBrand.
// Everything is composed AUTOMATICALLY by the source generator (no ForXxxChild calls) up to the max auto
// depth — in BOTH the list projection (correlated SQL) and the view (pair composition). The nested DTOs are
// plain classes shared by list and view (member names match the entity navigation names so the generator
// finds them). Set [ShiftEntityMapperMaxDepth(n)] on the Invoice entity to watch the cap take effect:
//   depth 1 = InvoiceLines, depth 2 = Product, depth 3 = ProductBrand.

public class InvoiceDeepDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }
    public string? ManualReference { get; set; }
    public long InvoiceNo { get; set; }

    public List<InvoiceLineDeepDTO> InvoiceLines { get; set; } = new();
}

public class InvoiceDeepListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
    public string? ManualReference { get; set; }
    public long InvoiceNo { get; set; }

    // Same nested shape as the view — composed automatically into the SQL list projection.
    public List<InvoiceLineDeepDTO> InvoiceLines { get; set; } = new();
}

public class InvoiceLineDeepDTO
{
    public string Description { get; set; } = default!;
    public decimal Price { get; set; }

    public ProductDeepDTO Product { get; set; } = default!;   // depth 2
}

public class ProductDeepDTO
{
    public string Name { get; set; } = default!;
    public int? Price { get; set; }

    public ProductBrandDeepDTO ProductBrand { get; set; } = default!;   // depth 3
}

public class ProductBrandDeepDTO
{
    public string Name { get; set; } = default!;
}
