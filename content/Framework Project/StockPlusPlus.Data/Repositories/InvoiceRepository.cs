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
    // Invoice demonstrates the automatic maps with DEEP (child collection) mapping AND a customization
    // configured IN the repository:
    //   - The children nest with nothing written: InvoiceDTO.InvoiceLines maps through InvoiceLine ↔
    //     InvoiceLineDTO and InvoiceListDTO.InvoiceLines through InvoiceLine → InvoiceLineListDTO → its
    //     Product (InvoiceLineProductListDTO), in memory AND in the list projection, which EF translates
    //     to JOINs. ShiftMapper declares those nested pairs from the DTO graph (ten levels deep by default;
    //     m.Nested(n) caps it). The write direction replaces the lines with new instances — the repository
    //     owns the old ones through the delete-and-recreate in UpsertAsync below — and the build says so (SM0049).
    //   - One member is CUSTOMIZED here, in ShiftMapper's vocabulary: InvoiceListDTO.Total has no column, so
    //     the list map is told to sum the lines. It is an expression, so it runs in SQL. The same customization
    //     applies wherever the map runs — the endpoint, a report service injecting Mapper — because the
    //     repository hands it to the host's mapper when it is constructed, and the mapper constructs the
    //     repository on its own when a service maps the pair first.

    private static readonly Action<ShiftRepositoryOptions<Invoice, InvoiceListDTO, InvoiceDTO>> IncludeOptions =
        option =>
        {
            option.IncludeRelatedEntitiesWithFindAsync(x => x.Include(entity => entity.InvoiceLines));
            option.Mapping(m => m.List.ForMember(d => d.Total, opt => opt.MapFrom(i => i.InvoiceLines.Sum(l => l.Price))));
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
