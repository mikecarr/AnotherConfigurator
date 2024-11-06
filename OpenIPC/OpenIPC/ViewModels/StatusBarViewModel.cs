using System.Data;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenIPC.Events;
using OpenIPC.Models;
using Prism.Events;
using ReactiveUI;
using Serilog;

namespace OpenIPC.ViewModels;

public class StatusBarViewModel : ObservableObject
{
    private readonly IEventAggregator _eventAggregator;

    private string _statusText;
    public string StatusText
    {
        get => _statusText;
        set
        {
            SetProperty(ref _statusText, value);
        }
    }
    
    private string _messageText;
    public string MessagesText
    {
        get => _messageText;
        set
        {
            SetProperty(ref _messageText, value);
        }
    }
    private string _hostNameText;
    public string HostNameText
    {
        get => _hostNameText;
        set
        {
            SetProperty(ref _hostNameText, value);
        }
    }
    
    public StatusBarViewModel()
    {
       _eventAggregator = App.EventAggregator;
       _eventAggregator.GetEvent<AppMessageEvent>().Subscribe(UpdateStatus);
    }

    private void UpdateStatus(AppMessage appMessage)
    {
        Log.Debug(appMessage.ToString());
        

        if (!string.IsNullOrEmpty(appMessage.Status))
        {
            StatusText = appMessage.Status;
        }
        if (!string.IsNullOrEmpty(appMessage.Message))
        {
            MessagesText = appMessage.Message;
        }
    
        if (!string.IsNullOrEmpty(appMessage.DeviceConfig.Hostname))
        {
            HostNameText = appMessage.DeviceConfig.Hostname;
        }
        
    }
}

