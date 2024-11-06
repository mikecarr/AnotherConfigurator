using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using OpenIPC.Events;
using Prism.Events;
using ReactiveUI;
using Serilog;

namespace OpenIPC.ViewModels;

public class LogViewerViewModel : ObservableObject
{
    private readonly IEventAggregator _eventAggregator;

    public ObservableCollection<string> LogMessages { get; set; }
    
    public LogViewerViewModel()
    {
        _eventAggregator = App.EventAggregator;
        LogMessages = new ObservableCollection<string>();
        _eventAggregator.GetEvent<AppMessageEvent>().Subscribe(AppMessageReceived);
        _eventAggregator.GetEvent<LogMessageEvent>().Subscribe(LogMessageReceived);
    }

    private void LogMessageReceived(string message)
    {
        string formattedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";
        
        //Log.Debug("Serilog: " + message);
        if(!message.Contains("Ping"))
            LogMessages.Insert(0,formattedMessage);
    }

    private string _messageText;
    public string MessageText
    {
        get => _messageText;
        set => SetProperty(ref _messageText, value);
    }
    
    private void AppMessageReceived(AppMessage message)
    {
        string formattedMessage = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {message}";

        if(message.UpdateLogView)
        {
            LogMessages.Insert(0,formattedMessage);
        }
        
            
    }
}