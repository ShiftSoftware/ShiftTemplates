using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.Model.Dtos;
using StockPlusPlus.Data.Entities;
using StockPlusPlus.Data.Repositories;
using StockPlusPlus.Shared.DTOs.Vehicle;

namespace StockPlusPlus.Test.Tests;

/// <summary>
/// Pins Vehicle's mapping through the repository's public MapToView / MapToEntity / MapToList / CopyEntity,
/// so the same assertions hold whichever door the repository uses (hand-written overrides or ShiftMapper's
/// automatic maps). The repository comes from the API host, so it maps with the host's IMapper exactly as a
/// request would; nothing here is saved.
/// </summary>
[Collection("API Collection")]
public class VehicleMappingTests : IDisposable
{
    private readonly IServiceScope scope;

    public VehicleMappingTests(CustomWebApplicationFactory factory) => scope = factory.Services.CreateScope();

    public void Dispose() => scope.Dispose();

    private VehicleRepository CreateRepository() => scope.ServiceProvider.GetRequiredService<VehicleRepository>();

    [Fact]
    public void MapToView_CopiesVin_AndTurnsCompanyKeysIntoSelectDTOs()
    {
        var created = new DateTimeOffset(2026, 9, 1, 8, 0, 0, TimeSpan.Zero);
        var entity = new Vehicle
        {
            ID = 42,
            VIN = "1HGCM82633A004352",
            CompanyID = 7,
            IntermediaryCompanyID = 9,
            CreateDate = created,
            CreatedByUserID = 3,
        };

        var dto = CreateRepository().MapToView(entity);

        Assert.Equal("42", dto.ID);
        Assert.Equal("1HGCM82633A004352", dto.VIN);
        Assert.Equal("7", dto.Company!.Value);
        Assert.Equal("9", dto.IntermediaryCompany!.Value);
        Assert.Equal(created, dto.CreateDate);
        Assert.Equal("3", dto.CreatedByUserID);
    }

    [Fact]
    public void MapToView_MissingCompanies_GiveNullSelectDTOs()
    {
        var dto = CreateRepository().MapToView(new Vehicle { VIN = "X", CompanyID = null, IntermediaryCompanyID = null });

        Assert.Null(dto.Company);
        Assert.Null(dto.IntermediaryCompany);
    }

    [Fact]
    public void MapToEntity_WritesVin_AndParsesCompanySelectDTOsBackToKeys()
    {
        var existing = new Vehicle { VIN = "OLD", CompanyID = 1, IntermediaryCompanyID = 2 };

        var result = CreateRepository().MapToEntity(new VehicleDTO
        {
            VIN = "NEW",
            Company = new ShiftEntitySelectDTO { Value = "11" },
            IntermediaryCompany = null,
        }, existing);

        Assert.Same(existing, result);
        Assert.Equal("NEW", existing.VIN);
        Assert.Equal(11, existing.CompanyID);
        Assert.Null(existing.IntermediaryCompanyID);
    }

    // Generated MapToEntity writes the audit fields from the DTO by default, and on UPDATE the AuditStamper only
    // refreshes LastSaveDate / LastSavedByUserID — so a request body could rewrite CreateDate / CreatedByUserID.
    // The hand-written Vehicle mapping never wrote them; the repository keeps that with IgnoreEntity (automapper
    // removal plan, Q7 / gap C-3: "the guard belongs in the repository or an explicit IgnoreEntity").
    [Fact]
    public void MapToEntity_LeavesServerControlledAuditFieldsAlone()
    {
        var created = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var existing = new Vehicle { VIN = "OLD", CreateDate = created, CreatedByUserID = 3, LastSaveDate = created, LastSavedByUserID = 3 };

        CreateRepository().MapToEntity(new VehicleDTO
        {
            VIN = "NEW",
            CreateDate = new DateTimeOffset(1999, 1, 1, 0, 0, 0, TimeSpan.Zero),
            CreatedByUserID = "666",
            LastSaveDate = default,
            LastSavedByUserID = null,
        }, existing);

        Assert.Equal("NEW", existing.VIN);
        Assert.Equal(created, existing.CreateDate);
        Assert.Equal(3, existing.CreatedByUserID);
        Assert.Equal(created, existing.LastSaveDate);
        Assert.Equal(3, existing.LastSavedByUserID);
    }

    [Fact]
    public void MapToList_ProjectsVin_AndCompanyKeysAsStrings()
    {
        var vehicles = new List<Vehicle>
        {
            new() { ID = 1, VIN = "A", CompanyID = 5, IntermediaryCompanyID = null, IsDeleted = false },
            new() { ID = 2, VIN = "B", CompanyID = null, IntermediaryCompanyID = 8, IsDeleted = true },
        }.AsQueryable();

        var rows = CreateRepository().MapToList(vehicles).ToList();

        Assert.Equal(2, rows.Count);

        Assert.Equal("1", rows[0].ID);
        Assert.Equal("A", rows[0].VIN);
        Assert.Equal("5", rows[0].CompanyID);
        Assert.Null(rows[0].IntermediaryCompanyID);
        Assert.False(rows[0].IsDeleted);

        Assert.Equal("2", rows[1].ID);
        Assert.Null(rows[1].CompanyID);
        Assert.Equal("8", rows[1].IntermediaryCompanyID);
        Assert.True(rows[1].IsDeleted);
    }

    [Fact]
    public void CopyEntity_CopiesVinAndCompanies_ButNotTheID()
    {
        var source = new Vehicle { ID = 1, VIN = "SRC", CompanyID = 5, IntermediaryCompanyID = 6, IsDeleted = true };
        var target = new Vehicle { ID = 99, VIN = "TGT" };

        CreateRepository().CopyEntity(source, target);

        Assert.Equal(99, target.ID);
        Assert.Equal("SRC", target.VIN);
        Assert.Equal(5, target.CompanyID);
        Assert.Equal(6, target.IntermediaryCompanyID);
        Assert.True(target.IsDeleted);
    }
}
