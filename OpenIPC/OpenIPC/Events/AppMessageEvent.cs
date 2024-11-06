using OpenIPC.Models;
using Prism.Events;

namespace OpenIPC.Events;

public class AppMessageEvent : PubSubEvent<AppMessage>
{
    
    
}

public class AppMessage
{
    private bool updateLogView = false;
    public bool UpdateLogView
    {
        get { return updateLogView; }
        set
        {
            updateLogView = value;
        }
    }
    
    private string _message = string.Empty;
    public string Message { 
        get { return _message; }
        set
        {
            _message = value;
        }
    }

    private string _status = string.Empty;

    public string? Status
    {
        get { return _status; } 
        set{
            _status = value;
            
        }
    }

    private DeviceConfig _deviceConfig = DeviceConfig.Instance;
    public DeviceConfig DeviceConfig
    {
        get { return _deviceConfig; }
        set
        {
            _deviceConfig = value;
        }
    }
    
    
    public bool CanConnect { get; set; }

    public override string ToString()
    {
        return $"{nameof(Message)}: {Message}, {nameof(Status)}: {Status}, " +
               $"{nameof(DeviceConfig)}: {DeviceConfig}, {nameof(CanConnect)}: {CanConnect}";
    }
}

