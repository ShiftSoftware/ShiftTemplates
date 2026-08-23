using System;
using System.Collections.Generic;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using ShiftSoftware.ShiftIdentity.Data.Entities;

namespace StockPlusPlus.Test.Tests.Parity;

/// <summary>
/// The replication fixtures, shared by the parity tests and the one-shot golden capture so both arms are fed
/// byte-identical input. They were inline in <c>ReplicationMappingParityTests</c>; a golden is only meaningful
/// if the input that produced it is the input the assertion replays.
/// <para>
/// Every value is fixed — literal IDs, literal <see cref="DateTimeOffset"/>s, no <c>Now</c> and no
/// <c>NewGuid</c> — because a golden compares against a constant. Distinct non-default audit values so a
/// dropped audit field is visible rather than coincidentally equal to a default.
/// </para>
/// </summary>
public sealed class ReplicationFixtures
{
    private static void Id(ShiftEntityBase e, long id) => e.ID = id;

    private static void Audit<T>(ShiftEntity<T> e, bool deleted = false) where T : class
    {
        e.IsDeleted = deleted;
        e.CreateDate = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero);
        e.LastSaveDate = new DateTimeOffset(2026, 6, 7, 8, 9, 10, TimeSpan.Zero);
        e.CreatedByUserID = 111;
        e.LastSavedByUserID = 222;
    }

    public Brand Brand(bool deleted = false)
    {
        var b = new Brand { Name = "Acme", IntegrationId = "B-1", BrandID = 77 };
        Id(b, 10); Audit(b, deleted);
        return b;
    }

    public Service Service(bool deleted = false)
    {
        var s = new Service { Name = "Wash", IntegrationId = "S-1" };
        Id(s, 20); Audit(s, deleted);
        return s;
    }

    public Department Department(bool deleted = false)
    {
        var d = new Department { Name = "Sales", IntegrationId = "D-1" };
        Id(d, 30); Audit(d, deleted);
        return d;
    }

    public Country Country(bool deleted = false)
    {
        var c = new Country { Name = "Iraq", IntegrationId = "C-1", ShortCode = "IQ", CallingCode = "+964", Flag = "iq.png", DisplayOrder = 3, IsProtected = true };
        Id(c, 40); Audit(c, deleted);
        return c;
    }

    public Region Region(bool withCountry = true, bool deleted = false)
    {
        var r = new Region { Name = "KRG", IntegrationId = "R-1", ShortCode = "KRG", Flag = "krg.png", DisplayOrder = 2, CountryID = 40, IsProtected = true };
        if (withCountry) r.Country = Country();
        Id(r, 50); Audit(r, deleted);
        return r;
    }

    public City City(bool withRegion = true, bool deleted = false)
    {
        var c = new City { Name = "Erbil", IntegrationId = "CT-1", CountryID = 40, RegionID = 50, CityID = 9, DisplayOrder = 1, IsProtected = true };
        if (withRegion) c.Region = Region();
        Id(c, 60); Audit(c, deleted);
        return c;
    }

    public Company Company(bool deleted = false)
    {
        var c = new Company
        {
            Name = "Shift", LegalName = "Shift LLC", IntegrationId = "CO-1", ShortCode = "SFT",
            CompanyType = ShiftSoftware.ShiftEntity.Model.Enums.CompanyTypes.NotSpecified,
            Logo = "logo.png", HQPhone = "123", HQEmail = "hq@x.com", HQAddress = "St 1", Website = "x.com",
            IsProtected = true, ParentCompanyID = 5, CompanyID = 70, DisplayOrder = 4,
        };
        Id(c, 70); Audit(c, deleted);
        return c;
    }

    public CompanyBranch CompanyBranch(bool deleted = false)
    {
        var b = new CompanyBranch
        {
            Name = "Main", Phone = "555", ShortPhone = "5", Email = "b@x.com", Address = "Addr",
            IntegrationId = "BR-1", ShortCode = "MB", Longitude = "44.1", Latitude = "36.2",
            Photos = "p", MobilePhotos = "mp", WorkingHours = "9-5", WorkingDays = "Mon-Fri",
            IsProtected = true, RegionID = 50, CityID = 60, CompanyID = 70, CountryID = 40, CompanyBranchID = 88,
            DisplayOrder = 6, DisplayName = "Main Branch", Description = "desc",
            City = City(), Company = Company(),
        };
        Id(b, 80); Audit(b, deleted);
        return b;
    }

    public User User(bool deleted = false)
    {
        var u = new User
        {
            FullName = "Aza", Username = "aza", Phone = "999", Email = "aza@x.com", IntegrationId = "U-1",
            IsProtected = true, CompanyID = 70, CompanyBranchID = 88, RegionID = 50, CountryID = 40,
        };
        Id(u, 90); Audit(u, deleted);
        return u;
    }

    public Team Team(bool withBranchNav = true, bool deleted = false)
    {
        var t = new Team
        {
            Name = "Ops", IntegrationId = "T-1",
            Tags = withBranchNav ? new List<string> { "a", "b" } : new List<string>(),
            CompanyID = 70, TeamID = 5,
        };
        Id(t, 200); Audit(t, deleted);

        var tcb = new TeamCompanyBranch { CompanyBranchID = 88, TeamID = 200 };
        if (withBranchNav) tcb.CompanyBranch = CompanyBranch();
        Id(tcb, 300); Audit(tcb);

        t.TeamCompanyBranches = new List<TeamCompanyBranch> { tcb };
        return t;
    }

    // ── M:N join rows. withNav:false is THE runtime case: the join is inserted carrying only its FK, so the
    // Service/Department/Brand navigation is null and the mapping must null-propagate to a null name rather
    // than throw. A non-null-safe map NREs here and silently kills replication.

    public CompanyBranchService BranchService(bool withNav = true)
    {
        var j = new CompanyBranchService { CompanyBranchID = 88, ServiceID = 20 };
        if (withNav) j.Service = Service();
        Id(j, 100); Audit(j);
        return j;
    }

    public CompanyBranchDepartment BranchDepartment(bool withNav = true)
    {
        var j = new CompanyBranchDepartment { CompanyBranchID = 88, DepartmentID = 30 };
        if (withNav) j.Department = Department();
        Id(j, 101); Audit(j);
        return j;
    }

    public CompanyBranchBrand BranchBrand(bool withNav = true)
    {
        var j = new CompanyBranchBrand { CompanyBranchID = 88, BrandID = 10 };
        if (withNav) j.Brand = Brand();
        Id(j, 102); Audit(j);
        return j;
    }

    /// <summary>
    /// A POPULATED destination for the apply-onto (UpdateReference) cases. Populated on purpose: the question
    /// those cases answer is which members get OVERWRITTEN and which survive, and an empty destination cannot
    /// tell the difference. <c>BranchID</c> is the Cosmos partition key and is deliberately never rewritten.
    /// </summary>
    public CompanyBranchSubItemModel ExistingSubItem() =>
        new() { BranchID = "88", id = "999", Name = "old", ItemType = "old" };
}
