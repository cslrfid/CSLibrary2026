# CSLibrary2026 — Project Constitution

> **Authority:** This document is the supreme governing specification for the CSLibrary2026 project. All code, documentation, CI/CD, and architectural decisions must conform to it. It may only be changed by a PR reviewed and approved by at least one maintainer — and the change itself must update this document in the same PR.

---

## 1. Project Identity

| Property | Value |
|---|---|
| **Name** | CSLibrary2026 |
| **Type** | .NET Standard / .NET 10 NuGet library |
| **Owner** | Convergence Systems Limited (CSL) |
| **License** | MIT |
| **Repository** | `https://github.com/cslrfid/CSLibrary2026` |
| **NuGet Package** | `CSLibrary2026` |
| **Target Frameworks** | `netstandard2.0` (Xamarin/legacy) · `net10.0` (modern .NET) |

### Mission Statement

CSLibrary2026 is the canonical .NET SDK for CSL RFID readers. It provides a unified, multi-platform API surface over hardware-specific BLE and TCP transport implementations, enabling MAUI, Xamarin, and other .NET consumers to communicate with CSL reader devices reliably and consistently.

---

## 2. Architecture

### 2.1 Layer Map

```
┌─────────────────────────────────────────────────┐
│  Consumer (MAUI App, Xamarin App, etc.)          │
│  → references CSLibrary2026 NuGet                │
└──────────────────────┬────────────────────────┘
                       │  RFIDReader / BarcodeReader / Battery / …
                       ▼
┌─────────────────────────────────────────────────┐
│  CSLUnifiedAPI (Basic_API)                      │
│  Public facing API — models, options, callbacks  │
│  Path: Source/RFIDReader/CSLUnifiedAPI/Basic_API/│
└──────────────────────┬────────────────────────┘
                       │  calls
                       ▼
┌─────────────────────────────────────────────────┐
│  Comm_Protocol / BluetoothProtocol              │
│  Low-level command encoding / response decoding │
│  Path: Source/RFIDReader/Comm_Protocol/         │
│        Source/BluetoothProtocol/                │
└──────────────────────┬────────────────────────┘
                       │  calls ITransport.SendAsync()
                       ▼
┌─────────────────────────────────────────────────┐
│  ITransport  (BLETransport | TCPTransport)       │
│  Physical transport abstraction                  │
│  Path: Source/Transport/                         │
└──────────────────────┬────────────────────────┘
                       │  Plugin.BLE or raw socket
                       ▼
┌─────────────────────────────────────────────────┐
│  Hardware (CS108, CS468, CS710S, CS203XL)       │
└─────────────────────────────────────────────────┘
```

### 2.2 HAL Pattern (Partial Classes)

`HighLevelInterface` is a **partial class** split across multiple HAL folders. The active HAL is selected at compile time via `.csproj` `<Compile Remove>` directives. Each HAL provides its own `CodeFileBLE.cs` and `ClassDeviceFinder.cs`.

**Current active HALs:**
- `Source/HAL/Plugin.BLE/` — primary; for Xamarin/MAUI
- `Source/HAL/TCPIP/` — CS203XL TCP socket

**Excluded HALs (legacy; must not be activated without review):**
- `Source/HAL/btframework/` — wclBluetooth wrapper (WinForms compat)
- `Source/HAL/UWP/` — UWP BLE
- `Source/HAL/WinFormsBLE/` — WinRT API (planned for .NET 10 Windows rewrite)
- `Source/HAL/Acr.ble/` — Acr.BLE
- `Source/HAL/MvvmCross.Plugin.BLE/` — MvvmCross BLE

### 2.3 Two Chipset Architectures

CSL readers use one of two firmware generations. All code paths must respect this distinction — they are **not interchangeable**.

| Chipset | Readers | Control Model | Protocol Path |
|---|---|---|---|
| **Rx000** | CS108, CS468, CS463, CS203X, CS468XJ | Direct MAC register read/write | `Comm_Protocol/RX000Commands/` |
| **E710/E910** | CS710S, CS203XL | High-level command/response | `Comm_Protocol/Ex10Commands/` |

