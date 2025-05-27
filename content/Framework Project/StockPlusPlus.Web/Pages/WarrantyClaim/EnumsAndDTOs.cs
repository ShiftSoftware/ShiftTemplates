using FluentValidation;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Web.Pages.FormTest;
using System.ComponentModel;
using System.Text.Json.Serialization;
using static MudBlazor.Icons.Custom;

namespace StockPlusPlus.Web.Pages.WarrantyClaim;

public class WarrantyClaimLaborLineDTO
{
    public long ID { get; set; }
    public string? PayCode { get; set; }
    public bool MainOperation { get; set; }
    public string? OperationNumber { get; set; }
    public decimal? Hour { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? TCAHour { get; set; }
}

public class WarrantyClaimSubletLineDTO
{
    public long ID { get; set; }
    public string? PayCode { get; set; }
    public string? SubletType { get; set; }
    public string? InvoiceNo { get; set; }
    public decimal? Amount { get; set; }
    public string? Description { get; set; }
}

public class WarrantyClaimPartLineDTO
{
    public long ID { get; set; }
    public string? PayCode { get; set; }
    public bool OFP { get; set; }
    public string? LocalF { get; set; }
    public string? PartNumber { get; set; }
    public string? PartDescription { get; set; }
    public int? Qty { get; set; }
    public decimal? Price { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? TCAPrice { get; set; }


    [JsonIgnore]
    public bool Loading { get; set; }

    public bool FoundInLookup { get; set; }
}

public class WarrantyClaimDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }
    public string TWCNo { get; set; } = default!;
    public string? InvoiceNo { get; set; }
    public string DealerCode { get; set; } = default!;
    public string DealerClaimNo { get; set; } = default!;
    public DateTime? DateOfReceipt { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProcessFlags ProcessFlg { get; set; } = ProcessFlags.FirstSubmissionFromDealer;
    public string WarrantyType { get; set; } = default!;
    public string Franchise { get; set; } = default!;

    public string? AP1 { get; set; }
    public string? AP2 { get; set; }
    public string? AP3 { get; set; }
    public string? AP4 { get; set; }
    public string? AP5 { get; set; }
    public bool NV { get; set; }
    public int? FV { get; set; }
    public string VIN_WMI { get; set; } = default!;
    public string VIN_VDS { get; set; } = default!;
    public string VIN_CD { get; set; } = default!;
    public string VIN_VIS { get; set; } = default!;
    public DateTime? DeliveryDate { get; set; }
    public DateTime? RepairDate { get; set; }
    public DateTime? RepairCompletionDate { get; set; }
    public int? Odometer { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public KMFlags KMFlg { get; set; } = KMFlags.K;
    public string RepairOrderNo { get; set; } = default!;
    public string DataID { get; set; } = DataIDs.W.Key;
    public List<WarrantyClaimLaborLineDTO> WarrantyClaimLaborLines { get; set; } = new();
    public List<WarrantyClaimSubletLineDTO> WarrantyClaimSubletLines { get; set; } = new();
    public List<WarrantyClaimPartLineDTO> WarrantyClaimPartLines { get; set; } = new();
    public string? LaborOperationNoMain { get; set; }
    public decimal LaborRate { get => Franchise == Franchises.Toyota.Key ? 25 : 25; }
    public decimal LaborRateJPY { get => LaborRate * this.LaborExchangeRate; }
    public decimal HourTotal { get => WarrantyClaimLaborLines.Sum(x => x.Hour ?? 0m); }
    public decimal HourTotalTCA { get => WarrantyClaimLaborLines.Sum(x => x.TCAHour ?? 0m); }
    public decimal? LaborTotalAmount { get => HourTotal * LaborRate; }
    public decimal? LaborTotalAmountTCA { get => HourTotalTCA * LaborRate; }

    [JsonIgnore]
    public decimal? LaborTotalAmountTCAJPY { get => LaborTotalAmountTCA * this.LaborExchangeRate; }

    public decimal? SubletTotalAmount { get => WarrantyClaimSubletLines.Sum(x => x.Amount); }
    public decimal? SubletTotalAmountJPY { get => SubletTotalAmount * this.SubletExchangeRate; }


    public string? SubletDescription { get; set; }
    public string? T1 { get; set; }
    public string? T2 { get; set; }
    public string? T3_1 { get; set; }
    public string? T3_2 { get; set; }
    public string? T3_3 { get; set; }
    public string? T3_4 { get; set; }
    public string? T3_5 { get; set; }
    public string? T3_6 { get; set; }
    public string? T3_7 { get; set; }
    public string Condition { get; set; } = default!;
    public string Cause { get; set; } = default!;
    public string Remedy { get; set; } = default!;
    public string? OFPLocalFlag { get; set; }
    public string? OFP { get; set; }
    public decimal? PartsTotalAmount { get => WarrantyClaimPartLines.Sum(x => (x.Price ?? 0m) * (x.Qty ?? 0)); }
    public decimal? PartsSubTotalAmountTCA { get => WarrantyClaimPartLines.Sum(x => (x.TCAPrice ?? 0m) * (x.Qty ?? 0)); }
    public decimal? PartsTotalAmountTCA { get => WarrantyClaimPartLines.Sum(x => (x.TCAPrice ?? 0m) * (x.Qty ?? 0) * PRR1); }
    public decimal? PartsTotalAmountTCAJPY { get => PartsTotalAmountTCA * this.PartExchangeRate; }
    public decimal? TotalClaimAmount { get => LaborTotalAmount + SubletTotalAmount + PartsTotalAmount; }
    public decimal? TotalClaimAmountTCA { get => LaborTotalAmountTCA + SubletTotalAmount + PartsTotalAmountTCA; }
    public decimal? TotalClaimAmountTCAJPY { get => LaborTotalAmountTCAJPY + SubletTotalAmountJPY + PartsTotalAmountTCAJPY; }
    public DateTime? ProcessDate { get; set; }
    public DateTime? TCAProcessDate { get; set; }
    public int LaborAdjustment { get; set; } = 100;
    public int SubletAdjustment { get; set; } = 100;
    public int PartsAdjustment { get; set; } = 100;
    public string? DistComment1 { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WarrantyClaimStatus? ClaimStatus { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public WarrantyManufacturerClaimStatus? ManufacturerStatus { get; set; }
    public string? DistributorErrorMessage { get; set; }
    public DateTime? AcInstallDate { get; set; }
    public int? AcInstallKm { get; set; }
    public string? ACPreviousRepairOrderNo { get; set; }
    public DateTime? AcPreviousRepairDate { get; set; }
    public int? AcPreviousRepairKm { get; set; }
    public string? AcPreviousInvoiceNo { get; set; }
    public string? AcCurrentInvoiceNo { get; set; }
    public bool? SpecialServiceCampaign { get; set; }
    public long? ToyotaFreeServiceRegisteredVehicleId { get; set; }
    public int? ToyotaFreeServiceBreakPart { get; set; }
    public string? DealerComments { get; set; }
    public string VIN { get => $"{VIN_WMI}{VIN_VDS}{VIN_CD}{VIN_VIS}"; }
    public string? ModelCode { get; set; }
    public int? YearModel { get; set; }
    public string? Katashiki { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OperationTypes OperationType { get; set; }

    public string? BatteryTestCode11 { get; set; }
    public string? BatteryTestCode12 { get; set; }
    public string? BatteryTestCode21 { get; set; }
    public string? BatteryTestCode22 { get; set; }
    public string? TSB { get; set; }

    public int? InvoiceCurrency { get; set; } = 1;
    public decimal PRR1 { get; set; }
    public decimal LaborExchangeRate { get; set; }
    public decimal PartExchangeRate { get; set; }
    public decimal SubletExchangeRate { get; set; }
}

public class WarrantyClaimValidator : AbstractValidator<WarrantyClaimDTO>
{
    public WarrantyClaimValidator()
    {
        RuleFor(x => x.InvoiceCurrency)
            .NotNull();

        RuleFor(x => x.PRR1)
            .NotEmpty();

        RuleFor(x => x.LaborExchangeRate)
            .NotEmpty();

        RuleFor(x => x.PartExchangeRate)
            .NotEmpty();

        RuleFor(x => x.SubletExchangeRate)
            .NotEmpty();

        RuleFor(x => x.TWCNo)
            .NotEmpty()
            .Length(7);

        RuleFor(x => x.DealerCode)
            .MaximumLength(10);

        RuleFor(x => x.DealerClaimNo)
            .MaximumLength(10);

        RuleFor(x => x.WarrantyType)
            .NotEmpty();

        RuleFor(x => x.OperationType)
            .NotEqual(x => OperationTypes.NotSpecified);

        RuleFor(x => x.Franchise)
            .NotEmpty();

        RuleFor(x => x.VIN_WMI)
            .NotEmpty()
            .Length(3)
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV));

        RuleFor(x => x.VIN_VDS)
            .NotEmpty()
            .Length(x => x.VIN_CD?.Length == 0 ? 6 : 5)
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV))
            .WithMessage("Should be 6 characters when CD is not provided or 5 when CD is provided.");

        RuleFor(x => x.VIN_CD)
            .Length(x => x.VIN_VDS?.Length == 5 ? 1 : 0)
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV))
            .WithMessage("Should be empty if VDS is 6 characters. or 1 character if VDS is 5 characters");

