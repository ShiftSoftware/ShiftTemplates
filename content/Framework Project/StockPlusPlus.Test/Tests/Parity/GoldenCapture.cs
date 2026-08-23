using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using AutoMapper;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Model.Replication.IdentityModels;
using ShiftSoftware.ShiftIdentity.Data.Entities;
using Xunit;

namespace StockPlusPlus.Test.Tests.Parity;

/// <summary>
/// ONE-SHOT TOOL, not a test of anything. Run it to print the AutoMapper arm's output for every replication
/// fixture, so those values can be frozen as constants in <c>ReplicationMappingParityTests</c>.
/// <para>
/// It exists because the oracle disappears. Today AutoMapper is still in the container, so "what should this
/// document look like?" has an authoritative answer. At Step F1 <c>AddShiftIdentityAutoMapper()</c> becomes
/// <c>[Obsolete(error: true)]</c> and that answer is gone permanently — after which the replication tests would
/// be re-deriving their expectation from the implementation under test, which proves nothing.
/// </para>
/// <para>
/// Skipped by default so it never runs in CI. To regenerate: remove the Skip, run it, and copy the printed
/// block. Deleting this file after F1 is correct — there is nothing left for it to capture.
/// </para>
/// </summary>
[Collection("API Collection")]
public class GoldenCapture
{
    private readonly IMapper mapper;

    public GoldenCapture(CustomWebApplicationFactory factory)
    {
        mapper = factory.Services.GetRequiredService<IMapper>();
    }

    [Fact(Skip = "One-shot capture tool. Remove the Skip to regenerate the goldens, then restore it.")]
    public void Capture()
    {
        var captured = new List<(string Name, string Json)>();

        void Emit<TModel>(string name, Func<TModel> map) =>
            captured.Add((name, JsonSerializer.Serialize(map(), new JsonSerializerOptions { WriteIndented = false })));

        var f = new ReplicationFixtures();

        Emit("Brand", () => mapper.Map<BrandModel>(f.Brand()));
        Emit("Service", () => mapper.Map<ServiceModel>(f.Service()));
        Emit("Department", () => mapper.Map<DepartmentModel>(f.Department()));
        Emit("Country", () => mapper.Map<CountryModel>(f.Country()));
        Emit("Region", () => mapper.Map<RegionModel>(f.Region()));
        Emit("RegionCityRegion", () => mapper.Map<CityRegionModel>(f.Region()));
        Emit("City", () => mapper.Map<CityModel>(f.City()));
        Emit("CityCompanyBranch", () => mapper.Map<CityCompanyBranchModel>(f.City()));
        Emit("Company", () => mapper.Map<CompanyModel>(f.Company()));
        Emit("CompanyBranch", () => mapper.Map<CompanyBranchModel>(f.CompanyBranch()));
        Emit("User", () => mapper.Map<UserModel>(f.User()));
        Emit("Team", () => mapper.Map<TeamModel>(f.Team()));

        Emit("CompanyBranchService_SubItem", () => mapper.Map<CompanyBranchSubItemModel>(f.BranchService()));
        Emit("CompanyBranchDepartment_SubItem", () => mapper.Map<CompanyBranchSubItemModel>(f.BranchDepartment()));
        Emit("CompanyBranchBrand_SubItem", () => mapper.Map<CompanyBranchSubItemModel>(f.BranchBrand()));

        Emit("CompanyBranchService_NullNav", () => mapper.Map<CompanyBranchSubItemModel>(f.BranchService(withNav: false)));
        Emit("CompanyBranchDepartment_NullNav", () => mapper.Map<CompanyBranchSubItemModel>(f.BranchDepartment(withNav: false)));
        Emit("CompanyBranchBrand_NullNav", () => mapper.Map<CompanyBranchSubItemModel>(f.BranchBrand(withNav: false)));
        Emit("Team_NullBranchNav", () => mapper.Map<TeamModel>(f.Team(withBranchNav: false)));

        Emit("Brand_ApplyToSubItem", () => { var d = f.ExistingSubItem(); mapper.Map(f.Brand(), d); return d; });
        Emit("Service_ApplyToSubItem", () => { var d = f.ExistingSubItem(); mapper.Map(f.Service(), d); return d; });
        Emit("Department_ApplyToSubItem", () => { var d = f.ExistingSubItem(); mapper.Map(f.Department(), d); return d; });

        // The tombstone case the existing suite has zero coverage of: every fixture is IsDeleted = false, so a
        // deleted row's document — the whole reason replication propagates the flag — is never exercised.
        Emit("Brand_Deleted", () => mapper.Map<BrandModel>(f.Brand(deleted: true)));
        Emit("CompanyBranch_Deleted", () => mapper.Map<CompanyBranchModel>(f.CompanyBranch(deleted: true)));

        var sb = new StringBuilder();
        foreach (var (name, json) in captured)
            sb.AppendLine($"    private const string {name}Golden =").AppendLine($"        {Quote(json)};").AppendLine();

        var path = Path.Combine(Path.GetTempPath(), "replication-goldens.txt");
        File.WriteAllText(path, sb.ToString());

        Assert.True(false, $"Captured {captured.Count} goldens to {path}\n\n{sb}");
    }

    private static string Quote(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
