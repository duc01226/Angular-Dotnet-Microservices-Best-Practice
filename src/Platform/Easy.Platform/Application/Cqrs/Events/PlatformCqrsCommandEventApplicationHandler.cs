using Easy.Platform.Common.Cqrs.Commands;
using Easy.Platform.Domain.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace Easy.Platform.Application.Cqrs.Events;

public abstract class PlatformCqrsCommandEventApplicationHandler<TCommand> : PlatformCqrsEventApplicationHandler<PlatformCqrsCommandEvent<TCommand>>
    where TCommand : class, IPlatformCqrsCommand, new()
{
    protected PlatformCqrsCommandEventApplicationHandler(
        ILoggerFactory loggerBuilder,
        IUnitOfWorkManager unitOfWorkManager) : base(
        loggerBuilder,
        unitOfWorkManager)
    {
    }

    protected override bool HandleWhen(PlatformCqrsCommandEvent<TCommand> @event)
    {
        return @event.Action == PlatformCqrsCommandEventAction.Executed;
    }
}
