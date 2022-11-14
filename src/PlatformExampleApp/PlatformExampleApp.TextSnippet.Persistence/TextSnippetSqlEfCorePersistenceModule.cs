using System.Linq.Expressions;
using Easy.Platform.EfCore;
using Easy.Platform.EfCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace PlatformExampleApp.TextSnippet.Persistence;

public class TextSnippetSqlEfCorePersistenceModule : PlatformEfCorePersistenceModule<TextSnippetDbContext>
{
    public TextSnippetSqlEfCorePersistenceModule(
        IServiceProvider serviceProvider,
        IConfiguration configuration) : base(serviceProvider, configuration)
    {
    }

    // Override using fulltext search index for BETTER PERFORMANCE
    protected override EfCorePlatformFullTextSearchPersistenceService FullTextSearchPersistenceServiceProvider(IServiceProvider serviceProvider)
    {
        return new TextSnippetSqlEfCorePlatformFullTextSearchPersistenceService(serviceProvider);
    }

    protected override bool EnableInboxBusMessage()
    {
        return true;
    }

    protected override bool EnableOutboxBusMessage()
    {
        return true;
    }

    // This example config help to override to config outbox config
    //protected override PlatformOutboxConfig OutboxConfigProvider(IServiceProvider serviceProvider)
    //{
    //    var defaultConfig = new PlatformOutboxConfig
    //    {
    //        // You may only want to set this to true only when you are using mix old system and new platform code. You do not call uow.complete
    //        // after call sendMessages. This will force sending message always start use there own uow
    //        ForceAlwaysSendOutboxInNewUow = true
    //    };

    //    return defaultConfig;
    //}

    protected override Action<DbContextOptionsBuilder> DbContextOptionsBuilderActionProvider(
        IServiceProvider serviceProvider)
    {
        return options =>
            options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"));
    }
}

public class TextSnippetSqlEfCorePlatformFullTextSearchPersistenceService : EfCorePlatformFullTextSearchPersistenceService
{
    public TextSnippetSqlEfCorePlatformFullTextSearchPersistenceService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
    }

    public static Expression<Func<TEntity, bool>> BuildSqlFullTextSearchPropPredicate<TEntity>(string fullTextSearchPropName, string searchWord)
    {
        return entity => EF.Functions.Contains(EF.Property<string>(entity, fullTextSearchPropName), searchWord);
    }

    protected override Expression<Func<TEntity, bool>> BuildFullTextSearchPropPredicate<TEntity>(string fullTextSearchPropName, string searchWord)
    {
        return BuildSqlFullTextSearchPropPredicate<TEntity>(fullTextSearchPropName, searchWord);
    }
}