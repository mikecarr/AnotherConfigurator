using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using DynamicData;
using OpenIPC.Events;
using OpenIPC.Messages;
using OpenIPC.Models;
using OpenIPC.Services;
using Prism.Events;
using ReactiveUI;
using Serilog;
using YamlDotNet.RepresentationModel;

namespace OpenIPC.ViewModels;

public class CameraSettingsTabViewModel : ViewModelBase
{
    
    private readonly ISshClientService _sshClientService;

    private IEventAggregator _eventAggregator;

    private DeviceConfig _deviceConfig;
    
    private Dictionary<string, string> _yamlConfig = new Dictionary<string, string>();
    
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
    
    private async void RestartMajestic()
    {
        Log.Debug("*** TODO : RestartMajesticCommand executed");

        await SaveRestartMajesticCommand();
    }

    public ICommand RestartMajesticCommand { get; private set; }
    // ObservableCollections
    public ObservableCollection<string> Resolution { get; set; }
    public ObservableCollection<string> FPS { get; set; }
    public ObservableCollection<string> Codec { get; set; }
    public ObservableCollection<string> Bitrate { get; set; }
    public ObservableCollection<string> Exposure { get; set; }
    public ObservableCollection<string> Contrast { get; set; }
    public ObservableCollection<string> Hue { get; set; }
    public ObservableCollection<string> Saturation { get; set; }
    public ObservableCollection<string> Luminance { get; set; }
    public ObservableCollection<string> Flip { get; set; }
    public ObservableCollection<string> Mirror { get; set; }
    
