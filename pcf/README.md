# PCF – Power Apps Component Framework

Custom UI controls built with TypeScript and the **Power Apps Component Framework**.

## Folder Structure

```
pcf/
└── LinearInputComponent/   # Starter sample: a numeric slider control
    ├── package.json
    ├── LinearInputComponent/
    │   ├── ControlManifest.Input.xml   # Control metadata & properties
    │   └── index.ts                    # Control logic
    └── .pcfproj
```

## Prerequisites

```bash
npm install -g microsoft-powerapps-cli   # or: npm install -g pac
# Verify
pac --version
```

## Scaffold a New Control

```bash
mkdir MyControl && cd MyControl
pac pcf init --namespace Contoso --name MyControl --template field
npm install
```

## Build & Test Locally

```bash
cd LinearInputComponent
npm install
npm run build        # production build
npm start watch      # hot-reload test harness at http://localhost:8181
```

## Deploy to an Environment

```bash
# 1. Create a solution folder
mkdir Solution && cd Solution
pac solution init --publisher-name Contoso --publisher-prefix cnt

# 2. Add the control to the solution
pac solution add-reference --path ../LinearInputComponent

# 3. Build and push
dotnet build
pac pcf push --publisher-prefix cnt
```

## PCF Lifecycle Methods

| Method | When called |
|--------|------------|
| `init` | Control mounted; set up DOM and subscribe to data |
| `updateView` | Property or dataset changed; re-render |
| `getOutputs` | Framework reads updated output property values |
| `destroy` | Control unmounted; clean up timers, listeners |

## References

- [PCF Overview](https://learn.microsoft.com/power-apps/developer/component-framework/overview)
- [PCF API Reference](https://learn.microsoft.com/power-apps/developer/component-framework/reference/)
- [Code Component Gallery](https://pcf.gallery/)
