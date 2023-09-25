using Easy.Platform.Common;
using Easy.Platform.Domain.Entities;
using Easy.Platform.Domain.Events;
using Easy.Platform.Domain.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace Easy.Platform.Application.Cqrs.Events;

public abstract class PlatformCqrsBulkEntitiesEventApplicationHandler<TEntity, TPrimaryKey>
    : PlatformCqrsEventApplicationHandler<PlatformCqrsBulkEntitiesEvent<TEntity, TPrimaryKey>>
    where TEntity : class, IEntity<TPrimaryKey>, new()
{
    protected PlatformCqrsBulkEntitiesEventApplicationHandler(
        ILoggerFactory loggerFactory,
        IUnitOfWorkManager unitOfWorkManager,
        IServiceProvider serviceProvider,
        IPlatformRootServiceProvider rootServiceProvider) : base(
        loggerFactory,
        unitOfWorkManager,
        serviceProvider,
        rootServiceProvider)
    {
    }
}
