using System.Reflection;
using Easy.Platform.Common.Timing;

namespace Easy.Platform.Infrastructures.MessageBus;

public class PlatformBusMessage<TPayload> : IPlatformTrackableBusMessage, IPlatformSelfRoutingKeyBusMessage, IPlatformWithPayloadBusMessage<TPayload>
    where TPayload : class, new()
{
    private string messageAction;
    private string messageGroup;
    private string messageType;
    private string producerContext;
    public PlatformBusMessageIdentity Identity { get; set; }

    public virtual string MessageGroup
    {
        get => messageGroup ?? PlatformBusMessageRoutingKey.DefaultMessageGroup;
        set => messageGroup = PlatformBusMessageRoutingKey.AutoFixKeyPart(value);
    }

    public string ProducerContext
    {
        get => producerContext ?? PlatformBusMessageRoutingKey.UnknownProducerContext;
        set => producerContext = PlatformBusMessageRoutingKey.AutoFixKeyPart(value);
    }

    public virtual string MessageType
    {
        get => messageType ?? GetDefaultMessageType<PlatformBusMessage<TPayload>>();
        set => messageType = PlatformBusMessageRoutingKey.AutoFixKeyPart(value);
    }

    public string MessageAction
    {
        get => messageAction;
        set => messageAction = PlatformBusMessageRoutingKey.AutoFixKeyPart(value);
    }

    public PlatformBusMessageRoutingKey RoutingKey()
    {
        return new PlatformBusMessageRoutingKey
        {
            MessageGroup = MessageGroup,
            ProducerContext = ProducerContext,
            MessageType = MessageType,
            MessageAction = MessageAction
        };
    }

    public string TrackingId { get; set; } = Guid.NewGuid().ToString();
    public DateTime? CreatedUtcDate { get; set; } = Clock.UtcNow;
    public string ProduceFrom { get; set; } = Assembly.GetEntryAssembly()?.FullName;
    public TPayload Payload { get; set; }

    public static TBusMessage New<TBusMessage>(
        string trackId,
        TPayload payload,
        PlatformBusMessageIdentity identity,
        string producerContext,
        string messageGroup,
        string messageAction)
        where TBusMessage : class, IPlatformWithPayloadBusMessage<TPayload>, IPlatformSelfRoutingKeyBusMessage, IPlatformTrackableBusMessage, new()
    {
        var message = Activator.CreateInstance<TBusMessage>();

        message.TrackingId = trackId;
        message.Payload = payload;
        message.Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        message.ProducerContext = producerContext;
        message.ProduceFrom = producerContext;
        if (messageGroup != null)
            message.MessageGroup = messageGroup;
        if (messageAction != null)
            message.MessageAction = messageAction;
        message.MessageType ??= GetDefaultMessageType<TBusMessage>();

        return message;
    }

    public static string GetDefaultMessageType<TBusMessage>()
        where TBusMessage : class, IPlatformWithPayloadBusMessage<TPayload>, IPlatformSelfRoutingKeyBusMessage, IPlatformTrackableBusMessage, new()
    {
        return typeof(TPayload).Name;
    }
}