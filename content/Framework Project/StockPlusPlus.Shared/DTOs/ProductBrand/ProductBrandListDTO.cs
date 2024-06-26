﻿
using ShiftSoftware.ShiftEntity.Model.Dtos;
#if (includeItemTemplateContent)
using ShiftSoftware.ShiftEntity.Model;
using System.Text.Json.Serialization;
#endif

namespace StockPlusPlus.Shared.DTOs.ProductBrand;
#if (includeItemTemplateContent)
[ShiftEntityKeyAndName(nameof(ID), nameof(Name))]
#endif
public class ProductBrandListDTO : ShiftEntityListDTO
{
    public override string? ID { get; set; }
#if (includeItemTemplateContent)
    [JsonConverter(typeof(LocalizedTextJsonConverter))]
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? Code { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.UserHashIdConverter]
    public string? CreatedByUserID { get; set; }

    [ShiftSoftware.ShiftEntity.Model.HashIds.TeamHashIdConverter]
    public string? TeamID { get; set; }
#endif
}