﻿

using AutoMapper;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Data.ReplicationModels;
using StockPlusPlus.Shared.DTOs.Product.Product;

namespace StockPlusPlus.Data.AutoMapperProfiles.Product;

public class Product : Profile
{
    public Product()
    {
        CreateMap<Entities.Product.Product, ProductDTO>()
            //We don't need to write mapping for Brand. There's a default mapper for ShiftEntity -> ShiftEntitySelectDto
            //.ForMember(
            //        dest => dest.Brand,
            //        opt => opt.MapFrom(src => new ShiftEntitySelectDTO { Value = src.BrandID.ToString()!, Text = src.Brand == null ? null : src.Brand.Name })
            //    )
            //The default mapper for ShiftEntity -> ShiftEntitySelectDto is not working for ProductCategory. Because product.ProductCategory is not included. So we use ProductCategoryID instead.
            .ForMember(
                    dest => dest.ProductCategory,
                    opt => opt.MapFrom(src => new ShiftEntitySelectDTO { Value = src.ProductCategoryID.ToString()!, Text = src.ProductCategory == null ? null : src.ProductCategory.Name })
                )
            .ReverseMap()
            .ForMember(dest => dest.ProductCategory, opt => opt.Ignore())
            .ForMember(dest => dest.Brand, opt => opt.Ignore())
            .ForMember(dest => dest.CountryOfOrigin, opt => opt.Ignore())
            .ForMember(
                    dest => dest.ProductCategoryID,
                    opt => opt.MapFrom(src => src.ProductCategory.Value.ToLong())
                )
            .ForMember(
                    dest => dest.BrandID,
                    opt => opt.MapFrom(src => src.Brand.Value.ToLong())
                )
            .ForMember(
                    dest => dest.CountryOfOriginID,
                    opt => opt.MapFrom(src => src.CountryOfOrigin == null ? new Nullable<long>() : src.CountryOfOrigin.Value.ToLong())
                );

        CreateMap<Entities.Product.Product, ProductListDTO>()
            .ForMember(dest => dest.Brand, opt => opt.MapFrom(src => src.Brand == null ? null : src.Brand.Name))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.ProductCategory == null ? null : src.ProductCategory.Name));

        CreateMap<Entities.Product.Product, ProductModel>();
    }
}
