using AutoMapper;
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

            var configuration = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new ShiftSoftware.ShiftEntity.Core.DefaultAutoMapperProfile(typeof(StockPlusPlus.Data.Marker).Assembly));
            });

            var mapper = new Mapper(configuration);

            var item = mapper.Map<ProductCategoryDTO>(productCategory, opt => opt.Items["lang"] = "en");

            return new OkObjectResult(new
            {
                AllProducts = allProductCategories,
                FirstProductCategory = productCategory is null ? null : item,
                Lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName,
            });
        }
    }
}