        RuleFor(x => x.VIN_VIS)
            .NotEmpty()
            .Length(8)
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV));

        RuleFor(x => x.DeliveryDate)
            .NotNull()
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV));

        RuleFor(x => x.RepairDate)
            .NotNull();
        //.When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV));

        RuleFor(x => x.Odometer)
            .NotNull()
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV));

        RuleFor(x => x.RepairOrderNo)
            .NotEmpty()
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV));

        RuleFor(x => x.AcPreviousRepairDate)
            .NotNull()
            .When(x => x.WarrantyType == WarrantyTypes.P1.Key || x.WarrantyType == WarrantyTypes.P2.Key);

        RuleFor(x => x.ACPreviousRepairOrderNo)
            .NotEmpty()
            .When(x => x.WarrantyType == WarrantyTypes.P1.Key);

        RuleFor(x => x.AcPreviousRepairKm)
            .NotNull()
            .When(x => x.WarrantyType == WarrantyTypes.P1.Key);


        RuleFor(x => x.AcPreviousInvoiceNo)
            .NotNull()
            .When(x => x.WarrantyType == WarrantyTypes.P2.Key);

        RuleFor(x => x.AcCurrentInvoiceNo)
            .NotNull()
            .When(x => x.WarrantyType == WarrantyTypes.P2.Key);
    }
}

