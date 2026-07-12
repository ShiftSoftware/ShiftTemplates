using System.Linq.Expressions;
using Microsoft.Azure.Cosmos.Linq;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model.Dtos;
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
    // DEEP LIST mapping (list → lines → product), told explicitly and directionally. MapToView composes
    // children automatically; MapToList is a single SQL-translatable projection, so it does NOT — we hand
    // it a correlated sub-select. EF translates l.Product.Name to a JOIN, so no Include is needed for the
    // list read. Exposed as a field so the DB-independent DeepMappingTests exercises the exact expression.

    private static readonly Action<ShiftRepositoryOptions<Invoice, InvoiceListDTO, InvoiceDTO>> IncludeOptions =
        option =>
        {
            option.IncludeRelatedEntitiesWithFindAsync(x => x.Include(entity => entity.InvoiceLines));
            option.UseGeneratedMapper(map =>
            {
                // Entity direction: replace-with-new via the pair (unchanged).
                map.ForEntityChildren(x => x.InvoiceLines, d => d.InvoiceLines);

                // List direction: project the lines (and each line's product) into the list row.
                map.ForListChildren(d => d.InvoiceLines, d => d.InvoiceLines);
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
