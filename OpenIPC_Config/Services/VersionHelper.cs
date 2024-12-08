using System;
using System.IO;
using System.Reflection;
using Serilog;

namespace OpenIPC_Config.Services;


public static class VersionHelper
{
    public static string GetAppVersion()
    {
        try
        {
            // Check if running in a local development environment
            if (IsDevelopment())
            {
                // Try to read the version from the VERSION file
                var versionFilePath = Path.Combine(AppContext.BaseDirectory, "VERSION");
                if (File.Exists(versionFilePath))
                {
                    return File.ReadAllText(versionFilePath).Trim();
                }
            }

            // Fallback to the assembly version
            return Assembly.GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion ?? "Unknown Version";
        }
        catch (Exception ex)
        {
            // Log the error and return a default version
            Log.Error($"Failed to get app version: {ex}");
            return "Unknown Version";
        }
    }


    private static bool IsDevelopment()
    {
        // Check the environment variable to determine if we are in development
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        return string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);
    }
}
