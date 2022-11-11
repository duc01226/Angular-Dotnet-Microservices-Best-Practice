using Easy.Platform.Common.Hosting;
using Microsoft.Extensions.Logging;

namespace Easy.Platform.RabbitMQ;

/// <summary>
/// This service main purpose is to configure RabbitMq Exchange, Declare Queue for each Consumer based on Consumer
/// Name/Consumer Message Name via RoutingKey.
/// Then start to connect listening messages, execute consumer which handle the suitable message
/// </summary>
public class PlatformRabbitMqStartProcessHostedService : PlatformHostedService
{
    private readonly PlatformRabbitMqProcessInitializerService rabbitMqProcessInitializerService;

    public PlatformRabbitMqStartProcessHostedService(
        IServiceProvider serviceProvider,
        ILoggerFactory loggerFactory,
        PlatformRabbitMqProcessInitializerService rabbitMqProcessInitializerService) : base(serviceProvider, loggerFactory)
    {
        this.rabbitMqProcessInitializerService = rabbitMqProcessInitializerService;
    }

    protected override async Task StartProcess(CancellationToken cancellationToken)
    {
        rabbitMqProcessInitializerService.StartProcess(cancellationToken);
    }

    protected override async Task StopProcess(CancellationToken cancellationToken)
    {
        rabbitMqProcessInitializerService.StopProcess();
    }
}
