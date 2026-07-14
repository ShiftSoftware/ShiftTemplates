using Microsoft.Azure.Cosmos.Linq;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Invoice;

namespace StockPlusPlus.Data.Repositories;

public class InvoiceRepository : ShiftRepository<DB, Entities.Invoice, InvoiceListDTO, InvoiceDTO>
{
    // Invoice demonstrates SOURCE-GENERATED mapping with DEEP (child collection) mapping:
    //   - MapToView AUTO-composes the InvoiceLines child collection (InvoiceLine → InvoiceLineDTO) via the
    //     auto pair mapper — zero-code, side-effect-free DTO building.
    //   - MapToEntity AUTO-writes the children back (replace-with-new via the pair's MapBack into fresh
    //     instances — the repository owns the old lines via the delete-and-recreate in UpsertAsync below).
    //   - MapToList is EXPLICIT and per level (deep children change the SQL query shape, so nothing goes
    //     deep silently): ForListChildren composes the lines, and its nested ForChild composes each line's
    //     custom Product DTO. This also opts the (InvoiceLine, InvoiceLineListDTO) and
    //     (Product, InvoiceLineProductListDTO) pair projections into source generation. EF translates it to JOINs.

    private static readonly Action<ShiftRepositoryOptions<Invoice, InvoiceListDTO, InvoiceDTO>> IncludeOptions =
        option =>
        {
            option.IncludeRelatedEntitiesWithFindAsync(x => x.Include(entity => entity.InvoiceLines));
            option.UseGeneratedMapper(map => map
                .ForListChildren(d => d.InvoiceLines, e => e.InvoiceLines, line =>
                    line.ForChild(l => l.Product, il => il.Product)));
        };

    public InvoiceRepository(DB db) : base(db, IncludeOptions)
    {
    }

    public override async ValueTask<Invoice> UpsertAsync(Invoice entity, InvoiceDTO dto, ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess, bool disableGlobalFilters)
    {

        if (actionType == ActionTypes.Update)
        {
            db.InvoiceLines.RemoveRange(entity.InvoiceLines.ToList());
        }

        var upserted = await base.UpsertAsync(entity, dto, actionType, userId, idempotencyKey, disableDefaultDataLevelAccess, disableGlobalFilters);

        if (actionType == ActionTypes.Insert)
        {

            var companyId = base.identityClaimProvider.GetCompanyID();

            var maxInvoiceNo = await db.Invoices
                .Where(x => x.CompanyID == companyId && !x.IsDeleted)
                .MaxAsync(x => (long?)x.InvoiceNo) ?? 1;

            upserted.InvoiceNo = maxInvoiceNo + 1;
        }

        return upserted;
    }
}
