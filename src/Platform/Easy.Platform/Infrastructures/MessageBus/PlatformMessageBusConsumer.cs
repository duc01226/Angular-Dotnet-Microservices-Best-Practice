using System.Text.Json;
using Easy.Platform.Common.Extensions;
using Easy.Platform.Common.Utils;
using Microsoft.Extensions.Logging;

namespace Easy.Platform.Infrastructures.MessageBus;

public interface IPlatformMessageBusConsumer
{
    public string HandleExistingInboxMessageTrackId { get; set; }

    /// <summary>
    /// Config the time in milliseconds to log warning if the process consumer time is over ProcessWarningTimeMilliseconds.
    /// </summary>
    long? SlowProcessWarningTimeMilliseconds();

    bool DisableSlowProcessWarning();

    JsonSerializerOptions CustomJsonSerializerOptions();

    /// <summary>
    /// Default is 0. Return bigger number order to execute it later by order ascending
    /// </summary>
    int ExecuteOrder();

    public static PlatformBusMessageRoutingKey BuildForConsumerDefaultBindingRoutingKey(Type consumerType)
    {
        var messageType = GetConsumerMessageType(consumerType);

        return PlatformBusMessageRoutingKey.BuildDefaultRoutingKey(messageType);
    }

    public static Type GetConsumerMessageType(Type consumerGenericType)
    {
        return consumerGenericType.GetGenericArguments()[0];
    }
}

public interface IPlatformMessageBusConsumer<in TMessage> : IPlatformMessageBusConsumer
    where TMessage : class, new()
{
    Task HandleAsync(TMessage message, string routingKey);
}

public abstract class PlatformMessageBusConsumer : IPlatformMessageBusConsumer
{
    public const long DefaultProcessWarningTimeMilliseconds = 5000;

    public string HandleExistingInboxMessageTrackId { get; set; }

    public virtual long? SlowProcessWarningTimeMilliseconds()
    {
        return DefaultProcessWarningTimeMilliseconds;
    }

    public virtual bool DisableSlowProcessWarning()
    {
        return false;
    }

    public virtual JsonSerializerOptions CustomJsonSerializerOptions()
    {
        return null;
    }

    public virtual int ExecuteOrder()
    {
        return 0;
    }

    /// <summary>
    /// Get <see cref="PlatformBusMessage{TPayload}" /> concrete message type from a <see cref="IPlatformMessageBusConsumer" /> consumer
    /// <br />
    /// Get a generic type: PlatformEventBusMessage{TMessage} where TMessage = TMessagePayload
    /// of IPlatformEventBusConsumer{TMessagePayload}
    /// </summary>
    public static Type GetConsumerMessageType(IPlatformMessageBusConsumer consumer)
    {
        var consumerGenericType = consumer
            .GetType()
            .GetInterfaces()
            .FirstOrDefault(x => x.IsGenericType && x.GetGenericTypeDefinition() == typeof(IPlatformMessageBusConsumer<>));

        // WHY: To ensure that the consumer implements the correct interface IPlatformEventBusConsumer<> OR IPlatformEventBusCustomMessageConsumer<>.
        // The IPlatformEventBusConsumer (non-generic version) is used for Interface Marker only.
        if (consumerGenericType == null)
            throw new Exception("Must be implementation of IPlatformMessageBusConsumer<>");

        return IPlatformMessageBusConsumer.GetConsumerMessageType(consumerGenericType);
    }

