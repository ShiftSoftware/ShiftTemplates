using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftEntity.EFCore;
using StockPlusPlus.Data.DbContext;
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.Model;
using StockPlusPlus.Shared.ActionTrees;
using StockPlusPlus.Shared.DTOs.SampleContact;

namespace StockPlusPlus.Data.Entities;

// Synthetic contacts demonstrate opt-in on independent list and detail DTOs.
[ShiftEntityKeyAndName(nameof(ID), nameof(Label))]
[ShiftEntitySecureEndpoint<SampleContactListDTO, SampleContactDTO, StockPlusPlusActionTree>(
    "api/sample-contact", nameof(StockPlusPlusActionTree.SampleContacts))]
public class SampleContact : ShiftEntity<SampleContact>,
    IConfiguresShiftRepository<SampleContact, SampleContactListDTO, SampleContactDTO>,
    IUpsertsShiftRepository<SampleContact, SampleContactListDTO, SampleContactDTO>,
    IUpsertsShiftRepository<SampleContact, SampleContactMinimalListDTO, SampleContactMinimalDTO>
{
    public string Label { get; set; } = default!;
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Identifier { get; set; }
    public ICollection<SampleContactPhone> Phones { get; set; } = new List<SampleContactPhone>();

    public void ConfigureRepository(ShiftRepositoryConfigurationContext<SampleContact, SampleContactListDTO, SampleContactDTO> context)
        => context.Options.IncludeRelatedEntitiesWithFindAsync(x => x.Include(contact => contact.Phones));

    public ValueTask<SampleContact> UpsertAsync(SampleContact entity, SampleContactDTO dto,
        ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess,
        bool disableGlobalFilters, ShiftRepositoryUpsertContext<SampleContact, SampleContactListDTO, SampleContactDTO> context)
    {
        RemoveOldPhones(entity, actionType, context.Services);
        return context.Base();
    }

    public ValueTask<SampleContact> UpsertAsync(SampleContact entity, SampleContactMinimalDTO dto,
        ActionTypes actionType, long? userId, Guid? idempotencyKey, bool disableDefaultDataLevelAccess,
        bool disableGlobalFilters, ShiftRepositoryUpsertContext<SampleContact, SampleContactMinimalListDTO, SampleContactMinimalDTO> context)
    {
        RemoveOldPhones(entity, actionType, context.Services);
        return context.Base();
    }

    // Like invoice lines, the generated write map recreates this sample's child rows.
    // The PII policy resolves keep values by the old child IDs before this hook runs.
    private static void RemoveOldPhones(SampleContact entity, ActionTypes actionType, IServiceProvider services)
    {
        if (actionType == ActionTypes.Update)
            services.GetRequiredService<DB>().SampleContactPhones.RemoveRange(entity.Phones);
    }
}
