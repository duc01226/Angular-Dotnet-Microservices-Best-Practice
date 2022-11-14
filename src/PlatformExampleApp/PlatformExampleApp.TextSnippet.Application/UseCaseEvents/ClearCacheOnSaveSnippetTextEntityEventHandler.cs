using Easy.Platform.Application.Cqrs.Events;
using Easy.Platform.Domain.Events;
using Easy.Platform.Domain.UnitOfWork;
using Easy.Platform.Infrastructures.Caching;
using Microsoft.Extensions.Logging;
using PlatformExampleApp.TextSnippet.Application.Caching;
using PlatformExampleApp.TextSnippet.Domain.Entities;

namespace PlatformExampleApp.TextSnippet.Application.UseCaseEvents;

public class ClearCacheOnSaveSnippetTextEntityEventHandler : PlatformCqrsEntityEventApplicationHandler<TextSnippetEntity>
{
    private readonly IPlatformCacheRepositoryProvider cacheRepositoryProvider;

    public ClearCacheOnSaveSnippetTextEntityEventHandler(
        ILoggerFactory loggerFactory,
        IUnitOfWorkManager unitOfWorkManager,
        IPlatformCacheRepositoryProvider cacheRepositoryProvider) : base(loggerFactory, unitOfWorkManager)
    {
        this.cacheRepositoryProvider = cacheRepositoryProvider;
    }

    protected override bool ExecuteSeparatelyInBackgroundThread()
    {
        return true;
    }

    protected override async Task HandleAsync(
        PlatformCqrsEntityEvent<TextSnippetEntity> @event,
        CancellationToken cancellationToken)
    {
        // Queue task to clear cache every 5 seconds for 3 times (mean that after 5,10,15s).
        // Delay because when save snippet text, fulltext index take amount of time to update, so that we wait
        // amount of time for fulltext index update
        // We also set executeOnceImmediately=true to clear cache immediately in case of some index is updated fast
        await Util.TaskRunner.QueueIntervalAsyncAction(
            token => cacheRepositoryProvider.Get().RemoveCollectionAsync<TextSnippetCollectionCacheKeyProvider>(token),
            intervalTimeInSeconds: 5,
            maximumIntervalExecutionCount: 3,
            executeOnceImmediately: true,
            cancellationToken);

        // In other service if you want to run something in the background thread with scope, follow this example
        // Util.TaskRunner.QueueAsyncActionInBackground(
        //             token => PlatformRootServiceProvider.RootServiceProvider
        //                 .ExecuteInjectScopedAsync(
        //                     (IPlatformCacheRepositoryProvider cacheRepositoryProvider) =>
        //                     {
        //                         cacheRepositoryProvider.Get().RemoveCollectionAsync[TextSnippetCollectionCacheKeyProvider](token);
        //                     }),
        //             TimeSpan.Zero,
        //             logger,
        //             cancellationToken);
    }
}