
using System.Text.Json;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Core.Attention;
using ShiftSoftware.ShiftEntity.Core.Flags;
using ShiftSoftware.ShiftEntity.Core.Tagging;
using ShiftSoftware.ShiftEntity.Model.Flags;
using StockPlusPlus.Shared.Enums;

namespace StockPlusPlus.Data.Entities;

[TemporalShiftEntity]
public class Product : ShiftEntity<Product>,
    IEntityHasDraft<Product>,
    IEntityHasCountry<Product>,
    IEntityHasRegion<Product>,
    IEntityHasCompany<Product>,
    IEntityHasCompanyBranch<Product>,
    IEntityHasIdempotencyKey<Product>,
    IEntityHasCity<Product>,
    IHasAttention,
    IHasAttentionEvaluator<Product>,
    IShiftEntityTaggable
{
    public string Name { get; set; } = default!;

    public TrackingMethod TrackingMethod { get; set; }
    public int? Price { get; set; }

    public long ProductCategoryID { get; set; }

    public long ProductBrandID { get; set; }
    public long? CountryOfOriginID { get; set; }

    public virtual ProductCategory? ProductCategory { get; set; }

    public virtual ProductBrand? ProductBrand { get; set; }
    public virtual Country? CountryOfOrigin { get; set; }

    public DateTimeOffset? ReleaseDate { get; set; }
    public bool IsDraft { get; set; }
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

    // IShiftEntityTaggable — framework auto-wires the M:N join table "ProductTags"
    public ICollection<Tag> Tags { get; set; } = new List<Tag>();

    public AttentionSignal? EvaluateAttention(AttentionContext<Product> context)
    {
        if (context.Entity.ReleaseDate is null)
            return null;

        if (context.Entity.Price is not null)
            return null;

        // Do not raise a signal while someone has this product open on their screen. The
        // product form reports viewing presence while the record is open: ProductForm sets
        // ReportViewingPresence, so the form calls StartViewingEntity on the attention hub
        // when the record loads and stops the report when the form closes. That person
        // already sees the change, and opening the product would clear the signal right away
        // anyway — raising it would only cause pointless refreshes in every list that listens
        // for updates. When no tracker is registered, or nobody is viewing, HasActiveViewers
        // returns false and the signal is raised as normal.
        if (context.HasActiveViewers())
            return null;

        return new AttentionSignal
        {
            Category = "ReleasedWithoutPrice",
            Severity = AttentionSeverity.Warning,
            Reason = "Product has been released without a price.",
            PayloadJson = JsonSerializer.Serialize(new
            {
                field = nameof(Price),
                releasedAt = context.Entity.ReleaseDate,
            }),
        };
    }
}