The Unified API (`ClassRFID.UnifiedAPI.cs`) dispatches to the correct chipset path based on `_deviceType`.

### 2.4 ITransport Contract

Transport is abstracted behind `ITransport` (Source/Transport/ITransport.cs). `HighLevelInterface` holds `_transport` and calls `_transport.SendAsync()` regardless of BLE or TCP. Adding a new transport (e.g., USB serial) means:

1. Implement `ITransport`
2. Add `ConnectAsync` overload in `HighLevelInterface` that sets `_transport`
3. Add compile guards in `.csproj` if needed

---

## 3. .NET and C# Standards

### 3.1 Language and Compiler

- **C# version:** Latest stable supported by both `netstandard2.0` and `net10.0` targets (C# 12 as of .NET 10)
- **Nullable reference types:** Must be enabled (`<Nullable>enable</Nullable>`) for `net10.0`; use `#nullable disable` in mixed files only when strictly necessary and documented
- **Treat warnings as errors:** `<TreatWarningsAsErrors>false</TreatWarningsAsErrors>` — currently many `CS0649`/`CS0169` unused-field warnings exist; fix them rather than suppressing
- **Build:** Must pass `dotnet build -c Release` with **0 errors** on both TFMs before any PR is merged

### 3.2 Async / Await

- All I/O operations (BLE, TCP, file) **must** be `async Task` or `async Task<T>`
- Never use `.Result` or `.GetAwaiter().GetResult()` on library I/O paths
- `async void` is **only** permitted for event handlers
- All `async` methods must have an `Async` suffix

### 3.3 Naming Conventions

| Element | Convention | Example |
|---|---|---|
| Namespace | `CSLibrary.{SubNamespace}` | `CSLibrary.Constants`, `CSLibrary.Events` |
| Public class | **PascalCase** | `RFIDReader`, `DeviceInformation` |
| Internal class | PascalCase | `HighLevelInterface`, `BLETransport` |
| Public method | PascalCase | `StartDeviceSearch()`, `SendAsync()` |
| Private field | `_camelCase` | `_isConnected`, `_receiveCallback` |
| Internal field | `_camelCase` | `_deviceType` |
| Enum value | **PascalCase** | `MODEL.CS108`, `CallbackType.TAG_SEARCHING` |
| Constant | PascalCase | `MaxRetryCount` |
| Event | PascalCase | `OnStateChanged`, `OnAsyncCallback` |
| Interface | `I` + PascalCase | `ITransport`, `IAdapter` |

**Legacy naming tolerance:** Existing files may use `Class`-prefixed class names (e.g., `ClassRFID`, `ClassBarCode`). New classes **must not** use this prefix. Refactoring old names is encouraged but done separately from functional changes.

### 3.4 File Organization

- One public type per file; file name matches type name
- File location mirrors namespace hierarchy
- `partial class` files for `HighLevelInterface` are an exception — they share the root `Source/` folder by historical convention
- No `.bak` files may be committed to the repository
- No generated files (e.g., `AssemblyInfo.cs`) may be committed

### 3.5 Target Framework Management

- Both `netstandard2.0` and `net10.0` must build and pass all applicable tests
- Platform-specific code uses `#if NET10_0` / `#if NETSTANDARD2_0` / `#if TCP` (TCP is a custom define in `.csproj`)
- Do **not** use `#if NETCOREAPP`, `#if NETFX` or other framework-family guards — use the specific TFM symbol
- .NET 10 SDK is installed at `/home/node/.dotnet`; add to PATH as `export PATH="/home/node/.dotnet:$PATH"`

---

## 4. Dependencies

### 4.1 NuGet Packages

| Package | Version | Rationale |
|---|---|---|
| `Plugin.BLE` | `3.0.0` | BLE abstraction for MAUI/Xamarin |

