# .NET 8.0 Upgrade Plan

## Execution Steps

Execute steps below sequentially one by one in the order they are listed.

1. Validate that a .NET 8.0 SDK required for this upgrade is installed on the machine and if not, help to get it installed.
2. Ensure that the SDK version specified in global.json files is compatible with the .NET 8.0 upgrade.
3. Upgrade HelixToolkit\HelixToolkit.csproj
4. Upgrade HelixToolkit.Wpf\HelixToolkit.Wpf_NET40.csproj
5. Upgrade HelixToolkit.Wpf.Input\HelixToolkit.Wpf.Input_NET40.csproj
6. Upgrade Examples\WPF\ExampleBrowser\ExampleBrowser_NET40.csproj

## Settings

This section contains settings and data used by execution steps.

### Aggregate NuGet packages modifications across all projects

NuGet packages used across all selected projects or their dependencies that need version update in projects that reference them.

| Package Name          | Current Version | New Version | Description   |
|:----------------------|:---------------:|:-----------:|:-------------------------------------------------|
| ExifLib   | 1.4.3.0    |       | No supported version found - needs to be removed |
| NAudio       | 1.7.1   | 2.2.1       | Recommended for .NET 8.0        |
| PropertyTools.Wpf     | 2015.2.0        | 3.1.0       | Recommended for .NET 8.0           |

### Project upgrade details

This section contains details about each project upgrade and modifications that need to be done in the project.

#### HelixToolkit\HelixToolkit.csproj modifications

Project properties changes:
  - Project file needs to be converted to SDK-style
  - Target framework should be changed from `.NETPortable,Version=v4.0,Profile=Profile136` to `net8.0`

#### HelixToolkit.Wpf\HelixToolkit.Wpf_NET40.csproj modifications

Project properties changes:
  - Project file needs to be converted to SDK-style
  - Target framework should be changed from `.NETFramework,Version=v4.8` to `net8.0-windows`

NuGet packages changes:
  - ExifLib version 1.4.3.0 needs to be removed - no supported version found

#### HelixToolkit.Wpf.Input\HelixToolkit.Wpf.Input_NET40.csproj modifications

Project properties changes:
  - Project file needs to be converted to SDK-style
  - Target framework should be changed from `.NETFramework,Version=v4.8` to `net8.0-windows`

#### Examples\WPF\ExampleBrowser\ExampleBrowser_NET40.csproj modifications

Project properties changes:
  - Project file needs to be converted to SDK-style
  - Target framework should be changed from `.NETFramework,Version=v4.8` to `net8.0-windows`

NuGet packages changes:
  - NAudio should be updated from `1.7.1` to `2.2.1` (recommended for .NET 8.0)
  - PropertyTools.Wpf should be updated from `2015.2.0` to `3.1.0` (recommended for .NET 8.0)