using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Layout;
using Avalonia.Logging;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenIPC_Config.Events;
using OpenIPC_Config.Models;
using OpenIPC_Config.Services;
using Prism.Events;
using Serilog;

namespace OpenIPC_Config.ViewModels;

public partial class ConnectControlsViewModel : ViewModelBase
{
    private readonly CancellationTokenSource? _cancellationTokenSourc;
    
    private readonly IEventSubscriptionService _eventSubscriptionService;

    private readonly DispatcherTimer _dispatcherTimer;

    private readonly SolidColorBrush _offlineColorBrush = new(Colors.Red);
    private readonly SolidColorBrush _onlineColorBrush = new(Colors.Green);
    private readonly Ping _ping = new();
    private DispatcherTimer _pingTimer;

    private bool _canConnect;

    private DeviceConfig _deviceConfig;

    [ObservableProperty] private string _ipAddress;

    [ObservableProperty] private string _password;

    [ObservableProperty] private int _port = 22;

    private SolidColorBrush _pingStatusColor = new(Colors.Red);
    
    private DeviceType _selectedDeviceType;

    private readonly TimeSpan _pingInterval = TimeSpan.FromSeconds(1);
    private readonly TimeSpan _pingTimeout = TimeSpan.FromMilliseconds(500);

    public ConnectControlsViewModel(ILogger logger,
        ISshClientService sshClientService,
        IEventSubscriptionService eventSubscriptionService)
        : base(logger, sshClientService, eventSubscriptionService)
    {
        
        SetDefaults();
        LoadSettings();

        ConnectCommand = new RelayCommand(() => Connect());

        _eventSubscriptionService = eventSubscriptionService ??
                                    throw new ArgumentNullException(nameof(eventSubscriptionService));

        
        UpdateUIMessage("Ready");
    }

    public ICommand ConnectCommand { get; private set; }

