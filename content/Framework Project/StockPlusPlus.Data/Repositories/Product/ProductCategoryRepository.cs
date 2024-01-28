
using Microsoft.EntityFrameworkCore;
using ShiftEntity.Print;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model.HashIds;
using StockPlusPlus.Data.Entities.Product;
using StockPlusPlus.Shared.DTOs.Product.ProductCategory;
using StockPlusPlus.Shared.Enums.Product;

namespace StockPlusPlus.Data.Repositories.Product;

public class ProductCategoryRepository : ShiftRepository<DB, ProductCategory, ProductCategoryListDTO, ProductCategoryDTO>
{
    public ProductCategoryRepository(DB db) : base(db)
    {
    }

    public override async Task<Stream> PrintAsync(string id)
    {
        var longId = ShiftEntityHashIdService.Decode<ProductCategoryDTO>(id);

        var item = (await this.FindAsync(longId))!;

        //Data source fo Fast Report
        var category = new
        {
            item.Name,
            item.Description,
            item.Code
        };

        var otherCategories = await
            this.GetIQueryable()
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
