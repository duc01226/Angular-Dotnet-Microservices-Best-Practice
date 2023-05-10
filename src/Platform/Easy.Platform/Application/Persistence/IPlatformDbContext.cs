using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Easy.Platform.Common.Extensions;
using Easy.Platform.Domain.Entities;
using Easy.Platform.Domain.Exceptions.Extensions;
using Easy.Platform.Domain.UnitOfWork;
using Easy.Platform.Persistence;
using Easy.Platform.Persistence.DataMigration;
using Microsoft.Extensions.Logging;

namespace Easy.Platform.Application.Persistence;

public interface IPlatformDbContext : IDisposable
{
    public IQueryable<PlatformDataMigrationHistory> ApplicationDataMigrationHistoryQuery { get; }

    public IUnitOfWork? MappedUnitOfWork { get; set; }

    public static async Task<TResult> ExecuteWithBadQueryWarningHandling<TResult, TSource>(
        Func<Task<TResult>> getResultFn,
        ILogger logger,
        IPlatformPersistenceConfiguration persistenceConfiguration,
        bool forWriteQuery,
        [AllowNull] IEnumerable<TSource> resultQuery,
        [AllowNull] Func<string> resultQueryStringBuilder)
    {
        // Must use stack trace BEFORE await fn() BECAUSE after call get data function, the stack trace get lost because
        // some unknown reason (ToListAsync, FirstOrDefault, XXAsync from ef-core, mongo-db). Could be the thread/task context has been changed
        // after get data from database, it switched to I/O thread pool
        var loggingFullStackTrace = Environment.StackTrace;

        HandleLogTooMuchDataInMemoryBadQueryWarning(resultQuery, persistenceConfiguration, logger, loggingFullStackTrace, resultQueryStringBuilder);

        var result = await HandleLogSlowQueryBadQueryWarning(
            getResultFn,
            persistenceConfiguration,
            logger,
            loggingFullStackTrace,
            forWriteQuery,
            resultQueryStringBuilder);

        return result;

        static void HandleLogTooMuchDataInMemoryBadQueryWarning(
            [AllowNull] IEnumerable<TSource> resultQuery,
            IPlatformPersistenceConfiguration persistenceConfiguration,
            ILogger logger,
            string loggingFullStackTrace,
            [AllowNull] Func<string> resultQueryStringBuilder)
        {
            var queryForResultList = resultQuery?.ToList();

            var queryResultCount = queryForResultList?.Count;

            if (queryForResultList?.Count >= persistenceConfiguration.BadQueryWarning.TotalItemsThreshold)
                LogTooMuchDataInMemoryBadQueryWarning(queryResultCount.Value, logger, persistenceConfiguration, loggingFullStackTrace, resultQueryStringBuilder);
        }

        static async Task<TResult> HandleLogSlowQueryBadQueryWarning(
            Func<Task<TResult>> getResultFn,
            IPlatformPersistenceConfiguration persistenceConfiguration,
            ILogger logger,
            string loggingFullStackTrace,
            bool forWriteQuery,
            [AllowNull] Func<string> resultQueryStringBuilder)
        {
            var startQueryTimeStamp = Stopwatch.GetTimestamp();

            var result = await getResultFn();

            var queryElapsedTime = Stopwatch.GetElapsedTime(startQueryTimeStamp);

            if (queryElapsedTime.TotalMilliseconds >= persistenceConfiguration.BadQueryWarning.GetSlowQueryMillisecondsThreshold(forWriteQuery))
                LogSlowQueryBadQueryWarning(queryElapsedTime, logger, persistenceConfiguration, loggingFullStackTrace, resultQueryStringBuilder);
            return result;
        }
    }

    public static void LogSlowQueryBadQueryWarning(
        TimeSpan queryElapsedTime,
        ILogger logger,
        IPlatformPersistenceConfiguration persistenceConfiguration,
        string loggingStackTrace,
        [AllowNull] Func<string> resultQueryStringBuilder)
    {
        logger.Log(
            persistenceConfiguration.BadQueryWarning.IsLogWarningAsError ? LogLevel.Error : LogLevel.Warning,
            "[BadQueryWarning][IsLogWarningAsError:{IsLogWarningAsError}] Slow query execution. QueryElapsedTime.TotalMilliseconds:{QueryElapsedTime}. SlowQueryMillisecondsThreshold:{SlowQueryMillisecondsThreshold}. QueryString:{QueryString}. FullTrackTrace:{TrackTrace}",
            persistenceConfiguration.BadQueryWarning.IsLogWarningAsError,
            queryElapsedTime.TotalMilliseconds,
            persistenceConfiguration.BadQueryWarning.SlowQueryMillisecondsThreshold,
            resultQueryStringBuilder?.Invoke(),
            loggingStackTrace);
    }

    public static void LogTooMuchDataInMemoryBadQueryWarning(
        int totalCount,
        ILogger logger,
        IPlatformPersistenceConfiguration persistenceConfiguration,
        string loggingStackTrace,
        [AllowNull] Func<string> resultQueryStringBuilder)
    {
        logger.Log(
            persistenceConfiguration.BadQueryWarning.IsLogWarningAsError ? LogLevel.Error : LogLevel.Warning,
            "[BadQueryWarning][IsLogWarningAsError:{IsLogWarningAsError}] Get too much of items into memory query execution. TotalItems:{TotalItems}; Threshold:{Threshold}. QueryString:{QueryString}. FullTrackTrace:{TrackTrace}",
            persistenceConfiguration.BadQueryWarning.IsLogWarningAsError,
            totalCount,
            persistenceConfiguration.BadQueryWarning.TotalItemsThreshold,
            resultQueryStringBuilder?.Invoke(),
            loggingStackTrace);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default);

