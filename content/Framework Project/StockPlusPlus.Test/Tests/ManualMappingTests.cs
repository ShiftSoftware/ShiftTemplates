    using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Data.DbContext;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.DTOs.Invoice;
using StockPlusPlus.Shared.DTOs.Product;
using StockPlusPlus.Shared.DTOs.ProductCategory;
using StockPlusPlus.Shared.Enums;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// Integration tests validating the manual mapping layer (IShiftEntityMapper implementations)
/// end-to-end through the repository CRUD operations.
/// </summary>
[Collection("API Collection")]
public class ManualMappingTests
{
    private readonly CustomWebApplicationFactory factory;

    public ManualMappingTests(CustomWebApplicationFactory factory)
    {
        this.factory = factory;
    }

    #region Product Mapper Tests

    [Fact]
    public async Task Product_InsertAndView_MapsAllFieldsCorrectly()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var brandRepo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ProductCategoryRepository>();
        var productRepo = scope.ServiceProvider.GetRequiredService<ProductRepository>();

        // Setup: create brand and category
        var brand = new ProductBrand { Name = "Mapping Test Brand" };
        brandRepo.Add(brand);
        await brandRepo.SaveChangesAsync();

        var category = new ProductCategory { Name = "Mapping Test Category" };
        categoryRepo.Add(category);
        await categoryRepo.SaveChangesAsync();

