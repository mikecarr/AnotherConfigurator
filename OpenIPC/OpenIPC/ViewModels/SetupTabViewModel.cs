using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls.Converters;
using Avalonia.Media;
using Avalonia.Rendering;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.Input;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using OpenIPC.Events;
using OpenIPC.Messages;
using OpenIPC.Models;
using OpenIPC.Services;
using Prism.Events;
using ReactiveUI;
using Serilog;

namespace OpenIPC.ViewModels
{
    public class SetupTabViewModel : ReactiveObject
    {

        private readonly ISshClientService _sshClientService;

        private IEventAggregator _eventAggregator;
        

        private bool _canConnect;

        private bool _isProgressBarVisible;
        public bool IsProgressBarVisible
        {
            get => _isProgressBarVisible;
            set => this.RaiseAndSetIfChanged(ref _isProgressBarVisible, value);
        }

        public ICommand ShowProgressBarCommand { get; set; }

        private int _downloadProgress;

        public int DownloadProgress
        {
            get => _downloadProgress;
            set => this.RaiseAndSetIfChanged(ref _downloadProgress, value);
        }

        private string _scanMessages;
        
        public string ScanMessages
        {
            get => _scanMessages;
            set => this.RaiseAndSetIfChanged(ref _scanMessages, value);
        }
        
        private string _progressText;

        public string ProgressText
        {
            get => _progressText;
            set => this.RaiseAndSetIfChanged(ref _progressText, value);
        }

        public ObservableCollection<string> LocalSensors { get; set; }
        public ObservableCollection<string> FirmwareVersions { get; set; }

        private ICommand _scriptFilesBackupCommand;
        private ICommand _scriptFilesRestoreCommand;
        private ICommand _sensorFilesUpdateCommand;
        private ICommand _sensorFilesBackupCommand;
        private ICommand _sensorDriverUpdateCommand;
        private ICommand _firmwareUpdateCommand;
        private ICommand _sendDroneKeyCommand;
        private ICommand _recvDroneKeyCommand;
        private ICommand _resetCameraCommand;
        private ICommand _offlineUpdateCommand;
        private ICommand _scanCommand;


        public ICommand ScriptFilesBackupCommand => _scriptFilesBackupCommand ??
                                                    (_scriptFilesBackupCommand = new RelayCommand(ScriptFilesBackup));

        public ICommand SensorFilesBackupCommand => _sensorFilesBackupCommand ??
                                                    (_scriptFilesBackupCommand = new RelayCommand(SensorFilesBackup));

        
        public ICommand SensorDriverUpdateCommand => _sensorDriverUpdateCommand ??
                                                     (_sensorDriverUpdateCommand = new RelayCommand(SensorDriverUpdate));

        
        public ICommand ScriptFilesRestoreCommand => _scriptFilesRestoreCommand ??
                                                     (_scriptFilesRestoreCommand = new RelayCommand(ScriptFilesRestore));

        
        // public ICommand ScriptFilesRestoreCommand { get; } = new RelayCommand(ScriptFilesRestore);

        public ICommand SensorFilesUpdateCommand => _sensorFilesUpdateCommand ??
                                                    (_sensorFilesUpdateCommand = new RelayCommand(SensorFilesUpdate));

        public ICommand FirmwareUpdateCommand =>
            _firmwareUpdateCommand ?? (_firmwareUpdateCommand = new RelayCommand(FirmwareUpdate));

        public ICommand SendDroneKeyCommand =>
            _sendDroneKeyCommand ?? (_sendDroneKeyCommand = new RelayCommand(SendDroneKey));

        public ICommand RecvDroneKeyCommand =>
            _recvDroneKeyCommand ?? (_sendDroneKeyCommand = new RelayCommand(RecvDroneKey));

        public ICommand ResetCameraCommand =>
            _resetCameraCommand ?? (_resetCameraCommand = new RelayCommand(ResetCamera));

        public ICommand OfflineUpdateCommand =>
            _offlineUpdateCommand ?? (_offlineUpdateCommand = new RelayCommand(OfflineUpdate));

        public ICommand ScanCommand => _scanCommand ?? (_scanCommand = new RelayCommand(ScanNetwork));
        
        
        private string _scapIpDescription;

        public string ScanIPDescription
        {
            get => _scapIpDescription;
            set
            {
                this.RaiseAndSetIfChanged(ref _scapIpDescription, value);
                Log.Debug($"ScanIPDescription updated to {value}");
            }
        }

        private string _scanIPLabel;


