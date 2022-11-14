using System.Linq.Expressions;
using Easy.Platform.Application.Persistence;
using Easy.Platform.Common.Cqrs;
using Easy.Platform.Common.Extensions;
using Easy.Platform.Domain.Entities;
using Easy.Platform.Domain.Repositories;
using Easy.Platform.Domain.UnitOfWork;

namespace Easy.Platform.Persistence.Domain;

public abstract class PlatformPersistenceRepository<TEntity, TPrimaryKey, TUow, TDbContext> : PlatformRepository<TEntity, TPrimaryKey, TUow>
    where TEntity : class, IEntity<TPrimaryKey>, new()
    where TUow : class, IPlatformPersistenceUnitOfWork<TDbContext>
    where TDbContext : IPlatformDbContext
{
    protected PlatformPersistenceRepository(
        IUnitOfWorkManager unitOfWorkManager,
        IPlatformCqrs cqrs,
        IServiceProvider serviceProvider) : base(unitOfWorkManager, cqrs, serviceProvider)
    {
    }

    protected virtual TDbContext DbContext => GetUowDbContext(CurrentActiveUow());

    public TDbContext GetUowDbContext(IUnitOfWork uow)
    {
        return uow.UowOfType<TUow>().DbContext;
    }

    public abstract Task<List<TSource>> ToListAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default);

    public abstract Task<TSource> FirstOrDefaultAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default);

    public abstract Task<TSource> FirstAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default);

    public abstract Task<int> CountAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default);

    public abstract Task<bool> AnyAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default);

    public override Task<TEntity> GetByIdAsync(TPrimaryKey id, CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForRead(
            (uow, query) => FirstAsync(query.Where(p => p.Id.Equals(id)), cancellationToken));
    }

    public override Task<List<TEntity>> GetByIdsAsync(List<TPrimaryKey> ids, CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForRead(
            (uow, query) => ToListAsync(query.Where(p => ids.Contains(p.Id)), cancellationToken));
    }

    public override Task<List<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForRead(
            (uow, query) => ToListAsync(query.WhereIf(predicate != null, predicate), cancellationToken));
    }

    public override Task<List<TEntity>> GetAllAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default)
    {
        return ToListAsync(query, cancellationToken);
    }

    public override Task<TEntity> FirstOrDefaultAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        return FirstOrDefaultAsync(query, cancellationToken);
    }

    public override Task<TEntity> FirstAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForRead(
            (uow, query) => FirstAsync(query.WhereIf(predicate != null, predicate), cancellationToken));
    }

    public override Task<TEntity> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForRead(
            (uow, query) => FirstOrDefaultAsync(query.WhereIf(predicate != null, predicate), cancellationToken));
    }

    public override Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForRead(
            (uow, query) => CountAsync(query.WhereIf(predicate != null, predicate), cancellationToken));
    }

    public override Task<int> CountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default)
    {
        return CountAsync(query, cancellationToken);
    }

    public override Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForRead(
            (uow, query) => AnyAsync(query.WhereIf(predicate != null, predicate), cancellationToken));
    }

    public override Task<int> CountAsync<TQueryItemResult>(
        Func<IQueryable<TEntity>, IQueryable<TQueryItemResult>> queryBuilder,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForRead(
            (uow, query) => CountAsync(queryBuilder(query), cancellationToken));
    }

    public override Task<int> CountAsync<TQueryItemResult>(
        Func<IUnitOfWork, IQueryable<TEntity>, IQueryable<TQueryItemResult>> queryBuilder,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForRead(
            (uow, query) => CountAsync(queryBuilder(uow, query), cancellationToken));
    }

    public override Task<List<TSelector>> GetAllAsync<TSelector>(
        Func<IQueryable<TEntity>, IQueryable<TSelector>> queryBuilder,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForRead(
            (uow, query) => ToListAsync(queryBuilder(query), cancellationToken));
    }

    public override Task<List<TSelector>> GetAllAsync<TSelector>(
        Func<IUnitOfWork, IQueryable<TEntity>, IQueryable<TSelector>> queryBuilder,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForRead(
            (uow, query) => ToListAsync(queryBuilder(uow, query), cancellationToken));
    }

    public override Task<TSelector> FirstOrDefaultAsync<TSelector>(
        Func<IQueryable<TEntity>, IQueryable<TSelector>> queryBuilder,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForRead(
            (uow, query) => FirstOrDefaultAsync(queryBuilder(query), cancellationToken));
    }

    public override Task<TSelector> FirstOrDefaultAsync<TSelector>(
        Func<IUnitOfWork, IQueryable<TEntity>, IQueryable<TSelector>> queryBuilder,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForRead(
            (uow, query) => FirstOrDefaultAsync(queryBuilder(uow, query), cancellationToken));
    }

    public override async Task<TEntity> CreateAsync(
        TEntity entity,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAutoOpenUowUsingOnceTimeForWrite(
            uow => GetUowDbContext(uow).CreateAsync<TEntity, TPrimaryKey>(entity, dismissSendEvent, cancellationToken));
    }

    public override Task<TEntity> CreateOrUpdateAsync(
        TEntity entity,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForWrite(
            uow => GetUowDbContext(uow).CreateOrUpdateAsync<TEntity, TPrimaryKey>(entity, null, dismissSendEvent, cancellationToken));
    }

    public override Task<TEntity> CreateOrUpdateAsync(
        TEntity entity,
        Expression<Func<TEntity, bool>> customCheckExistingPredicate = null,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForWrite(
            uow => GetUowDbContext(uow).CreateOrUpdateAsync<TEntity, TPrimaryKey>(entity, customCheckExistingPredicate, dismissSendEvent, cancellationToken));
    }

    public override async Task<List<TEntity>> CreateOrUpdateManyAsync(
        List<TEntity> entities,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default)
    {
        if (entities.IsEmpty()) return entities;

        return await ExecuteAutoOpenUowUsingOnceTimeForWrite(
            uow => GetUowDbContext(uow).CreateOrUpdateManyAsync<TEntity, TPrimaryKey>(entities, null, dismissSendEvent, cancellationToken));
    }

    public override async Task<TEntity> UpdateAsync(
        TEntity entity,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAutoOpenUowUsingOnceTimeForWrite(
            uow => GetUowDbContext(uow).UpdateAsync<TEntity, TPrimaryKey>(entity, dismissSendEvent, cancellationToken));
    }

    public override Task DeleteAsync(
        TPrimaryKey entityId,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForWrite(
            uow => GetUowDbContext(uow).DeleteAsync<TEntity, TPrimaryKey>(entityId, dismissSendEvent, cancellationToken));
    }

    public override Task DeleteAsync(
        TEntity entity,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForWrite(
            uow => GetUowDbContext(uow).DeleteAsync<TEntity, TPrimaryKey>(entity, dismissSendEvent, cancellationToken));
    }

    public override async Task<List<TEntity>> CreateManyAsync(
        List<TEntity> entities,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default)
    {
        if (entities.IsEmpty()) return entities;

        return await ExecuteAutoOpenUowUsingOnceTimeForWrite(
            uow => GetUowDbContext(uow).CreateManyAsync<TEntity, TPrimaryKey>(entities, dismissSendEvent, cancellationToken));
    }

    public override async Task<List<TEntity>> UpdateManyAsync(
        List<TEntity> entities,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default)
    {
        if (entities.IsEmpty()) return entities;

        return await ExecuteAutoOpenUowUsingOnceTimeForWrite(
            uow => GetUowDbContext(uow).UpdateManyAsync<TEntity, TPrimaryKey>(entities, dismissSendEvent, cancellationToken));
    }

    public virtual async Task<List<TEntity>> UpdateManyAsync(
        Expression<Func<TEntity, bool>> predicate,
        Action<TEntity> updateAction,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAutoOpenUowUsingOnceTimeForWrite(
            uow => GetUowDbContext(uow).UpdateManyAsync<TEntity, TPrimaryKey>(predicate, updateAction, dismissSendEvent, cancellationToken));
    }

    public override async Task<List<TEntity>> DeleteManyAsync(
        List<TPrimaryKey> entityIds,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteAutoOpenUowUsingOnceTimeForWrite(
            uow => GetUowDbContext(uow).DeleteManyAsync<TEntity, TPrimaryKey>(entityIds, dismissSendEvent, cancellationToken));
    }

    public override async Task<List<TEntity>> DeleteManyAsync(
        List<TEntity> entities,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default)
    {
        if (entities.IsEmpty()) return entities;

        return await ExecuteAutoOpenUowUsingOnceTimeForWrite(
            uow => GetUowDbContext(uow).DeleteManyAsync<TEntity, TPrimaryKey>(entities, dismissSendEvent, cancellationToken));
    }
}