public enum WarrantyClaimStatus
{
    [Description("Draft")]
    Draft = 0,

    [Description("Pending")]
    PendingProcess = 1,

    [Description("Accepted")]
    Accepted = 2,

    [Description("Error")]
    RejectedWithError = 3,

    [Description("Rejected")]
    RejectedPermanently = 4,

    [Description("Certified")]
    Certified = 5,

    [Description("Invoiced")]
    Invoiced = 6
}

public enum WarrantyManufacturerClaimStatus
{
    [Description("N/A")]
    NA = 0,

    [Description("Exported")]
    Exported = 1,

    [Description("Downloaded")]
    Downloaded = 2,

    [Description("Paid")]
    Paid = 3,

    [Description("Rejected")]
    Rejected = 4,

    [Description("On Hold")]
    OnHold = 5
}

public class FlatRateDTO
{
    public new string id { get; set; }

    public string LaborCode { get; set; }

    public string VDS { get; set; }

    public Dictionary<string, decimal?> Times { get; set; }

    public string WMI { get; set; }

    public Brands? Brand { get; set; }
}

public static class WarrantyTypes
{
    public static readonly KeyValuePair<string, string> SSC = new("SSC", "SSC: Special Service Campaign");
    public static readonly KeyValuePair<string, string> VE = new("VE", "VE: Vehicle Warranty");
    public static readonly KeyValuePair<string, string> P1 = new("P1", "P1: Service Parts Warranty");
    public static readonly KeyValuePair<string, string> P2 = new("P2", "P2: Counter Sales Parts Warranty");
    public static readonly KeyValuePair<string, string> A1 = new("A1", "A1: Accessories Warranty");
    public static readonly KeyValuePair<string, string> A2 = new("A2", "A2: Counter Sales Accessories Warranty");
}

