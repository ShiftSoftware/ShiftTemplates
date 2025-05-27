using ShiftSoftware.ShiftBlazor.Enums;
using StockPlusPlus.Web.Pages.FormTest;
using System.Net.Http.Json;

namespace StockPlusPlus.Web.Pages.WarrantyClaim;

public partial class WarrantyClaimForm
{
    private bool isDistributor = false;

    private bool loadingRates = false;
    private bool errorLoadingRates = false;

    public List<FlatRateDTO> FlatRates { get; set; } = new();
    public bool LoadingFlatRate { get; set; }

    protected async override Task OnInitializedAsync()
    {
        this.isDistributor = capabilityProvider.IsDistributor;

        await base.OnInitializedAsync();
    }

    private void LaborChanged(WarrantyClaimLaborLineDTO line, string newValue)
    {
        line.OperationNumber = newValue;

        if (Mode < FormModes.Edit)
            return;

        line.Hour = null;

        var flatRate = FlatRates.FirstOrDefault(x => x.LaborCode == newValue);

        if (flatRate is not null)
            line.Hour = flatRate.Times.Where(x => x.Key == TheItem.YearModel?.ToString()).Select(x => x.Value).FirstOrDefault();
    }

    private async void PartChanged(WarrantyClaimPartLineDTO line, string newValue)
    {
        line.PartNumber = newValue;

        if (Mode < FormModes.Edit)
            return;

        line.PartDescription = null;

        line.Price = null;

        line.Loading = true;

        try
        {
            //var part = (await this.Http.GetFromJsonAsync<PartLookupDTO>($"WarrantyClaim/part-lookup/{newValue}"))!;

            //line.PartDescription = part.PartDescription;

            //line.Price = part.Prices.FirstOrDefault()?.WarrantyPrice?.Value;

            //line.TCAPrice = part.DistributorPurchasePrice;

            //line.FoundInLookup = true;
        }
        catch
        {

        }

        line.Loading = false;

        this.StateHasChanged();
    }

    private async Task VinChanged()
    {
        this.FlatRates.Clear();

        if (string.IsNullOrWhiteSpace(TheItem?.Franchise) || TheItem?.YearModel == null || string.IsNullOrWhiteSpace(TheItem?.VIN_WMI) || string.IsNullOrWhiteSpace(TheItem?.VIN_VDS))
        {
            return;
        }

        this.LoadingFlatRate = true;

        //try
        //{
        //    var brand = (
        //        this.TheItem.Franchise == Franchises.Toyota.Key ? ShiftSoftware.ADP.Models.Enums.Brands.Toyota :
        //        this.TheItem.Franchise == Franchises.Lexus.Key ? ShiftSoftware.ADP.Models.Enums.Brands.Lexus :
        //        ShiftSoftware.ADP.Models.Enums.Brands.Other
        //    );

        //    this.FlatRates = (await this.Http.GetFromJsonAsync<List<FlatRateDTO>>($"WarrantyClaim/flat-rate/{this.TheItem.VIN_VDS}/{this.TheItem.VIN_WMI}/{brand}"))!;

        //    this.FlatRates = this.FlatRates
        //        .Where(x => x.Brand == brand)
        //        .ToList();
        //}
        //catch
        //{

        //}

        this.LoadingFlatRate = false;

        this.StateHasChanged();
    }

    private async Task<IEnumerable<string>> LaborSearch(string value, CancellationToken token)
    {
        // if text is null or empty, show complete list
        if (string.IsNullOrEmpty(value))
            return this.FlatRates.Select(x => x.LaborCode!);

        return FlatRates.Where(x => x.LaborCode!.Contains(value, StringComparison.InvariantCultureIgnoreCase)).Select(x => x.LaborCode!);
    }

    private async Task LoadRates()
    {
        loadingRates = true;
        errorLoadingRates = false;

        try
        {
            var warrantyRates = (await this.Http.GetFromJsonAsync<SettingDTO>("Setting/current-rates"))!;

            TheItem.LaborExchangeRate = warrantyRates.LaborExchangeRate;
            TheItem.SubletExchangeRate = warrantyRates.SubletExchangeRate;
            TheItem.PartExchangeRate = warrantyRates.PartExchangeRate;
            TheItem.PRR1 = warrantyRates.PRR;

            loadingRates = false;
        }

        catch
        {
            loadingRates = false;
            errorLoadingRates = true;
        }

        StateHasChanged();
    }

