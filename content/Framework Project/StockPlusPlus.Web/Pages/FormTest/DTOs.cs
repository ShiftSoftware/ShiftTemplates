using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace StockPlusPlus.Web.Pages.FormTest;

public class WarrantyClaimDTO : ShiftEntityViewAndUpsertDTO
{
    public override string? ID { get; set; }
    public string TWCNo { get; set; } = default!;
    public string? InvoiceNo { get; set; }
    public string DealerCode { get; set; } = default!;
    public string DealerClaimNo { get; set; } = default!;
    public DateTime? DateOfReceipt { get; set; }
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
    public KMFlags KMFlg { get; set; } = KMFlags.K;
    public string RepairOrderNo { get; set; } = default!;
    public int? InvoiceCurrency { get; set; }
    [Precision(18, 0)]
    public decimal? ExchangeRate { get; set; }
    public string DataID { get; set; } = DataIDs.W.Key;
    public List<WarrantyClaimLaborLineDTO> WarrantyClaimLaborLines { get; set; } = new();
    public List<WarrantyClaimSubletLineDTO> WarrantyClaimSubletLines { get; set; } = new();
    public List<WarrantyClaimPartLineDTO> WarrantyClaimPartLines { get; set; } = new();
    public string? LaborOperationNoMain { get; set; }
    public decimal LaborRate { get => Franchise == Franchises.Toyota.Key ? 5 : 10; }
    public decimal HourTotal { get => WarrantyClaimLaborLines.Sum(x => x.Hour ?? 0m); }
    public decimal HourTotalTCA { get => WarrantyClaimLaborLines.Sum(x => x.TCAHour ?? 0m); }
    public decimal? LaborTotalAmount { get => HourTotal * LaborRate; }
    public decimal? LaborTotalAmountTCA { get => HourTotalTCA * LaborRate; }
    public decimal? SubletTotalAmount { get => WarrantyClaimSubletLines.Sum(x => x.Amount); }
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
    public decimal? PRR1 { get; set; } = 1.00m;
    public decimal? PartsTotalAmount { get => WarrantyClaimPartLines.Sum(x => (x.Price ?? 0m) * (x.Qty ?? 0)); }
    public decimal? PartsSubTotalAmountTCA { get => WarrantyClaimPartLines.Sum(x => (x.TCAPrice ?? 0m) * (x.Qty ?? 0)); }
    public decimal? PartsTotalAmountTCA { get => WarrantyClaimPartLines.Sum(x => (x.TCAPrice ?? 0m) * (x.Qty ?? 0) * (PRR1 ?? 0m)); }
    public decimal? TotalClaimAmount { get => LaborTotalAmount + SubletTotalAmount + PartsTotalAmount; }
    public decimal? TotalClaimAmountTCA { get => LaborTotalAmountTCA + SubletTotalAmount + PartsTotalAmountTCA; }
    public DateTime? ProcessDate { get; set; }
    public DateTime? TCAProcessDate { get; set; }
    public int LaborAdjustment { get; set; } = 100;
    public int SubletAdjustment { get; set; } = 100;
    public int PartsAdjustment { get; set; } = 100;
    public string? DistComment1 { get; set; }
    public int? TWCStatus { get; set; }
    public int? SupplierTWCStatus { get; set; }
    public int? TWCTmcStatus { get; set; }
    public string? TWCErrorMsg { get; set; }
    public DateTime? AcInstallDate { get; set; }
    public int? AcInstallKm { get; set; }
    public string? ACPreviousRepairOrderNo { get; set; }
    public DateTime? AcPreviousRepairDate { get; set; }
    public int? AcPreviousRepairKm { get; set; }
    public string? AcPreviousInvoiceNo { get; set; }
    public string? AcCurrentInvoiceNo { get; set; }
    public int? TWCType { get; set; }
    public bool? SpecialServiceCampaign { get; set; }
    public long? ToyotaFreeServiceRegisteredVehicleId { get; set; }
    public int? ToyotaFreeServiceBreakPart { get; set; }
    public string? DealerComments { get; set; }
    public string VIN { get => $"{VIN_WMI}{VIN_VDS}{VIN_CD}{VIN_VIS}"; }
    public string? ModelCode { get; set; }
    public int? ProductionYear { get; set; }
    public int? ProductionMonth { get; set; }
    public string? Katashiki { get; set; }

