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
    // Invoice demonstrates the automatic maps with DEEP (child collection) mapping AND a customization — and
    // that the two live in different places:
    //   - The children nest with nothing written: InvoiceDTO.InvoiceLines maps through InvoiceLine ↔
    //     InvoiceLineDTO and InvoiceListDTO.InvoiceLines through InvoiceLine → InvoiceLineListDTO → its
    //     Product (InvoiceLineProductListDTO), in memory AND in the list projection, which EF translates
    //     to JOINs. ShiftMapper declares those nested pairs from the DTO graph, ten levels deep by default.
    //     HOW DEEP is the repository's one word about its maps: `option.Mapping(m => m.Nested(n))` here caps
    //     it (a constant; read at build time). The write direction replaces the lines with new instances — the
    //     repository owns the old ones through the delete-and-recreate in UpsertAsync below — and the build
    //     says so (SM0049).
    //   - WHAT a member maps from is not the repository's business. InvoiceListDTO.Total has no column and is
    //     summed from the lines in the project's one mapper class, Mappers/StockPlusPlusMapper.cs — an ordinary
    //     ShiftMapperBase whose CreateMap replaces the automatic list map for that pair (SM0047); the other
    //     three maps stay automatic. Nothing about it is plugged into this repository, and the same map serves
    //     any service injecting Mapper.

    private static readonly Action<ShiftRepositoryOptions<Invoice, InvoiceListDTO, InvoiceDTO>> IncludeOptions =
        option =>
        {
            option.IncludeRelatedEntitiesWithFindAsync(x => x.Include(entity => entity.InvoiceLines));
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
