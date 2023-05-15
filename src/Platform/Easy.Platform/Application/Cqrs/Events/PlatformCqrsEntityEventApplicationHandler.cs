using Easy.Platform.Application.MessageBus.Producers;
using Easy.Platform.Common.Extensions;
using Easy.Platform.Domain.Entities;
using Easy.Platform.Domain.Events;
using Easy.Platform.Domain.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace Easy.Platform.Application.Cqrs.Events;

public abstract class PlatformCqrsEntityEventApplicationHandler<TEntity> : PlatformCqrsEventApplicationHandler<PlatformCqrsEntityEvent<TEntity>>
    where TEntity : class, IEntity, new()
{
    protected PlatformCqrsEntityEventApplicationHandler(
        ILoggerFactory loggerFactory,
        IUnitOfWorkManager unitOfWorkManager) : base(
        loggerFactory,
        unitOfWorkManager)
    {
        IsInjectingApplicationBusMessageProducer = GetType().IsUsingGivenTypeViaConstructor<IPlatformApplicationBusMessageProducer>();
    }

    protected bool IsInjectingApplicationBusMessageProducer { get; }

    protected override bool AllowHandleInBackgroundThread(PlatformCqrsEntityEvent<TEntity> notification)
    {
        return !IsInjectingApplicationBusMessageProducer ||
               UnitOfWorkManager.TryGetCurrentActiveUow() == null ||
               UnitOfWorkManager.CurrentActiveUow().IsPseudoTransactionUow();
    }

    protected override bool HandleWhen(PlatformCqrsEntityEvent<TEntity> @event)
    {
        return @event.CrudAction switch
        {
            PlatformCqrsEntityEventCrudAction.Created => true,
            PlatformCqrsEntityEventCrudAction.Updated => true,
            PlatformCqrsEntityEventCrudAction.Deleted => true,
            _ => false
        };
    }
}