    public OperationTypes OperationType { get; set; }

    public string? BatteryTestCode11 { get; set; }
    public string? BatteryTestCode12 { get; set; }
    public string? BatteryTestCode21 { get; set; }
    public string? BatteryTestCode22 { get; set; }
    public string? TSB { get; set; }
}

public class WarrantyClaimListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
    public string TWCNo { get; set; } = default!;
    public string? InvoiceNo { get; set; }
    public string DealerCode { get; set; } = default!;
    public string DealerClaimNo { get; set; } = default!;
    public DateTime? DateOfReceipt { get; set; }
    public ProcessFlags ProcessFlg { get; set; } = ProcessFlags.FirstSubmissionFromDealer;
    public string WarrantyType { get; set; } = default!;
    public string Franchise { get; set; } = default!;

    [Column(TypeName = "varchar")]
    [MaxLength(1)]
    public string? AP1 { get; set; }
    [Column(TypeName = "varchar")]
    [MaxLength(1)]
    public string? AP2 { get; set; }
    [Column(TypeName = "varchar")]
    [MaxLength(1)]
    public string? AP3 { get; set; }
    [Column(TypeName = "varchar")]
    [MaxLength(1)]
    public string? AP4 { get; set; }
    [Column(TypeName = "varchar")]
    [MaxLength(1)]
    public string? AP5 { get; set; }
    public int? NV { get; set; }
    public int? FV { get; set; }
    public string VIN_WMI { get; set; } = default!;
    public string VIN_VDS { get; set; } = default!;
    public string VIN_CD { get; set; } = default!;
    public string VIN_VIS { get; set; } = default!;
    public DateTime? DeliveryDate { get; set; }
    public DateTime RepairDate { get; set; }
    public DateTime? RepairCompletionDate { get; set; }
    public int? Odometer { get; set; }
    public KMFlags KMFlg { get; set; } = KMFlags.K;
    public string RepairOrderNo { get; set; } = default!;
    public int? InvoiceCurrency { get; set; }
    [Precision(18, 0)]
    public decimal? ExchangeRate { get; set; }
    public string DataID { get; set; } = DataIDs.W.Key;

    public List<WarrantyClaimLaborLineDTO> WarrantyClaimLaborLines = new();

    public string? LaborOperationNoMain { get; set; }
    public decimal? LaborRate { get; set; }
    [Precision(18, 1)]
    public decimal? HourTotal { get; set; }
    [Precision(18, 1)]
    public decimal? HourTotalTIQ { get; set; }
    public decimal? LaborTotalAmount { get; set; }
    public decimal? LaborTotalAmountTIQ { get; set; }
    public decimal? SubletTotalAmount { get; set; }
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
    public decimal? PRR1 { get; set; }
    public decimal? PartsTotalAmount { get; set; }
    public decimal? PartsTotalAmountTIQ { get; set; }
    public decimal? TotalClaimAmount { get; set; }
    public decimal? TotalClaimAmountTIQ { get; set; }
    public DateTime? ProcessDate { get; set; }
    public DateTime? TIQProcessDate { get; set; }
    public int? LaborAdjustment { get; set; }
    public int? SubletAdjustment { get; set; }
    public int? PartsAdjustment { get; set; }
    public string? DistComment1 { get; set; }
    public int? TWCStatus { get; set; }
    public int? SupplierTWCStatus { get; set; }
    public int? TWCTmcStatus { get; set; }
    public string? TWCErrorMsg { get; set; }
    public DateTime? AcInstallDate { get; set; }
    public int? AcInstallKm { get; set; }
    public string? ACPreviousRepairOrderNo { get; set; }
    public DateTime? AcPreviousRepairDate { get; set; }
    public int? AcPreviousRepairKm { get; set; }
    public string? AcPreviousInvoiceNo { get; set; }
    public string? AcCurrentInvoiceNo { get; set; }
    public int? TWCType { get; set; }
    public bool? SpecialServiceCampaign { get; set; }
    public long? ToyotaFreeServiceRegisteredVehicleId { get; set; }
    public int? ToyotaFreeServiceBreakPart { get; set; }
    public string? DealerComments { get; set; }
    public string VIN { get; set; } = default!;
    public string? ModelCode { get; set; }
    public int? ProductionYear { get; set; }
    public int? ProductionMonth { get; set; }
    public string? Katashiki { get; set; }

    public OperationTypes OperationType { get; set; }

    public string? BatteryTestCode11 { get; set; }
    public string? BatteryTestCode12 { get; set; }
    public string? BatteryTestCode21 { get; set; }
    public string? BatteryTestCode22 { get; set; }
    public string? TSB { get; set; }
}

