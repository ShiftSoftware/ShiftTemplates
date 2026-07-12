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
    //   - MapToView auto-composes the InvoiceLines child collection (InvoiceLine → InvoiceLineDTO)
    //     through the source-generated pair mapper — zero code.
    //   - MapToEntity writes the children back with ONE explicit line, options.ForEntityChildren(...),
    //     which maps each DTO line to a NEW InvoiceLine via the pair (replace-with-new — the repository
    //     owns the old lines via the delete-and-recreate in UpsertAsync below).
    //   - MapToList is EXPLICIT and per level (nothing goes deep automatically in the list direction):
    //     ForListChildren composes the lines, and its nested callback composes each line's product (a
    //     custom DTO) and customizes one of the product's own properties. EF translates it to JOINs.

    private static readonly Action<ShiftRepositoryOptions<Invoice, InvoiceListDTO, InvoiceDTO>> IncludeOptions =
        option =>
        {
            option.IncludeRelatedEntitiesWithFindAsync(x => x.Include(entity => entity.InvoiceLines));
            option.UseGeneratedMapper(map =>
            {
                // Entity direction: replace-with-new via the pair (unchanged).
                map.ForEntityChildren(x => x.InvoiceLines, d => d.InvoiceLines);

                // List direction: compose the lines, and — explicitly, one level deeper — each line's
                // product, with a custom mapping for the product's Name.
                map.ForListChildren(d => d.InvoiceLines, e => e.InvoiceLines, line =>
                    line.ForListChild(l => l.Product, il => il.Product, product =>
                        product.ForList(p => p.Name, prod => prod.Name + " + Custom Mapping")));
            });
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
