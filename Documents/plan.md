# CSLibrary2026 — Technical Implementation Plan

> **Governs:** All implementation and refactoring work
> **Constitution:** [`Documents/constitution.md`](constitution.md) — this plan must comply with it
> **Spec:** [`Documents/specs/spec.md`](specs/spec.md) — this plan implements it
> **Version:** 0.1.0 · **Date:** 2026-04-05

---

## Table of Contents

1. [Principles](#1-principles)
2. [Phase Overview](#2-phase-overview)
3. [Phase 0 — Foundation: CI/CD and Test Infrastructure](#3-phase-0--foundation-cicd-and-test-infrastructure)
4. [Phase 1 — Quality Gates: Warnings, Nullable, .bak, CHANGELOG](#4-phase-1--quality-gates-warnings-nullable-bak-changelog)
5. [Phase 2 — Documentation: XML Docs, README, API.md](#5-phase-2--documentation-xml-docs-readme-apimd)
6. [Phase 3 — Class Naming Refactor (Optional / Long-Horizon)](#6-phase-3--class-naming-refactor-optional--long-horizon)
7. [Phase 4 — WinForms BLE HAL (Windows.Devices.Bluetooth)](#7-phase-4--winforms-ble-hal-windowsdevicesbluetooth)
8. [Phase 5 — New Reader Model Checklist](#8-phase-5--new-reader-model-checklist)
9. [Task Board: All Items Tracked](#9-task-board-all-items-tracked)
10. [Definition of Done per Phase](#10-definition-of-done-per-phase)
11. [Risk Register](#11-risk-register)

---

## 1. Principles

Every task in this plan obeys the following rules from the constitution:

| Rule | Source |
|---|---|
| Build must pass `dotnet build -c Release` with **0 errors** on both TFMs before any PR is merged | constitution §3.1 |
| All I/O operations are `async Task` with `Async` suffix; no `.Result` | constitution §3.2 |
| No `.bak` files in the repository | constitution §3.4 |
| Public API surface is semantically versioned; breaking changes require MAJOR bump + migration guide | constitution §10.3 |
| Unit tests use xUnit, cover Transport + BluetoothProtocol at 80%, integration tests tagged and excluded from CI | constitution §6.2 |
| New HAL backends require their own folder, `README.md`, and separate reviewed PR | constitution §7 |
| Conventional Commits for all commit messages | constitution §11.2 |
| Daily `develop` sync via cron at `30 18 * * *` UTC | constitution §11.4 |

---

## 2. Phase Overview

| Phase | Name | Priority | Effort | Blocks |
|---|---|---|---|---|
| **0** | CI/CD + Test Infrastructure | 🔴 High | 2–3 days | All later phases |
| **1** | Quality Gates (warnings, nullable, .bak, CHANGELOG) | 🔴 High | 2–3 days | All later phases |
| **2** | Documentation (XML docs, README, API.md) | 🟡 Medium | 1–2 days | Release readiness |
| **3** | Class Naming Refactor | 🟢 Low | Ongoing | None (cosmetic only) |
| **4** | WinForms BLE HAL (WinRT) | 🟡 Medium | 1–2 weeks | WinForms consumers |
| **5** | New Reader Model Checklist | 🟡 Medium | Per model | Per model |

**Start with Phase 0.** The CI gate and test infrastructure are prerequisites for confidence in all subsequent work.

---

## 3. Phase 0 — Foundation: CI/CD and Test Infrastructure

**Goal:** Every PR runs `dotnet build -c Release` + `dotnet test -c Release` automatically.
**Constitution reference:** §6 (Testing Strategy), §11.3 (PR Requirements), §12 (TD-2, TD-4)

---

### 3.1 GitHub Actions CI Pipeline

**File:** `.github/workflows/ci.yml`

```yaml
name: CI

on:
  push:
    branches: [main, develop]
  pull_request:
    branches: [main, develop]

env:
  DOTNET_NOLOGO: 1
  DOTNET_CLI_TELEMETRY_OPTOUT: 1

jobs:
  build:
    name: Build (${{ matrix.target }})
    runs-on: ubuntu-latest
    strategy:
      fail-fast: false
      matrix:
        target: [netstandard2.0, net10.0]

    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 10
        if: matrix.target == 'net10.0'
        run: |
          wget -q https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
          chmod +x dotnet-install.sh
          ./dotnet-install.sh --channel 10.0 --install-dir $HOME/.dotnet
          echo "$HOME/.dotnet" >> $GITHUB_PATH

      - name: Setup .NET (netstandard2.0 uses SDK from net10 install)
        run: echo "$HOME/.dotnet" >> $GITHUB_PATH

      - name: Restore
        run: dotnet restore

      - name: Build
        run: dotnet build -c Release --framework ${{ matrix.target }}

      - name: Pack
        run: dotnet pack -c Release -o ./nupkg --no-build

  test:
    name: Test
    runs-on: ubuntu-latest
    # Hardware-integration tests are skipped in CI (see constitution §6.2)
    # They run manually with [Trait("integration", "true")]
    steps:
      - name: Checkout
        uses: actions/checkout@v4

      - name: Setup .NET 10
        run: |
          wget -q https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
          chmod +x dotnet-install.sh
          ./dotnet-install.sh --channel 10.0 --install-dir $HOME/.dotnet
          echo "$HOME/.dotnet" >> $GITHUB_PATH

      - name: Restore tests
        run: dotnet restore CSLibrary2026.Tests/CSLibrary2026.Tests.csproj

      - name: Build tests
        run: dotnet build -c Release CSLibrary2026.Tests/CSLibrary2026.Tests.csproj

      - name: Run tests
        run: dotnet test -c Release CSLibrary2026.Tests/CSLibrary2026.Tests.csproj --filter "Category!=integration" --logger "trx;LogFileName=results.trx"
```

**Implementation steps:**

1. Create `.github/workflows/ci.yml` at repo root
2. Add `CSLibrary2026.Tests/` to `.gitignore`
3. Commit with `chore(ci): add GitHub Actions workflow for build and test`
4. Verify the workflow runs on the PR

---

### 3.2 Test Project Setup

**Directory:** `CSLibrary2026.Tests/`
**Framework:** xUnit (per constitution §6.2)
**Target:** `net10.0` (test project does not need to target netstandard2.0)

**Creation commands:**
```bash
cd CSLibrary2026
dotnet new xunit -n CSLibrary2026.Tests -o Tests/CSLibrary2026.Tests --framework net10.0
cd Tests/CSLibrary2026.Tests
dotnet add reference ../../CSLibrary2026.csproj
```

**Initial test project structure:**
```
CSLibrary2026.Tests/
├── CSLibrary2026.Tests.csproj
├── Transport/
│   ├── ITransportContractTests.cs     # verifies BLETransport and TCPTransport implement ITransport
│   ├── BLETransport_ConnectAsyncTests.cs
│   ├── TCPTransport_ConnectAsyncTests.cs
│   └── Transport_SendAsync_Tests.cs
├── BluetoothProtocol/
│   ├── BTSend_FramingTests.cs          # verifies destination ID framing, padding
│   ├── BTSend_DestinationIDTests.cs
│   └── BTReceive_DecodeTests.cs        # verifies uplink packet parsing
├── DeviceFinder/
│   ├── DeviceFinder_ModelDetectionTests.cs  # MODEL dispatch via mock IDevice
│   └── DeviceFinder_StartStopTests.cs
├── ModelDispatch/
│   └── UnifiedAPI_DispatchTests.cs     # verifies correct chipset path per MODEL
└── FrequencyBand/
    └── FrequencyBandLookupTests.cs
```

**Mock strategy:** Use in-memory mock implementations of `IAdapter`, `IDevice`, `IService`, `ICharacteristic` from Plugin.BLE. Create a `TestMocks/` folder with fake BLE objects that return controlled byte arrays.

**Test naming:** `{MethodName}_{Scenario}_{ExpectedResult}` (constitution §6.2)

**Example test:**
```csharp
[Fact]
public void BTSend_Framing_DestinationID_CorrectByteUsed()
{
    // Arrange
    var expectedDestId = (byte)DEVICEID.RFID; // 0xc2

    // Act — verify first byte of any framed packet is the RFID destination ID
    var framed = BTSend.FrameCommand(new byte[] { 0x80, 0x02 });
    
    // Assert
    Assert.Equal(expectedDestId, framed[0]);
}
```

**Coverage target:** 80% of `Transport/` + `BluetoothProtocol/` layers.
**Coverage tool:** `dotnet test /p:CollectCoverage=true /p:Threshold=80`

**Integration test convention (constitution §6.2):**
```csharp
[Trait("integration", "true")]
public class BLE_RealReader_IntegrationTests
{
    // Requires physical hardware — skipped in CI
    // Run manually: dotnet test --filter "Category=integration"
}
```

---

## 4. Phase 1 — Quality Gates: Warnings, Nullable, .bak, CHANGELOG

**Goal:** Reduce compiler warnings from 472 to < 20 and complete the CHANGELOG.
**Constitution reference:** §3.1 (warnings), §3.4 (.bak), §12 (TD-1, TD-5, TD-6, TD-7, TD-8)
**Spec reference:** Success Metrics (warnings: 472 → <20)

---

### 4.1 Remove .bak Files

**Constitution:** §3.4 — "No `.bak` files may be committed to the repository"
**Severity:** Low (TD-6)

**Command:**
```bash
find CSLibrary2026 -name "*.bak" -type f
# Review output before deleting
find CSLibrary2026 -name "*.bak" -type f -delete
```

**PR:** One commit: `chore(cleanup): remove .bak files from source tree`

---

### 4.2 Fix Compiler Warnings (472 → <20)

**Constitution:** §3.1 — "fix them rather than suppressing"
**Severity:** Medium (TD-1)

The 472 warnings are concentrated in a small number of files. The strategy is to fix the root cause in each file, not suppress individual warnings.

**Warning categories and fix strategy:**

| Category | Count (approx) | Fix |
|---|---|---|
| `CS0649` — field never assigned | ~10 fields | Either assign or remove the field |
| `CS0169` — field never used | ~10 fields | Remove the unused private field |
| `CS0414` — assigned but value never used | ~16 fields | Remove assignment or the variable |
| **Total blocking** | **~36** | These are the only ones that block the <20 target |

**Step-by-step:**

1. Run `dotnet build -c Release 2>&1 | grep "warning CS"` to get the full per-line list
2. Group warnings by file
3. For each file, open and assess each flagged field:
   - **Truly dead code** → delete the field
   - **Field is used but warning fires due to partial class split** → add `Debug.WriteLine` placeholder or `#pragma warning disable` with inline comment explaining why
   - **Field assigned but value read in a different partial class** → this should not happen in the current design; investigate

**Files to audit first** (highest warning count):
```
Source/Tools/ClassSystemParameters.cs      (~6 CS0649)
Source/SystemInformation/ClassFrequencyBandInformation.cs  (~1 CS0169)
Source/HAL/TCPIP/Class_CSLibrary.CS203XL.NetFinder.cs    (~3 CS0414)
Source/RFIDReader/CSLUnifiedAPI/Basic_Structures/CSLibrary.Structures.cs  (~6 CS0169)
Source/RFIDReader/CSLUnifiedAPI/Basic_API/ClassRFID.Private.cs           (~1 CS0414)
Source/HAL/Plugin.BLE/CodeFileBLE.cs        (~2 CS0169/CS0414)
Source/BarcodeReader/ClassBarCode.cs        (~4 CS0414)
Source/Notification/ClassNotification.cs    (~2 CS0414)
Source/RFIDReader/CSLUnifiedAPI/TAG_ASYN/ClassRFID.ASYN.cs  (~1 CS0414)
Source/RFIDReader/CSLUnifiedAPI/Basic_API/ClassRFID.Public.FrequencyChannel.cs (~1 CS0414)
Source/RFIDReader/Comm_Protocol/Ex10Commands/ClassRFID.WriteRegister.cs     (~1 CS0414)
```

**PR strategy:** One warning-fix PR per logical group (e.g., `fix(warnings): resolve unused fields in ClassSystemParameters and ClassFrequencyBandInformation`). Keep PRs reviewable — max 5–6 files per PR.

---

### 4.3 Enable Nullable Reference Types on netstandard2.0

**Constitution:** §12 TD-8 — "address as fields are cleaned up"
**Severity:** Low

Currently nullable is enabled for `net10.0` but not `netstandard2.0`. The `.csproj` has no `<Nullable>enable</Nullable>` in the shared PropertyGroup.

**Implementation:**
```xml
<!-- In CSLibrary2026.csproj, shared PropertyGroup -->
<Nullable>enable</Nullable>
```

After enabling, rebuild both TFMs. Expect new nullable warnings to appear (CS8600, CS8602, CS8603, etc.). Fix them file by file using the same grouping strategy as warning fixes.

**Rule:** Use `#nullable restore` at the top of files that cannot yet be nullable-clean (e.g., files with extensive byte array handling) rather than disabling for the whole file.

---

### 4.4 Create and Maintain CHANGELOG.md

**Constitution:** §10.2 Required Documents + §12 TD-5
**Severity:** Medium
**Spec:** Success Metrics (CHANGELOG: not maintained → updated per release)

**File location:** `CSLibrary2026/CHANGELOG.md`

**Format:** [Keep a Changelog](https://keepachangelog.com/) style

```markdown
# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
### Changed
### Fixed
### Removed
### Security

---

## [1.0.0-beta.2] — YYYY-MM-DD

### Added
- `Documents/constitution.md` — project governance document
- `Documents/specs/spec.md` — project specification
- `Documents/plan.md` — implementation roadmap
- `Source/Transport/ITransport.cs` — transport abstraction interface
- `Source/Transport/BLETransport.cs` — Plugin.BLE transport implementation
- `Source/Transport/TCPTransport.cs` — TCP/IP transport implementation
- `Source/HAL/Plugin.BLE/DeviceFinder.cs` — extracted BLE device discovery (replaces inline CodeFileBLE)

### Fixed
- `MODEL.CS468` missing from BLE service UUID switch in `BLETransport` (was falling through = couldn't connect)
- `.csproj` Readme file mismatch: `Readme.md` → `README.md`

### Changed
- Refactored `HighLevelInterface` connection to use `ITransport` abstraction — BLE and TCP are now swappable without API changes

---

## [1.0.0-beta.1] — 2026-04-01
### Added
- Initial NuGet package publication
- Support for CS108, CS468, CS710S, CS203XL
- Plugin.BLE 3.0.0 BLE support
- TCP/IP support for CS203XL
```

**Rule:** Every PR merged to `develop` that has a user-visible change MUST add an entry to the `[Unreleased]` section of CHANGELOG.md before merge. This is enforced in the CI pipeline (add a simple regex check step in CI).

---

### 4.5 Monitor net10.0 Stability

**Constitution:** §12 TD-7 — "Monitor — may need to retarget to `net9.0` or `net8.0`"
**Severity:** Medium

**Action items:**
1. After .NET 10 SDK officially releases, update the CI matrix to test against the stable channel
2. If .NET 10 has a release candidate or goes GA before the project publishes stable, update `.csproj` from `net10.0` to the correct version (e.g., `net10.0-windows` for WinForms support)
3. Watch [dotnet/core](https://github.com/dotnet/core) for .NET 10 release announcements
4. Add a note to `TOOLS.md` and `MEMORY.md` to check .NET 10 status quarterly

---

## 5. Phase 2 — Documentation: XML Docs, README, API.md

**Goal:** 100% XML docs on public API; complete README; API.md reference.
**Constitution reference:** §10 (Documentation Standards), §10.1 (XML Docs), §10.2 (Required Documents)
**Spec reference:** NFR-M02, Success Metrics (XML docs: partial → 100%)

---

### 5.1 Audit and Complete XML Documentation

**Scope:** All public types and members in these namespaces:
- `CSLibrary` (root — `RFIDReader`, `BarcodeReader`, `Battery`, `DeviceFinder`, `Notification`, `Debug`)
- `CSLibrary.Constants`
- `CSLibrary.Events`
- `CSLibrary.Structures`

**Approach:**
1. Run a doc generation tool (DocFX or `msbuild /t:GenerateDoc`) to identify undocumented public members
2. For each undocumented member, add:
```csharp
/// <summary>
/// [One clear sentence describing what this does, in present tense.]
/// </summary>
/// <param name="paramName">[Description of param, including units if applicable]</param>
/// <returns>[Description of return value, or void]</returns>
/// <exception cref="InvalidOperationException">Thrown when [condition]</exception>
```
3. Internal members may use `//` line comments

**Priority order:**
1. `RFIDReader` class and all its public methods
2. `DeviceFinder` static class and all public members
3. `BarcodeReader` class
4. `Battery` class
5. Constants and structures (event args first, then enums)

---

### 5.2 Expand README.md

**Current state:** Minimal (one line `# CSLibrary2026`)
**Target:** Complete installation, quick-start, supported readers table

**Required sections:**
```markdown
# CSLibrary2026

[![NuGet](https://img.shields.io/nuget/v/CSLibrary2026.svg)](https://www.nuget.org/packages/CSLibrary2026/)

CSL RFID Reader Library for .NET — Supports BLE and TCP/IP communication with CSL RFID readers.

## Supported Readers

| Model | Transport | Chipset |
|---|---|---|
| CS108 | Bluetooth LE | Rx000 |
| CS468 | Bluetooth LE | Rx000 |
| CS710S | Bluetooth LE | E710 |
| CS203XL | Bluetooth LE + TCP/IP | E910 |

## Installation

dotnet add package CSLibrary2026

## Quick Start

[2–3 code snippets: discovery, connect, inventory]

## Documentation

- [API Reference](Documents/API.md) (future)
- [Constitution](Documents/constitution.md)
- [Specification](Documents/specs/spec.md)
- [Plan](Documents/plan.md)

## Requirements

- netstandard2.0 or .NET 10
- Plugin.BLE 3.0.0 (included via NuGet)

## License

MIT — see LICENSE
```

---

### 5.3 Create API.md

**Location:** `CSLibrary2026/Documents/API.md`
**Purpose:** Full public API reference, auto-generated ideally

**If auto-generating:** Use DocFX or Sandcastle. If those are too heavy, manually write the API surface (it is finite — ~30 public types).

**Manual approach (fallback):** List all public entry points from constitution §5.1:
- `CSLibrary.RFIDReader`
- `CSLibrary.BarcodeReader`
- `CSLibrary.Battery`
- `CSLibrary.DeviceFinder`
- `CSLibrary.Notification`
- `CSLibrary.Debug`

For each, list the key public methods, events, and properties with one-line descriptions.

---

## 6. Phase 3 — Class Naming Refactor (Optional / Long-Horizon)

**Goal:** Remove `Class`-prefix from legacy class names.
**Constitution reference:** §3.3 — "Legacy naming tolerance: Refactoring old names is encouraged but done separately from functional changes."
**Severity:** Low (TD-3) — does not block anything

**Strategy:** This is a breaking API change because some of these classes are public:
- `ClassRFID` → `RFIDOperations` (public, in `CSLibrary.RFIDReader`)
- `ClassBarCode` → `BarcodeScanner` (public, in `CSLibrary.BarcodeReader`)
- `ClassBattery` → `BatteryMonitor` (public, in `CSLibrary.Battery`)

**Approach:**
1. Add new names as proper aliases or new classes that delegate to the old ones (no breaking change yet)
2. Mark old names `[Obsolete]` with a message pointing to the new name — this gives consumers a migration path
3. In the next major version (2.0.0), remove the old names

**PR format:** One class rename per PR. Commit: `refactor(naming): deprecate ClassRFID in favour of RFIDOperations`

---

## 7. Phase 4 — WinForms BLE HAL (Windows.Devices.Bluetooth)

**Goal:** Enable `Source/HAL/WinFormsBLE/` for `net10.0-windows` using `Windows.Devices.Bluetooth` APIs.
**Constitution reference:** §7 (HAL Extensibility Rules)
**Spec reference:** §13.3 (Future Extensibility — Windows.Devices.Bluetooth for WinForms)
**Severity:** Medium — unblocks WinForms desktop consumers

---

### 7.1 Architecture for the WinForms BLE HAL

The WinFormsBLE HAL already exists at `Source/HAL/WinFormsBLE/` but is excluded from the build. It must be rewritten to:

1. Implement `ITransport` using `Windows.Devices.Bluetooth.Advertisement.BluetoothLEAdvertisementWatcher` for scanning and `BluetoothDevice.FromBluetoothAddressAsync()` for connection
2. Add `Source/HAL/WinFormsBLE/WinRTTransport.cs` implementing `ITransport`
3. Add `#if NET10_0_OR_GREATER && WINDOWS` guards in the HAL partial class

### 7.2 Implementation Checklist (per constitution §7)

- [ ] `Source/HAL/WinFormsBLE/README.md` — explain platform requirements (Windows 10 1809+, `<UseWindowsForms>true</UseWindowsForms>` in the consumer's `.csproj`)
- [ ] `Source/HAL/WinFormsBLE/WinRTTransport.cs` — implement `ITransport`:
  - `ConnectAsync(object[] args)` — `Windows.Devices.Bluetooth.BluetoothDevice.FromBluetoothAddressAsync()`
  - `SendAsync(byte[] data)` — `BluetoothCharacteristic.WriteValueAsync()`
  - `Disconnect()` — close GATT session
  - `SetReceiveCallback(Action<byte[]> callback)` — subscribe to `Characteristic.ValueChanged`
  - `IsConnected` / `ConnectionInfo`
- [ ] Update `.csproj` to include WinFormsBLE for `net10.0-windows`:
```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0-windows'">
  <Compile Remove="Source/HAL/WinFormsBLE/**" />
  <!-- ^ Remove the exclusion — but only for net10.0-windows, not netstandard2.0 -->
</PropertyGroup>
```
- [ ] Add `Windows.Devices.Bluetooth` and `Windows.Devices.Bluetooth.Advertisement` as WinRT references in the consumer's `net10.0-windows` TFM
- [ ] Functional review PR with manual test notes

### 7.3 README Template for HAL Folders

Per constitution §7 item 5, every new HAL folder needs a `README.md`:

```markdown
# WinFormsBLE HAL — Windows.Devices.Bluetooth Backend

## Platform Requirements

- Windows 10 version 1809 (Build 17763) or later
- .NET 10.0 targeting `net10.0-windows`
- `<UseWindowsForms>true</UseWindowsForms>` in the consuming application's `.csproj`

## Activation

This HAL is activated automatically when building for `net10.0-windows`.

## BLE Service UUIDs

Same UUIDs as Plugin.BLE HAL:
- CS108/CS468: `00009800-0000-1000-8000-00805f9b34fb`
- CS710S/CS203XL: `00009802-0000-1000-8000-00805f9b34fb`

## Architecture Notes

- Uses `BluetoothLEAdvertisementWatcher` for scanning
- Uses `BluetoothDevice.FromBluetoothAddressAsync()` for connection
- GATT characteristic discovery mirrors `BLETransport`
```

---

## 8. Phase 5 — New Reader Model Checklist

**Goal:** Document and automate the steps for adding a new CSL reader model.
**Constitution reference:** §5.4 (MODEL Enum), spec §13.1
**Trigger:** When CSL releases a new reader (e.g., CS469)

### 8.1 New Model Checklist

For each new reader, the following files must be updated:

| # | File | Change |
|---|---|---|
| 1 | `Source/RFIDReader/CSLUnifiedAPI/Basic_Constants/CSLibrary.Constants.cs` | Add `MODEL.CSXXX` to the `MODEL` enum |
| 2 | `Source/Transport/BLETransport.cs` | Add `case MODEL.CSXXX:` to the service UUID switch in `ConnectAsync` |
| 3 | `Source/HAL/Plugin.BLE/DeviceFinder.cs` | Add UUID or OUI prefix detection in `DetectDeviceModel()` |
| 4 | `Source/RFIDReader/CSLUnifiedAPI/Basic_API/ClassRFID.UnifiedAPI.cs` | Verify chipset path (Rx000 or E710/E910); add dispatch if needed |
| 5 | `README.md` | Add new model to supported readers table |
| 6 | `CHANGELOG.md` | Add `[Unreleased]` entry under `Added` |
| 7 | `spec.md` | Add new model to reader compatibility matrix (§7.3) |

### 8.2 Chipset Path Determination

If the new model uses:
- Rx000 chipset → verify `Comm_Protocol/RX000Commands/` covers all needed MAC registers
- E710/E910 chipset → verify `Comm_Protocol/Ex10Commands/` covers all needed commands

If neither exists, a new `Comm_Protocol/{ChipsetName}Commands/` folder is required — this is a major addition and needs its own spec and review.

---

## 9. Task Board: All Items Tracked

| # | Task | Phase | Priority | Effort | Status | PR |
|---|---|---|---|---|---|---|
| T-01 | Create `.github/workflows/ci.yml` | 0 | 🔴 High | 1h | Pending | — |
| T-02 | Create `CSLibrary2026.Tests/` with xUnit | 0 | 🔴 High | 1d | Pending | — |
| T-03 | Write `ITransport` contract tests | 0 | 🔴 High | 2h | Pending | — |
| T-04 | Write `BTSend` framing tests | 0 | 🔴 High | 2h | Pending | — |
| T-05 | Write `DeviceFinder` model detection tests | 0 | 🔴 High | 2h | Pending | — |
| T-06 | Write `UnifiedAPI` MODEL dispatch tests | 0 | 🔴 High | 2h | Pending | — |
| T-07 | Add CI coverage gate (`Threshold=80`) | 0 | 🔴 High | 1h | Pending | — |
| T-08 | Remove `.bak` files | 1 | 🔴 High | 30m | Pending | — |
| T-09 | Fix unused-field warnings (CS0649/CS0169/CS0414) | 1 | 🔴 High | 2d | Pending | — |
| T-10 | Enable nullable reference types on netstandard2.0 | 1 | 🟡 Medium | 1d | Pending | — |
| T-11 | Create `CHANGELOG.md` with v1.0.0-beta.1 entries | 1 | 🔴 High | 2h | Pending | — |
| T-12 | Add CHANGELOG regex check to CI | 1 | 🔴 High | 1h | Pending | — |
| T-13 | Monitor .NET 10 GA status | 1 | 🟡 Medium | recurring | Pending | — |
| T-14 | Audit and complete XML documentation | 2 | 🟡 Medium | 2d | Pending | — |
| T-15 | Expand `README.md` | 2 | 🟡 Medium | 2h | Pending | — |
| T-16 | Create `Documents/API.md` | 2 | 🟡 Medium | 1d | Pending | — |
| T-17 | Class naming refactor (ClassRFID → RFIDOperations, etc.) | 3 | 🟢 Low | ongoing | Future | — |
| T-18 | WinFormsBLE HAL: WinRTTransport implementation | 4 | 🟡 Medium | 1–2w | Future | — |
| T-19 | WinFormsBLE HAL: README + .csproj activation | 4 | 🟡 Medium | 1h | Future | — |
| T-20 | New reader model checklist (per model) | 5 | 🟡 Medium | per model | Future | — |
| T-21 | Publish NuGet beta.2 to nuget.org | All | 🔴 High | 1h | Pending | — |

---

## 10. Definition of Done per Phase

### Phase 0 — Done when:
- [ ] `.github/workflows/ci.yml` exists and passes on a test PR
- [ ] `CSLibrary2026.Tests/` builds and `dotnet test` runs (even if 0 tests at first)
- [ ] CI runs `dotnet build -c Release` on both `netstandard2.0` and `net10.0`
- [ ] Test project references the main project

### Phase 1 — Done when:
- [ ] `find . -name "*.bak" -type f` returns empty
- [ ] `dotnet build -c Release 2>&1 | grep -c "warning"` returns < 20
- [ ] `CHANGELOG.md` exists and has entries for all releases including `[Unreleased]`
- [ ] Nullable is enabled for both TFMs and the build passes with < 20 warnings

### Phase 2 — Done when:
- [ ] Every public type in `CSLibrary.RFIDReader`, `CSLibrary.BarcodeReader`, `CSLibrary.Battery`, `CSLibrary.DeviceFinder` has `/// <summary>`
- [ ] `README.md` has supported readers table, installation snippet, quick-start code
- [ ] `Documents/API.md` lists all 6 public entry points with their key members

### Phase 4 — Done when:
- [ ] `Source/HAL/WinFormsBLE/WinRTTransport.cs` exists and implements `ITransport`
- [ ] `Source/HAL/WinFormsBLE/README.md` documents platform requirements
- [ ] `.csproj` is updated to include WinFormsBLE for `net10.0-windows`
- [ ] Functional review PR is approved

---

## 11. Risk Register

| Risk | Likelihood | Impact | Mitigation |
|---|---|---|---|
| .NET 10 SDK delays or changes break `net10.0` build | Medium | Medium | Monitor release; retarget to `net9.0` if needed; CI matrix tests both |
| Plugin.BLE 3.0.0 has a breaking change in a future update | Low | High | Pin to 3.0.0 in `.csproj`; review breaking changes before updating |
| Physical hardware required for integration tests cannot be automated in CI | High | Low | Constitution §6.2: integration tests opt-in and tagged; CI skips them |
| Adding WinFormsBLE `Windows.Devices.Bluetooth` adds WinRT dependency to consumer's app | Medium | Medium | Document `<UseWindowsForms>true</UseWindowsForms>` requirement clearly in README |
| Adding nullable to netstandard2.0 creates many new warnings | Medium | Low | Enable gradually; use `#nullable restore` where needed; fix in batches |
| CSL firmware team changes BLE UUIDs in a future reader | Low | High | `DetectDeviceModel()` and `BLETransport` are isolated; update per new model checklist |
| xUnit tests cannot mock Plugin.BLE interfaces cleanly | Medium | Low | Extract interface wrappers (`IBLEAdapter`, `IBLEDevice`) in Tools/ for testability |

---

## Appendix A — Document History

| Version | Date | Author | Change |
|---|---|---|---|
| 0.1.0 | 2026-04-05 | GithubProject-CSLibrary2026 | Initial technical plan |

## Appendix B — File Map (This Plan and Its Governing Docs)

```
CSLibrary2026/
├── Documents/
│   ├── constitution.md          ← Governing document (supreme authority)
│   ├── spec.md                 ← What to build (scope, requirements)
│   ├── plan.md                 ← THIS FILE — How to build it (implementation)
│   └── specs/
│       └── spec.md             ← Alias for spec.md (same content)
├── CHANGELOG.md                ← Maintained per release (constitution §10.2)
├── README.md                   ← Installation + quick start (constitution §10.2)
└── Source/
    └── ...                     ← The library itself
```

---

*End of technical plan. Changes to this plan must follow the same governance as the constitution (constitution §13).*
