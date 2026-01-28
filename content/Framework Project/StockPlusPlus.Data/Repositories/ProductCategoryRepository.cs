
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
using System.Security.Claims;

namespace StockPlusPlus.Data.Repositories;

public class ProductCategoryRepository : ShiftRepository<DB, Entities.ProductCategory, ProductCategoryListDTO, ProductCategoryDTO>
{
    public ProductCategoryRepository(DB db, ICurrentUserProvider currentUserProvider, IServiceProvider serviceProvider) : base(db, o =>
    {
        //o.FilterByCustomValue<List<long>>(x => x.CustomValue.Contains(x.Entity.ID))
        //.ValueProvider(() =>
        //{
        //    var user = currentUserProvider.GetUser();

        //    return new ValueTask<List<long>>(new List<long>() { user.GetCountryID()!.Value });
        //});

        //o.FilterByClaimValues(x => x.ClaimValues != null && x.ClaimValues.Contains(x.Entity.ID.ToString()))
        //.ValueProvider<CompanyBranchDTO>(Constants.CompanyBranchIdClaim);

        //o.FilterByTypeAuthValues(x => (x.ReadableTypeAuthValues != null && x.ReadableTypeAuthValues.Contains(x.Entity.ID.ToString())) || x.WildCardRead)
        //.ValueProvider<ProductBrandDTO>(
        //    Shared.ActionTrees.StockPlusPlusActionTree.DataLevelAccess.ProductBrand,
        //    Constants.CompanyBranchIdClaim
        //);
    })
    {
    }

    public override async Task<Stream> PrintAsync(string id)
    {
        var longId = ShiftEntityHashIdService.Decode<ProductCategoryDTO>(id);

        var item = (await FindAsync(longId, null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;

        //Data source fo Fast Report
        var category = new
        {
            item.Name,
            item.Description,
            item.Code
        };

        var q = await GetIQueryable(asOf: null, includes: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);

        var otherCategories = await
            q
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