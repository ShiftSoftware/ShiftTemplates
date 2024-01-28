﻿using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftFrameworkTestingTools;
using StockPlusPlus.Data.Entities.Product;
using StockPlusPlus.Data.Repositories.Product;

namespace StockPlusPlus.Test.Tests;

[TestCaseOrderer(Constants.OrdererTypeName, Constants.OrdererAssemblyName)]
[Collection("API Collection")]
public class Repositories
{
    private readonly CustomWebApplicationFactory factory;
    private readonly ITestOutputHelper output;

    private readonly BrandRepository brandRepository;
    private readonly ProductCategoryRepository productCategoryRepository;
    private readonly ProductRepository productRepository;

    public Repositories(CustomWebApplicationFactory factory, ITestOutputHelper output)
    {
        this.factory = factory;
        this.output = output;

        this.brandRepository = factory.Services.GetRequiredService<BrandRepository>();
        this.productCategoryRepository = factory.Services.GetRequiredService<ProductCategoryRepository>();
        this.productRepository = factory.Services.GetRequiredService<ProductRepository>();
    }

    [Fact]
    public async Task ProductRepository()
    {
        var brand = new Brand { Name = "Brand One" };

        brandRepository.Add(brand);

        await brandRepository.SaveChangesAsync();

        var foundBrand = (await this.brandRepository.FindAsync(brand.ID))!;

        var viewedBrand = await this.brandRepository.ViewAsync(foundBrand);

        Assert.NotNull(viewedBrand);

        Assert.Equal(brand.Name, foundBrand.Name);

        var productCategory = new ProductCategory { Name = "Product Category One" };

        productCategoryRepository.Add(productCategory);

        await productCategoryRepository.SaveChangesAsync();

        var product = new Product
        {
            Name = "Product One",
            Brand = foundBrand,
            ProductCategory = productCategory
        };

        productRepository.Add(product);

        await productRepository.SaveChangesAsync();

        this.productRepository.db.ChangeTracker.Clear();

        var foundProduct = (await this.productRepository.FindAsync(product.ID))!;

        var viewedProduct = await productRepository.ViewAsync(foundProduct);

        Assert.NotNull(viewedProduct);

        Assert.Equal(foundProduct.Name, viewedProduct.Name); 

        Assert.Equal(foundProduct.Brand!.Name, brand.Name);

        //Product Repository does not include the product category
        Assert.Null(foundProduct.ProductCategory);

        Assert.Equal(foundProduct.ProductCategoryID, productCategory.ID);
    }
}