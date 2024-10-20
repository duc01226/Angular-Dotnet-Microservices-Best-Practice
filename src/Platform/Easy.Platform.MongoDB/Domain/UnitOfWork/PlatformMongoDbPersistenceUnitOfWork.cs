using Easy.Platform.Common;
using Easy.Platform.Persistence.Domain;

namespace Easy.Platform.MongoDB.Domain.UnitOfWork;

public interface IPlatformMongoDbPersistenceUnitOfWork<out TDbContext> : IPlatformPersistenceUnitOfWork<TDbContext>
    where TDbContext : PlatformMongoDbContext<TDbContext>
{
}

public class PlatformMongoDbPersistenceUnitOfWork<TDbContext>
    : PlatformPersistenceUnitOfWork<TDbContext>, IPlatformMongoDbPersistenceUnitOfWork<TDbContext> where TDbContext : PlatformMongoDbContext<TDbContext>
{
    public PlatformMongoDbPersistenceUnitOfWork(IPlatformRootServiceProvider rootServiceProvider, IServiceProvider serviceProvider) : base(
        rootServiceProvider,
        serviceProvider)
    {
    }

    public override bool IsPseudoTransactionUow()
    {
        return true;
    }

    public override bool MustKeepUowForQuery()
    {
        return false;
    }

    public override bool DoesSupportParallelQuery()
    {
        return true;
    }

    protected override bool ShouldDisposeDbContext()
    {
        // Override and do not dispose db context because mongodb db context is singleton
        return false;
    }
}
