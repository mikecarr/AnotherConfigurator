using OpenIPC.Services;
using Prism.Events;

namespace OpenIPC.Messages;

public class WfbConfContentUpdatedEvent : PubSubEvent<WfbConfContentUpdatedMessage>
{
    
}

public class WfbConfContentUpdatedMessage
{
    public string Content { get; set; }

    public WfbConfContentUpdatedMessage(string content)
    {
        Content = content;
    }
}