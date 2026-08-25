using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using ShiftSoftware.TypeAuth.AspNetCore;
using ShiftSoftware.TypeAuth.Core;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.ProductCategory;
using System.Globalization;

namespace StockPlusPlus.Functions
{
    public class ProductCategories
    {
        private readonly ProductCategoryRepository productCategoryRepository;
        private readonly ITypeAuthService typeAuth;
        
        public ProductCategories(ProductCategoryRepository productCategoryRepository, ITypeAuthService typeAuth)
        {
            this.productCategoryRepository = productCategoryRepository;
            this.typeAuth = typeAuth;
        }

        [Function("ProductCategories")]
        [TypeAuth(typeof(StockPlusPlusActionTree), nameof(StockPlusPlusActionTree.ProductCategory), Access.Read)]
        public async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req)
        {
            var odataList = await this.productCategoryRepository.OdataList(queryable: null);

            var allProductCategories = await odataList.ToArrayAsync();

            Data.Entities.ProductCategory? productCategory = null;

            if (allProductCategories.Length != 0 && allProductCategories.First().ID is string id)
            {
                var productCategoryId = long.Parse(id);
                productCategory = await this.productCategoryRepository.FindAsync(productCategoryId, asOf: null, disableDefaultDataLevelAccess: false, disableGlobalFilters: false);
            }

            // The repository's own view mapping (source-generated for ProductCategory). This used to build a
            // throwaway AutoMapper over the whole Data assembly on every request, and passed it a "lang" item
            // that no map ever read.
            var item = productCategory is null
                ? null
                : await this.productCategoryRepository.ViewAsync(productCategory);

            return new OkObjectResult(new
            {
                AllProducts = allProductCategories,
                FirstProductCategory = item,
                Lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            });
        }
    }
}