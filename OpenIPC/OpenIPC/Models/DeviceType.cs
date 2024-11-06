using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace OpenIPC.Models;

[JsonConverter(typeof(StringEnumConverter))]
public enum DeviceType
{
    None,
    Camera,
    Radxa,
    NVR
}