        // Act: insert Product through DTO → Entity mapping (UpsertAsync)
        var dto = new ProductDTO
        {
            Name = "Mapped Product",
            TrackingMethod = TrackingMethod.Batch_LOT,
            Price = 99,
            ReleaseDate = new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero),
            IsDraft = false,
            ProductBrand = new ShiftEntitySelectDTO { Value = brand.ID.ToString(), Text = brand.Name },
            ProductCategory = new ShiftEntitySelectDTO { Value = category.ID.ToString(), Text = category.Name },
            CountryOfOrigin = null,
        };

        var newProduct = new Product();
        productRepo.Add(newProduct);
        var inserted = await productRepo.UpsertAsync(newProduct, dto, ActionTypes.Insert, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await productRepo.SaveChangesAsync();

        // Clear tracker and re-fetch to verify persistence
        db.ChangeTracker.Clear();
        var found = await productRepo.FindAsync(inserted.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        Assert.NotNull(found);

        // Act: view through Entity → DTO mapping (ViewAsync)
        var viewDto = await productRepo.ViewAsync(found!);

        // Assert: all fields round-tripped correctly
        Assert.Equal("Mapped Product", viewDto.Name);
        Assert.Equal(TrackingMethod.Batch_LOT, viewDto.TrackingMethod);
        Assert.Equal(99, viewDto.Price);
        Assert.Equal(new DateTimeOffset(2025, 6, 15, 0, 0, 0, TimeSpan.Zero), viewDto.ReleaseDate);
        Assert.False(viewDto.IsDraft);

        // FK → ShiftEntitySelectDTO mapping
        Assert.NotNull(viewDto.ProductBrand);
        Assert.Equal(brand.ID.ToString(), viewDto.ProductBrand.Value);
        Assert.Equal(brand.Name, viewDto.ProductBrand.Text);

        Assert.NotNull(viewDto.ProductCategory);
        Assert.Equal(category.ID.ToString(), viewDto.ProductCategory.Value);

        // Nullable FK: CountryOfOrigin was null
        Assert.Null(viewDto.CountryOfOrigin);

        // Audit fields via MapBaseFields
        Assert.NotNull(viewDto.ID);
        Assert.NotEqual(default, viewDto.CreateDate);
        Assert.NotEqual(default, viewDto.LastSaveDate);
        Assert.False(viewDto.IsDeleted);
    }

    [Fact]
    public async Task Product_Update_MapsChangesCorrectly()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var brandRepo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ProductCategoryRepository>();
        var productRepo = scope.ServiceProvider.GetRequiredService<ProductRepository>();

        // Setup
        var brand = new ProductBrand { Name = "Update Test Brand" };
        brandRepo.Add(brand);
        await brandRepo.SaveChangesAsync();

        var category = new ProductCategory { Name = "Update Test Category" };
        categoryRepo.Add(category);
        await categoryRepo.SaveChangesAsync();

        var insertDto = new ProductDTO
        {
            Name = "Before Update",
            TrackingMethod = TrackingMethod.Batch_LOT,
            Price = 50,
            ProductBrand = new ShiftEntitySelectDTO { Value = brand.ID.ToString() },
            ProductCategory = new ShiftEntitySelectDTO { Value = category.ID.ToString() },
        };

        var product = new Product();
        productRepo.Add(product);
        await productRepo.UpsertAsync(product, insertDto, ActionTypes.Insert, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await productRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        // Act: update
        var found = (await productRepo.FindAsync(product.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;
        var updateDto = new ProductDTO
        {
            Name = "After Update",
            TrackingMethod = TrackingMethod.Serial,
            Price = 200,
            ProductBrand = new ShiftEntitySelectDTO { Value = brand.ID.ToString() },
            ProductCategory = new ShiftEntitySelectDTO { Value = category.ID.ToString() },
        };

        await productRepo.UpsertAsync(found, updateDto, ActionTypes.Update, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await productRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        // Assert
        var updated = (await productRepo.FindAsync(product.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;
        var viewDto = await productRepo.ViewAsync(updated);

        Assert.Equal("After Update", viewDto.Name);
        Assert.Equal(TrackingMethod.Serial, viewDto.TrackingMethod);
        Assert.Equal(200, viewDto.Price);
    }

    [Fact]
    public async Task Product_MapToList_ProjectsCorrectly()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var brandRepo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ProductCategoryRepository>();
        var productRepo = scope.ServiceProvider.GetRequiredService<ProductRepository>();

        // Setup
        var brand = new ProductBrand { Name = "List Test Brand" };
        brandRepo.Add(brand);
        await brandRepo.SaveChangesAsync();

        var category = new ProductCategory { Name = "List Test Category" };
        categoryRepo.Add(category);
        await categoryRepo.SaveChangesAsync();

        var product = new Product
        {
            Name = "List Test Product",
            TrackingMethod = TrackingMethod.NoTracking,
            Price = 42,
            ProductBrand = brand,
            ProductCategory = category,
        };
        productRepo.Add(product);
        await productRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        // Act: get list via OdataList (uses MapToList)
        var entityQuery = await productRepo.GetIQueryable(asOf: null, includes: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        var queryable = await productRepo.OdataList(entityQuery);
        var listItems = await queryable.Where(x => x.Name == "List Test Product").ToListAsync();

        // Assert
        Assert.Single(listItems);
        var item = listItems[0];
        Assert.Equal("List Test Product", item.Name);
        Assert.Equal(42, item.Price);
        Assert.Equal(TrackingMethod.NoTracking, item.TrackingMethod);
        Assert.Equal(brand.Name, item.ProductBrand);
        Assert.Equal(category.Name, item.Category);
        Assert.Equal(category.ID.ToString(), item.ProductCategoryID);
        Assert.Equal(brand.ID.ToString(), item.ProductBrandID);
    }

    #endregion

    #region ProductCategory Mapper Tests

    [Fact]
    public async Task ProductCategory_InsertAndView_MapsAllFieldsIncludingFiles()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ProductCategoryRepository>();

        // Act: insert through mapper
        var dto = new ProductCategoryDTO
        {
            Name = "File Category",
            Description = "Has photos",
            Code = "FC-001",
            TrackingMethod = TrackingMethod.Serial,
            Photos = new List<ShiftFileDTO>
            {
                new ShiftFileDTO { Blob = "photos/cat1.jpg", Name = "cat1.jpg" },
                new ShiftFileDTO { Blob = "photos/cat2.jpg", Name = "cat2.jpg" },
            },
            Brand = null,
        };

        var entity = new ProductCategory();
        categoryRepo.Add(entity);
        await categoryRepo.UpsertAsync(entity, dto, ActionTypes.Insert, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await categoryRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        // Re-fetch and view
        var found = (await categoryRepo.FindAsync(entity.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;
        var viewDto = await categoryRepo.ViewAsync(found);

        // Assert scalar fields
        Assert.Equal("File Category", viewDto.Name);
        Assert.Equal("Has photos", viewDto.Description);
        Assert.Equal("FC-001", viewDto.Code);
        Assert.Equal(TrackingMethod.Serial, viewDto.TrackingMethod);

        // Assert ShiftFileDTO round-trip (JSON string ↔ List<ShiftFileDTO>)
        Assert.NotNull(viewDto.Photos);
        Assert.Equal(2, viewDto.Photos!.Count);
        Assert.Equal("photos/cat1.jpg", viewDto.Photos[0].Blob);
        Assert.Equal("cat1.jpg", viewDto.Photos[0].Name);
        Assert.Equal("photos/cat2.jpg", viewDto.Photos[1].Blob);

        // Nullable FK: Brand was null
        Assert.Null(viewDto.Brand);

        // Audit fields
        Assert.NotNull(viewDto.ID);
        Assert.NotEqual(default, viewDto.CreateDate);
    }

    [Fact]
    public async Task ProductCategory_MapToList_ProjectsCorrectly()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ProductCategoryRepository>();

        var entity = new ProductCategory
        {
            Name = "List Category",
            Description = "For list test",
            Code = "LC-001",
            TrackingMethod = TrackingMethod.Batch_LOT,
        };
        categoryRepo.Add(entity);
        await categoryRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        // Act
        var entityQuery = await categoryRepo.GetIQueryable(asOf: null, includes: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        var queryable = await categoryRepo.OdataList(entityQuery);
        var listItems = await queryable.Where(x => x.Name == "List Category").ToListAsync();

        // Assert
        Assert.Single(listItems);
        var item = listItems[0];
        Assert.Equal("List Category", item.Name);
        Assert.Equal("For list test", item.Description);
        Assert.Equal("LC-001", item.Code);
        Assert.Equal(TrackingMethod.Batch_LOT, item.TrackingMethod);
    }

    #endregion

    #region Invoice Mapper Tests (Collection Mapping)

    [Fact]
    public async Task Invoice_InsertAndView_MapsCollectionCorrectly()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var brandRepo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ProductCategoryRepository>();
        var productRepo = scope.ServiceProvider.GetRequiredService<ProductRepository>();
        var invoiceRepo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();

        // Setup: create a product (needed for invoice lines)
        var brand = new ProductBrand { Name = "Invoice Test Brand" };
        brandRepo.Add(brand);
        await brandRepo.SaveChangesAsync();

        var category = new ProductCategory { Name = "Invoice Test Category" };
        categoryRepo.Add(category);
        await categoryRepo.SaveChangesAsync();

        var product = new Product
        {
            Name = "Invoice Test Product",
            ProductBrand = brand,
            ProductCategory = category,
        };
        productRepo.Add(product);
        await productRepo.SaveChangesAsync();

        // Act: insert invoice with lines through mapper
        var invoiceDto = new InvoiceDTO
        {
            ManualReference = "INV-MANUAL-001",
            InvoiceDate = new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero),
            InvoiceLines = new List<InvoiceLineDTO>
            {
                new InvoiceLineDTO
                {
                    Description = "Line 1",
                    Price = 100.50m,
                    Product = new ShiftEntitySelectDTO { Value = product.ID.ToString(), Text = product.Name },
                },
                new InvoiceLineDTO
                {
                    Description = "Line 2",
                    Price = 200.75m,
                    Product = new ShiftEntitySelectDTO { Value = product.ID.ToString(), Text = product.Name },
                },
            },
        };

        var invoice = new Invoice();
        invoiceRepo.Add(invoice);
        await invoiceRepo.UpsertAsync(invoice, invoiceDto, ActionTypes.Insert, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await invoiceRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        // Re-fetch and view
        var found = (await invoiceRepo.FindAsync(invoice.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;
        var viewDto = await invoiceRepo.ViewAsync(found);

        // Assert parent fields
        Assert.Equal("INV-MANUAL-001", viewDto.ManualReference);
        Assert.Equal(new DateTimeOffset(2025, 7, 1, 0, 0, 0, TimeSpan.Zero), viewDto.InvoiceDate);
        Assert.NotNull(viewDto.ID);
        Assert.NotEqual(default, viewDto.CreateDate);

        // Assert collection mapping
        Assert.Equal(2, viewDto.InvoiceLines.Count);

        var line1 = viewDto.InvoiceLines.First(l => l.Description == "Line 1");
        Assert.Equal(100.50m, line1.Price);
        Assert.NotNull(line1.Product);
        Assert.Equal(product.ID.ToString(), line1.Product.Value);

        var line2 = viewDto.InvoiceLines.First(l => l.Description == "Line 2");
        Assert.Equal(200.75m, line2.Price);
        Assert.NotNull(line2.Product);

        // Child IDs are populated after save
        Assert.NotNull(line1.ID);
    }

    [Fact]
    public async Task Invoice_Update_ReplacesCollectionCorrectly()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var brandRepo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ProductCategoryRepository>();
        var productRepo = scope.ServiceProvider.GetRequiredService<ProductRepository>();
        var invoiceRepo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();

        // Setup
        var brand = new ProductBrand { Name = "Invoice Update Brand" };
        brandRepo.Add(brand);
        await brandRepo.SaveChangesAsync();

        var category = new ProductCategory { Name = "Invoice Update Category" };
        categoryRepo.Add(category);
        await categoryRepo.SaveChangesAsync();

        var product = new Product { Name = "Invoice Update Product", ProductBrand = brand, ProductCategory = category };
        productRepo.Add(product);
        await productRepo.SaveChangesAsync();

        // Insert invoice with 2 lines
        var insertDto = new InvoiceDTO
        {
            ManualReference = "INV-UPD-001",
            InvoiceDate = new DateTimeOffset(2025, 8, 1, 0, 0, 0, TimeSpan.Zero),
            InvoiceLines = new List<InvoiceLineDTO>
            {
                new InvoiceLineDTO { Description = "Original Line 1", Price = 10m, Product = new ShiftEntitySelectDTO { Value = product.ID.ToString() } },
                new InvoiceLineDTO { Description = "Original Line 2", Price = 20m, Product = new ShiftEntitySelectDTO { Value = product.ID.ToString() } },
            },
        };

        var invoice = new Invoice();
        invoiceRepo.Add(invoice);
        await invoiceRepo.UpsertAsync(invoice, insertDto, ActionTypes.Insert, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await invoiceRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        // Act: update with different lines (delete-and-recreate pattern)
        var found = (await invoiceRepo.FindAsync(invoice.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;
        var updateDto = new InvoiceDTO
        {
            ManualReference = "INV-UPD-002",
            InvoiceDate = new DateTimeOffset(2025, 9, 1, 0, 0, 0, TimeSpan.Zero),
            InvoiceLines = new List<InvoiceLineDTO>
            {
                new InvoiceLineDTO { Description = "Replaced Line A", Price = 99m, Product = new ShiftEntitySelectDTO { Value = product.ID.ToString() } },
            },
        };

        await invoiceRepo.UpsertAsync(found, updateDto, ActionTypes.Update, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await invoiceRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        // Assert
        var updated = (await invoiceRepo.FindAsync(invoice.ID, asOf: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true))!;
        var viewDto = await invoiceRepo.ViewAsync(updated);

        Assert.Equal("INV-UPD-002", viewDto.ManualReference);
        Assert.Equal(new DateTimeOffset(2025, 9, 1, 0, 0, 0, TimeSpan.Zero), viewDto.InvoiceDate);

        // Old 2 lines replaced by 1 new line
        Assert.Single(viewDto.InvoiceLines);
        Assert.Equal("Replaced Line A", viewDto.InvoiceLines[0].Description);
        Assert.Equal(99m, viewDto.InvoiceLines[0].Price);
    }

    [Fact]
    public async Task Invoice_MapToList_ProjectsCorrectly()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<DB>();
        var brandRepo = scope.ServiceProvider.GetRequiredService<ProductBrandRepository>();
        var categoryRepo = scope.ServiceProvider.GetRequiredService<ProductCategoryRepository>();
        var productRepo = scope.ServiceProvider.GetRequiredService<ProductRepository>();
        var invoiceRepo = scope.ServiceProvider.GetRequiredService<InvoiceRepository>();

        // Setup
        var brand = new ProductBrand { Name = "Invoice List Brand" };
        brandRepo.Add(brand);
        await brandRepo.SaveChangesAsync();

        var category = new ProductCategory { Name = "Invoice List Category" };
        categoryRepo.Add(category);
        await categoryRepo.SaveChangesAsync();

        var product = new Product { Name = "Invoice List Product", ProductBrand = brand, ProductCategory = category };
        productRepo.Add(product);
        await productRepo.SaveChangesAsync();

        var invoiceDto = new InvoiceDTO
        {
            ManualReference = "INV-LIST-001",
            InvoiceDate = new DateTimeOffset(2025, 10, 1, 0, 0, 0, TimeSpan.Zero),
            InvoiceLines = new List<InvoiceLineDTO>
            {
                new InvoiceLineDTO { Description = "List Line", Price = 55m, Product = new ShiftEntitySelectDTO { Value = product.ID.ToString() } },
            },
        };

        var invoice = new Invoice();
        invoiceRepo.Add(invoice);
        await invoiceRepo.UpsertAsync(invoice, invoiceDto, ActionTypes.Insert, userId: null, idempotencyKey: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        await invoiceRepo.SaveChangesAsync();

        db.ChangeTracker.Clear();

        // Act
        var entityQuery = await invoiceRepo.GetIQueryable(asOf: null, includes: null, disableDefaultDataLevelAccess: true, disableGlobalFilters: true);
        var queryable = await invoiceRepo.OdataList(entityQuery);
        var listItems = await queryable.Where(x => x.ManualReference == "INV-LIST-001").ToListAsync();

        // Assert
        Assert.Single(listItems);
        var item = listItems[0];
        Assert.Equal("INV-LIST-001", item.ManualReference);
        Assert.Equal(new DateTimeOffset(2025, 10, 1, 0, 0, 0, TimeSpan.Zero), item.InvoiceDate);
    }

    #endregion
}
