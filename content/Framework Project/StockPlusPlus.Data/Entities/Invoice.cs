using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Attention;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Model.Flags;
using StockPlusPlus.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockPlusPlus.Data.Entities;


[TemporalShiftEntity]
public class Invoice : ShiftEntity<Invoice>,
    IEntityHasCountry<Invoice>,
    IEntityHasRegion<Invoice>,
    IEntityHasCompany<Invoice>,
    IEntityHasCompanyBranch<Invoice>,
    IEntityHasIdempotencyKey<Invoice>,
    IEntityHasCity<Invoice>,
    IEntityHasUniqueHash<Invoice>,
    IHasIndexedAttention,
    IHasDueDate
{
    public string? ManualReference { get; set; }
    public DateTimeOffset? InvoiceDate { get; set; }
    public long InvoiceNo { get; set; }

    public DateTimeOffset? ReleaseDate { get; set; }
    public DateTimeOffset? DueDate { get; set; }
    public long? RegionID { get; set; }
    public long? CompanyID { get; set; }
    public long? CompanyBranchID { get; set; }
    public Guid? IdempotencyKey { get; set; }
    public long? CityID { get; set; }
    public long? CountryID { get; set; }

    // IHasAttention — framework-maintained summary columns
    public bool HasActiveAttention { get; set; }
    public AttentionSeverity? HighestSeverity { get; set; }
    public int ActiveSignalCount { get; set; }

    public virtual ICollection<InvoiceLine> InvoiceLines { get; set; } = new HashSet<InvoiceLine>();

    public string? CalculateUniqueHash()
    {
        return $"{CompanyID}|{InvoiceNo}";
    }
}