        public string ScanIPLabel
        {
            get => _scanIPLabel;
            set
            {
                this.RaiseAndSetIfChanged(ref _scanIPLabel, value);
                //Log.Debug($"ScanIPLabel updated to {value}");
            }
        }

        private string _scanIPResultTextBox;

        public string ScanIPResultTextBox
        {
            get => _scanIPResultTextBox;
            set
            {
                this.RaiseAndSetIfChanged(ref _scanIPResultTextBox, value);
                Log.Debug($"IpLabelText updated to {value}");
            }
        }


        private string _selectedSensor;

        public string SelectedSensor
        {
            get => _selectedSensor;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedSensor, value);
                Log.Debug($"SelectedSensor updated to {value}");
            }
        }
        
        private string _selectedFwVersion;

        public string SelectedFwVersion
        {
            get => _selectedFwVersion;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedFwVersion, value);
                Log.Debug($"SelectedFwVersion updated to {value}");
            }
        }

        public bool CanConnect
        {
            get => _canConnect;
            set
            {
                this.RaiseAndSetIfChanged(ref _canConnect, value);
                Log.Debug($"CanConnect {value}");
            }
        }


        public SetupTabViewModel()
        {
            InitializeCollections();

            ShowProgressBarCommand = new RelayCommand(() => IsProgressBarVisible = true);

            _eventAggregator = App.EventAggregator;
            _eventAggregator.GetEvent<AppMessageEvent>().Subscribe(OnAppMessage);
            //_eventAggregator?.GetEvent<DeviceStateUpdatedEvent>().Subscribe(OnDeviceStateUpdated);

            
            
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


        private void InitializeCollections()
        {
            // load sensor files from local folder
            string directoryPath = Models.OpenIPC.LocalSensorsFolder;
            PopulateSensorFileNames(directoryPath);

            ScanIPDescription = "Enter an IP address to scan.";
            ScanIPLabel = "192.168.1.";

            FirmwareVersions = new ObservableCollection<string>
            {
                "ssc338q_fpv_emax-wyvern-link-nor",
                "ssc338q_fpv_openipc-mario-aio-nor",
                "ssc338q_fpv_openipc-urllc-aio-nor",
                "ssc338q_fpv_runcam-wifilink-nor",
                "openipc.ssc338q-nor-fpv",
                "openipc.ssc338q-nor-rubyfpv",
                "openipc.ssc338q-nand-fpv",
                "openipc.ssc338q-nand-rubyfpv",
                "openipc.ssc30kq-nor-fpv",
                "openipc.ssc30kq-nor-rubyfpv",
                "openipc.hi3536dv100-nor-fpv",
                "openipc.gk7205v200-nor-fpv",
                "openipc.gk7205v200-nor-rubyfpv",
                "openipc.gk7205v210-nor-fpv",
                "openipc.gk7205v210-nor-rubyfpv",
                "openipc.gk7205v300-nor-fpv",
                "openipc.gk7205v300-nor-rubyfpv",
                "openipc.hi3516ev300-nor-fpv",
                "openipc.hi3516ev200-nor-fpv"
            };
        }

        // private void OnDeviceStateUpdated(DeviceStateUpdatedMessage message)
        // { 
        //     DeviceConfig.Instance = message.DeviceConfig;
        //     if (message.CanConnect != null)
        //     {
        //         CanConnect = message.CanConnect;
        //     }
        // }

        private async void ScriptFilesBackup()
        {
            Log.Debug("Backup script executed");
            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/usr/bin/channels.sh", "channels.sh");
            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/816.sh", "816.sh");
            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/1080.sh", "1080.sh");
            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/1080b.sh", "1080b.sh");
            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/1264.sh", "1264.sh");
            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/3K.sh", "3K.sh");

            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/4K.sh", "4K.sh");
            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/1184p100.sh", "1184p100.sh");
            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/1304p80.sh", "1304p80.sh");
            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/1440p60.sh", "1440p60.sh");
            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/1920p30.sh", "1920p30.sh");

            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/1080p60.sh", "1080p60.sh");
            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/720p120.sh", "720p120.sh");
            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/720p90.sh", "720p90.sh");
            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/720p60.sh", "720p60.sh");
            _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/1080p120.sh", "1080p120.sh");
            Thread.Sleep(1000);
            _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/1248p90.sh", "1248p90.sh");
            _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/1304p80.sh", "1304p80.sh");
            _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/1416p70.sh", "1416p70.sh");
            _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, "/root/kill.sh", "kill.sh");
            Log.Debug("Backup script executed...done");
        }


        private async void ScriptFilesRestore()
        {
            Log.Debug("Restore script executed");
        }

        private void PopulateSensorFileNames(string directoryPath)
        {
            try
            {
                var files = Directory.GetFiles(directoryPath);
                LocalSensors = new ObservableCollection<string>(files.Select(f => Path.GetFileName(f)));
            }
            catch (Exception ex)
            {
                Log.Debug($"Error populating file names: {ex.Message}");
            }
        }

        private async void SensorDriverUpdate()
        {
            Log.Debug("SensorDriverUpdate executed");
            DownloadProgress = 0;
            IsProgressBarVisible = true;
            //TODO: finish this
            //try=""
            //koup
            //echo y | pscp -scp -pw %3 %4 root@%2:/lib/modules/4.9.84/sigmastar/
            
                DownloadProgress = 100;
            ProgressText = "Sensor driver updated!";
            
            Log.Debug("SensorDriverUpdate executed..done");
        }

        private async void SensorFilesUpdate()
        {
            DownloadProgress = 0;
            IsProgressBarVisible = true;
            
            var selectedSensor = SelectedSensor;
            if (selectedSensor == null)
            {
                MessageBoxManager.GetMessageBoxStandard("Error", "No sensor selected", ButtonEnum.Ok);
                
                var box = MessageBoxManager
                    .GetMessageBoxStandard("error!", "No sensor selected!",
                        ButtonEnum.Ok);
                await box.ShowAsync();
                return;

            }
            
            ProgressText = "Starting upload...";
            DownloadProgress = 50;
            _sshClientService.UploadBinaryAsync(DeviceConfig.Instance,Models.OpenIPC.RemoteSensorsFolder, 
                Models.OpenIPC.FileType.Sensors, selectedSensor);
            
            ProgressText = "Updating Majestic file...";
            DownloadProgress = 75;
            // update majestic file
            // what is .video0.sensorConfig used for?
            //_sshClientService.ExecuteCommandAsync(DeviceConfig.Instance, $"yaml-cli -s .video0.sensorConfig {OpenIPC.RemoteSensorsFolder}/{selectedSensor}");
            _sshClientService.ExecuteCommandAsync(DeviceConfig.Instance, 
                $"yaml-cli -s .isp.sensorConfig {Models.OpenIPC.RemoteSensorsFolder}/{selectedSensor}");
            
            // echo y | pscp -scp -pw %3 sensors/%4 root@%2:/etc/sensors/ 
            //     plink -ssh root@%2 -pw %3 yaml-cli -s .isp.sensorConfig /etc/sensors/%4
            //echo y | pscp -scp -pw %3 %4 root@%2:/etc/sensors/


            //_sshClientService.UploadDirectoryAsync(DeviceConfig.Instance, OpenIPC.LocalSensorsFolder,
            // OpenIPC.RemoteSensorsFolder);
            ProgressText = "Done updating sensor...";
            DownloadProgress = 100;
        }

        private async void OfflineUpdate()
        {
            Log.Debug("OfflineUpdate executed");
            IsProgressBarVisible = true;
            DownloadStart();
            Log.Debug("OfflineUpdate executed..done");
        }

        private async void ScanNetwork()
        {
            ScanMessages = "Starting scan...";
            ScanIPResultTextBox = "Available IP Addresses on your network:";
            await Task.Delay(500); // Replace Thread.Sleep with async-friendly delay

            List<Task> pingTasks = new List<Task>();

            for (int i = 0; i < 254; i++)
            {
                var host = ScanIPLabel + i.ToString();
                Log.Debug($"Scanning {host}()");

                // Use async ping operation
                var pingTask = Task.Run(async () =>
                {
                    Ping ping = new Ping();
                    PingReply pingReply = await ping.SendPingAsync(host);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        ScanMessages = $"Scanned {host}, result: {pingReply.Status}";
                        //ScanIPResultTextBox += Environment.NewLine + host + ": " + pingReply.Status.ToString();
                        if (pingReply.Status == IPStatus.Success)
                        {
                            ScanIPResultTextBox += Environment.NewLine + host;
                        }
                    });
                });
                pingTasks.Add(pingTask);
            }

            ScanMessages = "Waiting for scan results.....";
            // Wait for all ping tasks to complete
            await Task.WhenAll(pingTasks);

            ScanMessages = "Scan completed";
            //var confirmBox = MessageBoxManager.GetMessageBoxStandard("Scan completed", "Scan completed", ButtonEnum.Ok);
            //await confirmBox.ShowAsync();
        }

        

        /// <summary>
        /// Extracts a value from a string using a regular expression pattern.
        /// </summary>
        /// <param name="input">The string to extract the value from.</param>
        /// <param name="pattern">The regular expression pattern to use for extraction.</param>
        /// <returns>The extracted value, or null if the pattern does not match.</returns>
        public static string ExtractValue(string input, string pattern)
        {
            Match match = Regex.Match(input, pattern);
            if (match.Success)
            {
                if (match.Groups.Count > 1)
                {
                    return match.Groups[1].Value;
                }
                else
                {
                    return match.Groups[0].Value;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Downloads the latest firmware version of the selected type from the official OpenIPC repositories.
        /// </summary>
        /// <param name="SelectedFwVersion">The firmware version to download. This should be one of the following: 
        /// "ssc338q_fpv_emax-wyvern-link-nor", "ssc338q_fpv_openipc-mario-aio-nor", "ssc338q_fpv_openipc-urllc-aio-nor", "ssc338q_fpv_runcam-wifilink-nor".
        /// </param>
        public async void DownloadStart()
        {
            //TODO: add more checks here, this can brick a device
            //updateDeviceConfig();
            IsProgressBarVisible = true; // Show the progress bar when the download starts
            string kernelPath = string.Empty;
            string rootfsPath = string.Empty;
            string sensorType = string.Empty;

            string url = string.Empty;
            if (SelectedFwVersion == "ssc338q_fpv_emax-wyvern-link-nor" ||
                SelectedFwVersion == "ssc338q_fpv_openipc-mario-aio-nor" ||
                SelectedFwVersion == "ssc338q_fpv_openipc-urllc-aio-nor" ||
                SelectedFwVersion == "ssc338q_fpv_runcam-wifilink-nor")
            {
                url = $"https://github.com/OpenIPC/builder/releases/download/latest/{SelectedFwVersion}.tgz";
                string aioPattern = "^[^_]+";
                sensorType = ExtractValue($"{SelectedFwVersion}", aioPattern);
            }
            else
            {
                url = $"https://github.com/OpenIPC/firmware/releases/download/latest/{SelectedFwVersion}.tgz";
                string openipcPattern = @"openipc\.([^-]+)";
                sensorType = ExtractValue($"{SelectedFwVersion}", openipcPattern);
            }

            if ((SelectedFwVersion != string.Empty) && (sensorType != string.Empty))
            {
                string firmwarePath = $"{Models.OpenIPC.LocalFirmwareFolder}/{SelectedFwVersion}.tgz";
                string localTmpPath = $"{Models.OpenIPC.LocalFirmwareFolder}";
                if (!Directory.Exists(localTmpPath))
                {
                    Directory.CreateDirectory(localTmpPath);
                }

                string firmwareUrl = new Uri(url).ToString();
                Log.Debug($"Downloading firmware {firmwareUrl}");

                // Reset progress and attach progress event
                DownloadProgress = 0;
                ProgressText = "Starting download...";

                // Use HttpClient instead of WebClient
                using (var client = new HttpClient())
                {
                    using (var response = await client.GetAsync(firmwareUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        // Download file with progress
                        var totalBytes = response.Content.Headers.ContentLength ?? 1;
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(firmwarePath, FileMode.Create, FileAccess.Write,
                                   FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            int bytesRead;
                            double totalRead = 0;

                            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalRead += bytesRead;
                                DownloadProgress =
                                    (int)(totalRead / totalBytes * 50); // Assume download is 50% of the process
                                ProgressText = $"Downloading... {DownloadProgress}%";
                            }
                        }
                    }
                }

                // Continue with the rest of the process
                DownloadProgress = 50;
                ProgressText = "Download complete, starting upload...";

                // Step 2: Upload file
                await _sshClientService.UploadFileAsync(DeviceConfig.Instance, firmwarePath,
                    $"{Models.OpenIPC.RemoteTempFolder}/{SelectedFwVersion}.tgz");
                DownloadProgress = 60;
                ProgressText = "Upload complete, decompressing...";

                // Step 3: Decompress using gzip
                string remoteFilePath = Path.Combine(Models.OpenIPC.RemoteTempFolder, $"{SelectedFwVersion}.tgz");
                await _sshClientService.ExecuteCommandAsync(DeviceConfig.Instance, $"gzip -d {remoteFilePath}");
                DownloadProgress = 70;
                ProgressText = "Decompression complete, extracting files...";

                // Step 4: Extract files using tar
                await _sshClientService.ExecuteCommandAsync(DeviceConfig.Instance,
                    $"tar -xvf {Models.OpenIPC.RemoteTempFolder}/{SelectedFwVersion}.tar -C /tmp");
                DownloadProgress = 85;
                ProgressText = "Extraction complete, upgrading system...";

                // Step 5: Execute sysupgrade
                kernelPath = $"{Models.OpenIPC.RemoteTempFolder}/uImage.{sensorType}";
                rootfsPath = $"{Models.OpenIPC.RemoteTempFolder}/rootfs.squashfs.{sensorType}";
                
                //sysupgrade --kernel=/tmp/uImage.%4 --rootfs=/tmp/rootfs.squashfs.%4 -n
                await _sshClientService.ExecuteCommandAsync(DeviceConfig.Instance,
                    $"sysupgrade --kernel={kernelPath} --rootfs={rootfsPath} -n");
                DownloadProgress = 100;
                ProgressText = "System upgrade complete!";
            }
        }


        private async void FirmwareUpdate()
        {
            Log.Debug("FirmwareUpdate executed");
            // if "%1" == "sysup" (
            //     plink -ssh root@%2 -pw %3 sysupgrade -k -r -n --force_ver
            //     )
            Log.Debug("This command will only succeed if the device has access to the internet");
            await _sshClientService.ExecuteCommandAsync(DeviceConfig.Instance, DeviceCommands.FirmwareUpdateCommand);
            Log.Debug("FirmwareUpdate executed..done");
        }

        private async void RecvDroneKey()
        {
            Log.Debug("RecvDroneKeyCommand executed");

            if (File.Exists("drone.key"))
            {
                Log.Debug("drone.key already exists locally, do you want to overwrite it?");
                var msBox = MessageBoxManager.GetMessageBoxStandard("File exists!",
                    "File drone.key already exists locally, do you want to overwrite it?", ButtonEnum.OkCancel);

                var result = await msBox.ShowAsync();
                if (result == ButtonResult.Cancel)
                {
                    Log.Debug("local drone.key was not overwritten");
                    return;
                }
            }

            await _sshClientService.DownloadFileLocalAsync(DeviceConfig.Instance, Models.OpenIPC.RemoteEtcFolder + "/drone.key",
                "drone.key");
            if (!File.Exists("drone.key"))
            {
                Log.Debug("RecvDroneKeyCommand failed");
            }

            Log.Debug("RecvDroneKeyCommand executed...done");
        }

        private async void SendDroneKey()
        {
            Log.Debug("SendDroneKey executed");
            // if "%1" == "keysulcam" (
            //     echo y | pscp -scp -pw %3 drone.key root@%2:/etc
            //     )
            await _sshClientService.UploadFileAsync(DeviceConfig.Instance, Models.OpenIPC.DroneKeyPath, Models.OpenIPC.RemoteDroneKeyPath);

            Log.Debug("SendDroneKey executed...done");
        }

        private async void ResetCamera()
        {
            Log.Debug("ResetCamera executed");
            // if "%1" == "resetcam" (
            //     plink -ssh root@%2 -pw %3 firstboot
            //     )
            var box = MessageBoxManager
                .GetMessageBoxStandard("Warning!", "All OpenIPC camera settings will be restored to default.",
                    ButtonEnum.OkAbort);
            var result = await box.ShowAsync();
            if (result == ButtonResult.Ok)
            {
                await _sshClientService.ExecuteCommandAsync(DeviceConfig.Instance, DeviceCommands.ResetCameraCommand);
            }
            else
            {
                Log.Debug("ResetCamera Aborted!");
                var confirmBox = MessageBoxManager
                    .GetMessageBoxStandard("Warning!", "No changes applied.", ButtonEnum.Ok);
                await confirmBox.ShowAsync();
                return;
            }

            return;
            Log.Debug("ResetCamera executed...done");
        }

        private async void SensorFilesBackup()
        {
            // if "%1" == "bindl" (
            //     echo y | mkdir backup
            // echo y | pscp -scp -pw %3 root@%2:/etc/sensors/%4 ./backup/
            //     )
            Log.Debug("SensorFilesBackup executed");
            await _sshClientService.DownloadDirectoryAsync(DeviceConfig.Instance, "/etc/sensors",
                $"{Models.OpenIPC.LocalBackUpFolder}");
            Log.Debug("SensorFilesBackup executed...done");
        }
    }
}