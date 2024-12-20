using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using OpenIPC_Config.Models;

namespace OpenIPC_Config.Services;

public class DeviceConfigValidator
{
    private readonly IConfiguration _configuration;
    private readonly Dictionary<DeviceType, List<string>> _deviceHostnameMapping;

    public DeviceConfigValidator(IConfiguration configuration)
    {
        _configuration = configuration;

        _deviceHostnameMapping = _configuration.GetSection("DeviceHostnameMapping")
                                     .Get<Dictionary<DeviceType, List<string>>>()
                                 ?? new Dictionary<DeviceType, List<string>>();
    }

    public bool IsDeviceConfigValid(DeviceConfig deviceConfig)
    {
        if (_deviceHostnameMapping.TryGetValue(deviceConfig.DeviceType, out var allowedHostnames))
        {
            return allowedHostnames.Any(hostname => deviceConfig.Hostname.Contains(hostname));
        }

        return false; // Invalid if no mapping exists
    }
}

