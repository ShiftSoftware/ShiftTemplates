using ShiftSoftware.ShiftEntity.Core;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Invoice;

namespace StockPlusPlus.Data.Mappers;

public class InvoiceMapper : IShiftEntityMapper<Invoice, InvoiceListDTO, InvoiceDTO>
{
    public InvoiceDTO MapToView(Invoice entity)
    {
        return new InvoiceDTO
        {
            ManualReference = entity.ManualReference ?? "",
            InvoiceDate = entity.InvoiceDate,
            InvoiceNo = entity.InvoiceNo,
            CompanyID = entity.CompanyID?.ToString(),

            // Collection mapping: entity children → DTO children
            InvoiceLines = entity.InvoiceLines.Select(line => new InvoiceLineDTO
            {
                ID = line.ID.ToString(),
                Description = line.Description,
                Price = line.Price,
                IsDeleted = line.IsDeleted,
                CreateDate = line.CreateDate,
                LastSaveDate = line.LastSaveDate,

                Product = line.ProductID.ToSelectDTO(line.Product?.Name),
            }).ToList(),
        }.MapBaseFields(entity);
    }

    public Invoice MapToEntity(InvoiceDTO dto, Invoice existing)
    {
        existing.ManualReference = dto.ManualReference;
        existing.InvoiceDate = dto.InvoiceDate;

        // Collection mapping: DTO children → entity children
        // Note: InvoiceRepository.UpsertAsync handles delete-and-recreate,
        // so we always create new InvoiceLine entities from the DTO.
        existing.InvoiceLines = dto.InvoiceLines.Select(lineDto => new InvoiceLine
        {
            Description = lineDto.Description,
            Price = lineDto.Price,
            ProductID = lineDto.Product.ToForeignKey(),
        }).ToList();

        return existing;
    }

    public IQueryable<InvoiceListDTO> MapToList(IQueryable<Invoice> query)
    {
        return query.Select(e => new InvoiceListDTO
        {
            ID = e.ID.ToString(),
            ManualReference = e.ManualReference ?? "",
            InvoiceDate = e.InvoiceDate,
            InvoiceNo = e.InvoiceNo,
            IsDeleted = e.IsDeleted,
        });
    }

    public void CopyEntity(Invoice source, Invoice target)
    {
        source.ShallowCopyTo(target);
    }
}
