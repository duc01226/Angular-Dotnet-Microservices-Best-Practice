using Easy.Platform.Common.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Easy.Platform.Common.Hosting;

/// <summary>
/// https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-6.0 <br />
/// It's usually used as a background service , will start on WebApplication.Run()
/// </summary>
public abstract class PlatformHostedService : IHostedService, IDisposable
{
    protected readonly ILogger Logger;
    protected readonly IServiceProvider ServiceProvider;
    private readonly object startProcessLock = new();
    private readonly object stopProcessLock = new();

    protected bool ProcessStarted;
    protected bool ProcessStopped;

    protected PlatformHostedService(IServiceProvider serviceProvider, ILoggerFactory loggerFactory)
    {
        ServiceProvider = serviceProvider;
        Logger = loggerFactory.CreateLogger(GetType());
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        lock (startProcessLock)
        {
            if (ProcessStarted) return;

            StartProcess(cancellationToken).WaitResult();

            ProcessStarted = true;

            Logger.LogInformation($"Process of {GetType().Name} Started");
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        lock (stopProcessLock)
        {
            if (!ProcessStarted || ProcessStopped) return;

            StopProcess(cancellationToken).Wait(cancellationToken);

            ProcessStopped = true;

            Logger.LogInformation($"Process of {GetType().Name} Stopped");
        }
    }

    protected abstract Task StartProcess(CancellationToken cancellationToken);
    protected virtual Task StopProcess(CancellationToken cancellationToken) { return Task.CompletedTask; }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
            DisposeManagedResource();
    }

    protected virtual void DisposeManagedResource() { }
}