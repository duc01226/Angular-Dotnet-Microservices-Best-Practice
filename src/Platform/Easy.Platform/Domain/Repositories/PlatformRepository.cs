using System.Linq.Expressions;
using Easy.Platform.Common.Cqrs;
using Easy.Platform.Common.Extensions;
using Easy.Platform.Common.Utils;
using Easy.Platform.Domain.Entities;
using Easy.Platform.Domain.UnitOfWork;

namespace Easy.Platform.Domain.Repositories;

public abstract class PlatformRepository<TEntity, TPrimaryKey, TUow> : IPlatformQueryableRepository<TEntity, TPrimaryKey>
    where TEntity : class, IEntity<TPrimaryKey>, new()
    where TUow : class, IUnitOfWork
{
    public PlatformRepository(IUnitOfWorkManager unitOfWorkManager, IPlatformCqrs cqrs, IServiceProvider serviceProvider)
    {
        UnitOfWorkManager = unitOfWorkManager;
        Cqrs = cqrs;
        ServiceProvider = serviceProvider;
    }

    public IUnitOfWorkManager UnitOfWorkManager { get; }
    protected IPlatformCqrs Cqrs { get; }
    protected IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// Override this return True to force using the same current active uow for query. May only needed to support old legacy code using platform repository.
    /// </summary>
    protected virtual bool ForceUseSameCurrentActiveUowIfExistingForQuery => false;

    /// <summary>
    /// Return current active uow. May throw exception if not existing one.
    /// </summary>
    public IUnitOfWork CurrentActiveUow()
    {
        return UnitOfWorkManager.CurrentActiveUow().UowOfType<TUow>();
    }

    public IUnitOfWorkManager UowManager()
    {
        return UnitOfWorkManager;
    }

    public abstract Task<TEntity> GetByIdAsync(
        TPrimaryKey id,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities);

    public abstract Task<List<TEntity>> GetByIdsAsync(
        List<TPrimaryKey> ids,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities);

    public virtual async Task<List<TEntity>> GetAllAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities)
    {
        return await ExecuteAutoOpenUowUsingOnceTimeForRead(
            async (uow, query) => await GetAllAsync(query.WhereIf(predicate != null, predicate), cancellationToken),
            loadRelatedEntities);
    }

    public async Task<List<TEntity>> GetAllAsync(
        Func<IUnitOfWork, IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities)
    {
        return await ExecuteAutoOpenUowUsingOnceTimeForRead(
            async (uow, query) => await GetAllAsync(queryBuilder(uow, query), cancellationToken),
            loadRelatedEntities);
    }

    public abstract Task<List<TEntity>> GetAllAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken = default);

    public virtual IAsyncEnumerable<TEntity> GetAllAsyncEnumerable(
        Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForRead(
                (uow, query) => GetAllAsyncEnumerable(queryBuilder(query), cancellationToken),
                loadRelatedEntities)
            .GetResult();
    }

    public virtual IAsyncEnumerable<TEntity> GetAllAsyncEnumerable(
        Func<IUnitOfWork, IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities)
    {
        return ExecuteAutoOpenUowUsingOnceTimeForRead(
                (uow, query) => GetAllAsyncEnumerable(queryBuilder(uow, query), cancellationToken),
                loadRelatedEntities)
            .GetResult();
    }

    public abstract IAsyncEnumerable<TEntity> GetAllAsyncEnumerable(IQueryable<TEntity> query, CancellationToken cancellationToken = default);

    public abstract Task<TEntity> FirstAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities);

    public abstract Task<TEntity> FirstOrDefaultAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities);

    public Task<TEntity> FirstOrDefaultAsync(
        IQueryable<TEntity> query,
        CancellationToken cancellationToken = default)
    {
        return FirstOrDefaultAsync(query.As<IEnumerable<TEntity>>(), cancellationToken);
    }

    public abstract Task<int> CountAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        CancellationToken cancellationToken = default);

    public abstract Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate = null,
        CancellationToken cancellationToken = default);

    public abstract IEnumerable<TEntity> GetAllEnumerable(
        Expression<Func<TEntity, bool>> predicate = null,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities);

    public async Task<List<TEntity>> GetAllAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities)
    {
        return await ExecuteAutoOpenUowUsingOnceTimeForRead(
            async (uow, query) => await GetAllAsync(queryBuilder(query), cancellationToken),
            loadRelatedEntities);
    }

    public async Task<TEntity> FirstOrDefaultAsync(
        Func<IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities)
    {
        return await ExecuteAutoOpenUowUsingOnceTimeForRead(
            async (uow, query) => await FirstOrDefaultAsync(queryBuilder(query), cancellationToken),
            loadRelatedEntities);
    }

    public async Task<TEntity> FirstOrDefaultAsync(
        Func<IUnitOfWork, IQueryable<TEntity>, IQueryable<TEntity>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities)
    {
        return await ExecuteAutoOpenUowUsingOnceTimeForRead(
            async (uow, query) => await FirstOrDefaultAsync(queryBuilder(uow, query), cancellationToken),
            loadRelatedEntities);
    }

    public async Task<TSelector> FirstOrDefaultAsync<TSelector>(
        Func<IQueryable<TEntity>, IEnumerable<TSelector>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] loadRelatedEntities)
    {
        return await ExecuteAutoOpenUowUsingOnceTimeForRead(
            async (uow, query) => await queryBuilder(query).First().ToTask(),
            loadRelatedEntities);
    }

    public async Task<TSelector> FirstOrDefaultAsync<TSelector>(
        Func<IUnitOfWork, IQueryable<TEntity>, IEnumerable<TSelector>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] loadRelatedEntities)
    {
        return await ExecuteAutoOpenUowUsingOnceTimeForRead(
            async (uow, query) => await FirstOrDefaultAsync(queryBuilder(uow, query).AsQueryable()),
            loadRelatedEntities);
    }

    public abstract Task<List<TSelector>> GetAllAsync<TSelector>(
        Func<IQueryable<TEntity>, IQueryable<TSelector>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities);

    public abstract Task<List<TSelector>> GetAllAsync<TSelector>(
        Func<IUnitOfWork, IQueryable<TEntity>, IQueryable<TSelector>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities);

    public abstract Task<List<TSelector>> GetAllAsync<TSelector>(
        Func<IQueryable<TEntity>, IEnumerable<TSelector>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] loadRelatedEntities);

    public abstract Task<List<TSelector>> GetAllAsync<TSelector>(
        Func<IUnitOfWork, IQueryable<TEntity>, IEnumerable<TSelector>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object>>[] loadRelatedEntities);

    public abstract IAsyncEnumerable<TSelector> GetAllAsyncEnumerable<TSelector>(
        Func<IQueryable<TEntity>, IQueryable<TSelector>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities);

    public abstract IAsyncEnumerable<TSelector> GetAllAsyncEnumerable<TSelector>(
        Func<IUnitOfWork, IQueryable<TEntity>, IQueryable<TSelector>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities);

    public abstract Task<TSelector> FirstOrDefaultAsync<TSelector>(
        Func<IQueryable<TEntity>, IQueryable<TSelector>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities);

    public abstract Task<TSelector> FirstOrDefaultAsync<TSelector>(
        Func<IUnitOfWork, IQueryable<TEntity>, IQueryable<TSelector>> queryBuilder,
        CancellationToken cancellationToken = default,
        params Expression<Func<TEntity, object?>>[] loadRelatedEntities);

    public abstract Task<int> CountAsync<TQueryItemResult>(
        Func<IUnitOfWork, IQueryable<TEntity>, IQueryable<TQueryItemResult>> queryBuilder,
        CancellationToken cancellationToken = default);

    public abstract Task<int> CountAsync(IQueryable<TEntity> query, CancellationToken cancellationToken = default);

    public abstract Task<int> CountAsync<TQueryItemResult>(
        Func<IQueryable<TEntity>, IQueryable<TQueryItemResult>> queryBuilder,
        CancellationToken cancellationToken = default);

    public IQueryable<TEntity> GetGlobalUowQuery(params Expression<Func<TEntity, object?>>[] loadRelatedEntities)
    {
        return GetQuery(GlobalUow(), loadRelatedEntities);
    }

    public Func<IQueryable<TEntity>, IQueryable<TResult>> GetQueryBuilder<TResult>(Func<IQueryable<TEntity>, IQueryable<TResult>> builderFn)
    {
        return builderFn;
    }

    public Func<IUnitOfWork, IQueryable<TEntity>, IQueryable<TResult>> GetQueryBuilder<TResult>(Func<IUnitOfWork, IQueryable<TEntity>, IQueryable<TResult>> builderFn)
    {
        return builderFn;
    }

    public Func<IQueryable<TEntity>, IQueryable<TEntity>> GetQueryBuilder(Expression<Func<TEntity, bool>> predicate)
    {
        return query => query.Where(predicate);
    }

    public abstract IQueryable<TEntity> GetQuery(IUnitOfWork uow, params Expression<Func<TEntity, object?>>[] loadRelatedEntities);

    public IQueryable<TEntity> GetCurrentUowQuery(params Expression<Func<TEntity, object?>>[] loadRelatedEntities)
    {
        return GetQuery(CurrentActiveUow(), loadRelatedEntities);
    }

    public abstract Task<TSource> FirstOrDefaultAsync<TSource>(
        IEnumerable<TSource> query,
        CancellationToken cancellationToken = default);

    public async Task<List<TEntity>> UpdateManyAsync(
        Expression<Func<TEntity, bool>> predicate,
        Action<TEntity> updateAction,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default)
    {
        var updateItems = await GetAllAsync(predicate, cancellationToken)
            .ThenSideEffectAction(items => items.ForEach(updateAction));

        return await UpdateManyAsync(updateItems, dismissSendEvent, cancellationToken);
    }

    public async Task<List<TEntity>> DeleteManyAsync(
        Expression<Func<TEntity, bool>> predicate,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default)
    {
        var toDeleteEntities = await GetAllAsync(predicate, cancellationToken);

        return await DeleteManyAsync(toDeleteEntities, dismissSendEvent, cancellationToken);
    }

    public IUnitOfWork TryGetCurrentActiveUow()
    {
        return UnitOfWorkManager.TryGetCurrentActiveUow()?.UowOfType<TUow>();
    }

    public TUow GlobalUow()
    {
        return UnitOfWorkManager.GlobalUow.UowOfType<TUow>();
    }

    public abstract Task<TEntity> CreateAsync(
        TEntity entity,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default);

    public abstract Task<TEntity> CreateOrUpdateAsync(
        TEntity entity,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default);

    public abstract Task<List<TEntity>> CreateOrUpdateManyAsync(
        List<TEntity> entities,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default);

    public abstract Task<TEntity> UpdateAsync(
        TEntity entity,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default);

    public abstract Task DeleteAsync(
        TPrimaryKey entityId,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default);

    public abstract Task DeleteAsync(
        TEntity entity,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default);

    public abstract Task<List<TEntity>> CreateManyAsync(
        List<TEntity> entities,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default);

    public abstract Task<List<TEntity>> UpdateManyAsync(
        List<TEntity> entities,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default);

    public abstract Task<List<TEntity>> DeleteManyAsync(
        List<TPrimaryKey> entityIds,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default);

    public abstract Task<List<TEntity>> DeleteManyAsync(
        List<TEntity> entities,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default);

    public abstract Task<TEntity> CreateOrUpdateAsync(
        TEntity entity,
        Expression<Func<TEntity, bool>> customCheckExistingPredicate,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default);

    protected abstract void HandleDisposeUsingOnceTimeContextLogic<TResult>(
        IUnitOfWork uow,
        bool doesNeedKeepUowForQueryOrEnumerableExecutionLater,
        Expression<Func<TEntity, object>>[]? loadRelatedEntities,
        TResult result);

    protected virtual async Task<TResult> ExecuteAutoOpenUowUsingOnceTimeForRead<TResult>(
        Func<IUnitOfWork, IQueryable<TEntity>, Task<TResult>> readDataFn,
        Expression<Func<TEntity, object>>[]? loadRelatedEntities)
    {
        if (UnitOfWorkManager.TryGetCurrentActiveUow() == null ||
            (!UnitOfWorkManager.CurrentActiveUow().IsPseudoTransactionUow() && !ForceUseSameCurrentActiveUowIfExistingForQuery))
        {
            var uow = UnitOfWorkManager.CreateNewUow();

            var result = await readDataFn(uow, GetQuery(uow, loadRelatedEntities));

            HandleDisposeUsingOnceTimeContextLogic(uow, DoesNeedKeepUowForQueryOrEnumerableExecutionLater(result, uow), loadRelatedEntities, result);

            return result;
        }

        // Do retry if the uow do not support parallel query so that if there's other uow running query in parallel, it could retry get data again to have chance to make it work
        if (UnitOfWorkManager.CurrentActiveUow().DoesSupportParallelQuery() == false)
            return await Util.TaskRunner.WaitRetryThrowFinalExceptionAsync(
                async () => await ExecuteReadDataUsingCurrentActiveUow(readDataFn, loadRelatedEntities),
                retryAttempt => SupportParallelQueryRetrySleepTime(),
                retryCount: SupportParallelQueryRetryCount());

        return await ExecuteReadDataUsingCurrentActiveUow(readDataFn, loadRelatedEntities);
    }

    protected virtual int SupportParallelQueryRetryCount()
    {
        return 20;
    }

    protected virtual TimeSpan SupportParallelQueryRetrySleepTime()
    {
        return 100.Milliseconds();
    }

    protected virtual async Task<TResult> ExecuteReadDataUsingCurrentActiveUow<TResult>(
        Func<IUnitOfWork, IQueryable<TEntity>, Task<TResult>> readDataFn,
        Expression<Func<TEntity, object>>[]? loadRelatedEntities)
    {
        return await readDataFn(UnitOfWorkManager.CurrentActiveUow(), GetQuery(UnitOfWorkManager.CurrentActiveUow(), loadRelatedEntities));
    }

    protected virtual async Task<TResult> ExecuteAutoOpenUowUsingOnceTimeForRead<TResult>(
        Func<IUnitOfWork, IQueryable<TEntity>, TResult> readDataFn,
        Expression<Func<TEntity, object>>[]? loadRelatedEntities)
    {
        return await ExecuteAutoOpenUowUsingOnceTimeForRead(async (uow, entities) => await ReadDataFnAsync(uow, entities), loadRelatedEntities);

        async Task<TResult> ReadDataFnAsync(IUnitOfWork unitOfWork, IQueryable<TEntity> entities)
        {
            return await readDataFn(unitOfWork, entities).ToTask();
        }
    }

    protected virtual async Task<TResult> ExecuteAutoOpenUowUsingOnceTimeForWrite<TResult>(
        Func<IUnitOfWork, Task<TResult>> action)
    {
        if (UnitOfWorkManager.TryGetCurrentActiveUow() == null)
        {
            var uow = UnitOfWorkManager.CreateNewUow();

            var result = await action(uow);

            await uow.CompleteAsync();

            if (!DoesNeedKeepUowForQueryOrEnumerableExecutionLater(result, uow)) uow.Dispose();

            return result;
        }

        return await action(UnitOfWorkManager.CurrentActiveUow());
    }

    protected async Task ExecuteAutoOpenUowUsingOnceTimeForWrite(
        Func<IUnitOfWork, Task> action)
    {
        await ExecuteAutoOpenUowUsingOnceTimeForWrite(async uow => ActionWithResult(uow));

        async Task<object> ActionWithResult(IUnitOfWork unitOfWork)
        {
            await action(unitOfWork);

            return null;
        }
    }

    protected abstract bool DoesNeedKeepUowForQueryOrEnumerableExecutionLater<TResult>(TResult result, IUnitOfWork uow);
}
