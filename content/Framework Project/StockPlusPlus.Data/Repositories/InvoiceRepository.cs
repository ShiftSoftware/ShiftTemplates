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
    private static readonly Action<ShiftRepositoryOptions<Invoice>> IncludeOptions =
        option => option.IncludeRelatedEntitiesWithFindAsync(x => x.Include(entity => entity.InvoiceLines));

    // When an IShiftEntityMapper<Invoice, ...> is registered in DI, this constructor is used.
    public InvoiceRepository(DB db, IShiftEntityMapper<Invoice, InvoiceListDTO, InvoiceDTO> mapper)
        : base(db, mapper, IncludeOptions)
    {
    }

    // Fallback: when no IShiftEntityMapper is registered, DI uses this constructor (AutoMapper).
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