public class WarrantyClaimLaborLineDTO
{
    public string? PayCode { get; set; } = default!;
    public bool MainOperation { get; set; }
    public string? OperationNumber { get; set; } = default!;
    public decimal? Hour { get; set; }
    public decimal? TCAHour { get; set; }
    public decimal? Amount { get; set; }
    public decimal? TCAAmount { get; set; }
}

public class WarrantyClaimPartLineDTO
{
    public string? PayCode { get; set; } = default!;
    public bool OFP { get; set; }
    public string? LocalF { get; set; } = default!;
    public string? PartNumber { get; set; } = default!;
    public string? PartDescription { get; set; }
    public int? Qty { get; set; }
    public decimal? Price { get; set; }
    public decimal? TCAPrice { get; set; }
}

public class WarrantyClaimSubletLineDTO
{
    public string PayCode { get; set; } = default!;
    public string? SubletType { get; set; }
    public string? InvoiceNo { get; set; }
    public decimal Amount { get; set; }
    public decimal TCAAmount { get; set; }
    public string? Description { get; set; }
}

public class WarrantyClaimSubletLineValidator : AbstractValidator<WarrantyClaimSubletLineDTO>
{
    public WarrantyClaimSubletLineValidator()
    {
        RuleFor(x => x.PayCode).NotEmpty();
        //RuleFor(x => x.OperationNumber)
        //    .NotEmpty()
        //    .WithMessage("Required");
        //RuleFor(x => x.Hour).GreaterThan(0);
        //RuleFor(x => x.TCAHour).GreaterThan(0);
        //RuleFor(x => x.Amount).GreaterThan(0);
        //RuleFor(x => x.TCAAmount).GreaterThan(0);
    }
}

public class WarrantyClaimLaborLineValidator : AbstractValidator<WarrantyClaimLaborLineDTO>
{
    public WarrantyClaimLaborLineValidator()
    {
        RuleFor(x => x.PayCode).NotEmpty();
        RuleFor(x => x.OperationNumber)
            .NotEmpty()
            .WithMessage("Required");
        RuleFor(x => x.Hour).GreaterThan(0);
        RuleFor(x => x.TCAHour).GreaterThan(0);
        RuleFor(x => x.Amount).GreaterThan(0);
        RuleFor(x => x.TCAAmount).GreaterThan(0);
    }
}

public class WarrantyClaimValidator : AbstractValidator<WarrantyClaimDTO>
{
    public WarrantyClaimValidator()
    {
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
            .Length(5)
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV));

        RuleFor(x => x.VIN_CD)
            .NotEmpty()
            .Length(1)
            .When(x => !(x.WarrantyType == WarrantyTypes.P2.Key || x.NV));

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

public static class SystemExtentsions
{
    private static readonly string CurrencyFormat = "{0:$#,0.00}";

    public static string ToCurrencyFormat(this decimal value)
    {
        return string.Format(CurrencyFormat, value);
    }

    public static string? ToCurrencyFormat(this decimal? value)
    {
        if (value is null)
            return null;

        return value.Value.ToCurrencyFormat();
    }
}