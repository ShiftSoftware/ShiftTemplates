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
    // Invoice demonstrates SOURCE-GENERATED mapping with DEEP (child collection) mapping. All three
    // directions are EXPLICIT and per level (the programmer decides how deep and in which direction):
    //   - MapToView composes the InvoiceLines child collection with ForViewChildren(...) (InvoiceLine →
    //     InvoiceLineDTO); its nested callback could customize a deep property (see DeepMappingTests).
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
            option.UseGeneratedMapper();
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
