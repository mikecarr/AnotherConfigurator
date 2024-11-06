# Another Configurator



## App Logs
* Android: /data/user/0/org.openipc.OpenIPC/files/.config/openipc-configurator.log
* Mac: /Users/mcarr/Library/Application Support/openipc-configurator.log

## TODO:
* Fix UI and views
* Cleanup code
* Add more features

Based on your folder structure, here’s an explanation of each project and what it likely targets:

### Project Breakdown

1. **OpenIPC**
    - This appears to be the core or shared library project that other platform-specific projects reference.
    - It likely contains shared code, models, services, or ViewModels that can be used across all platforms (Desktop, Android, Browser, iOS).

2. **OpenIPC.Desktop**
    - This project is intended for **desktop platforms** (Windows, macOS, and Linux).
    - In Avalonia, a "Desktop" project can target multiple operating systems using `net7.0-windows`, `net7.0-macos`, and `net7.0-linux`.
    - You can configure it to build for any or all of these desktop operating systems.

3. **OpenIPC.Android**
    - This project targets **Android** devices.
    - It will have Android-specific configurations and might use `net8.0-android` or `net7.0-android` as the `TargetFramework`.
    - Contains Android-specific files and setup, like Android permissions, manifest configurations, etc.

4. **OpenIPC.Browser**
    - This project is meant for **web applications**, targeting **WebAssembly (WASM)** using Avalonia's browser support.
    - The target framework is typically `net8.0-browser`.
    - This allows you to run the app in a web browser, making use of WebAssembly technology to execute .NET code in a web environment.

5. **OpenIPC.iOS**
    - This project is intended for **iOS devices** (iPhones and iPads).
    - Uses a target framework like `net8.0-ios` or `net7.0-ios` with iOS-specific configurations.
    - Contains iOS-specific files such as `Info.plist` for app metadata, `Entitlements.plist` for permissions, and `AppDelegate.cs` for application lifecycle management.

### Summary of Target Platforms for Each Project

| Project            | Target Platform(s)     | Description                                                                                      |
|--------------------|------------------------|--------------------------------------------------------------------------------------------------|
| **OpenIPC**        | Shared (all platforms) | Core library or shared code used across all platform-specific projects.                          |
| **OpenIPC.Desktop**| Windows, macOS, Linux  | Targets desktop OSs, can be configured to support `win-x64`, `osx-arm64`, `linux-x64`, etc.     |
| **OpenIPC.Android**| Android                | Targets Android devices with `net8.0-android` or `net7.0-android`.                               |
| **OpenIPC.Browser**| Browser (WebAssembly)  | Targets web browsers with WebAssembly using `net8.0-browser`.                                    |
| **OpenIPC.iOS**    | iOS                    | Targets iOS devices with `net8.0-ios` or `net7.0-ios`.                                          |

### Building Each Project

To build each project, navigate to the specific project folder and run the `dotnet publish` command with the appropriate runtime identifier for that platform.

For example:
- **macOS Desktop**: Go to `OpenIPC.Desktop` and run:

  ```bash
  dotnet publish -c Release -r osx-arm64 --self-contained