    public Orientation Orientation
    {
        get
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return Orientation.Horizontal;
            return Orientation.Vertical;
        }
    }

    partial void OnPortChanged(int value)
    {
        CheckIfCanConnect();
    }

    partial void OnPasswordChanged(string value)
    {
        CheckIfCanConnect();
    }

    partial void OnIpAddressChanged(string value)
    {
        CheckIfCanConnect();
        if (Utilities.IsValidIpAddress(IpAddress))
        {
            Log.Debug("Valid IP address: {IpAddress}", IpAddress);
            StartPingTimer();
        }
        else
        {
            StopPingTimer();
        }
    }


    public bool CanConnect
    {
        get => _canConnect;
        set => SetProperty(ref _canConnect, value);
    }

    public SolidColorBrush PingStatusColor
    {
        get => _pingStatusColor;
        set
        {
            _pingStatusColor = value;
            OnPropertyChanged();
        }
    }

    public DeviceType SelectedDeviceType
    {
        get => _selectedDeviceType;
        set
        {
            // Ignore setting to None if it's due to a binding update
            if (value == DeviceType.None) return;

            if (_selectedDeviceType != value)
            {
                _selectedDeviceType = value;

                // Now only send the message with the selected device type
                SendDeviceTypeMessage(_selectedDeviceType);

                // Trigger any other actions, like OnPropertyChanged if needed
                OnPropertyChanged();
                CheckIfCanConnect();
            }
        }
    }

    private void LoadSettings()
    {
        var settings = SettingsManager.LoadSettings();
        _deviceConfig = DeviceConfig.Instance;
        IpAddress = settings.IpAddress;
        Password = settings.Password;
        SelectedDeviceType = settings.DeviceType;
        
        EventSubscriptionService.Publish<DeviceTypeChangeEvent, DeviceType>(SelectedDeviceType);
        
    }


    private void SetDefaults()
    {
        PingStatusColor = _offlineColorBrush;
        //IpAddress = "192.168.1.10";
    }

    private void SendDeviceTypeMessage(DeviceType deviceType)
    {
        // Insert logic to send a message based on the selected device type
        // For example, use an event aggregator, messenger, or direct call
        Log.Debug($"Device type selected: {deviceType}");
        //Console.WriteLine($"Device type selected: {deviceType}");
        
        EventSubscriptionService.Publish<DeviceTypeChangeEvent, DeviceType>(deviceType);
    }

    private void CheckIfCanConnect()
    {
        Dispatcher.UIThread.InvokeAsync(() =>
        {
            var isValidIp = Utilities.IsValidIpAddress(IpAddress);
            CanConnect = !string.IsNullOrWhiteSpace(Password)
                         && isValidIp
                         && !SelectedDeviceType.Equals(DeviceType.None);
        });
    }

    private void StartPingTimer()
    {
        if (_pingTimer == null)
        {
            _pingTimer = new DispatcherTimer
            {
                Interval = _pingInterval
            };
            _pingTimer.Tick += PingTimer_Tick;
        }
        _pingTimer.Start();
    }

    private void StopPingTimer()
    {
        _pingTimer?.Stop();
    }

    private async void PingTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            var reply = await _ping.SendPingAsync(IpAddress, (int)_pingTimeout.TotalMilliseconds);
            PingStatusColor = reply.Status == IPStatus.Success ? _onlineColorBrush : _offlineColorBrush;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error occurred during ping");
            PingStatusColor = _offlineColorBrush;
        }
    }
    
    private async void Connect()
    {
        var appMessage = new AppMessage();
        //DeviceConfig deviceConfig = new DeviceConfig();
        _deviceConfig.Username = "root";
        _deviceConfig.IpAddress = IpAddress;
        _deviceConfig.Password = Password;
        _deviceConfig.Port = Port;
        _deviceConfig.DeviceType = SelectedDeviceType;

        UpdateUIMessage("Getting hostname");

        await getHostname(_deviceConfig);
        if (_deviceConfig.Hostname == string.Empty)
        {
            Log.Error("Failed to get hostname, stopping");
            return;
        }

        var validator = App.ServiceProvider.GetRequiredService<DeviceConfigValidator>();
        if (!validator.IsDeviceConfigValid(_deviceConfig))
        {
            UpdateUIMessage("Hostname Error!");
            var msBox = MessageBoxManager.GetMessageBoxStandard("Hostname Error!",
                $"Hostname does not match device type! \nHostname: {_deviceConfig.Hostname} Device Type: {_selectedDeviceType}.\nPlease check device..\nOk to continue anyway\nCancel to quit",
                ButtonEnum.OkCancel);

            var result = await msBox.ShowAsync();
            if (result == ButtonResult.Cancel)
            {
                Log.Debug("Device selection and hostname mismatch, stopping");
                return;
            }
        }
        
        // if ((_deviceConfig.Hostname.Contains("radxa") && _deviceConfig.DeviceType != DeviceType.Radxa) ||
        //     (_deviceConfig.Hostname.Contains("openipc") && _deviceConfig.DeviceType != DeviceType.Camera))
        // {
        //     UpdateUIMessage("Hostname Error!");
        //     var msBox = MessageBoxManager.GetMessageBoxStandard("Hostname Error!",
        //         $"Hostname does not match device type! \nHostname: {_deviceConfig.Hostname} Device Type: {_selectedDeviceType}.\nPlease check device..\nOk to continue anyway\nCancel to quit",
        //         ButtonEnum.OkCancel);
        //
        //     var result = await msBox.ShowAsync();
        //     if (result == ButtonResult.Cancel)
        //     {
        //         Log.Debug("Device selection and hostname mismatch, stopping");
        //         return;
        //     }
        // }

        // Save the config to app settings
        SaveConfig();
        
        // Publish the event
        EventSubscriptionService.Publish<AppMessageEvent, AppMessage>(new AppMessage { DeviceConfig = _deviceConfig});
            

        appMessage.DeviceConfig = _deviceConfig;

        if (_deviceConfig != null)
        {
            if (_deviceConfig.DeviceType == DeviceType.Camera)
            {
                UpdateUIMessage("Processing Camera..." );
                processCameraFiles();
                UpdateUIMessage("Processing Camera...done");
            }
            else if (_deviceConfig.DeviceType == DeviceType.Radxa)
            {
                UpdateUIMessage("Processing Radxa...");
                processRadxaFiles();
                UpdateUIMessage("Processing Radxa...done");
            }
        }

        UpdateUIMessage("Connected");
    }

    private async void processRadxaFiles()
    {
        try
        {
            UpdateUIMessage("Downloading wifibroadcast.cfg" );

            // get /etc/wifibroadcast.cfg
            var wifibroadcastContent =
                await SshClientService.DownloadFileAsync(_deviceConfig, Models.OpenIPC.WifiBroadcastFileLoc);

            if (!string.IsNullOrEmpty(wifibroadcastContent))
            {
                var radxaContentUpdatedMessage = new RadxaContentUpdatedMessage();
                radxaContentUpdatedMessage.WifiBroadcastContent = wifibroadcastContent;

                EventSubscriptionService.Publish<RadxaContentUpdateChangeEvent, 
                    RadxaContentUpdatedMessage>(new RadxaContentUpdatedMessage { WifiBroadcastContent = wifibroadcastContent });
                
            }
            else
            {
                await MessageBoxManager.GetMessageBoxStandard("Error", "Failed to download /etc/wifibroadcast.cfg")
                    .ShowAsync();
            }
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            throw;
        }


        try
        {
            UpdateUIMessage("Downloading modprod.d/wfb.conf" );
            // get /etc/modprobe.d/wfb.conf
            var wfbModProbeContent =
                await SshClientService.DownloadFileAsync(_deviceConfig, Models.OpenIPC.WifiBroadcastModProbeFileLoc);

            if (wfbModProbeContent != null)
            {
                var radxaContentUpdatedMessage = new RadxaContentUpdatedMessage();
                radxaContentUpdatedMessage.WfbConfContent = wfbModProbeContent;

                EventSubscriptionService.Publish<RadxaContentUpdateChangeEvent, 
                    RadxaContentUpdatedMessage>(new RadxaContentUpdatedMessage { WfbConfContent = wfbModProbeContent });

                
            }
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            throw;
        }


        try
        {
            UpdateUIMessage("Downloading screen-mode");
            // get /home/radxa/scripts/screen-mode
            var screenModeContent =
                await SshClientService.DownloadFileAsync(_deviceConfig, Models.OpenIPC.ScreenModeFileLoc);

            if (screenModeContent != null)
            {
                var radxaContentUpdatedMessage = new RadxaContentUpdatedMessage();
                radxaContentUpdatedMessage.ScreenModeContent = screenModeContent;

                EventSubscriptionService.Publish<RadxaContentUpdateChangeEvent, 
                    RadxaContentUpdatedMessage>(new RadxaContentUpdatedMessage { ScreenModeContent = screenModeContent });
                
            }
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            throw;
        }

        try
        {
            UpdateUIMessage("Downloading gskey" );

            var gsKeyContent =
                await SshClientService.DownloadFileBytesAsync(_deviceConfig, Models.OpenIPC.RemoteGsKeyPath);

            if (gsKeyContent != null)
            {
                var droneKey = Utilities.ComputeMd5Hash(gsKeyContent);
                if (droneKey != OpenIPC.KeyMD5Sum)
                {
                    Log.Warning("GS key MD5 checksum mismatch");
                }
                else
                {
                    Log.Information("GS key MD5 checksum matched default key");
                }

                EventSubscriptionService.Publish<RadxaContentUpdateChangeEvent, 
                    RadxaContentUpdatedMessage>(new RadxaContentUpdatedMessage { DroneKeyContent = droneKey });
                

                UpdateUIMessage("Downloading gskey...done" );
            }
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            throw;
        }

        EventSubscriptionService.Publish<AppMessageEvent, AppMessage>(new AppMessage { CanConnect = DeviceConfig.Instance.CanConnect, DeviceConfig = _deviceConfig});
        

    }

    private async void processCameraFiles()
    {
        // download file wfb.conf
        var wfbConfContent = await SshClientService.DownloadFileAsync(_deviceConfig, Models.OpenIPC.WfbConfFileLoc);

        

        if (wfbConfContent != null)
            EventSubscriptionService.Publish<WfbConfContentUpdatedEvent, 
                WfbConfContentUpdatedMessage>(new WfbConfContentUpdatedMessage(wfbConfContent));

        try
        {
            var majesticContent =
                await SshClientService.DownloadFileAsync(_deviceConfig, Models.OpenIPC.MajesticFileLoc);
            // Publish a message to WfbSettingsTabViewModel
            EventSubscriptionService.Publish<MajesticContentUpdatedEvent, 
                MajesticContentUpdatedMessage>(new MajesticContentUpdatedMessage(majesticContent));
            
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
        }

        try
        {
            var telemetryContent =
                await SshClientService.DownloadFileAsync(_deviceConfig, Models.OpenIPC.TelemetryConfFileLoc);
            // Publish a message to WfbSettingsTabViewModel
            
            EventSubscriptionService.Publish<TelemetryContentUpdatedEvent, 
                TelemetryContentUpdatedMessage>(new TelemetryContentUpdatedMessage(telemetryContent));

        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            throw;
        }

        try
        {
            // get /home/radxa/scripts/screen-mode
            var droneKeyContent =
                await SshClientService.DownloadFileBytesAsync(_deviceConfig, Models.OpenIPC.RemoteDroneKeyPath);

            
            
            if (droneKeyContent != null)
            {
                //byte[] fileBytes = Encoding.UTF8.GetBytes(droneKeyContent);
                
                var droneKey = Utilities.ComputeMd5Hash(droneKeyContent);

                var deviceContentUpdatedMessage = new DeviceContentUpdatedMessage();
                _deviceConfig = DeviceConfig.Instance;
                _deviceConfig.KeyChksum = droneKey;
                deviceContentUpdatedMessage.DeviceConfig = _deviceConfig;

                EventSubscriptionService.Publish<DeviceContentUpdateEvent, 
                    DeviceContentUpdatedMessage>(deviceContentUpdatedMessage);
                
            }
        }
        catch (Exception e)
        {
            Log.Error(e.Message);
            throw;
        }

        EventSubscriptionService.Publish<AppMessageEvent, 
            AppMessage>(new AppMessage() { CanConnect = DeviceConfig.Instance.CanConnect, DeviceConfig = _deviceConfig});

        
    }

    /// <summary>
    ///     Retrieves the hostname of the device asynchronously using SSH.
    ///     <para>
    ///         The command execution is cancelled after 10 seconds if no response is received.
    ///         If the command execution times out, a message box is displayed with an error message.
    ///     </para>
    /// </summary>
    /// <param name="deviceConfig">The device configuration to use for the SSH connection.</param>
    private async Task getHostname(DeviceConfig deviceConfig)
    {
        deviceConfig.Hostname = string.Empty;

        var cts = new CancellationTokenSource(10000); // 10 seconds
        var cancellationToken = cts.Token;

        var cmdResult =
            await SshClientService.ExecuteCommandWithResponse(deviceConfig, DeviceCommands.GetHostname,
                cancellationToken);

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
        _deviceConfig.Port = Port;
        _deviceConfig.Password = Password;

        // save config to file
        SettingsManager.SaveSettings(_deviceConfig);
    }
}