public enum ProcessFlags
{
    [Description("First Submission From Dealer")]
    FirstSubmissionFromDealer = 0,

    [Description("Resubmission from dealer for that once returned")]
    ResubmissionFromDealerForThatOnceReturned = 1,

    [Description("Recycled internally in Dealer")]
    RecycledInternallyInDealer = 2,

    [Description("Resubmission to vender for that once returned or rejected by vendor")]
    ResubmissionToVendorForThatOnceReturnedOrRejectedByVendor = 3,
}

public enum OperationTypes
{
    [Description("Not Specified")]
    NotSpecified = 0,

    [Description("General")]
    General = 1,

    [Description("Paint")]
    Paint = 2,

    [Description("Noise")]
    Noise = 3,

    [Description("Rain")]
    Rain = 4,

    [Description("Campaign")]
    Campaign = 5,

    [Description("Dealer Stock Disposal")]
    DealerStockDisposal = 6,

    [Description("Distributor Stock Disposal")]
    DistributorStockDisposal = 7
}

public static class Franchises
{
    public static readonly KeyValuePair<string, string> Toyota = new("TYT", "Toyota");
    public static readonly KeyValuePair<string, string> Lexus = new("LEX", "Lexus");
}

public enum KMFlags
{
    [Description("Kilometers")]
    K = 1,

    [Description("Miles")]
    M = 2,
}

public class DataIDs
{
    public static readonly KeyValuePair<string, string> W = new("W", "W: Warranty");
    public static readonly KeyValuePair<string, string> S = new("S", "S: SETR/Monitor");
    public static readonly KeyValuePair<string, string> T = new("T", "T: Technical Warranty Data");
}

public class SubletTypes
{
    public static readonly KeyValuePair<string, string> AC = new("AC", "AC");
    public static readonly KeyValuePair<string, string> AL = new("AL", "AL");
    public static readonly KeyValuePair<string, string> BA = new("BA", "BA");
    public static readonly KeyValuePair<string, string> DS = new("DS", "DS");
    public static readonly KeyValuePair<string, string> EL = new("EL", "EL");
    public static readonly KeyValuePair<string, string> GL = new("GL", "GL");
    public static readonly KeyValuePair<string, string> MC = new("MC", "MC");
    public static readonly KeyValuePair<string, string> OF = new("OF", "OF");
    public static readonly KeyValuePair<string, string> PT = new("PT", "PT");
    public static readonly KeyValuePair<string, string> RA = new("RA", "RA");
    public static readonly KeyValuePair<string, string> SL = new("SL", "SL");
    public static readonly KeyValuePair<string, string> TW = new("TW", "TW");
    public static readonly KeyValuePair<string, string> TY = new("TY", "TY");
    public static readonly KeyValuePair<string, string> UP = new("UP", "UP");
    public static readonly KeyValuePair<string, string> WD = new("WD", "WD");
    public static readonly KeyValuePair<string, string> ZZ = new("ZZ", "ZZ");
}

public class SettingDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }
    public decimal PRR { get; set; }
    public decimal LaborExchangeRate { get; set; }
    public decimal SubletExchangeRate { get; set; }
    public decimal PartExchangeRate { get; set; }
}