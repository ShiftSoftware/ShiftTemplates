using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using ShiftSoftware.ShiftIdentity.Data.Entities;
using ShiftSoftware.ShiftIdentity.Data.Replication;
using System;
using System.Collections.Generic;
using System.Text.Json;
using Xunit;

namespace StockPlusPlus.Test.Tests;

// Correctness guard for the AutoMapper-free Cosmos replication mappings (ShiftIdentity.Data/Replication +
// Dashboard.AspNetCore/Replication). For every Entity → *Model map it runs BOTH the host's AutoMapper profile and the
// hand-written ToXModel()/ApplyTo…() and asserts the two documents are byte-identical (serialized JSON). This is the
// "replication map — compiles fine, only fails in production" regression the plan flags, caught without a Cosmos emulator.
[Collection("API Collection")]
public class ReplicationMappingParityTests
{
    private readonly IMapper mapper;

    public ReplicationMappingParityTests(CustomWebApplicationFactory factory)
    {
        // The real replication mapper — the same AutoMapper the trigger used before this change.
        mapper = factory.Services.GetRequiredService<IMapper>();
    }

    // Both objects are the same CLR type, so System.Text.Json serializes them deterministically (same property order,
    // same attribute converters applied to both) — string equality proves every mapped value matches.
    private static void AssertParity<TModel>(TModel autoMapped, TModel manual)
    {
        var opts = new JsonSerializerOptions { WriteIndented = false };
        var a = JsonSerializer.Serialize(autoMapped, opts);
        var m = JsonSerializer.Serialize(manual, opts);
        Assert.Equal(a, m);
    }

    // Distinct, non-default audit values so a dropped audit field is caught.
    private static void Audit(ShiftSoftware.ShiftEntity.Core.ShiftEntityBase e, long id)
    {
        e.ID = id;
    }

    private static void AuditDates<T>(ShiftSoftware.ShiftEntity.Core.ShiftEntity<T> e) where T : class
    {
        e.IsDeleted = false;
        e.CreateDate = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        e.LastSaveDate = new DateTimeOffset(2026, 6, 7, 8, 9, 10, TimeSpan.Zero);
        e.CreatedByUserID = 111;
        e.LastSavedByUserID = 222;
    }

    private static Brand SampleBrand()
    {
        var b = new Brand { Name = "Acme", IntegrationId = "B-1", BrandID = 77 };
        Audit(b, 10); AuditDates(b);
        return b;
    }

    private static Service SampleService()
    {
        var s = new Service { Name = "Wash", IntegrationId = "S-1" };
        Audit(s, 20); AuditDates(s);
        return s;
    }

    private static Department SampleDepartment()
    {
        var d = new Department { Name = "Sales", IntegrationId = "D-1" };
        Audit(d, 30); AuditDates(d);
        return d;
    }

    private static Country SampleCountry()
    {
        var c = new Country { Name = "Iraq", IntegrationId = "C-1", ShortCode = "IQ", CallingCode = "+964", Flag = "iq.png", DisplayOrder = 3, IsProtected = true };
        Audit(c, 40); AuditDates(c);
        return c;
    }

    private static Region SampleRegion(bool withCountry = true)
    {
        var r = new Region { Name = "KRG", IntegrationId = "R-1", ShortCode = "KRG", Flag = "krg.png", DisplayOrder = 2, CountryID = 40, IsProtected = true };
        if (withCountry) r.Country = SampleCountry();
        Audit(r, 50); AuditDates(r);
        return r;
    }

    private static City SampleCity(bool withRegion = true)
    {
        var c = new City { Name = "Erbil", IntegrationId = "CT-1", CountryID = 40, RegionID = 50, CityID = 9, DisplayOrder = 1, IsProtected = true };
        if (withRegion) c.Region = SampleRegion();
        Audit(c, 60); AuditDates(c);
        return c;
    }

    private static Company SampleCompany()
    {
        var c = new Company
        {
            Name = "Shift", LegalName = "Shift LLC", IntegrationId = "CO-1", ShortCode = "SFT",
            CompanyType = ShiftSoftware.ShiftEntity.Model.Enums.CompanyTypes.NotSpecified,
            Logo = "logo.png", HQPhone = "123", HQEmail = "hq@x.com", HQAddress = "St 1", Website = "x.com",
            IsProtected = true, ParentCompanyID = 5, CompanyID = 70, DisplayOrder = 4,
        };
        Audit(c, 70); AuditDates(c);
        return c;
    }

