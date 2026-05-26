# Dynamics 365 / Power Platform Practice Repository

A hands-on collection of samples and exercises covering the full Dynamics 365 and Power Platform development stack — from client-side scripting to server-side plugins and custom UI components.

## Topics Covered

| Area | Language / Technology | Description |
|------|-----------------------|-------------|
| [JavaScript / TypeScript](./javascript/) | JS / TS | Form scripts, ribbon commands, web resources |
| [C# Plugins](./csharp/) | C# | Server-side plugins, custom workflow activities |
| [PCF Controls](./pcf/) | TypeScript | Power Apps Component Framework custom controls |
| [Power Automate](./power-automate/) | JSON / YAML | Flow definitions and patterns |

## Prerequisites

| Tool | Purpose |
|------|---------|
| [Visual Studio 2022](https://visualstudio.microsoft.com/) | C# plugin development |
| [VS Code](https://code.visualstudio.com/) | JavaScript / TypeScript / PCF |
| [Node.js 18+](https://nodejs.org/) | PCF tooling, TypeScript compilation |
| [Power Platform CLI (`pac`)](https://learn.microsoft.com/power-platform/developer/cli/introduction) | PCF scaffolding, solution management |
| [.NET 6+ SDK](https://dotnet.microsoft.com/) | C# projects |
| [Dynamics 365 SDK NuGet packages](https://www.nuget.org/packages/Microsoft.CrmSdk.CoreAssemblies/) | Plugin development |

## Repository Structure

```
├── javascript/          # Client-side scripts (JS + TypeScript)
│   ├── web-resources/   # Form scripts, ribbon button handlers
│   └── typescript/      # Typed versions with tsconfig
├── csharp/              # Server-side C# code
│   ├── Plugins/         # IPlugin implementations
│   └── CustomWorkflowActivities/  # CodeActivity implementations
├── pcf/                 # Power Apps Component Framework controls
│   └── LinearInputComponent/      # Starter PCF sample
└── power-automate/      # Flow samples and patterns
    └── samples/
```

## Getting Started

1. **Clone** this repository
2. Navigate to the topic folder you want to explore
3. Follow the `README.md` inside each folder for setup instructions

## Learning Path

If you are new to Dynamics 365 / Power Platform development, recommended order:

1. **JavaScript** — understand the Xrm client API and form scripting
2. **C# Plugins** — learn the server-side event pipeline
3. **PCF** — build custom UI controls with TypeScript
4. **Power Automate** — automate business processes without code

## Resources

- [Microsoft Learn – Dynamics 365](https://learn.microsoft.com/dynamics365/)
- [Microsoft Learn – Power Platform](https://learn.microsoft.com/power-platform/)
- [Power Apps Component Framework docs](https://learn.microsoft.com/power-apps/developer/component-framework/overview)
- [Dataverse SDK for .NET](https://learn.microsoft.com/power-apps/developer/data-platform/org-service/overview)
- [Xrm Client API Reference](https://learn.microsoft.com/power-apps/developer/model-driven-apps/clientapi/reference)

<!-- 学習メモ: 2026-05-26 GitHubの操作を練習中 -->
