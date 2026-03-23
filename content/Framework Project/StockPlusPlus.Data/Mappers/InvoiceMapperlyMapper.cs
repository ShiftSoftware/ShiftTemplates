using Riok.Mapperly.Abstractions;
using ShiftSoftware.ShiftEntity.Core;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.Invoice;

namespace StockPlusPlus.Data.Mappers;

[Mapper]
public partial class InvoiceMapperlyMapper : IShiftEntityMapper<Invoice, InvoiceListDTO, InvoiceDTO>
{
    // --- MapToView: Entity → DTO ---

    public InvoiceDTO MapToView(Invoice entity)
    {
        var dto = MapToViewGenerated(entity);

        // Collection mapping: entity children → DTO children
        dto.InvoiceLines = entity.InvoiceLines.Select(line => new InvoiceLineDTO
        {
            ID = line.ID.ToString(),
            Description = line.Description,
            Price = line.Price,
            IsDeleted = line.IsDeleted,
            CreateDate = line.CreateDate,
            LastSaveDate = line.LastSaveDate,
            Product = line.ProductID.ToSelectDTO(line.Product?.Name),
        }).ToList();

        return dto.MapBaseFields(entity);
    }

    [MapperIgnoreSource(nameof(Invoice.InvoiceLines))]
    [MapperIgnoreSource(nameof(Invoice.ReloadAfterSave))]
    [MapperIgnoreSource(nameof(Invoice.AuditFieldsAreSet))]
    [MapperIgnoreSource(nameof(Invoice.RegionID))]
    [MapperIgnoreSource(nameof(Invoice.CompanyBranchID))]
    [MapperIgnoreSource(nameof(Invoice.IdempotencyKey))]
    [MapperIgnoreSource(nameof(Invoice.CityID))]
    [MapperIgnoreSource(nameof(Invoice.CountryID))]
    [MapperIgnoreSource(nameof(Invoice.ReleaseDate))]
    [MapperIgnoreSource(nameof(Invoice.CreatedByUserID))]
    [MapperIgnoreSource(nameof(Invoice.LastSavedByUserID))]
    [MapperIgnoreTarget(nameof(InvoiceDTO.ID))]
    [MapperIgnoreTarget(nameof(InvoiceDTO.IsDeleted))]
    [MapperIgnoreTarget(nameof(InvoiceDTO.CreateDate))]
    [MapperIgnoreTarget(nameof(InvoiceDTO.LastSaveDate))]
    [MapperIgnoreTarget(nameof(InvoiceDTO.CreatedByUserID))]
    [MapperIgnoreTarget(nameof(InvoiceDTO.LastSavedByUserID))]
    [MapperIgnoreTarget(nameof(InvoiceDTO.InvoiceLines))]
    private partial InvoiceDTO MapToViewGenerated(Invoice entity);

    // Custom type conversions for Mapperly to use
    private string? NullableLongToString(long? value) => value?.ToString();

    // --- MapToEntity: DTO → Entity ---

    public Invoice MapToEntity(InvoiceDTO dto, Invoice existing)
    {
        MapToEntityGenerated(dto, existing);

        // Collection mapping: DTO children → entity children
        // InvoiceRepository.UpsertAsync handles delete-and-recreate,
        // so we always create new InvoiceLine entities from the DTO.
        existing.InvoiceLines = dto.InvoiceLines.Select(lineDto => new InvoiceLine
        {
            Description = lineDto.Description,
            Price = lineDto.Price,
            ProductID = lineDto.Product.ToForeignKey(),
        }).ToList();

        return existing;
    }

    [MapperIgnoreTarget(nameof(Invoice.InvoiceLines))]
    [MapperIgnoreTarget(nameof(Invoice.ID))]
    [MapperIgnoreTarget(nameof(Invoice.CreateDate))]
    [MapperIgnoreTarget(nameof(Invoice.LastSaveDate))]
    [MapperIgnoreTarget(nameof(Invoice.IsDeleted))]
    [MapperIgnoreTarget(nameof(Invoice.ReloadAfterSave))]
    [MapperIgnoreTarget(nameof(Invoice.AuditFieldsAreSet))]
    [MapperIgnoreTarget(nameof(Invoice.RegionID))]
    [MapperIgnoreTarget(nameof(Invoice.CompanyID))]
    [MapperIgnoreTarget(nameof(Invoice.CompanyBranchID))]
    [MapperIgnoreTarget(nameof(Invoice.IdempotencyKey))]
    [MapperIgnoreTarget(nameof(Invoice.CityID))]
    [MapperIgnoreTarget(nameof(Invoice.CountryID))]
    [MapperIgnoreTarget(nameof(Invoice.ReleaseDate))]
    [MapperIgnoreTarget(nameof(Invoice.InvoiceNo))]
    [MapperIgnoreTarget(nameof(Invoice.CreatedByUserID))]
    [MapperIgnoreTarget(nameof(Invoice.LastSavedByUserID))]
    [MapperIgnoreSource(nameof(InvoiceDTO.ID))]
    [MapperIgnoreSource(nameof(InvoiceDTO.IsDeleted))]
    [MapperIgnoreSource(nameof(InvoiceDTO.CreateDate))]
    [MapperIgnoreSource(nameof(InvoiceDTO.LastSaveDate))]
    [MapperIgnoreSource(nameof(InvoiceDTO.CreatedByUserID))]
    [MapperIgnoreSource(nameof(InvoiceDTO.LastSavedByUserID))]
    [MapperIgnoreSource(nameof(InvoiceDTO.InvoiceLines))]
    [MapperIgnoreSource(nameof(InvoiceDTO.InvoiceNo))]
    [MapperIgnoreSource(nameof(InvoiceDTO.CompanyID))]
    private partial void MapToEntityGenerated(InvoiceDTO dto, Invoice existing);

    // --- MapToList: IQueryable projection ---

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

    // --- CopyEntity ---

    [MapperIgnoreTarget(nameof(Invoice.ReloadAfterSave))]
    [MapperIgnoreTarget(nameof(Invoice.AuditFieldsAreSet))]
    [MapperIgnoreTarget(nameof(Invoice.ID))]
    public partial void CopyEntity(Invoice source, Invoice target);
}