- No additional first-party dependencies may be added without architectural review
- Transitive dependencies must not pull in platform-specific (iOS/Android-only) packages into the netstandard2.0 TFM
- Update `Plugin.BLE` only after smoke-testing against a real reader device

### 4.2 Internal Dependencies

- `Source/Tools/` utilities (`ClassCRC16`, `ClassFIFOQueue`, `HexEncoding`, `CSLTCPSTREAM`) are internal and must not expose public APIs beyond what the HAL and BluetoothProtocol layers need

---

## 5. API Design Rules

### 5.1 Public Surface

The **only** public entry points are:

- `CSLibrary.RFIDReader`
- `CSLibrary.BarcodeReader`
- `CSLibrary.Battery`
- `CSLibrary.DeviceFinder` (static)
- `CSLibrary.Notification`
- `CSLibrary.Debug` (static utility)

All other namespaces (`HAL.*`, `BluetoothProtocol`, `Comm_Protocol`, `Tools`) are `internal`.

### 5.2 Events and Callbacks

- Use standard C# `event EventHandler<T>` pattern
- Event args structs/classes go in `Source/{Component}/Events/`
- Never use raw delegate fields as public API
- All event callbacks must be `virtual` or interface-implemented if a subscriber needs to unsubscribe cleanly

### 5.3 Result Codes and Errors

- Use `Result` enum values from `CSLibrary.Constants.Result` for operation outcomes
- `CONNECTION_LOST` and `COMMUNICATION_ERROR` must be raised as `ReaderCallbackType` events
- Never throw exceptions from async event handlers

### 5.4 MODEL Enum

The `MODEL` enum is the single source of truth for hardware variant detection. Adding a new model:

1. Add to `MODEL` enum in `CSLibrary.Constants.cs`
2. Add BLE service UUID mapping in `BLETransport.ConnectAsync()`
3. Add protocol dispatch in `ClassRFID.UnifiedAPI.cs`
4. Add device-finder detection in `DeviceFinder.DetectDeviceModel()`

---

## 6. Testing Strategy

### 6.1 Current State

**There are no tests.** This is a critical gap. The constitution requires:

- A unit test project targeting `net10.0` must be added in `CSLibrary2026.Tests/`
- Tests must cover: transport abstraction, command encoding/decoding, MODEL dispatch, event firing, frequency band lookup

### 6.2 Test Conventions

- Framework: **xUnit** or **MSTest** (decide before writing first test)
- No integration tests that require physical hardware in the CI pipeline
- Integration tests (real BLE/TCP with a reader) must be opt-in, tagged `[Trait("integration", "true")]`, and disabled in CI
- Code coverage target: **80%** for `Transport/` and `BluetoothProtocol/` layers
- Test naming: `{MethodName}_{Scenario}_{ExpectedResult}`

### 6.3 CI / Build Gate

```
dotnet build -c Release  → 0 errors
dotnet test -c Release   → all pass (excluding integration tests in CI)
```

---

## 7. HAL Extensibility Rules

Any new HAL backend (e.g., WinRT for WinForms) must:

1. Live in `Source/HAL/{BackendName}/`
2. Provide `CodeFileBLE.cs` (partial to `HighLevelInterface`) and `ClassDeviceFinder.cs`
3. Be added to `.csproj` `<Compile Remove>` exceptions for the target platform TFM
4. Implement the same `ITransport`-based connection model
5. Include a `README.md` inside the folder explaining the platform requirements and how to activate it
6. Be reviewed separately before activation — do not merge new HAL backends without a functional review

---

## 8. Security and Bluetooth Safety

### 8.1 No Sensitive Data in Logs

- `Debug.WriteBytes()` is only active in `DEBUG` builds
- Never log MAC addresses, device names, or RF data in production
- Connection metadata (MAC, IP:port) may only appear in debug traces

### 8.2 BLE Pairing

- No shared secrets stored in the library
- The library does not implement custom pairing — it relies on OS-level BLE pairing before `ConnectAsync()` is called

