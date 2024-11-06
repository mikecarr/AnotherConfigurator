using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using OpenIPC.Logging;
using OpenIPC.ViewModels;
using OpenIPC.Views;
using Prism.Events;
using Serilog;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;

namespace OpenIPC;

public partial class App : Application
{
    public static IEventAggregator EventAggregator { get; private set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        EventAggregator = new EventAggregator();

        
    }

    void CreateAppSettings()
    {
        string configPath;
        string configDirectory;

        string appName = Assembly.GetExecutingAssembly().GetName().Name;

        if (OperatingSystem.IsAndroid())
        {
            // Android-specific path
            configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName);
            configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName, "appsettings.json");
        }
        else if (OperatingSystem.IsWindows())
        {
            configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName);
            configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName,
                "appsettings.json");
        }
        else if (OperatingSystem.IsMacOS())
        {
            configDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName);
            configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), appName,
                "appsettings.json");
        }
        else // Assume Linux
        {
            configDirectory = Path.Combine($"./config/{appName}");
            configPath = Path.Combine($"./config/{appName}", "appsettings.json");
        }

        if (!Directory.Exists(configDirectory))
            Directory.CreateDirectory(configDirectory);

        // Check if appsettings.json exists, otherwise create a default one
        if (!File.Exists(configPath))
        {
            // Create default settings
            var defaultSettings = createDefaultAppSettings();


            File.WriteAllText(configPath, defaultSettings.ToString());
            
            Thread.Sleep(2000);
            
            Log.Information($"Default appsettings.json created at {configPath}");
        }

        
        var configuration = new ConfigurationBuilder()
            .AddJsonFile(configPath, optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.json",optional: true, reloadOnChange: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", true)
            .Build();
            
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .WriteTo.Sink(new EventAggregatorSink(EventAggregator))
            .CreateLogger();
        
        Log.Information($"Using appsettings.json from {configPath}");
        // Log.Logger = new LoggerConfiguration()
        //     .MinimumLevel.Debug()
        //     .WriteTo.Sink(new EventAggregatorSink(EventAggregator))
        //     //.ReadFrom.Configuration(configuration)
        //     .ReadFrom.AppSettings()
        //     .CreateLogger();
        
        Log.Information("Starting up log");
    }

    private JObject createDefaultAppSettings()
    {
        // Create default settings
        var defaultSettings = new JObject(
            new JProperty("Serilog",
                new JObject(
                    new JProperty("Using", new JArray("Serilog.Sinks.Console", "Serilog.Sinks.File")),
                    new JProperty("MinimumLevel", "Debug"),
                    new JProperty("WriteTo",
                        new JArray(
                            new JObject(
                                new JProperty("Name", "Console")
                            ),
                            new JObject(
                                new JProperty("Name", "File"),
                                new JProperty("Args",
                                    new JObject(
                                        new JProperty("path", "/Users/mcarr/Library/Application Support/OpenIPC/configurator.log")
                                    )
                                )
                            )
                        )
                    ),
                    new JProperty("Properties",
                        new JObject(
                            new JProperty("Application", "OpenIPC")
                        )
                    )
                )
            )
        );
        return defaultSettings;
    }

    public override void OnFrameworkInitializationCompleted()
    {
        CreateAppSettings();
        
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Line below is needed to remove Avalonia data validation.
            // Without this line you will get duplicate validations from both Avalonia and CT
            BindingPlugins.DataValidators.RemoveAt(0);
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}