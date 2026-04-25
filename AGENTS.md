# AGENTS.md

## Project Overview

- Repository type: .NET/C# solution for Meadow-based IoT apps.
- Root solution: `projet-iot.sln`.
- Nested solution: `projet-iot/projet-iot.sln` (same four projects, Visual Studio metadata differs).
- Language/runtime: C# with `LangVersion` 10 and nullable reference types enabled.
- Main app targets:
  - `projet-iot/projet-iot.F7` (Meadow F7 hardware)
  - `projet-iot/projet-iot.RPi` (Raspberry Pi)
  - `projet-iot/projet-iot.Desktop` (Desktop simulator)
  - `projet-iot/projet-iot.Core` (shared library)

## Repository Layout

- `projet-iot/projet-iot.Core`: shared controllers, contracts, telemetry, layouts.
- `projet-iot/projet-iot.F7`: Meadow F7 app + hardware wiring.
- `projet-iot/projet-iot.RPi`: Meadow Linux/RPi app + hardware wiring.
- `projet-iot/projet-iot.Desktop`: desktop Meadow app + hardware wiring.
- `projet-iot.sln`: top-level entry for CLI and IDE workflows.

## Prerequisites

- .NET SDK compatible with `net8.0` projects.
- Meadow SDK/tooling and package restore access (projects reference `Meadow.*` and `Meadow.Sdk/1.1.0`).
- Recommended IDE: Visual Studio 2022 / JetBrains Rider with Meadow support.

## Build, Lint, and Test Commands

Run commands from repository root (`C:\Users\rezpo\dev\Projet-IoT`) unless noted.

### Restore

```bash
dotnet restore "projet-iot.sln"
```

### Build

```bash
dotnet build "projet-iot.sln"
dotnet build "projet-iot.sln" -c Release
```

Build a specific project:

```bash
dotnet build "projet-iot/projet-iot.Core/projet-iot.Core.csproj"
dotnet build "projet-iot/projet-iot.Desktop/projet-iot.Desktop.csproj"
dotnet build "projet-iot/projet-iot.RPi/projet-iot.RPi.csproj"
dotnet build "projet-iot/projet-iot.F7/projet-iot.F7.csproj"
```

### Run

```bash
dotnet run --project "projet-iot/projet-iot.Desktop/projet-iot.Desktop.csproj"
dotnet run --project "projet-iot/projet-iot.RPi/projet-iot.RPi.csproj"
```

Notes:
- F7 deployment/run generally uses Meadow device tooling from IDE/Meadow workflows.
- Runtime config files (`app.config.yaml`, `wifi.config.yaml`, `meadow.config.yaml`) are copied to output by project settings.

### Lint / Formatting

No dedicated linter config (`.editorconfig`, StyleCop, ruleset) was found.
Use `dotnet format` as the formatting/lint gate:

```bash
dotnet format "projet-iot.sln"
dotnet format "projet-iot.sln" --verify-no-changes
```

If `dotnet format` is unavailable in your environment, at minimum run:

```bash
dotnet build "projet-iot.sln" -warnaserror
```

### Test

There are currently no test projects in the repository (`*Test*.csproj` not found).

When tests are added, use:

```bash
dotnet test "projet-iot.sln"
```

Run a single test (xUnit/NUnit/MSTest compatible filter):

```bash
dotnet test "projet-iot.sln" --filter "FullyQualifiedName~Namespace.ClassName.TestMethod"
```

Run one test class:

```bash
dotnet test "projet-iot.sln" --filter "FullyQualifiedName~Namespace.ClassName"
```

## Code Style and Conventions

These conventions are inferred from current source and project settings; follow them unless the team updates tooling.

### Imports / Usings

- Keep `using` directives at top of file.
- Group framework usings before project usings (current pattern).
- Avoid unused usings.
- Prefer explicit namespaces over global usings (none configured currently).

### Formatting

- Use 4 spaces for indentation; no tabs.
- Use file-scoped namespaces (`namespace X.Y;`).
- Keep braces on new lines for types/methods/control blocks.
- Keep lines readable; wrap long method calls similarly to existing telemetry/controller code.
- Preserve existing ASCII style unless file already requires other characters.

### Types and Nullability

- Nullable is enabled: model nulls explicitly with `?` and guard clauses.
- Validate required constructor/method arguments (`ArgumentNullException`, `InvalidOperationException`) as done in controllers/apps.
- Prefer concrete domain types (`Temperature`, telemetry models) over primitive-only APIs where practical.
- Keep async APIs returning `Task`/`Task<T>`; avoid `async void` except event handlers.

### Naming

- Public types/methods/properties/events: PascalCase.
- Local variables/parameters/private fields: camelCase.
- Interfaces: `I` prefix (for example `INetworkController`, `ITelemetrySink`).
- Preserve existing repository naming where intentional, including `projet_iot` namespace and hardware type names.
- Event names should describe state/action (`NetworkStatusChanged`, `ThresholdTemperatureChangeRequested`).

### Error Handling and Logging

- Fail fast for invalid state (for example not initialized) with explicit exceptions.
- Use non-fatal logging (`Resolver.Log.Warn/Info`) for recoverable runtime issues (network/cloud publish failures).
- Catch broad exceptions only at boundaries where the app must continue running (for example telemetry publishing).
- Include useful context in logs (reason/result codes), but avoid leaking secrets/tokens.

### Configuration and Secrets

- Treat environment variables and YAML config as primary runtime configuration sources.
- Do not hardcode credentials (Azure IoT SAS tokens, hostnames, device IDs).
- Keep precedence predictable: environment variables should override config defaults where applicable.

### Architecture Expectations

- Keep platform-specific wiring in `projet-iot.F7`, `projet-iot.RPi`, and `projet-iot.Desktop`.
- Keep reusable business logic in `projet-iot.Core`.
- Prefer constructor injection for dependencies like telemetry publishers/sinks.
- Use contracts in `Core/Contracts` to decouple hardware and controller implementations.

## Agent Instructions from Existing Rules

The following rule sources were checked and not found in this repository at time of writing:

- Cursor rules: `.cursor/rules/` and `.cursorrules`
- Copilot rules: `.github/copilot-instructions.md`
- Agent OS standards: `agent-os/standards/`

If any of these files are added later, update this section and merge relevant guidance into the conventions above.

## Change Checklist for Agents

Before submitting changes:

1. Restore/build the solution successfully.
2. Run formatting check (`dotnet format --verify-no-changes`) when available.
3. Run tests (or document that no test projects exist yet).
4. Ensure nullability warnings and initialization guards are addressed.
5. Confirm no secrets were added to source/config.