    private string _selectedResolution;
    public string SelectedResolution
    {
        get => _selectedResolution;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedResolution, value);
            Log.Debug($"SelectedResolution updated to {value}");
            UpdateYamlConfig(Majestic.VideoSize, value.ToString());
        }
    }

    private string _selectedFps;
    public string SelectedFps
    {
        get => _selectedFps;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedFps, value);
            Log.Debug($"SelectedFps updated to {value}");
            UpdateYamlConfig(Majestic.VideoFps, value.ToString());
        }
    }
    
    private string _selectedCodec;
    public string SelectedCodec
    {
        get => _selectedCodec;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedCodec, value);
            Log.Debug($"SelectedCodec updated to {value}");
            UpdateYamlConfig(Majestic.VideoCodec, value.ToString());
        }
    }
    
    private string _selectedBitrate;
    public string SelectedBitrate
    {
        get => _selectedBitrate;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedBitrate, value);
            Log.Debug($"SelectedBitrate updated to {value}");
            UpdateYamlConfig(Majestic.VideoBitrate, value.ToString());
        }
    }

    private string _selectedExposure;
    public string SelectedExposure
    {
        get => _selectedExposure;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedExposure, value);
            Log.Debug($"SelectedExposure updated to {value}");
            UpdateYamlConfig(Majestic.IspExposure, value.ToString());
        }
    }

    private string _selectedHue;
    public string SelectedHue
    {
        get => _selectedHue;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedHue, value);
            Log.Debug($"SelectedHue updated to {value}");
            UpdateYamlConfig(Majestic.ImageHue, value.ToString());
        }
    }
    
    private string _selectedContrast;
    public string SelectedContrast
    {
        get => _selectedContrast;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedContrast, value);
            Log.Debug($"SelectedContrast updated to {value}");
            UpdateYamlConfig(Majestic.ImageContrast, value.ToString());
        }
    }

    private string _selectedSaturation;
    public string SelectedSaturation
    {
        get => _selectedSaturation;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedSaturation, value);
            Log.Debug($"SelectedSaturation updated to {value}");
            UpdateYamlConfig(Majestic.ImageSaturation, value.ToString());
        }
    }

    private string _selectedLuminance;
    public string SelectedLuminance
    {
        get => _selectedLuminance;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedLuminance, value);
            Log.Debug($"SelectedLuminance updated to {value}");
            UpdateYamlConfig(Majestic.ImageLuminance, value.ToString());
        }
    }

    private string _selectedFlip;
    public string SelectedFlip
    {
        get => _selectedFlip;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedFlip, value);
            Log.Debug($"SelectedFlip updated to {value}");
            UpdateYamlConfig(Majestic.ImageFlip, value.ToString());
        }
    }

    private string _selectedMirror;
    public string SelectedMirror
    {
        get => _selectedMirror;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedMirror, value);
            Log.Debug($"SelectedMirror updated to {value}");
            UpdateYamlConfig(Majestic.ImageMirror, value.ToString());
        }
    }
    
    
    public CameraSettingsTabViewModel()
    {
        InitializeCollections();
        
        _eventAggregator = App.EventAggregator;
        _eventAggregator?.GetEvent<MajesticContentUpdatedEvent>().Subscribe(OnMajesticContentUpdated);
        _eventAggregator.GetEvent<AppMessageEvent>().Subscribe(OnAppMessage);
        
        
        RestartMajesticCommand = new RelayCommand(() => RestartMajestic());
        
        _sshClientService = new SshClientService(_eventAggregator);
    }

    private void OnAppMessage(AppMessage appMessage)
    {
        if (appMessage.CanConnect)
        {
            CanConnect = appMessage.CanConnect;
            Log.Information($"CanConnect {CanConnect.ToString()}");
        }

    }

    // private void OnDeviceStateUpdated(DeviceStateUpdatedMessage message)
    // { 
    //     _deviceConfig = message.DeviceConfig;
    //     if (message.CanConnect != null)
    //     {
    //         CanConnect = message.CanConnect;
    //     }
    // }
    
    private void OnMajesticContentUpdated(MajesticContentUpdatedMessage message)
    {
        var majesticContent = message.Content;
        CanConnect = true;
        ParseYamlConfig(majesticContent);

    }
    
    private void InitializeCollections()
    {
        Resolution = new ObservableCollection<string>
        {
            "1280x720", "1456x816", "1920x1080", "2104x1184", "2208x1248", "2240x1264", "2312x1304",
            "2512x1416", "2560x1440", "2560x1920", "3200x1800", "3840x2160"
        };

        FPS = new ObservableCollection<string>
        {
            "20", "30", "40", "50", "60", "70", "80", "90", "100", "110", "120"
        };

        Codec = new ObservableCollection<string> { "h264", "h265" };
        Bitrate = new ObservableCollection<string> { "1024", "2048", "3072", "4096", "5120", "6144", "7168", "8192", "9216" };
        Exposure = new ObservableCollection<string> { "5", "6", "8", "10", "11", "12", "14", "16", "33", "50" };
        Contrast = new ObservableCollection<string> { "1", "5", "10", "20", "30", "40", "50", "60", "70", "80", "90", "100" };
        Hue = new ObservableCollection<string> { "1", "5", "10", "20", "30", "40", "50", "60", "70", "80", "90", "100" };
        Saturation = new ObservableCollection<string> { "1", "5", "10", "20", "30", "40", "50", "60", "70", "80", "90", "100" };
        Luminance = new ObservableCollection<string> { "1", "5", "10", "20", "30", "40", "50", "60", "70", "80", "90", "100" };
        Flip = new ObservableCollection<string> { "true", "false" };
        Mirror = new ObservableCollection<string> { "true", "false" };
    }
    
        // YAML parsing and updating methods (unchanged)
        private void ParseYamlConfig(string content)
        {
            using var reader = new StringReader(content);
            var yaml = new YamlStream();
            yaml.Load(reader);
            
            if(yaml.Documents.Count == 0) 
            {
                Log.Debug("No content in yaml"); 
                
                return; // empty yaml (no content in yaml)
            }

            var root = (YamlMappingNode)yaml.Documents[0].RootNode;
            foreach (var entry in root.Children)
            {
                ParseYamlNode(entry.Key.ToString(), entry.Value);
            }
        }
    private void ParseYamlNode(string parentKey, YamlNode node)
    {
        if (node is YamlMappingNode mappingNode)
        {
            foreach (var child in mappingNode.Children)
            {
                string childKey = child.Key.ToString();
                ParseYamlNode($"{parentKey}.{childKey}", child.Value);
            }
        }
        else if (node is YamlScalarNode scalarNode)
        {
            string fullKey = parentKey;
            var value = scalarNode.Value;

            if (_yamlConfig.ContainsKey(fullKey))
            {
                _yamlConfig[fullKey] = value;
            }
            else
            {
                _yamlConfig.Add(fullKey, value);
            }

            Log.Debug($"Camera Found {fullKey}: {scalarNode.Value}");

            // Update UI properties based on the keys found
            switch (fullKey)
            {
                case Majestic.VideoSize:
                    if (Resolution?.Contains(value) ?? false)
                    {
                        SelectedResolution = value;
                    }
                    else
                    {
                        Resolution.Add(value);
                        SelectedResolution = value;
                    }
                    break;
                case Majestic.VideoFps:
                    if(FPS?.Contains(value) ?? false)
                    {
                        SelectedFps = value;
                    }
                    else
                    {
                        FPS.Add(value);
                        SelectedFps = value;
                    }
                    break;
                case Majestic.VideoCodec:
                    if (Codec?.Contains(value) ?? false)
                    {
                        SelectedCodec = value;
                    }
                    else
                    {
                        Codec.Add(value);
                        SelectedCodec = value;
                    }
                    break;
                case Majestic.VideoBitrate:
                    if (Bitrate?.Contains(value) ?? false)
                    {
                        SelectedBitrate = value;
                    }
                    else
                    {
                        Bitrate.Add(value);
                        SelectedBitrate = value;
                    }
                    break;
                case Majestic.IspExposure:
                    if (Exposure?.Contains(value) ?? false)
                    {
                        SelectedExposure = value;
                    }
                    else
                    {
                        Exposure.Add(value);
                        SelectedExposure = value;
                    }
                    break;
                case Majestic.ImageContrast:
                    if (Contrast?.Contains(value) ?? false)
                    {
                        SelectedContrast = value;
                    }
                    else
                    {
                        Contrast.Add(value);
                        SelectedContrast = value;
                    }
                    break;
                case Majestic.ImageHue:
                    if (Hue?.Contains(value) ?? false)
                    {
                        SelectedHue = value;
                    }
                    else
                    {
                        Hue.Add(value);
                        SelectedHue = value;
                    }
                    break;
                case Majestic.ImageSaturation:
                    if(Saturation?.Contains(value) ?? false)
                    {
                        SelectedSaturation = value;
                    }
                    else
                    {
                        Saturation.Add(value);
                        SelectedSaturation = value;
                    }
                    break;
                case Majestic.ImageLuminance:
                    if (Luminance?.Contains(value) ?? false)
                    {
                        SelectedLuminance = value;
                    }
                    else
                    {
                        Luminance.Add(value);
                        SelectedLuminance = value;
                    }
                    break;
                case Majestic.ImageFlip:
                    if (Flip?.Contains(value) ?? false)
                    {
                        SelectedFlip = value;
                    }
                    else
                    {
                        Flip.Add(value);
                        SelectedFlip = value;
                    }
                    break;
                case Majestic.ImageMirror:
                    if (Mirror?.Contains(value) ?? false)
                    {
                        SelectedMirror = value;
                    }
                    else
                    {
                        Mirror.Add(value);
                        SelectedMirror = value;
                    }
                    break;
                default:
                    Log.Debug($"Unknown key: {fullKey}");
                    break;
            }
        }
    }

    public async Task SaveRestartMajesticCommand()
    {
        Log.Debug("Preparing to Save Majestic file.");
        var majesticYamlContent = await _sshClientService.DownloadFileAsync(_deviceConfig, Models.OpenIPC.MajesticFileLoc);

        try
        {
            var yamlStream = new YamlStream();
            using (var reader = new StringReader(majesticYamlContent))
            {
                yamlStream.Load(reader);
            }

            var root = (YamlMappingNode)yamlStream.Documents[0].RootNode;

            foreach (var update in _yamlConfig)
            {
                UpdateYamlNode(root, update.Key, update.Value);
            }

            string updatedFileContent;
            using (var writer = new StringWriter())
            {
                yamlStream.Save(writer, false);
                updatedFileContent = writer.ToString();
            }

            await _sshClientService.UploadFileStringAsync(_deviceConfig, Models.OpenIPC.MajesticFileLoc, updatedFileContent);
            await _sshClientService.ExecuteCommandAsync(_deviceConfig, DeviceCommands.MajesticRestartCommand);
            await Task.Delay(5000);
            

            Log.Debug("YAML file saved and majestic service restarted successfully.");
        }
        catch (Exception ex)
        {
            Log.Debug($"Failed to save YAML file: {ex.Message}");
        }
    }

    public void UpdateYamlConfig(string key, string newValue)
    {
        if (_yamlConfig.ContainsKey(key))
        {
            _yamlConfig[key] = newValue;
        }
        else
        {
            _yamlConfig.Add(key, newValue);
        }
    }
    
    // Recursively update YAML node based on key path
    private void UpdateYamlNode(YamlMappingNode root, string keyPath, string newValue)
    {
        var keys = keyPath.Split('.');
        YamlMappingNode currentNode = root;

        for (int i = 0; i < keys.Length - 1; i++)
        {
            var key = keys[i];
            if (currentNode.Children.ContainsKey(new YamlScalarNode(key)))
            {
                currentNode = (YamlMappingNode)currentNode.Children[new YamlScalarNode(key)];
            }
            else
            {
                throw new KeyNotFoundException($"Key '{key}' not found in YAML.");
            }
        }

        var lastKey = keys[^1];
        if (currentNode.Children.ContainsKey(new YamlScalarNode(lastKey)))
        {
            currentNode.Children[new YamlScalarNode(lastKey)] = new YamlScalarNode(newValue);
        }
        else
        {
            throw new KeyNotFoundException($"Key '{lastKey}' not found in YAML.");
        }
    }
}