### 8.3 Firmware and Command Validation

- All inbound byte arrays from the reader must be length-checked before indexing
- No command bytes are sent to the reader without proper destination ID (`destinationsID[]`) framing
- The library must never be the only line of defense for physical security — consumers must implement their own access control

---

## 9. Performance Requirements

- **No blocking I/O on the main thread** — all BLE and TCP operations are async
- **BLE MTU:** Request 255 bytes for CS710S/CS203XL connections
- **Reconnect:** Connection-loss must fire `CONNECTION_LOST` event; `Disconnect()` must clean up all BLE subscriptions
- **FIFO queue** (`ClassFIFOQueue`) must be used for all inbound protocol frames — never process directly from BLE characteristic callbacks in parallel paths
- **Memory:** No unbounded `List<T>` growth; inbound buffers are fixed-size

---

## 10. Documentation Standards

### 10.1 Inline XML Docs

Every **public** type and member must have `/// <summary>` XML documentation. Fields may use `//` comments.

### 10.2 Required Documents

| Document | Location | Purpose |
|---|---|---|
| `constitution.md` | `Documents/` | This file — governance |
| `README.md` | repo root | Installation, quick start, supported readers |
| `CHANGELOG.md` | repo root | Per-release breaking changes and additions |
| `API.md` | `Documents/` | (Future) Full public API reference |

### 10.3 API Stability

- The public API surface is **semantically versioned** — MAJOR.MINOR.PATCH per NuGet conventions
- `netstandard2.0` and `net10.0` share the same API contract — no platform-specific public methods
- Breaking changes to the public API require a MAJOR version bump and a migration guide

---

## 11. Git Workflow

### 11.1 Branching

| Branch | Purpose |
|---|---|
| `main` | Stable, production-ready |
| `develop` | Active development (PR target) |

### 11.2 Commit Messages

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
feat(ble): add CS468 support to DeviceFinder
fix(transport): handle null receive callback on disconnect
docs(constitution): add HAL extensibility rules
```

### 11.3 PR Requirements

- PRs must pass the build gate (0 errors on both TFMs)
- PRs touching public API must update relevant documentation
- New HAL backends require a separate approved PR with a functional review note
- No direct pushes to `main` — all changes via PR

### 11.4 Daily Sync Cron

A daily cron job (`30 18 * * *` UTC) commits and pushes `develop` with any local changes. Do not let the repo drift more than 24 hours unsynced.

---

## 12. Pain Points and Technical Debt

The following issues are **acknowledged** and must be addressed before they block future work:

| # | Issue | Severity | Status |
|---|---|---|---|
| 1 | 22 unused private fields causing `CS0649`/`CS0169` warnings | Medium | Open — fix in dedicated cleanup PR |
| 2 | No unit test project exists | High | Open — create `CSLibrary2026.Tests/` |
| 3 | `Class`-prefixed public class names violate modern conventions | Low | Open — refactor as separate cleanup PRs |
| 4 | No CI/CD GitHub Actions configured | High | Open — add workflow for build + NuGet publish |
| 5 | No CHANGELOG.md | Medium | Open — create and maintain per-release |
| 6 | `.bak` files present in source tree | Low | Open — remove in dedicated cleanup PR |
| 7 | `net10.0` is cutting-edge (.NET 10 not yet GA as of 2026-04) | Medium | Monitor — may need to retarget to `net9.0` or `net8.0` |
| 8 | `netstandard2.0` nullable references not fully enforced | Low | Open — address as fields are cleaned up |

---

## 13. Change Governance

This constitution may be amended only by:

1. A dedicated PR with `docs(constitution):` prefix
2. At least one approving review from a maintainer
3. The PR updates this file in place (no side-channel constitution changes)

When amending, add a changelog entry at the top of this section:

```markdown
## Changelog

| Date | Change | Author |
|---|---|---|
| 2026-04-05 | Initial constitution | GithubProject-CSLibrary2026 |
```

---

*Last updated: 2026-04-05*
