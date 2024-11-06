using OpenIPC.Services;
using Prism.Events;

namespace OpenIPC.Messages;

public class MajesticContentUpdatedEvent : PubSubEvent<MajesticContentUpdatedMessage>
{
    
}

public class MajesticContentUpdatedMessage
{
    public string Content { get; set; }

    public MajesticContentUpdatedMessage(string content)
    {
        Content = content;
    }
}