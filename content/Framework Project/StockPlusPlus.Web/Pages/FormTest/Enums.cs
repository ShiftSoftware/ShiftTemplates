using System.ComponentModel;

namespace StockPlusPlus.Web.Pages.FormTest;

public enum ProcessFlags
{
    [Description("FirstSubmissionFromDealer")]
    FirstSubmissionFromDealer = 0,

    [Description("ResubmissionFromDealerForThatOnceReturned")]
    ResubmissionFromDealerForThatOnceReturned = 1,

    [Description("RecycledInternallyInDealer")]
    RecycledInternallyInDealer = 2,

    [Description("ResubmissionToVendorForThatOnceReturnedOrRejectedByVendor")]
    ResubmissionToVendorForThatOnceReturnedOrRejectedByVendor = 3,
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
    public static readonly KeyValuePair<string, string> W = new("W", "W: W");
    public static readonly KeyValuePair<string, string> S = new("S", "S: S");
    public static readonly KeyValuePair<string, string> T = new("T", "T: T");
}

public static class Franchises
{
    public static readonly KeyValuePair<string, string> Toyota = new("TYT", "T");
    public static readonly KeyValuePair<string, string> Lexus = new("LEX", "L");
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

public static class WarrantyTypes
{
    public static readonly KeyValuePair<string, string> SSC = new("SSC", "SSC: ");
    public static readonly KeyValuePair<string, string> VE = new("VE", "VE: ");
    public static readonly KeyValuePair<string, string> AC = new("AC", "AC: ");
    public static readonly KeyValuePair<string, string> P1 = new("P1", "P1: ");
    public static readonly KeyValuePair<string, string> P2 = new("P2", "P2: ");
    public static readonly KeyValuePair<string, string> A1 = new("A1", "A1: ");
    public static readonly KeyValuePair<string, string> A2 = new("A2", "A2: ");
}

public enum OperationTypes
{
    [Description("NotSpecified")]
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

    [Description("DealerStockDisposal")]
    DealerStockDisposal = 6,

    [Description("DistributorStockDisposal")]
    DistributorStockDisposal = 7
}