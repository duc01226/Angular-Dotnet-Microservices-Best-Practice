using System.Reflection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using RabbitMQ.Client;

namespace Easy.Platform.RabbitMQ;

/// <summary>
/// Use ObjectBool to manage chanel because HostService is singleton, and we don't want re-init chanel is heavy and wasting time.
/// We want to use pool when object is expensive to allocate/initialize
/// References: https://docs.microsoft.com/en-us/aspnet/core/performance/objectpool?view=aspnetcore-5.0
/// </summary>
public class PlatformRabbitMqChannelPool : DefaultObjectPool<IModel>, IDisposable
{
    private readonly PlatformRabbitMqChannelPoolPolicy channelPoolPolicy;

    public PlatformRabbitMqChannelPool(PlatformRabbitMqChannelPoolPolicy channelPoolPolicy) : base(
        channelPoolPolicy,
        maximumRetained: 1)
    {
        this.channelPoolPolicy = channelPoolPolicy;
    }

    /// <summary>
    /// GlobalChannel should be used for consumer only
    /// </summary>
    public IModel GlobalChannel { get; protected set; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public override IModel Get()
    {
        var channelFromPool = base.Get();

        if (channelFromPool.IsOpen == false || channelFromPool.IsClosed)
            return channelPoolPolicy.Create();

        return channelFromPool;
    }

    public IModel InitGlobalChannel()
    {
        ResetGlobalChannel();
        return GlobalChannel ??= channelPoolPolicy.Create();
    }

    public void ResetGlobalChannel()
    {
        if (GlobalChannel?.IsOpen == true) GlobalChannel.Close();
        GlobalChannel?.Dispose();
        GlobalChannel = null;
    }

    protected virtual void Dispose(bool disposing)
    {
        GlobalChannel?.Dispose();
    }
}

public class PlatformRabbitMqChannelPoolPolicy : IPooledObjectPolicy<IModel>
{
    private readonly IConnectionFactory connectionFactory;

    private Lazy<IConnection> connectionInitializer;
    private readonly ILogger<PlatformRabbitMqChannelPoolPolicy> logger;
    private readonly PlatformRabbitMqOptions options;

    public PlatformRabbitMqChannelPoolPolicy(
        PlatformRabbitMqOptions options,
        ILogger<PlatformRabbitMqChannelPoolPolicy> logger)
    {
        this.options = options;
        this.logger = logger;

        connectionFactory = InitializeFactory();
        connectionInitializer = new Lazy<IConnection>(CreateConnection);
    }

    public IModel Create()
    {
        try
        {
            return connectionInitializer.Value.CreateModel();
        }
        catch (Exception)
        {
            ReInitNewConnectionInitializer();
            throw;
        }
    }

    public bool Return(IModel obj)
    {
        return obj is { IsClosed: false, IsOpen: true };
    }

    /// <summary>
    /// Connection hang up during broker node restarted
    /// in this case, try to close old and create new connection
    /// </summary>
    private void ReInitNewConnectionInitializer()
    {
        try
        {
            connectionInitializer.Value.Close();
            connectionInitializer.Value.Dispose();
        }
        catch (Exception releaseEx)
        {
            logger.LogDebug(releaseEx, "Release rabbit-mq old connection failed.");
        }
        finally
        {
            connectionInitializer = new Lazy<IConnection>(CreateConnection);
        }
    }

    private IConnectionFactory InitializeFactory()
    {
        var connectionFactoryResult = new ConnectionFactory
        {
            AutomaticRecoveryEnabled = true, //https://www.rabbitmq.com/dotnet-api-guide.html#recovery
            NetworkRecoveryInterval = TimeSpan.FromSeconds(options.NetworkRecoveryIntervalSeconds),
            UserName = options.Username,
            Password = options.Password,
            VirtualHost = options.VirtualHost,
            Port = options.Port,
            DispatchConsumersAsync = true,
            RequestedConnectionTimeout = TimeSpan.FromSeconds(options.RequestedConnectionTimeoutSeconds),
            ClientProvidedName = options.ClientProvidedName ?? Assembly.GetEntryAssembly()?.FullName
        };

        return connectionFactoryResult;
    }

    private IConnection CreateConnection()
    {
        // Store stack trace before call CreateConnection to keep the original stack trace to log
        // after CreateConnection will lose full stack trace (may because it connect async to other external service)
        var stackTrace = Environment.StackTrace;

        return Util.TaskRunner.WaitRetryThrowFinalException(
            () =>
            {
                try
                {
                    var hostNames = options.HostNames.Split(',')
                        .Where(hostName => hostName.IsNotNullOrEmpty())
                        .ToArray();

                    return connectionFactory.CreateConnection(hostNames);
                }
                catch (Exception e)
                {
                    throw new Exception($"{GetType().Name} CreateConnection failed. {e.Message}. [[FullStackTrace: {stackTrace}]]", e);
                }
            },
            retryAttempt => 1.Seconds(),
            retryCount: 10);
    }
}
