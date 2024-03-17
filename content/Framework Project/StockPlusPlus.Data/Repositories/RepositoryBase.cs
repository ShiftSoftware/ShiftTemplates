
using ShiftSoftware.ShiftEntity.Core;
using ShiftSoftware.ShiftEntity.EFCore;
using ShiftSoftware.ShiftEntity.Model.Dtos;

namespace StockPlusPlus.Data.Repositories;

public abstract class RepositoryBase<TEntity, TListDTO, TViewAndUpsertDTO> : ShiftRepository<DB, TEntity, TListDTO, TViewAndUpsertDTO>
    where TEntity : ShiftEntity<TEntity>, new()
    where TListDTO : ShiftEntityListDTO
    where TViewAndUpsertDTO : ShiftEntityViewAndUpsertDTO
{
    protected RepositoryBase(DB db, Action<ShiftRepositoryOptions<TEntity>>? shiftRepositoryBuilder = null) : base(db, shiftRepositoryBuilder)
    {
    }
}