    private static CompanyBranch SampleCompanyBranch()
    {
        var b = new CompanyBranch
        {
            Name = "Main", Phone = "555", ShortPhone = "5", Email = "b@x.com", Address = "Addr",
            IntegrationId = "BR-1", ShortCode = "MB", Longitude = "44.1", Latitude = "36.2",
            Photos = "p", MobilePhotos = "mp", WorkingHours = "9-5", WorkingDays = "Mon-Fri",
            IsProtected = true, RegionID = 50, CityID = 60, CompanyID = 70, CountryID = 40, CompanyBranchID = 88,
            DisplayOrder = 6, DisplayName = "Main Branch", Description = "desc",
            City = SampleCity(), Company = SampleCompany(),
        };
        Audit(b, 80); AuditDates(b);
        return b;
    }

    [Fact] public void Brand_Parity() => AssertParity(mapper.Map<BrandModel>(SampleBrand()), SampleBrand().ToBrandModel());
    [Fact] public void Service_Parity() => AssertParity(mapper.Map<ServiceModel>(SampleService()), SampleService().ToServiceModel());
    [Fact] public void Department_Parity() => AssertParity(mapper.Map<DepartmentModel>(SampleDepartment()), SampleDepartment().ToDepartmentModel());
    [Fact] public void Country_Parity() => AssertParity(mapper.Map<CountryModel>(SampleCountry()), SampleCountry().ToCountryModel());
    [Fact] public void Region_Parity() => AssertParity(mapper.Map<RegionModel>(SampleRegion()), SampleRegion().ToRegionModel());
    [Fact] public void RegionCityRegion_Parity() => AssertParity(mapper.Map<CityRegionModel>(SampleRegion()), SampleRegion().ToCityRegionModel());
    [Fact] public void City_Parity() => AssertParity(mapper.Map<CityModel>(SampleCity()), SampleCity().ToCityModel());
    [Fact] public void CityCompanyBranch_Parity() => AssertParity(mapper.Map<CityCompanyBranchModel>(SampleCity()), SampleCity().ToCityCompanyBranchModel());
    [Fact] public void Company_Parity() => AssertParity(mapper.Map<CompanyModel>(SampleCompany()), SampleCompany().ToCompanyModel());
    [Fact] public void CompanyBranch_Parity() => AssertParity(mapper.Map<CompanyBranchModel>(SampleCompanyBranch()), SampleCompanyBranch().ToCompanyBranchModel());
    [Fact] public void User_Parity() => AssertParity(mapper.Map<UserModel>(SampleUser()), SampleUser().ToUserModel());

    [Fact]
    public void Team_Parity()
    {
        var t = SampleTeam();
        AssertParity(mapper.Map<TeamModel>(t), t.ToTeamModel());
    }

    private static Team SampleTeam()
    {
        var t = new Team { Name = "Ops", IntegrationId = "T-1", Tags = new List<string> { "a", "b" }, CompanyID = 70, TeamID = 5 };
        Audit(t, 200); AuditDates(t);
        var tcb = new TeamCompanyBranch { CompanyBranchID = 88, TeamID = 200, CompanyBranch = SampleCompanyBranch() };
        Audit(tcb, 300); AuditDates(tcb);
        t.TeamCompanyBranches = new List<TeamCompanyBranch> { tcb };
        return t;
    }

    private static ShiftSoftware.ShiftIdentity.Data.Entities.User SampleUser()
    {
        var u = new ShiftSoftware.ShiftIdentity.Data.Entities.User
        {
            FullName = "Aza", Username = "aza", Phone = "999", Email = "aza@x.com", IntegrationId = "U-1",
            IsProtected = true, CompanyID = 70, CompanyBranchID = 88, RegionID = 50, CountryID = 40,
        };
        Audit(u, 90); AuditDates(u);
        return u;
    }

    // ── M:N join entities → CompanyBranchSubItemModel (fresh) ──
    [Fact]
    public void CompanyBranchService_SubItem_Parity()
    {
        var j = new CompanyBranchService { CompanyBranchID = 88, ServiceID = 20, Service = SampleService() };
        Audit(j, 100); AuditDates(j);
        AssertParity(mapper.Map<CompanyBranchSubItemModel>(j), j.ToCompanyBranchSubItemModel());
    }

