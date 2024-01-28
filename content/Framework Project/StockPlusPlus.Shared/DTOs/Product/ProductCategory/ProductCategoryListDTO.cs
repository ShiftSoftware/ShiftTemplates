﻿
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Shared.Enums.Product;
using System.Text.Json.Serialization;

namespace StockPlusPlus.Shared.DTOs.Product.ProductCategory;

[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
public class ProductCategoryListDTO : ShiftEntityListDTO
{
    [_ProductCategoryHashId]
    public override string? ID { get; set; }

    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Code { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public TrackingMethod? TrackingMethod { get; set; }
}