    public static async Task InvokeConsumerAsync(
        IPlatformMessageBusConsumer consumer,
        object busMessage,
        string routingKey,
        bool isLogConsumerProcessTime,
        double slowProcessWarningTimeMilliseconds = DefaultProcessWarningTimeMilliseconds,
        ILogger logger = null,
        CancellationToken cancellationToken = default)
    {
        if (logger == null)
            throw new ArgumentNullException(nameof(logger));

        logger.LogDebug(
            "[MessageBus] Start invoking consumer. Name: {ConsumerName}. RoutingKey: {RoutingKey}. TrackingId: {TrackingId}",
            consumer.GetType().FullName,
            routingKey,
            busMessage.As<IPlatformTrackableBusMessage>()?.TrackingId ?? "n/a");

        if (isLogConsumerProcessTime && !consumer.DisableSlowProcessWarning())
        {
            await Util.TaskRunner.ProfilingAsync(
                asyncTask: () => DoInvokeConsumer(consumer, busMessage, routingKey, cancellationToken),
                afterExecution: elapsedMilliseconds =>
                {
                    var logMessage =
                        $"ElapsedMilliseconds:{elapsedMilliseconds}. Consumer:{consumer.GetType().FullName}. RoutingKey:{routingKey}. TrackingId:{busMessage.As<IPlatformTrackableBusMessage>()?.TrackingId ?? "n/a"}.";

                    var toCheckSlowProcessWarningTimeMilliseconds = consumer.SlowProcessWarningTimeMilliseconds() ??
                                                                    slowProcessWarningTimeMilliseconds;
                    if (elapsedMilliseconds >= toCheckSlowProcessWarningTimeMilliseconds)
                        logger.LogWarning(
                            $"[MessageBus] SlowProcessWarningTimeMilliseconds:{toCheckSlowProcessWarningTimeMilliseconds}. {logMessage}. MessageContent: {{BusMessage}}",
                            busMessage.AsJson());
                    else
                        logger.LogDebug($"[MessageBus] Finished invoking consumer. {logMessage}");
                });
        }
        else
        {
            await DoInvokeConsumer(
                consumer,
                busMessage,
                routingKey,
                cancellationToken);

            logger.LogDebug(
                "[MessageBus] Finished invoking consumer. Name: {ConsumerName}. RoutingKey: {RoutingKey}. TrackingId: {TrackingId}",
                consumer.GetType().FullName,
                routingKey,
                busMessage.As<IPlatformTrackableBusMessage>()?.TrackingId ?? "n/a");
        }
    }

    private static async Task DoInvokeConsumer(
        IPlatformMessageBusConsumer consumer,
        object eventBusMessage,
        string routingKey,
        CancellationToken cancellationToken)
    {
        var handleMethodName = nameof(IPlatformMessageBusConsumer<object>.HandleAsync);

        var methodInfo = consumer.GetType().GetMethod(handleMethodName);
        if (methodInfo == null)
            throw new Exception(
                $"Can not find execution handle method {handleMethodName} from {consumer.GetType().FullName}");

        try
        {
            var invokeResult = methodInfo.Invoke(
                consumer,
                Util.ListBuilder.NewArray(eventBusMessage, routingKey));
            if (invokeResult is Task invokeTask)
                await invokeTask;
        }
        catch (Exception e)
        {
            throw new PlatformInvokeConsumerException(e, consumer.GetType().FullName, eventBusMessage);
        }
    }
}

public abstract class PlatformMessageBusConsumer<TMessage> : PlatformMessageBusConsumer, IPlatformMessageBusConsumer<TMessage>
    where TMessage : class, new()
{
    protected readonly ILogger Logger;

    public PlatformMessageBusConsumer(ILoggerFactory loggerFactory)
    {
        Logger = loggerFactory.CreateLogger(GetType());
    }

    public virtual async Task HandleAsync(TMessage message, string routingKey)
    {
        try
        {
            await InternalHandleAsync(message, routingKey);
        }
        catch (Exception e)
        {
            Logger.LogError(
                e,
                $"Error Consume message [RoutingKey:{{RoutingKey}}], [Type:{{MessageName}}].{Environment.NewLine}" +
                $"Message Info: {{BusMessage}}.{Environment.NewLine}",
                routingKey,
                message.GetType().GetNameOrGenericTypeName(),
                message.AsJson());
            throw;
        }
    }

    protected abstract Task InternalHandleAsync(TMessage message, string routingKey);
}