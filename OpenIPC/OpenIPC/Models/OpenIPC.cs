using System;
using System.IO;
using System.Reflection;
using Prism.Modularity;

namespace OpenIPC.Models;

public class OpenIPC
{
    public const string AppName = "OpenIPC";
    
    public const string MajesticFileLoc = "/etc/majestic.yaml";
    public const string WfbConfFileLoc = "/etc/wfb.conf";
    public const string TelemetryConfFileLoc = "/etc/telemetry.conf";
    
    public const string LocalBackUpFolder = "binaries";
    public const string LocalBinariesFolder = "binaries";
    public const string LocalSensorsFolder = "binaries/sensors";
    public const string LocalFontsFolder = "binaries/fonts";
    public const string LocalBetaFlightFontsFolder = "binaries/fonts/bf";
    public const string LocalINavFontsFolder = "binaries/fonts/inav";
    
    // Base application data folder path for your app
    // public static readonly string LocalAppDataFolder = Path.Combine(
    //     Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), AppName);
    
    public static readonly string LocalTempFile = Path.Combine(Path.GetTempPath(), AppName);
    
    public static readonly string LocalFirmwareFolder = $"{LocalTempFile}/firmware";
    
    public const string LocalFwFolder = "binaries/fw";
    
    
    public const string RemoteEtcFolder = "/etc";
    public const string RemoteBinariesFolder = "/usr/bin";
    public const string RemoteFontsFolder = "/usr/share/fonts/";
    public const string RemoteSensorsFolder = "/etc/sensors";
    public const string RemoteDroneKeyPath = "/etc/drone.key"; 
    public const string RemoteTempFolder = "/tmp";
    
    public const string DroneKeyPath = "binaries/drone.key";
    


    public enum FileType
    {
        Normal,
        BetaFlightFonts,
        iNavFonts,
        Sensors
    }
    
    // Expose configPath and configDirectory as public static properties
    public static string AppDataConfigDirectory { get; private set; }
    public static string AppDataConfigPath { get; private set; }
    
    public static string DeviceSettingsConfigPath { get; private set; }
    static OpenIPC()
    {
        InitializePaths();
        // Ensure directories are created when paths are initialized
        // Console.WriteLine($"creating application folder: {LocalAppDataFolder}");
        // Directory.CreateDirectory(LocalAppDataFolder);
        // Console.WriteLine($"creating temp folder: {LocalFirmwareFolder}");
        // Directory.CreateDirectory(LocalFirmwareFolder);
        //Directory.CreateDirectory(LocalConfigFolder);


    }

    public static string DeviceUsername = "root";

    private static void InitializePaths()
    {
        string appName = Assembly.GetExecutingAssembly().GetName().Name;

        if (OperatingSystem.IsAndroid())
        {
            // Android-specific path
            AppDataConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName);
            AppDataConfigPath = Path.Combine(AppDataConfigDirectory, "appsettings.json");
            DeviceSettingsConfigPath = Path.Combine(AppDataConfigDirectory, "openipc_config.json");
        }
        else if (OperatingSystem.IsIOS())
        {
            AppDataConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                appName);
            AppDataConfigPath = Path.Combine(AppDataConfigDirectory, "appsettings.json");
            DeviceSettingsConfigPath = Path.Combine(AppDataConfigDirectory, "openipc_config.json");
        }
        else if (OperatingSystem.IsWindows())
        {
            AppDataConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName);
            AppDataConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName, "appsettings.json");
            DeviceSettingsConfigPath = Path.Combine(AppDataConfigDirectory, "openipc_config.json");
        }
        else if (OperatingSystem.IsMacOS())
        {
            AppDataConfigDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), appName);
            AppDataConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName, "appsettings.json");
            DeviceSettingsConfigPath = Path.Combine(AppDataConfigDirectory, "openipc_config.json");
        }
        else // Assume Linux
        {
            AppDataConfigDirectory = Path.Combine($"./config/{appName}");
            AppDataConfigPath = Path.Combine(AppDataConfigDirectory, "appsettings.json");
            DeviceSettingsConfigPath = Path.Combine(AppDataConfigDirectory, "openipc_config.json");
        }

        // Ensure the config directory exists
        Directory.CreateDirectory(AppDataConfigDirectory);
    }
}