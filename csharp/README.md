# C# – Plugins & Custom Workflow Activities

Server-side extensibility for Dynamics 365 / Dataverse using the **Dataverse SDK for .NET**.

## Folder Structure

```
csharp/
├── Plugins/                      # IPlugin implementations
│   ├── Plugins.csproj
│   ├── PluginBase.cs             # Abstract base class (error handling, tracing)
│   └── AccountPreCreate.cs      # Sample: Pre-Create plugin on Account
└── CustomWorkflowActivities/    # CodeActivity implementations
    ├── CustomWorkflowActivities.csproj
    └── FormatPhoneNumber.cs      # Sample: Format phone number in a Flow/Process
```

## Prerequisites

- Visual Studio 2022 (or `dotnet` CLI)
- NuGet: `Microsoft.CrmSdk.CoreAssemblies`
- [Plugin Registration Tool](https://learn.microsoft.com/power-apps/developer/data-platform/download-tools-nuget) (or `pac plugin push`)

## Build & Deploy

```bash
# Restore NuGet packages and build
dotnet restore csharp/Plugins/Plugins.csproj
dotnet build   csharp/Plugins/Plugins.csproj --configuration Release
```

Register the assembly via **Plugin Registration Tool** or Power Platform CLI:

```bash
pac plugin push --pluginFile bin/Release/net462/Plugins.dll
```

## Plugin Pipeline Quick Reference

| Stage | Number | Description |
|-------|--------|-------------|
| Pre-Validation | 10 | Before security checks; runs outside transaction |
| Pre-Operation  | 20 | Before DB write; runs inside transaction |
| Post-Operation | 40 | After DB write; runs inside transaction |

## References

- [Write a plug-in](https://learn.microsoft.com/power-apps/developer/data-platform/write-plug-in)
- [Debug a plug-in](https://learn.microsoft.com/power-apps/developer/data-platform/debug-plug-in)
- [Custom workflow activities](https://learn.microsoft.com/power-apps/developer/data-platform/workflow/workflow-extensions)
