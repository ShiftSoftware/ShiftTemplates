
using Microsoft.EntityFrameworkCore;
using ShiftEntity.Print;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using ShiftSoftware.ShiftIdentity.Core.DTOs.CompanyBranch;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Shared.DTOs.ProductBrand;
using StockPlusPlus.Shared.DTOs.ProductCategory;
using StockPlusPlus.Shared.Enums;

namespace StockPlusPlus.Data.Repositories;

public class ProductCategoryRepository : ShiftRepository<DB, Entities.ProductCategory, ProductCategoryListDTO, ProductCategoryDTO>
{
    public ProductCategoryRepository(DB db, ICurrentUserProvider currentUserProvider) : base(db, o =>
    {
        o.FilterBy<List<long>>(x => 
                x.Value.Contains(x.Entity.ID) || 
                (x.ClaimValues != null && x.ClaimValues.Contains(x.Entity.ID.ToString())) ||
                (x.TypeAuthValues != null && x.TypeAuthValues.Contains(x.Entity.BrandID.ToString()!)) ||
                x.WildCard
        )
        .CustomValueProvider(x => { return new List<long>() { 0 }; })
        .ClaimValuesProvider<CompanyBranchDTO>(Constants.CompanyBranchIdClaim)
        .TypeAuthValuesProvider<ProductBrandDTO>(Shared.ActionTrees.StockPlusPlusActionTree.DataLevelAccess.ProductBrand);

        //o.TypeAuth(
        //    Shared.ActionTrees.StockPlusPlusActionTree.DataLevelAccess.ProductBrand
        //);

        //o.TypeAuth<ProductBrandDTO>(
        //    Shared.ActionTrees.StockPlusPlusActionTree.DataLevelAccess.ProductBrand
        //);

        //o.TypeAuth<ProductBrandDTO>(
        //    Shared.ActionTrees.StockPlusPlusActionTree.DataLevelAccess.ProductBrand,
        //    ShiftSoftware.ShiftEntity.Core.Constants.CompanyIdClaim
        //);

        //o.TypeAuth<ProductBrandDTO>(
        //    Shared.ActionTrees.StockPlusPlusActionTree.DataLevelAccess.ProductBrand
        //)
        //.SelfClaimValueProvider(ShiftSoftware.ShiftEntity.Core.Constants.CompanyIdClaim);
    })
    {
    }

    public override async Task<Stream> PrintAsync(string id)
    {
        var longId = ShiftEntityHashIdService.Decode<ProductCategoryDTO>(id);

        var item = (await FindAsync(longId, null, true))!;

        //Data source fo Fast Report
        var category = new
        {
            item.Name,
            item.Description,
            item.Code
        };

        var otherCategories = await
            GetIQueryable()
            .Where(x => x.ID != longId)
            .Select(x => new
            {
                x.Name,
                x.Description,
                x.Code,
                TrackingMethod = (int?)x.TrackingMethod
            })
            .ToListAsync();

        var trackingMethods = Enum.GetValues<TrackingMethod>().Select(x => new { Value = (int)x, Name = x.Describe() });

        return await new FastReportBuilder()
            .AddFastReportFile("Reports/ProductCategory.frx")
            .AddDataObject("Category", category)

            .AddDataList("OtherCategories", "OtherCategoriesDataBand", otherCategories.ToList<object>())
            .AddDataList("TrackingMethods", "TrackingMethodsDataBand", trackingMethods.ToList<object>())
            .AddDataList("OtherCategories", "OtherCategoriesByTrackingMethodDataBand", otherCategories.ToList<object>(), 3, "[TrackingMethods.Value] == [OtherCategories.TrackingMethod]")

            .HideDataBandIfEmpty("OtherCategoriesDataBand", "OtherCategoriesHeaderBand")
            .HideDataBandIfEmpty("TrackingMethodsDataBand", "TrackingMethodsHeaderBand")
            .HideDataBandIfEmpty("OtherCategoriesByTrackingMethodDataBand")

            .GetPDFStream(report =>
            {
                (report.FindObject("CellCodeHeader") as FastReport.Table.TableCell)!.FillColor = System.Drawing.Color.FromArgb(255, 255, 0, 0);
            });
    }
}
