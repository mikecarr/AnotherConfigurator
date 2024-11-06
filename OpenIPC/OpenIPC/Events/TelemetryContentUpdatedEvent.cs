using OpenIPC.Services;
using Prism.Events;

namespace OpenIPC.Messages;

public class TelemetryContentUpdatedEvent : PubSubEvent<TelemetryContentUpdatedMessage>
{
    
}

public class TelemetryContentUpdatedMessage
{
    public string Content { get; set; }

    public TelemetryContentUpdatedMessage(string content)
    {
        Content = content;
    }
}