    protected override async void OnAfterRender(bool firstRender)
    {
        if (firstRender)
        {
            if (Mode == FormModes.Create)
            {
                await this.LoadRates();

                //SetTestData();
            }

            await this.VinChanged();
        }
    }

    private async Task OnTaskFinished(FormTasks task)
    {
        if (Mode != FormModes.View)
            return;

        //Load Labor Rates
        await this.VinChanged();

        if (!capabilityProvider.IsDistributor)
        {
            StateHasChanged();

            return;
        }

        foreach (var partLine in TheItem.WarrantyClaimPartLines.Where(x => x.TCAPrice is null && x.FoundInLookup && x.TCAPrice is null))
        {
            //try
            //{
            //    var part = (await this.Http.GetFromJsonAsync<PartLookupDTO>($"WarrantyClaim/part-lookup/{partLine.PartNumber}"))!;

            //    partLine.TCAPrice = part.DistributorPurchasePrice;
            //}
            //catch
            //{

            //}
        }

        StateHasChanged();
    }

    private void SetTestData()
    {
        //var mock = new Services.Shared.Mocks.WarrantyClaim().DealerClaim;

        //mock.WarrantyClaimLaborLines.ForEach(x => x.ID = 0);
        //mock.WarrantyClaimSubletLines.ForEach(x => x.ID = 0);
        //mock.WarrantyClaimPartLines.ForEach(x => x.ID = 0);

        //TheItem.TWCNo = mock.TWCNo;
        //TheItem.DealerCode = mock.DealerCode;
        //TheItem.DealerClaimNo = mock.DealerClaimNo;
        //TheItem.DateOfReceipt = mock.DateOfReceipt;
        //TheItem.ProcessFlg = mock.ProcessFlg;
        //TheItem.WarrantyType = mock.WarrantyType;
        //TheItem.OperationType = mock.OperationType;
        //TheItem.Franchise = mock.Franchise;
        //TheItem.VIN_WMI = mock.VIN_WMI;
        //TheItem.VIN_VDS = mock.VIN_VDS;
        //TheItem.VIN_CD = mock.VIN_CD;
        //TheItem.VIN_VIS = mock.VIN_VIS;
        //TheItem.DeliveryDate = mock.DeliveryDate;
        //TheItem.RepairDate = mock.RepairDate;
        //TheItem.RepairCompletionDate = mock.RepairCompletionDate;
        //TheItem.Odometer = mock.Odometer;
        //TheItem.KMFlg = mock.KMFlg;
        //TheItem.RepairOrderNo = mock.RepairOrderNo;
        //TheItem.DataID = mock.DataID;
        //TheItem.T1 = mock.T1;
        //TheItem.T2 = mock.T2;
        //TheItem.T3_1 = mock.T3_1;
        //TheItem.T3_2 = mock.T3_2;
        //TheItem.T3_3 = mock.T3_3;
        //TheItem.T3_4 = mock.T3_4;
        //TheItem.T3_5 = mock.T3_5;
        //TheItem.T3_6 = mock.T3_6;
        //TheItem.T3_7 = mock.T3_7;
        //TheItem.Condition = mock.Condition;
        //TheItem.Cause = mock.Cause;
        //TheItem.Remedy = mock.Remedy;
        //TheItem.DealerComments = mock.DealerComments;
        //TheItem.DistComment1 = mock.DistComment1;
        //TheItem.BatteryTestCode11 = mock.BatteryTestCode11;
        //TheItem.BatteryTestCode12 = mock.BatteryTestCode12;
        //TheItem.BatteryTestCode21 = mock.BatteryTestCode21;
        //TheItem.BatteryTestCode22 = mock.BatteryTestCode22;
        //TheItem.TSB = mock.TSB;
        //TheItem.SubletDescription = mock.SubletDescription;
        //TheItem.YearModel = mock.YearModel;

        //TheItem.WarrantyClaimLaborLines.AddRange(mock.WarrantyClaimLaborLines);

        //TheItem.WarrantyClaimSubletLines.AddRange(mock.WarrantyClaimSubletLines);

        //TheItem.WarrantyClaimPartLines.AddRange(mock.WarrantyClaimPartLines);

        //StateHasChanged();
    }
}