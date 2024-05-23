using Easy.Platform.Common;
using Easy.Platform.Common.Extensions;
using Easy.Platform.Common.HostingBackgroundServices;
using Easy.Platform.Common.Utils;
using Easy.Platform.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Easy.Platform.Application.MessageBus.InboxPattern;

public class PlatformInboxBusMessageCleanerHostedService : PlatformIntervalHostingBackgroundService
{
    public const int MinimumRetryCleanInboxMessageTimesToWarning = 3;

    private bool isProcessing;

    public PlatformInboxBusMessageCleanerHostedService(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory,
        IPlatformApplicationSettingContext applicationSettingContext,
        PlatformInboxConfig inboxConfig) : base(serviceProvider, loggerFactory)
    {
        ApplicationSettingContext = applicationSettingContext;
        InboxConfig = inboxConfig;
    }

    public override bool LogIntervalProcessInformation => InboxConfig.LogIntervalProcessInformation;

    protected IPlatformApplicationSettingContext ApplicationSettingContext { get; }

    protected PlatformInboxConfig InboxConfig { get; }

    protected override async Task IntervalProcessAsync(CancellationToken cancellationToken)
    {
        await IPlatformModule.WaitAllModulesInitiatedAsync(ServiceProvider, typeof(IPlatformPersistenceModule), Logger, $"process {GetType().Name}");

        if (!HasInboxEventBusMessageRepositoryRegistered() || isProcessing) return;

        isProcessing = true;

        try
        {
            // WHY: Retry in case of the db is not started, initiated or restarting
            await Util.TaskRunner.WaitRetryThrowFinalExceptionAsync(
                () => CleanInboxEventBusMessage(cancellationToken),
                retryAttempt => 10.Seconds(),
                retryCount: ProcessClearMessageRetryCount(),
                onRetry: (ex, timeSpan, currentRetry, ctx) =>
                {
                    if (currentRetry >= MinimumRetryCleanInboxMessageTimesToWarning)
                        Logger.LogError(
                            ex.BeautifyStackTrace(),
                            "Retry CleanInboxEventBusMessage {CurrentRetry} time(s) failed. [ApplicationName:{ApplicationSettingContext.ApplicationName}]. [ApplicationAssembly:{ApplicationSettingContext.ApplicationAssembly.FullName}]",
                            currentRetry,
                            ApplicationSettingContext.ApplicationName,
                            ApplicationSettingContext.ApplicationAssembly.FullName);
                },
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Logger.LogError(
                ex.BeautifyStackTrace(),
                "CleanInboxEventBusMessage failed. [ApplicationName:{ApplicationSettingContext.ApplicationName}]. [ApplicationAssembly:{ApplicationSettingContext.ApplicationAssembly.FullName}]",
                ApplicationSettingContext.ApplicationName,
                ApplicationSettingContext.ApplicationAssembly.FullName);
        }

        isProcessing = false;
    }

    protected override TimeSpan ProcessTriggerIntervalTime()
    {
        return InboxConfig.MessageCleanerTriggerIntervalInMinutes.Minutes();
    }

    protected virtual int ProcessClearMessageRetryCount()
    {
        return InboxConfig.ProcessClearMessageRetryCount;
    }

    /// <inheritdoc cref="PlatformInboxConfig.NumberOfDeleteMessagesBatch" />
    protected virtual int NumberOfDeleteMessagesBatch()
    {
        return InboxConfig.NumberOfDeleteMessagesBatch;
    }

    protected bool HasInboxEventBusMessageRepositoryRegistered()
    {
        return ServiceProvider.ExecuteScoped(scope => scope.ServiceProvider.GetService<IPlatformInboxBusMessageRepository>() != null);
    }

    protected async Task CleanInboxEventBusMessage(CancellationToken cancellationToken)
    {
        await ProcessCleanMessageByMaxStoreProcessedMessageCount(cancellationToken);
        await ProcessCleanMessageByExpiredTime(cancellationToken);
        await ProcessIgnoreFailedMessageByExpiredTime(cancellationToken);
    }

    private async Task ProcessCleanMessageByMaxStoreProcessedMessageCount(CancellationToken cancellationToken)
    {
        try
        {
            var totalProcessedMessages = await ServiceProvider.ExecuteScopedAsync(
                p => p.ServiceProvider.GetRequiredService<IPlatformInboxBusMessageRepository>()
                    .CountAsync(p => p.ConsumeStatus == PlatformInboxBusMessage.ConsumeStatuses.Processed, cancellationToken));
            if (totalProcessedMessages <= InboxConfig.MaxStoreProcessedMessageCount) return;

            await ServiceProvider.ExecuteInjectScopedScrollingPagingAsync<PlatformInboxBusMessage>(
                maxExecutionCount: await ServiceProvider.ExecuteScopedAsync(
                    p => p.ServiceProvider.GetRequiredService<IPlatformInboxBusMessageRepository>()
                        .CountAsync(p => p.ConsumeStatus == PlatformInboxBusMessage.ConsumeStatuses.Processed, cancellationToken: cancellationToken)
                        .Then(total => total / NumberOfDeleteMessagesBatch())),
                async (IPlatformInboxBusMessageRepository inboxEventBusMessageRepo) =>
                {
                    var toDeleteMessages = await inboxEventBusMessageRepo.GetAllAsync(
                        queryBuilder: query => query
                            .Where(p => p.ConsumeStatus == PlatformInboxBusMessage.ConsumeStatuses.Processed)
                            .OrderByDescending(p => p.CreatedDate)
                            .Skip(InboxConfig.MaxStoreProcessedMessageCount)
                            .Take(NumberOfDeleteMessagesBatch()),
                        cancellationToken);

                    if (toDeleteMessages.Count > 0)
                        await inboxEventBusMessageRepo.DeleteManyAsync(
                            toDeleteMessages,
                            dismissSendEvent: true,
                            eventCustomConfig: null,
                            cancellationToken);

                    return toDeleteMessages;
                });

            Logger.LogInformation(
                "ProcessCleanMessageByMaxStoreProcessedMessageCount success. Number of deleted messages: {DeletedMessagesCount}",
                totalProcessedMessages - InboxConfig.MaxStoreProcessedMessageCount);
        }
        catch (Exception e)
        {
            Logger.LogError(e.BeautifyStackTrace(), "ProcessCleanMessageByMaxStoreProcessedMessageCount failed");
        }
    }

    private async Task ProcessCleanMessageByExpiredTime(CancellationToken cancellationToken)
    {
        try
        {
            var toDeleteMessageCount = await ServiceProvider.ExecuteScopedAsync(
                p => p.ServiceProvider.GetRequiredService<IPlatformInboxBusMessageRepository>()
                    .CountAsync(
                        PlatformInboxBusMessage.ToCleanExpiredMessagesExpr(
                            InboxConfig.DeleteProcessedMessageInSeconds,
                            InboxConfig.DeleteExpiredIgnoredMessageInSeconds),
                        cancellationToken));

            if (toDeleteMessageCount > 0)
            {
                await ServiceProvider.ExecuteInjectScopedScrollingPagingAsync<PlatformInboxBusMessage>(
                    maxExecutionCount: toDeleteMessageCount / NumberOfDeleteMessagesBatch(),
                    async (IPlatformInboxBusMessageRepository inboxEventBusMessageRepo) =>
                    {
                        var expiredMessages = await inboxEventBusMessageRepo.GetAllAsync(
                            queryBuilder: query => query
                                .Where(
                                    PlatformInboxBusMessage.ToCleanExpiredMessagesExpr(
                                        InboxConfig.DeleteProcessedMessageInSeconds,
                                        InboxConfig.DeleteExpiredIgnoredMessageInSeconds))
                                .OrderBy(p => p.CreatedDate)
                                .Take(NumberOfDeleteMessagesBatch()),
                            cancellationToken);

                        if (expiredMessages.Count > 0)
                            await inboxEventBusMessageRepo.DeleteManyAsync(
                                expiredMessages,
                                dismissSendEvent: true,
                                eventCustomConfig: null,
                                cancellationToken);

                        return expiredMessages;
                    });

                Logger.LogInformation("ProcessCleanMessageByExpiredTime success. Number of deleted messages: {DeletedMessageCount}", toDeleteMessageCount);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e.BeautifyStackTrace(), "ProcessCleanMessageByExpiredTime failed");
        }
    }

    private async Task ProcessIgnoreFailedMessageByExpiredTime(CancellationToken cancellationToken)
    {
        try
        {
            var toIgnoreMessageCount = await ServiceProvider.ExecuteScopedAsync(
                p => p.ServiceProvider.GetRequiredService<IPlatformInboxBusMessageRepository>()
                    .CountAsync(
                        PlatformInboxBusMessage.ToIgnoreFailedExpiredMessagesExpr(
                            InboxConfig.IgnoreExpiredFailedMessageInSeconds),
                        cancellationToken));

            if (toIgnoreMessageCount > 0)
            {
                await ServiceProvider.ExecuteInjectScopedScrollingPagingAsync<PlatformInboxBusMessage>(
                    maxExecutionCount: toIgnoreMessageCount / NumberOfDeleteMessagesBatch(),
                    async (IPlatformInboxBusMessageRepository inboxEventBusMessageRepo) =>
                    {
                        var expiredMessages = await inboxEventBusMessageRepo.GetAllAsync(
                            queryBuilder: query => query
                                .Where(
                                    PlatformInboxBusMessage.ToIgnoreFailedExpiredMessagesExpr(
                                        InboxConfig.IgnoreExpiredFailedMessageInSeconds))
                                .OrderBy(p => p.CreatedDate)
                                .Take(NumberOfDeleteMessagesBatch()),
                            cancellationToken);

                        if (expiredMessages.Count > 0)
                            await inboxEventBusMessageRepo.UpdateManyAsync(
                                expiredMessages.SelectList(p => p.With(x => x.ConsumeStatus = PlatformInboxBusMessage.ConsumeStatuses.Ignored)),
                                dismissSendEvent: true,
                                eventCustomConfig: null,
                                cancellationToken);

                        return expiredMessages;
                    });

                Logger.LogInformation("ProcessIgnoreFailedMessageByExpiredTime success. Number of ignored messages: {DeletedMessageCount}", toIgnoreMessageCount);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e.BeautifyStackTrace(), "ProcessIgnoreFailedMessageByExpiredTime failed");
        }
    }
}
