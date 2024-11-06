using System.IO;
using Newtonsoft.Json;
using OpenIPC.Models;
using Prism.Events;
using Serilog;


namespace OpenIPC.Services;

public static class SettingsManager
{
    private static string AppSettingsName = "openipc_settings.json";
    private static string AppSettingFilename = $"{Models.OpenIPC.AppDataConfigDirectory}/{AppSettingsName}";
    
    
/// <summary>
/// Loads the device configuration settings from a JSON file.
/// </summary>
/// <returns>
/// A <see cref="DeviceConfig"/> object containing the loaded settings.
/// If the settings file does not exist, returns a <see cref="DeviceConfig"/> 
/// with default values.
/// </returns>
    public static DeviceConfig? LoadSettings(IEventAggregator eventAggregator)
    {
        DeviceConfig deviceConfig;
        if (File.Exists(AppSettingFilename))
        {
            var json = File.ReadAllText(AppSettingFilename);

            deviceConfig = JsonConvert.DeserializeObject<DeviceConfig>(json);
            if (deviceConfig != null)
            {
                // DeviceStateUpdatedMessage deviceStateUpdatedMessage = new DeviceStateUpdatedMessage(true, deviceConfig);
                // eventAggregator?.GetEvent<DeviceStateUpdatedEvent>()
                //     .Publish(deviceStateUpdatedMessage);
            }
            else
            {
                Log.Error("LoadSettings: deviceConfig is null");
            }
            return deviceConfig;
        }

        // Default values if no settings file exists
        return new DeviceConfig
        {
            IpAddress = "",
            Username = "",  
            Password = "",
            DeviceType = DeviceType.Camera
        };
    }

/// <summary>
/// Saves the device configuration settings to a JSON file.
/// </summary>
/// <param name="settings">The <see cref="DeviceConfig"/> object containing the settings to be saved.</param>
/// <remarks>
/// This method serializes the provided <see cref="DeviceConfig"/> object into a JSON format and writes it to a file.
/// </remarks>
    public static void SaveSettings(DeviceConfig settings)
    {
        var json = JsonConvert.SerializeObject(settings, Formatting.Indented);
        File.WriteAllText(AppSettingFilename, json);
    }
}