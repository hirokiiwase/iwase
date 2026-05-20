# Power Automate – Flow Samples

Cloud flow definitions and patterns for automating Dynamics 365 / Dataverse processes.

## Folder Structure

```
power-automate/
└── samples/
    ├── send-email-on-account-create.json   # Automated: notify on new Account
    └── approval-on-opportunity-close.json  # Approval: gate on Opportunity close
```

## How to Import a Sample

1. Go to [make.powerapps.com](https://make.powerapps.com) and select your environment.
2. Open **My flows** → **Import** → **Import Package (Legacy)**.
3. Upload the `.zip` of the exported solution, or manually re-create from the JSON schema below.

> **Tip:** For repeatable deployments, package flows inside a **Dataverse Solution** and use `pac solution export/import`.

## Key Trigger Types

| Trigger | Use case |
|---------|----------|
| When a row is added, modified or deleted | React to Dataverse data changes |
| Recurrence | Scheduled batch jobs |
| When an HTTP request is received | Expose a webhook from a plugin or PCF |
| Manual trigger (button) | Ad-hoc actions from a mobile app |

## Best Practices

- Use **environment variables** for connection references and configuration values.
- Prefer **Child Flows** to reuse logic across multiple flows.
- Always set a **run-after** on error branches to handle failures gracefully.
- Tag flows with a **solution** for ALM (export → test → production).

## References

- [Power Automate documentation](https://learn.microsoft.com/power-automate/)
- [Use Dataverse triggers and actions](https://learn.microsoft.com/power-automate/dataverse/overview)
- [Power Platform CLI – solution management](https://learn.microsoft.com/power-platform/developer/cli/reference/solution)
