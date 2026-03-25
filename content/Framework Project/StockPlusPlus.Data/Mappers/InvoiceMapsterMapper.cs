using Mapster;
using ShiftSoftware.ShiftEntity.Core;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Invoice;

namespace StockPlusPlus.Data.Mappers;

public class InvoiceMapsterMapper : IShiftEntityMapper<Invoice, InvoiceListDTO, InvoiceDTO>
{
    private static readonly TypeAdapterConfig _config = BuildConfig();

    private static TypeAdapterConfig BuildConfig()
    {
        var config = MapsterShiftEntityDefaults.CreateConfig();

        // Collection mapping needs explicit config; scalar FKs handled by convention.
        config.EntityToView<Invoice, InvoiceDTO>()
            .Map(d => d.ManualReference, s => s.ManualReference ?? "")
            .Map(d => d.CompanyID, s => s.CompanyID.HasValue ? s.CompanyID.Value.ToString() : null)
            .Map(d => d.InvoiceLines, s => s.InvoiceLines.Select(line => new InvoiceLineDTO
            {
                ID = line.ID.ToString(),
                Description = line.Description,
                Price = line.Price,
                IsDeleted = line.IsDeleted,
                CreateDate = line.CreateDate,
                LastSaveDate = line.LastSaveDate,
                Product = line.ProductID.ToSelectDTO(line.Product == null ? null : line.Product.Name),
            }).ToList());

        config.ViewToEntity<InvoiceDTO, Invoice>()
            .Ignore(d => d.ReleaseDate!, d => d.InvoiceNo)
            .AfterMapping((dto, entity) =>
            {
                entity.InvoiceLines = dto.InvoiceLines.Select(lineDto => new InvoiceLine
                {
                    Description = lineDto.Description,
                    Price = lineDto.Price,
                    ProductID = lineDto.Product.ToForeignKey(),
                }).ToList();
            });

        config.EntityToList<Invoice, InvoiceListDTO>()
            .Map(d => d.ManualReference, s => s.ManualReference ?? "");

        config.EntityCopy<Invoice>();

        return config;
    }

    public InvoiceDTO MapToView(Invoice entity)
    {
        return entity.Adapt<InvoiceDTO>(_config).MapBaseFields(entity);
    }

    public Invoice MapToEntity(InvoiceDTO dto, Invoice existing)
    {
        dto.Adapt(existing, _config);
        return existing;
    }

    public IQueryable<InvoiceListDTO> MapToList(IQueryable<Invoice> query)
    {
        return query.ProjectToType<InvoiceListDTO>(_config);
    }

    public void CopyEntity(Invoice source, Invoice target)
    {
        source.Adapt(target, _config);
    }
}
