using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using Newtonsoft.Json.Linq;
using OpenIPC.Messages;
using OpenIPC.Services;
using OpenIPC.Events;
using OpenIPC.Models;
using OpenIPC.Services;
using Prism.Events;
using ReactiveUI;
using Renci.SshNet;
using Serilog;
using Color = System.Drawing.Color;


namespace OpenIPC.ViewModels;

public class ConnectControlsViewModel : ObservableObject
{
    private readonly Ping _ping = new Ping();
    
    ISshClientService _sshClientService;
    
    private readonly CancellationTokenSource _cancellationTokenSource;
    
    private readonly IEventAggregator _eventAggregator;

    private readonly DispatcherTimer _dispatcherTimer;
    private readonly SolidColorBrush _onlineColorBrush = new SolidColorBrush(Colors.Green);
    private readonly SolidColorBrush _offlineColorBrush = new SolidColorBrush(Colors.Red);
    public ICommand ConnectCommand { get; private set; }

    private DeviceConfig _deviceConfig;
    
    
    public ConnectControlsViewModel()
    {
        _sshClientService = new SshClientService(_eventAggregator);
        _eventAggregator = App.EventAggregator;
        
        SetDefaults();
        LoadSettings();
        
        ConnectCommand = new RelayCommand(() => Connect());
        
        _cancellationTokenSource = new CancellationTokenSource();
        _dispatcherTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _dispatcherTimer.Tick += DispatcherTimer_Tick;
        _dispatcherTimer.Start();
    }

    private void LoadSettings()
    {
        var settings = SettingsManager.LoadSettings(_eventAggregator);
        _deviceConfig = DeviceConfig.Instance;
        IpAddress = settings.IpAddress;
        Password = settings.Password;
        SelectedDeviceType = settings.DeviceType;
    }

