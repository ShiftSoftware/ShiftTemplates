using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Attention;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model.Flags;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.Invoice;
using StockPlusPlus.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPlusPlus.Data.Entities;


// "api/invoice-deep" — a read-only demo endpoint (built-in repository, no repository class) that exercises
// AUTOMATIC deep mapping across three levels: Invoice → InvoiceLines → Product → ProductBrand, in BOTH the
// list and the view. Distinct DTOs keep it isolated from the InvoiceController's InvoiceDTO triple. The
// includes below load the graph for the view; the list projection composes it in SQL. The depth is ten by
// default; ConfigureRepository below shows how to cap it (n = 1 lines, 2 product, 3 brand).
[TemporalShiftEntity]
[ShiftEntitySecureEndpoint<InvoiceDeepListDTO, InvoiceDeepDTO, StockPlusPlusActionTree>("api/invoice-deep", nameof(StockPlusPlusActionTree.Invoice))]
public class Invoice : ShiftEntity<Invoice>,
    IEntityHasCountry<Invoice>,
    IEntityHasRegion<Invoice>,
    IEntityHasCompany<Invoice>,
    IEntityHasCompanyBranch<Invoice>,
    IEntityHasIdempotencyKey<Invoice>,
    IEntityHasCity<Invoice>,
    IEntityHasUniqueHash<Invoice>,
    IHasIndexedAttention,
    IHasDueDate,
    IConfiguresShiftRepository<Invoice, InvoiceDeepListDTO, InvoiceDeepDTO>
{
    // Applies only to the "api/invoice-deep" triple (keyed by its DTOs). Loads the deep graph for MapToView
    // (the list side composes the same graph as a correlated SQL projection, no Include needed).
    public void ConfigureRepository(ShiftRepositoryConfigurationContext<Invoice, InvoiceDeepListDTO, InvoiceDeepDTO> context)
    {
        context.Options.IncludeRelatedEntitiesWithFindAsync(x =>
            x.Include(i => i.InvoiceLines).ThenInclude(l => l.Product).ThenInclude(p => p.ProductBrand));

        // The maps themselves need nothing: they are declared by the endpoint attribute above. To cap the
        // automatic depth, configure it here (read at build time, so a constant):
        // context.Options.Mapping(m => m.Nested(2));   // ← depth 2 = Product; ProductBrand (depth 3) drops out
    }

    public string? ManualReference { get; set; }
    public DateTimeOffset? InvoiceDate { get; set; }
    public long InvoiceNo { get; set; }

    public DateTimeOffset? ReleaseDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public long? RegionID { get; set; }
    public long? CompanyID { get; set; }
    public long? CompanyBranchID { get; set; }
    public Guid? IdempotencyKey { get; set; }
    public long? CityID { get; set; }
    public long? CountryID { get; set; }

    // IHasAttention — framework-maintained summary columns
    public bool HasActiveAttention { get; set; }
    public AttentionSeverity? HighestSeverity { get; set; }
    public int ActiveSignalCount { get; set; }

    public virtual ICollection<InvoiceLine> InvoiceLines { get; set; } = new HashSet<InvoiceLine>();

    public string? CalculateUniqueHash()
    {
        return $"{CompanyID}|{InvoiceNo}";
    }
}
