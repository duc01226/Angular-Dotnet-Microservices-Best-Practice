using Easy.Platform.Domain.Exceptions;
using Easy.Platform.Domain.UnitOfWork;
using Easy.Platform.Persistence;
using Easy.Platform.Persistence.Domain;
using Microsoft.EntityFrameworkCore;

namespace Easy.Platform.EfCore.Domain.UnitOfWork;

public interface IPlatformEfCorePersistenceUnitOfWork<out TDbContext> : IPlatformPersistenceUnitOfWork<TDbContext>
    where TDbContext : PlatformEfCoreDbContext<TDbContext>
{
}

public class PlatformEfCorePersistenceUnitOfWork<TDbContext>
    : PlatformPersistenceUnitOfWork<TDbContext>, IPlatformEfCorePersistenceUnitOfWork<TDbContext> where TDbContext : PlatformEfCoreDbContext<TDbContext>
{
    public PlatformEfCorePersistenceUnitOfWork(
        TDbContext dbContext,
        PlatformPersistenceConfiguration<TDbContext> persistenceConfiguration,
        DbContextOptions<TDbContext> dbContextOptions) : base(dbContext)
    {
        PersistenceConfiguration = persistenceConfiguration;
        DbContextOptions = dbContextOptions;
    }

    protected IPlatformPersistenceConfiguration PersistenceConfiguration { get; }

    protected DbContextOptions<TDbContext> DbContextOptions { get; }

    public override async Task CompleteAsync(CancellationToken cancellationToken = default)
    {
        if (Completed)
            return;

        // Store stack trace before save changes so that if something went wrong in save into db, stack trace could
        // be tracked. Because call to db if failed lose stack trace
        var fullStackTrace = Environment.StackTrace;

        try
        {
            await InnerUnitOfWorks.Where(p => p.IsActive()).Select(p => p.CompleteAsync(cancellationToken)).WhenAll();

            await SaveChangesAsync(cancellationToken);

            Completed = true;

            InvokeOnCompleted(this, EventArgs.Empty);
        }
        catch (DbUpdateConcurrencyException e)
        {
            throw new PlatformDomainRowVersionConflictException(
                $"{GetType().Name} complete uow failed. [Message: {e.Message}]. [FullStackTrace:{fullStackTrace}]",
                e);
        }
        catch (Exception e)
        {
            InvokeOnFailed(this, new UnitOfWorkFailedArgs(e));
            throw new Exception($"{GetType().Name} complete uow failed. [Message: {e.Message}]. [FullStackTrace:{fullStackTrace}]", e);
        }
    }

    public override bool IsPseudoTransactionUow()
    {
        return false;
    }

    public override bool MustKeepUowForQuery()
    {
        if (PersistenceConfiguration.MustKeepUowForQuery == null)
            return DbContextOptions.IsUsingLazyLoadingProxy();

        return PersistenceConfiguration.MustKeepUowForQuery == true;
    }

    public override bool DoesSupportParallelQuery()
    {
        return false;
    }

    protected override async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await DbContext.SaveChangesAsync(cancellationToken);
    }
}