    public IQueryable<TEntity> GetQuery<TEntity>() where TEntity : class, IEntity;

    public void RunCommand(string command);

    public Task MigrateApplicationDataAsync(IServiceProvider serviceProvider);

    public Task Initialize(IServiceProvider serviceProvider);

    public Task<TSource> FirstOrDefaultAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default);

    public Task<TSource> FirstAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default);

    public Task<int> CountAsync<TEntity>(
        Expression<Func<TEntity, bool>> predicate = null,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity;

    public Task<int> CountAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default);

    public Task<bool> AnyAsync<TEntity>(
        Expression<Func<TEntity, bool>> predicate = null,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity;

    public Task<bool> AnyAsync<TSource>(
        IQueryable<TSource> source,
        CancellationToken cancellationToken = default);

    Task<TResult> FirstOrDefaultAsync<TEntity, TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> queryBuilder,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity;

    Task<List<T>> GetAllAsync<T>(IQueryable<T> source, CancellationToken cancellationToken = default);

    Task<List<TResult>> GetAllAsync<TEntity, TResult>(
        Func<IQueryable<TEntity>, IQueryable<TResult>> queryBuilder,
        CancellationToken cancellationToken = default)
        where TEntity : class, IEntity;

    public Task<List<TEntity>> CreateManyAsync<TEntity, TPrimaryKey>(
        List<TEntity> entities,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>, new();

    public Task<TEntity> UpdateAsync<TEntity, TPrimaryKey>(
        TEntity entity,
        bool dismissSendEvent,
        CancellationToken cancellationToken) where TEntity : class, IEntity<TPrimaryKey>, new();

    public Task<List<TEntity>> UpdateManyAsync<TEntity, TPrimaryKey>(
        List<TEntity> entities,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>, new();

    public async Task<List<TEntity>> UpdateManyAsync<TEntity, TPrimaryKey>(
        Expression<Func<TEntity, bool>> predicate,
        Action<TEntity> updateAction,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>, new()
    {
        var toUpdateEntities = await GetAllAsync<TEntity, TEntity>(query => query.Where(predicate), cancellationToken)
            .ThenAction(items => items.ForEach(updateAction));

        return await UpdateManyAsync<TEntity, TPrimaryKey>(toUpdateEntities, dismissSendEvent, cancellationToken);
    }

    public Task DeleteAsync<TEntity, TPrimaryKey>(
        TPrimaryKey entityId,
        bool dismissSendEvent,
        CancellationToken cancellationToken) where TEntity : class, IEntity<TPrimaryKey>, new();

    public Task DeleteAsync<TEntity, TPrimaryKey>(
        TEntity entity,
        bool dismissSendEvent,
        CancellationToken cancellationToken) where TEntity : class, IEntity<TPrimaryKey>, new();

    public Task<List<TEntity>> DeleteManyAsync<TEntity, TPrimaryKey>(
        List<TPrimaryKey> entityIds,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>, new();

    public Task<List<TEntity>> DeleteManyAsync<TEntity, TPrimaryKey>(
        List<TEntity> entities,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>, new();

    public async Task<List<TEntity>> DeleteManyAsync<TEntity, TPrimaryKey>(
        Expression<Func<TEntity, bool>> predicate,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>, new()
    {
        var toDeleteEntities = await GetAllAsync(GetQuery<TEntity>().Where(predicate), cancellationToken);

        return await DeleteManyAsync<TEntity, TPrimaryKey>(toDeleteEntities, dismissSendEvent, cancellationToken);
    }

    public Task<TEntity> CreateAsync<TEntity, TPrimaryKey>(
        TEntity entity,
        bool dismissSendEvent,
        CancellationToken cancellationToken) where TEntity : class, IEntity<TPrimaryKey>, new();

    public Task<TEntity> CreateOrUpdateAsync<TEntity, TPrimaryKey>(
        TEntity entity,
        Expression<Func<TEntity, bool>> customCheckExistingPredicate = null,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>, new();

    /// <summary>
    /// CreateOrUpdateManyAsync. <br />
    /// Example for customCheckExistingPredicate: createOrUpdateEntity => existingEntity => existingEntity.XXX == createOrUpdateEntity.XXX
    /// </summary>
    public Task<List<TEntity>> CreateOrUpdateManyAsync<TEntity, TPrimaryKey>(
        List<TEntity> entities,
        Func<TEntity, Expression<Func<TEntity, bool>>> customCheckExistingPredicateBuilder = null,
        bool dismissSendEvent = false,
        CancellationToken cancellationToken = default) where TEntity : class, IEntity<TPrimaryKey>, new();

    public async Task EnsureEntitiesValid<TEntity, TPrimaryKey>(List<TEntity> entities, CancellationToken cancellationToken)
        where TEntity : class, IEntity<TPrimaryKey>, new()
    {
        await entities.EnsureEntitiesValid<TEntity, TPrimaryKey>(
            (predicate, token) => AnyAsync(GetQuery<TEntity>().Where(predicate), token),
            cancellationToken);
    }

    public async Task EnsureEntityValid<TEntity, TPrimaryKey>(TEntity entity, CancellationToken cancellationToken)
        where TEntity : class, IEntity<TPrimaryKey>, new()
    {
        await entity.EnsureEntityValid<TEntity, TPrimaryKey>(
            (predicate, token) => AnyAsync(GetQuery<TEntity>().Where(predicate), token),
            cancellationToken);
    }
}