    [Fact]
    public void CompanyBranchDepartment_SubItem_Parity()
    {
        var j = new CompanyBranchDepartment { CompanyBranchID = 88, DepartmentID = 30, Department = SampleDepartment() };
        Audit(j, 101); AuditDates(j);
        AssertParity(mapper.Map<CompanyBranchSubItemModel>(j), j.ToCompanyBranchSubItemModel());
    }

    [Fact]
    public void CompanyBranchBrand_SubItem_Parity()
    {
        var j = new CompanyBranchBrand { CompanyBranchID = 88, BrandID = 10, Brand = SampleBrand() };
        Audit(j, 102); AuditDates(j);
        AssertParity(mapper.Map<CompanyBranchSubItemModel>(j), j.ToCompanyBranchSubItemModel());
    }

    // ── The ACTUAL runtime case: the join is inserted with only its FK (Service/Department/Brand nav is NULL). Both
    // AutoMapper and the manual map must null-propagate to a null name — NOT throw. (This is the bug the user hit:
    // a non-null-safe manual map NREs here and silently kills replication, while AutoMapper produced a null-name doc.)
    [Fact]
    public void CompanyBranchService_NullServiceNav_Parity()
    {
        var j = new CompanyBranchService { CompanyBranchID = 88, ServiceID = 20 }; // Service nav intentionally null
        Audit(j, 100); AuditDates(j);
        AssertParity(mapper.Map<CompanyBranchSubItemModel>(j), j.ToCompanyBranchSubItemModel());
    }

    [Fact]
    public void CompanyBranchDepartment_NullDepartmentNav_Parity()
    {
        var j = new CompanyBranchDepartment { CompanyBranchID = 88, DepartmentID = 30 };
        Audit(j, 101); AuditDates(j);
        AssertParity(mapper.Map<CompanyBranchSubItemModel>(j), j.ToCompanyBranchSubItemModel());
    }

    [Fact]
    public void CompanyBranchBrand_NullBrandNav_Parity()
    {
        var j = new CompanyBranchBrand { CompanyBranchID = 88, BrandID = 10 };
        Audit(j, 102); AuditDates(j);
        AssertParity(mapper.Map<CompanyBranchSubItemModel>(j), j.ToCompanyBranchSubItemModel());
    }

    [Fact]
    public void Team_NullBranchNav_Parity()
    {
        var t = new Team { Name = "Ops", IntegrationId = "T-1", Tags = new List<string>(), CompanyID = 70, TeamID = 5 };
        Audit(t, 200); AuditDates(t);
        var tcb = new TeamCompanyBranch { CompanyBranchID = 88, TeamID = 200 }; // CompanyBranch nav intentionally null
        Audit(tcb, 300); AuditDates(tcb);
        t.TeamCompanyBranches = new List<TeamCompanyBranch> { tcb };
        AssertParity(mapper.Map<TeamModel>(t), t.ToTeamModel());
    }

    // ── Brand/Service/Department → CompanyBranchSubItemModel ONTO an existing doc (UpdateReference) ──
    [Fact]
    public void Brand_ApplyToSubItem_Parity()
    {
        var brand = SampleBrand();
        var existingAuto = new CompanyBranchSubItemModel { BranchID = "88", id = "999", Name = "old", ItemType = "old" };
        var existingManual = new CompanyBranchSubItemModel { BranchID = "88", id = "999", Name = "old", ItemType = "old" };
        mapper.Map(brand, existingAuto);
        brand.ApplyToCompanyBranchSubItem(existingManual);
        AssertParity(existingAuto, existingManual);
    }

    [Fact]
    public void Service_ApplyToSubItem_Parity()
    {
        var service = SampleService();
        var existingAuto = new CompanyBranchSubItemModel { BranchID = "88", id = "999", Name = "old", ItemType = "old" };
        var existingManual = new CompanyBranchSubItemModel { BranchID = "88", id = "999", Name = "old", ItemType = "old" };
        mapper.Map(service, existingAuto);
        service.ApplyToCompanyBranchSubItem(existingManual);
        AssertParity(existingAuto, existingManual);
    }

    [Fact]
    public void Department_ApplyToSubItem_Parity()
    {
        var dep = SampleDepartment();
        var existingAuto = new CompanyBranchSubItemModel { BranchID = "88", id = "999", Name = "old", ItemType = "old" };
        var existingManual = new CompanyBranchSubItemModel { BranchID = "88", id = "999", Name = "old", ItemType = "old" };
        mapper.Map(dep, existingAuto);
        dep.ApplyToCompanyBranchSubItem(existingManual);
        AssertParity(existingAuto, existingManual);
    }
}
