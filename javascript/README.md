# JavaScript / TypeScript – Dynamics 365 Client-Side Development

This folder contains client-side scripting samples for model-driven apps using the **Xrm Client API**.

## Folder Structure

```
javascript/
├── web-resources/
│   ├── form-scripts/     # Event handlers attached to model-driven app forms
│   └── ribbon-commands/  # Button click handlers for the command bar
└── typescript/
    ├── tsconfig.json
    └── src/              # TypeScript versions (type-safe Xrm API)
```

## Key Concepts

| Concept | Description |
|---------|-------------|
| `Xrm.Page` (legacy) | Old form API — avoid in new projects |
| `executionContext.getFormContext()` | Current recommended entry point |
| `formContext.getAttribute()` | Read / write field values |
| `formContext.ui.tabs` / `.sections` | Show / hide UI elements |
| `Xrm.Navigation` | Open forms, dialogs, URLs |
| `Xrm.WebApi` | Call Dataverse Web API from client side |

## How to Use

1. Edit the JS file under `web-resources/form-scripts/`.
2. Upload it to your environment as a **Web Resource** (type: Script/JScript).
3. Register the function on the relevant **Form Event** (OnLoad, OnChange, OnSave).

For TypeScript, compile first:

```bash
cd typescript
npm install
npm run build
# upload dist/*.js as the web resource
```

## References

- [Xrm Client API Reference](https://learn.microsoft.com/power-apps/developer/model-driven-apps/clientapi/reference)
- [Use the Web API from client-side code](https://learn.microsoft.com/power-apps/developer/data-platform/webapi/get-started-web-api-client-side-javascript)
