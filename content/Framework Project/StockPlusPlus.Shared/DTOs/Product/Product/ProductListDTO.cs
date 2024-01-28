﻿
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Shared.DTOs.Product.ProductCategory;
using StockPlusPlus.Shared.Enums.Product;
using System.Text.Json.Serialization;

namespace StockPlusPlus.Shared.DTOs.Product.Product;

public class ProductListDTO : ShiftEntityListDTO
{
    [_ProductHashId]
    public override string? ID { get; set; }
    public string Name { get; set; } = default!;

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string? Brand { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string? Category { get; set; }

    [_ProductCategoryHashId]
    public string? ProductCategoryID { get; set; }

    public string? BrandID { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TrackingMethod TrackingMethod { get; set; }
    public DateTimeOffset ReleaseDate { get; set; }
    public DateTimeOffset LastSaveDate { get; set; }
}