    public Orientation Orientation
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || 
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Orientation.Horizontal;
            }
            else
            {
                return Orientation.Vertical;
            }
            
        }
    }
    
    private async void DispatcherTimer_Tick(object sender, EventArgs e)
    {
        await PingDeviceAsync();
    }

    private async Task PingDeviceAsync()
    {
        while (!_cancellationTokenSource.IsCancellationRequested)
        {
            try
            {
                IPAddress ipAddress = IPAddress.Parse(IpAddress);
                PingReply reply = await _ping.SendPingAsync(ipAddress, TimeSpan.FromSeconds(1), null, null, _cancellationTokenSource.Token);
                //Log.Debug("Ping result: " + reply.Status.ToString());
                //_eventAggregator.GetEvent<AppMessageEvent>().Publish(new AppMessage { Message = "Ping...." });
                
                if (reply.Status == IPStatus.Success)
                {
                    await Dispatcher.UIThread.InvokeAsync(() => PingStatusColor = _onlineColorBrush);
                    _eventAggregator.GetEvent<AppMessageEvent>().Publish(new AppMessage { Status = "Ping OK", DeviceConfig = _deviceConfig });
                }
                else
                {
                    await Dispatcher.UIThread.InvokeAsync(() => PingStatusColor = _offlineColorBrush);
                    _eventAggregator.GetEvent<AppMessageEvent>().Publish(new AppMessage { Status = "No device", DeviceConfig = _deviceConfig });
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                // Handle exception
            }
            await Task.Delay(TimeSpan.FromSeconds(1));
        }
    }
    
    private void SetDefaults()
    {
        PingStatusColor = _offlineColorBrush;
        IpAddress = "192.168.1.10";
    }

    private string _ipAddress;
    public string? IpAddress
    {
        get => _ipAddress;
        set
        {
            SetProperty(ref _ipAddress, value);
            CheckIfCanConnect();    
        }
    }
    
    private string _password;
    public string? Password
    {
        get => _password;
        set
        {
            SetProperty(ref _password, value);
            CheckIfCanConnect();
        }
    }
    
    
    private bool _canConnect;
    

    public bool CanConnect
    {
        get => _canConnect;
        set => SetProperty(ref _canConnect, value);
    }

    private SolidColorBrush _pingStatusColor;
    public SolidColorBrush PingStatusColor
    {
        get { return _pingStatusColor; }
        set
        {
            _pingStatusColor = value;
            OnPropertyChanged(nameof(PingStatusColor));
        }
    }

    private DeviceType _selectedDeviceType;
    public DeviceType SelectedDeviceType
    {
        get => _selectedDeviceType;

        set
        {
            if (_selectedDeviceType != value)
            {
                _selectedDeviceType = value;
                CheckIfCanConnect();

            }
        }
    }

    

    private void CheckIfCanConnect()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            CanConnect = !string.IsNullOrWhiteSpace(Password)
                         && !string.IsNullOrWhiteSpace(IpAddress)
                            && !SelectedDeviceType.Equals(DeviceType.None);
            
            // CanConnect = !string.IsNullOrWhiteSpace(Password)
            //              && !string.IsNullOrWhiteSpace(IpAddress)
            //              && !SelectedDeviceType.Equals(DeviceType.None);
            //
            // Logger.Instance(_eventAggregator)
            //     .Log(
            //         $"********* CanConnect: {CanConnect}, Username: {Username}, Password: *****, IP: {IpAddress}, DeviceType: {SelectedDeviceType}()");
            //
            // _deviceConfig = new DeviceConfig();
            // _deviceConfig.Username = "root";
            // _deviceConfig.IpAddress = IpAddress;
            // _deviceConfig.Username = Username;
            // _deviceConfig.Password = Password;
            // _deviceConfig.DeviceType = DeviceType;
            //
            // // fire event
            // _eventAggregator?.GetEvent<DeviceStateUpdatedEvent>()
            //     .Publish(new DeviceStateUpdatedMessage(CanConnect, _deviceConfig));
        });
    }
    
    private async void Connect()
    {
        AppMessage appMessage = new AppMessage();
        //DeviceConfig deviceConfig = new DeviceConfig();
        _deviceConfig.Username = "root";
        _deviceConfig.IpAddress = IpAddress;
        _deviceConfig.Password = Password;
        _deviceConfig.DeviceType = SelectedDeviceType;
        
        
        await getHostname(_deviceConfig);
         if (_deviceConfig.Hostname == string.Empty)
         {
             Log.Error("Failed to get hostname, stopping");
             return;
         }
         // Save the config to app settings
         SaveConfig();

         _eventAggregator?.GetEvent<AppMessageEvent>()
             .Publish(new AppMessage{ DeviceConfig = _deviceConfig});
         
        
        appMessage.DeviceConfig = _deviceConfig;
        
        // download file wfb.conf
        var wfbConfContent = await _sshClientService.DownloadFileAsync(_deviceConfig, Models.OpenIPC.WfbConfFileLoc);

        if (wfbConfContent != null)
        {
            _eventAggregator?.GetEvent<WfbConfContentUpdatedEvent>()
                .Publish(new WfbConfContentUpdatedMessage(wfbConfContent));            
        }
        
        var majesticContent = await _sshClientService.DownloadFileAsync(_deviceConfig, Models.OpenIPC.MajesticFileLoc);
        // Publish a message to WfbSettingsTabViewModel
        _eventAggregator?.GetEvent<MajesticContentUpdatedEvent>()
            .Publish(new MajesticContentUpdatedMessage(majesticContent));
        
        var telemetryContent = await _sshClientService.DownloadFileAsync(_deviceConfig, Models.OpenIPC.TelemetryConfFileLoc);
        // Publish a message to WfbSettingsTabViewModel
        _eventAggregator?.GetEvent<TelemetryContentUpdatedEvent>()
            .Publish(new TelemetryContentUpdatedMessage(telemetryContent));
    }

    /// <summary>
    /// Retrieves the hostname of the device asynchronously using SSH.
    /// <para>
    /// The command execution is cancelled after 10 seconds if no response is received.
    /// If the command execution times out, a message box is displayed with an error message.
    /// </para>
    /// </summary>
    /// <param name="deviceConfig">The device configuration to use for the SSH connection.</param>
    private async Task getHostname(DeviceConfig deviceConfig)
    {
        deviceConfig.Hostname = string.Empty;
        
        CancellationTokenSource cts = new CancellationTokenSource(10000); // 10 seconds
        CancellationToken cancellationToken = cts.Token;
        
        SshCommand cmdResult =  await _sshClientService.ExecuteCommandWithResponse(deviceConfig, DeviceCommands.GetHostname, cancellationToken);
        
        // If the command execution takes longer than 10 seconds, the task will be cancelled
        if (cmdResult == null)
        {
            // Handle the timeout
            // .
            var resp = MessageBoxManager.GetMessageBoxStandard("Timeout Error!",
                "The command took too long to execute. Please check device..");
            await resp.ShowAsync();
            return;
        }

        var hostName = Utilities.RemoveSpecialCharacters(cmdResult.Result);
        deviceConfig.Hostname = hostName;
        //_deviceConfig.Hostname = hostName;
        //Hostname = hostName;
        
        // Cleanup
        cts.Dispose();
    }
    
    private void SaveConfig()
    {
        _deviceConfig.DeviceType = SelectedDeviceType;
        _deviceConfig.IpAddress = IpAddress;
        _deviceConfig.Password = Password;
        
        // save config to file
        SettingsManager.SaveSettings(_deviceConfig);
        
    }
}