using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using OpenIPC.Messages;
using OpenIPC.Events;
using OpenIPC.Models;
using OpenIPC.Services;
using Prism.Events;
using ReactiveUI;
using Serilog;

namespace OpenIPC.ViewModels;

public class WfbTabViewModel : ReactiveObject
{
    private readonly IEventAggregator _eventAggregator;
    
    private readonly ISshClientService? _sshClientService;
    
    
    public ObservableCollection<string> Frequencies58GHz { get; set; }
    public ObservableCollection<string> Frequencies24GHz { get; set; }

    public ObservableCollection<int> Power58GHz { get; set; }
    public ObservableCollection<int> Power24GHz { get; set; }
    public ObservableCollection<int> MCSIndex { get; set; }
    public ObservableCollection<int> STBC { get; set; }
    public ObservableCollection<int> LDPC { get; set; }
    public ObservableCollection<int> FecK { get; set; }
    public ObservableCollection<int> FecN { get; set; }
    public ICommand RestartWfbCommand { get; private set; }

    
    
    private readonly Dictionary<int, string> _58frequencyMapping = new()
    {
        { 36, "5180 MHz [36]" },
        { 40, "5200 MHz [40]" },
        { 44, "5220 MHz [44]" },
        { 48, "5240 MHz [48]" },
        { 52, "5260 MHz [52]" },
        { 56, "5280 MHz [56]" },
        { 60, "5300 MHz [60]" },
        { 64, "5320 MHz [64]" },
        { 100, "5500 MHz [100]" },
        { 104, "5520 MHz [104]" },
        { 108, "5540 MHz [108]" },
        { 112, "5560 MHz [112]" },
        { 116, "5580 MHz [116]" },
        { 120, "5600 MHz [120]" },
        { 124, "5620 MHz [124]" },
        { 128, "5640 MHz [128]" },
        { 132, "5660 MHz [132]" },
        { 136, "5680 MHz [136]" },
        { 140, "5700 MHz [140]" },
        { 144, "5720 MHz [144]" },
        { 149, "5745 MHz [149]" },
        { 153, "5765 MHz [153]" },
        { 157, "5785 MHz [157]" },
        { 161, "5805 MHz [161]" },
        { 165, "5825 MHz [165]" },
        { 169, "5845 MHz [169]" },
        { 173, "5865 MHz [173]" },
        { 177, "5885 MHz [177]" }
    };
    
    private readonly Dictionary<int, string> _24frequencyMapping = new()
    {
        { 1, "2412 MHz [1]" },
        { 2, "2417 MHz [2]" },
        { 3, "2422 MHz [3]" },
        { 4, "2427 MHz [4]" },
        { 5, "2432 MHz [5]" },
        { 6, "2437 MHz [6]" },
        { 7, "2442 MHz [7]" },
        { 8, "2447 MHz [8]" },
        { 9, "2452 MHz [9]" },
        { 10, "2457 MHz [10]" },
        { 11, "2462 MHz [11]" },
        { 12, "2467 MHz [12]" },
        { 13, "2472 MHz [13]" },
        { 14, "2484 MHz [14]" }
    };
    
    private int _selectedPower24GHz;

