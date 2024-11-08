using System;
using System.Globalization;
using OpenIPC.Events;
using Prism.Events;
using Serilog.Core;
using Serilog.Events;

namespace OpenIPC.Logging;

public class EventAggregatorSink : ILogEventSink
{
    private readonly IFormatProvider _formatProvider;
    private readonly IEventAggregator _eventAggregator;

    public EventAggregatorSink(IEventAggregator eventAggregator, IFormatProvider formatProvider = null)
    {
        _eventAggregator = eventAggregator;
        _formatProvider = formatProvider ?? CultureInfo.InvariantCulture;
    }

    public void Emit(LogEvent logEvent)
    {
        var message = logEvent.RenderMessage(_formatProvider);
        _eventAggregator.GetEvent<LogMessageEvent>().Publish(message);
    }
}