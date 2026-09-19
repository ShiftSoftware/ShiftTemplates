
using Microsoft.EntityFrameworkCore;
using ShiftEntity.Print;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftIdentity.Core.DTOs.CompanyBranch;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Shared.DTOs.ProductBrand;
using StockPlusPlus.Shared.DTOs.ProductCategory;
using StockPlusPlus.Shared.Enums;
using System.Security.Claims;

namespace StockPlusPlus.Data.Repositories;

public class ProductCategoryRepository : ShiftRepository<DB, Entities.ProductCategory, ProductCategoryListDTO, ProductCategoryDTO>
{
    private readonly IHashIdService hashIdService;

    // ProductCategory demonstrates AUTOMATIC mapping on an entity with a relationship (Brand ↔ BrandID via
    // ShiftEntitySelectDTO) AND file upload (Photos ↔ List<ShiftFileDTO> JSON): ShiftMapper declares the four
    // maps for this repository's triple from its type arguments, the framework's conventions
    // (ShiftEntityConversions) shape the select and the files, and nothing is written for it — no mapper
    // class, no configuration line. The files member is mapped in memory only (a database cannot parse
    // JSON into objects), so the list projection leaves it out, as the build says (SM0030).
    public ProductCategoryRepository(DB db, ICurrentUserProvider currentUserProvider, IServiceProvider serviceProvider, IHashIdService hashIdService) : base(db, o =>
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
        this.hashIdService = hashIdService;
    }

    public override async Task<Stream> PrintAsync(string id)
    {
        var longId = hashIdService.Decode<ProductCategoryDTO>(id);

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