    public int SelectedPower24GHz
    {
        get => _selectedPower24GHz;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPower24GHz, value);
            Log.Debug($"SelectedPower (2.4) updated to {value}");
        }
    }

    private int _selectedChannel;

    public int SelectedChannel
    {
        get => _selectedChannel;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedChannel, value);
            Log.Debug($"SelectedChannel updated to {value}");
        }
    }
    
    private int _selectedPower;

    public int SelectedPower
    {
        get => _selectedPower;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedPower, value);
            Log.Debug($"SelectedPower (5.8) updated to {value}");
        }
    }

    private int _selectedLdpc;

    public int SelectedLdpc
    {
        get => _selectedLdpc;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedLdpc, value);
            Log.Debug($"SelectedLdpc updated to {value}");
        }
    }

    private int _selectedStbc;

    public int SelectedStbc
    {
        get => _selectedStbc;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedStbc, value);
            Log.Debug($"SelectedStbc updated to {value}");
        }
    }

    private int _selectedMcsIndex;

    public int SelectedMcsIndex
    {
        get => _selectedMcsIndex;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedMcsIndex, value);
            Log.Debug($"SelectedMcsIndex updated to {value}");
        }
    }

    private int _selectedFecK;

    public int SelectedFecK
    {
        get => _selectedFecK;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedFecK, value);
            Log.Debug($"SelectedFecK updated to {value}");
        }
    }

    private int _selectedFecN;

    public int SelectedFecN
    {
        get => _selectedFecN;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedFecN, value);
            Log.Debug($"SelectedFecN updated to {value}");
        }
    }

    private string _selectedFrequency58String;

    public string SelectedFrequency58String
    {
        get => _selectedFrequency58String;
        set => this.RaiseAndSetIfChanged(ref _selectedFrequency58String, value);
    }

    private string _selectedFrequency24String;

    public string SelectedFrequency24String
    {
        get => _selectedFrequency58String;
        set => this.RaiseAndSetIfChanged(ref _selectedFrequency24String, value);
    }

    private string _wfbConfContent;
    public string? WfbConfContent
    {
        get => _wfbConfContent;
        set
        {
            this.RaiseAndSetIfChanged(ref _wfbConfContent, value);
            //CanConnect = true;
            //ParseWfbConfContent();
        }
    }
    
    private bool _canConnect;
    public bool CanConnect
    {
        get => _canConnect;
        set
        {
            this.RaiseAndSetIfChanged(ref _canConnect, value);
            Log.Debug($"CanConnect {value}");
        }
    }
    
    public WfbTabViewModel()
    {
        InitializeCollections();
        
        _sshClientService = new SshClientService(_eventAggregator);
        RestartWfbCommand = new RelayCommand(() => RestartWfb());
        
        _eventAggregator = App.EventAggregator;
        
        _eventAggregator.GetEvent<TabMessageEvent>().Subscribe(MessageReceived);
        _eventAggregator.GetEvent<WfbConfContentUpdatedEvent>().Subscribe(WfbConfContentUpdated);
        _eventAggregator.GetEvent<AppMessageEvent>().Subscribe(OnAppMessage);
    }

    
    
    
    private void WfbConfContentUpdated(WfbConfContentUpdatedMessage obj)
    {
        WfbConfContent = obj.Content;
        ParseWfbConfContent();
    }

    // Method that will save settings and then restart device
    private async void RestartWfb()
    {
        _eventAggregator.GetEvent<TabMessageEvent>().Publish("Restart Pushed");
        _eventAggregator.GetEvent<AppMessageEvent>().Publish(new AppMessage
            { Message = "Getting new content", DeviceConfig = DeviceConfig.Instance });

        var newFrequency58 = SelectedFrequency58String;
        var newFrequency24 = SelectedFrequency24String;

        var newPower58 = SelectedPower;
        var newPower24 = SelectedPower24GHz;
        var newMcsIndex = SelectedMcsIndex;
        var newStbc = SelectedStbc;
        var newLdpc = SelectedLdpc;
        var newFecK = SelectedFecK;
        var newFecN = SelectedFecN;
        var newChannel = SelectedChannel;

        // Update WfbConfContent with the new values
        var updatedWfbConfContent = UpdateWfbConfContent(
            WfbConfContent,
            newFrequency58,
            newFrequency24,
            newPower58,
            newPower24,
            newMcsIndex,
            newStbc,
            newLdpc,
            newFecK,
            newFecN,
            newChannel
        );

        WfbConfContent = updatedWfbConfContent;

        // make sure we are not uploading an empty string/file
        if (string.IsNullOrEmpty(updatedWfbConfContent))
        {
            await MessageBoxManager.GetMessageBoxStandard("Error", "WfbConfContent is empty").ShowAsync();
        }

        _eventAggregator.GetEvent<AppMessageEvent>().Publish(new AppMessage { Message = $"Uploading new {Models.OpenIPC.WfbConfFileLoc}", DeviceConfig = DeviceConfig.Instance, UpdateLogView = true});
        
        Log.Information($"Uploading new : {Models.OpenIPC.WfbConfFileLoc}");
        _sshClientService.UploadFileStringAsync(DeviceConfig.Instance, Models.OpenIPC.WfbConfFileLoc, WfbConfContent);

        _eventAggregator.GetEvent<AppMessageEvent>().Publish(new AppMessage { Message = "Restarting Wfb", DeviceConfig = DeviceConfig.Instance, UpdateLogView = true});
        Log.Information("Restarting Wfb");
        _sshClientService.ExecuteCommandAsync(DeviceConfig.Instance, DeviceCommands.WfbRestartCommand);
    }

    private void MessageReceived(string obj)
    {
        //Log.Debug($"******* {this.GetType().Name} : MessageReceived: {obj}");
    }

    private void InitializeCollections()
    {
        // Convert the dictionary values to an ObservableCollection for binding
        Frequencies58GHz = new ObservableCollection<string>(_58frequencyMapping.Values);
        Frequencies24GHz = new ObservableCollection<string>(_24frequencyMapping.Values);

        Power58GHz = new ObservableCollection<int> { 1, 5, 10, 15, 20, 25, 30 };
        Power24GHz = new ObservableCollection<int> { 1, 20, 25, 30, 35, 40 };
        MCSIndex = new ObservableCollection<int>
        {
            1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29,
            30, 31
        };
        STBC = new ObservableCollection<int> { 0, 1 };
        LDPC = new ObservableCollection<int> { 0, 1 };
        FecK = new ObservableCollection<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };
        FecN = new ObservableCollection<int> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 };


    }
    
    // Method to parse the wfbConfContent
    private void ParseWfbConfContent()
    {
        Log.Debug("Parsing wfbConfContent.");

        if (string.IsNullOrEmpty(WfbConfContent)) return;

        // Logic to parse wfbConfContent, e.g., split by lines or delimiters
        var lines = WfbConfContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            // Example: Parse key-value pairs
            var parts = line.Split('=');
            if (parts.Length == 2)
            {
                var key = parts[0].Trim();
                var value = parts[1].Trim();

                switch (key)
                {
                    case Wfb.Frequency:
                        if (int.TryParse(value, out var frequency))
                        {
                            string frequencyString;

                            if (_58frequencyMapping.TryGetValue(frequency, out frequencyString ))
                                SelectedFrequency58String = frequencyString;
                            else if (_24frequencyMapping.TryGetValue(frequency, out frequencyString))
                                SelectedFrequency24String = frequencyString;
                            // Handle unknown frequency value
                        }

                        break;
                    case Wfb.DriverTxpowerOverride:
                        if (int.TryParse(value, out var parsedPower))
                            // Ensure the parsed power exists in the collection, or set a fallback
                            if (Power58GHz.Contains(parsedPower))
                            {
                                SelectedPower = parsedPower;
                            }
                            else
                            {
                                Power58GHz.Add(parsedPower);
                                SelectedPower = parsedPower;
                            }
                        break;
                    case Wfb.Ldpc:
                        if (int.TryParse(value, out var parsedLdpc))
                            // Ensure the parsed power exists in the collection, or set a fallback
                            if (LDPC.Contains(parsedLdpc))
                            {
                                SelectedLdpc = parsedLdpc;
                            }
                            else
                            {
                                LDPC.Add(parsedLdpc);
                                SelectedLdpc = parsedLdpc;
                            }

                        break;
                    case Wfb.Stbc:
                        if (int.TryParse(value, out var parsedStbc))
                            if (STBC.Contains(parsedStbc))
                            {
                                SelectedStbc = parsedStbc;
                            }
                            else
                            {
                                STBC.Add(parsedStbc);
                                SelectedStbc = parsedStbc;
                            }

                        break;
                    case Wfb.Txpower:
                        if (int.TryParse(value, out var parsedTxpower))
                        {
                            if (Power24GHz.Contains(parsedTxpower))
                            {
                                SelectedPower24GHz = parsedTxpower;
                            }
                            else
                            {
                                Power24GHz.Add(parsedTxpower);
                                SelectedPower24GHz = parsedTxpower;
                            }
                        }

                        break;
                    case Wfb.McsIndex:
                        if (int.TryParse(value, out var parsedMcsIndex))
                            if (MCSIndex.Contains(parsedMcsIndex))
                            {
                                SelectedMcsIndex = parsedMcsIndex;
                            }
                            else
                            {
                                MCSIndex.Add(parsedMcsIndex);
                                SelectedMcsIndex = parsedMcsIndex;
                            }

                        break;
                    case Wfb.FecK:
                        if (int.TryParse(value, out var parsedFecK))

                            if (FecK.Contains(parsedFecK))
                            {
                                SelectedFecK = parsedFecK;
                            }
                            else
                            {
                                FecK.Add(parsedFecK);
                                SelectedFecK = parsedFecK;
                            }

                        break;
                    case Wfb.FecN:
                        if (int.TryParse(value, out var parsedFecN))
                            if (FecN.Contains(parsedFecN))
                            {
                                SelectedFecN = parsedFecN;
                            }
                            else
                            {
                                FecN.Add(parsedFecN);
                                SelectedFecN = parsedFecN;
                            }
                        break;
                    case Wfb.Channel:
                        if (int.TryParse(value, out var channel))
                        {
                            SelectedChannel = channel;
                            if (_58frequencyMapping.TryGetValue(channel, out var frequency58String))
                                SelectedFrequency58String = frequency58String;
                            else if (_24frequencyMapping.TryGetValue(channel, out var frequency24String))
                                SelectedFrequency24String = frequency24String;
                            // Handle unknown channel value
                            
                        }

                        break;
                }


                // Handle parsed data, e.g., store in a dictionary or bind to properties
                Log.Debug($"WFB - Key: {key}, Value: {value}");
            }
        }
    }
    
    private void OnAppMessage(AppMessage appMessage)
    {
        if (appMessage.CanConnect)
        {
            CanConnect = appMessage.CanConnect;
            Log.Information($"CanConnect {CanConnect.ToString()}");
        }

    }
    
    private string UpdateWfbConfContent(
        string wfbConfContent,
        string newFrequency58,
        string newFrequency24,
        int newPower58,
        int newPower24,
        int newMcsIndex,
        int newStbc,
        int newLdpc,
        int newFecK,
        int newFecN,
        int newChannel
    )
    {
        // Logic to update WfbConfContent with the new values
        var lines = wfbConfContent.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var regex = new Regex(
            @"(frequency|channel|driver_txpower_override|frequency24|txpower|mcsindex|stbc|ldpc|feck|fecN)=.*");
        var updatedContent = regex.Replace(wfbConfContent, match =>
        {
            switch (match.Groups[1].Value)
            {
                case "frequency":
                // TODO: what should we do here?
                //return $"frequency={newFrequency58}";
                case "channel":
                    return $"channel={newChannel}";
                case "frequency24":
                // TODO: what should we do here?
                //return $"frequency24={newFrequency24}";
                case "driver_txpower_override":
                    return $"driver_txpower_override={newPower58}";
                case "txpower":
                    return $"txpower={newPower24}";
                case "mcsindex":
                    return $"mcsindex={newMcsIndex}";
                case "stbc":
                    return $"stbc={newStbc}";
                case "ldpc":
                    return $"ldpc={newLdpc}";
                case "feck":
                    return $"feck={newFecK}";
                case "fecN":
                    return $"fecN={newFecN}";
                default:
                    return match.Value;
            }
        });
        return updatedContent;
    }
}