# Contributing

Thank you for your interest in contributing to this practice repository!

## Branch Naming

| Purpose | Pattern | Example |
|---------|---------|---------|
| New exercise | `exercise/<topic>-<short-desc>` | `exercise/pcf-color-picker` |
| Bug fix | `fix/<short-desc>` | `fix/account-plugin-null-ref` |
| Documentation | `docs/<short-desc>` | `docs/update-pcf-readme` |

## Adding a New Sample

1. **Fork** (external contributors) or **branch** (collaborators) from `main`.
2. Place your code under the relevant top-level folder (`javascript/`, `csharp/`, `pcf/`, `power-automate/`).
3. Include a `README.md` (or inline comments) explaining:
   - What the sample demonstrates
   - How to set it up / run it
   - Any Dataverse entity / field prerequisites
4. Open a Pull Request with a clear title and description.

## Code Style

- **C#**: follow the existing style (4-space indent, XML doc on public members).
- **TypeScript / JS**: 4-space indent, `"use strict"` for plain JS files.
- **PCF**: generated files (under `generated/`) are not committed.
- No secrets, API keys, or personal connection strings in any file.

## Questions

Open a GitHub Issue or start a Discussion.
