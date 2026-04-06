# CSLibrary2026 — Project Specification

> **Status:** Draft · **Version:** 0.1.0 · **Date:** 2026-04-05
> **Governing document:** [`CSLibrary2026/Documents/constitution.md`](../constitution.md)

---

## Table of Contents

1. [Overview](#1-overview)
2. [Goals and Business Value](#2-goals-and-business-value)
3. [Scope — What This Project Does and Does Not Do](#3-scope--what-this-project-does-and-does-not-do)
4. [Users and Stakeholders](#4-users-and-stakeholders)
5. [High-Level Requirements](#5-high-level-requirements)
6. [User Stories](#6-user-stories)
7. [Domain Overview](#7-domain-overview)
8. [Current System Behaviour](#8-current-system-behaviour)
9. [Assumptions and Constraints](#9-assumptions-and-constraints)
10. [Non-Functional Requirements](#10-non-functional-requirements)
11. [Acceptance Criteria](#11-acceptance-criteria)
12. [Success Metrics](#12-success-metrics)
13. [Future Extensibility](#13-future-extensibility)
14. [Out of Scope](#14-out-of-scope)
15. [Glossary](#15-glossary)

---

## 1. Overview

CSLibrary2026 is a .NET Standard / .NET 10 class library that enables consumer applications to communicate with Convergence Systems Limited (CSL) RFID readers over Bluetooth Low Energy (BLE) and TCP/IP. It presents a unified API surface regardless of which reader model or transport is in use.

The library is packaged as `CSLibrary2026` and distributed via NuGet. Its primary consumer is the MAUI mobile application (`CSLRFIDReader-C-Sharp-MAUIAPP`), though any .NET application targeting `netstandard2.0` or `net10.0` can use it.

---

## 2. Goals and Business Value

| Goal | Business Value |
|---|---|
| **Unified reader API** | Consumer apps write once against CSLibrary2026; support for CS108, CS468, CS710S, and CS203XL is handled internally. No per-model application code. |
| **Cross-platform BLE** | Works on Android (Xamarin), iOS (Xamarin), and modern .NET via MAUI using Plugin.BLE abstraction. |
| **Transport independence** | BLE and TCP/IP are swappable at runtime. New transports (USB, serial) can be added without consumer changes. |
| **Multi-chipset support** | The same API handles Rx000-based readers (CS108, CS468) and E710/E910-based readers (CS710S, CS203XL) with correct protocol dispatch. |
| **NuGet distribution** | Simple `dotnet add package` integration for consumer projects. |
| **Long-term maintainability** | HAL backend abstraction and clean architecture rules (constitution §2, §7) ensure the codebase does not become a single large blob as new readers are added. |

---

## 3. Scope — What This Project Does and Does Not Do

### In Scope

- BLE discovery and connection management for CSL readers
- TCP/IP connection for CS203XL
- RFID tag inventory, read, write, lock, and kill operations
- Barcode scanning (integrated scanner on supported readers)
- Battery status monitoring
- Antenna and power configuration
- Frequency band and country regulation management
- Event-driven async callback architecture
- A stable, versioned public API published to NuGet

### Out of Scope

- **Firmware upgrade** (NetFinder TFTP is partially present for CS203XL but not exposed as a public API)
- **R1000/R2000 TCP hardware** — protocol not sourced; see constitution §「R1000/R2000 TCP」note
- **WinForms desktop BLE** via `Windows.Devices.Bluetooth` — planned but not yet implemented
- **iOS/Android-specific UI** — this is a library only
- **Direct tag manufacturing operations** beyond ISO 18000-6C EPC GEN2
- **Reader hardware repair or diagnostics** beyond what the firmware exposes

---

## 4. Users and Stakeholders

| User / Stakeholder | Role |
|---|---|
| **Mobile app developer** | Uses `CSLibrary2026` NuGet package in a MAUI or Xamarin app to build an RFID scanning application |
| **Desktop app developer** | Uses `CSLibrary2026` from a WinForms/WPF app on Windows to connect to CS203XL over TCP |
| **CSL firmware team** | Owns the BLE/TCP protocol specification that CSLibrary2026 implements |
| **CSL hardware team** | Produces readers (CS108, CS468, CS710S, CS203XL) that CSLibrary2026 communicates with |
| **CSL integration engineers** | Evaluate, test, and troubleshoot reader–library integration |
| **Open-source community** | Contributors via GitHub PRs, issue reports |

---

## 5. High-Level Requirements

### 5.1 Functional Requirements

| ID | Requirement |
|---|---|
| FR-01 | The library SHALL discover BLE CSL readers by scanning for known service UUIDs and CSL OUI MAC prefixes |
| FR-02 | The library SHALL connect to a selected BLE reader and maintain the connection until explicitly disconnected or connection is lost |
| FR-03 | The library SHALL connect to a CS203XL reader over TCP/IP given an IP address and port |
| FR-04 | The library SHALL send RFID inventory commands and stream tag callback events back to the consumer via `OnAsyncCallback` |
| FR-05 | The library SHALL send RFID tag read/write/lock/kill commands and return results via `OnAccessCompleted` |
| FR-06 | The library SHALL configure per-reader antenna ports, RF power, frequency band, and country regulations |
| FR-07 | The library SHALL expose barcode scan data via `BarcodeReader.OnBarcodeEvent` |
| FR-08 | The library SHALL expose battery status via `Battery` |
| FR-09 | The library SHALL fire `ReaderCallbackType.CONNECTION_LOST` when a BLE or TCP connection is unexpectedly terminated |
| FR-10 | The library SHALL route commands to the correct chipset handler (Rx000 or E710/E910) based on the connected reader model |

### 5.2 Non-Functional Requirements

See [§10](#10-non-functional-requirements) for full detail.

---

## 6. User Stories

### 6.1 Mobile Developer — Connect and Inventory

> **As a** mobile developer building a warehouse scanning app
> **I want to** discover nearby CSL RFID readers over BLE, connect to one, and receive a stream of tag epc values
> **So that** I can display inventory counts in real time

**Acceptance:** After calling `DeviceFinder.StartDeviceSearch()` and handling `OnDeviceFound`, calling `rfidReader.StartInventory()` produces `OnAsyncCallback` events with `CallbackType.TAG_SEARCHING` containing EPC data.

---

### 6.2 Mobile Developer — Read a Specific Tag

> **As a** mobile developer
> **I want to** select a specific tag by its EPC and read its user memory
> **So that** I can verify or update tag-encoded product information

**Acceptance:** After inventory, calling `rfidReader.TagSelect(TagSelected EPC)` followed by `rfidReader.TagRead(...)` produces an `OnAccessCompleted` callback with the read data.

---

### 6.3 Desktop Developer — TCP Connection to CS203XL

> **As a** desktop developer in a fixed-reader installation
> **I want to** connect to a CS203XL reader over the local network
> **So that** my WinForms application can operate a conveyor-belt tag read line

**Acceptance:** Calling `rfidReader.ConnectAsync("192.168.1.100", 5000)` establishes a TCP connection; `rfidReader.State` becomes `READYFORUSING`; `OnStateChanged` fires with `CONNECT_SUCESS`.

---

### 6.4 QA Engineer — Multi-Reader Support

> **As a** QA engineer testing CSL reader compatibility
> **I want to** run the same application code against CS108, CS710S, and CS203XL readers
> **So that** I can verify that inventory speed and accuracy are equivalent across models

**Acceptance:** The same `StartInventory()` call produces equivalent `OnAsyncCallback` stream behaviour on all supported models with no model-specific branching in the consumer app.

---

## 7. Domain Overview

### 7.1 Ubiquitous Language

| Term | Definition |
|---|---|
| **Reader / Device** | Physical CSL RFID reader hardware (CS108 handheld, CS710S, CS203XL fixed) |
| **RFID Reader API** | `RFIDReader` class — primary public entry point for RFID operations |
| **Tag** | UHF RFID transponder (ISO 18000-6C EPC GEN2) |
| **EPC** | Electronic Product Code — the unique identifier stored in a tag's bank |
| **Inventory** | The act of energizing the RF field and collecting responses from all tags in range |
| **Access** | A specific tag operation: read, write, lock, or kill |
| **BLE** | Bluetooth Low Energy — the primary transport for handheld readers |
| **TCP/IP** | Ethernet transport for fixed CS203XL readers |
| **Chipset** | The embedded firmware generation running inside the reader: Rx000 (CS108/468) or E710/E910 (CS710S/CS203XL) |
| **HAL** | Hardware Abstraction Layer — per-transport partial class implementations of `HighLevelInterface` |
| **ITransport** | The interface abstracting BLE and TCP send/receive/disconnect |

### 7.2 Key Abstractions

```
RFIDReader          — public API facade
  └─ HighLevelInterface  — internal coordinator (partial class, multiple HALs)
       ├─ ITransport          (BLETransport | TCPTransport)
       └─ BluetoothProtocol  (downlink command framing / uplink decoding)

ClassRFID.UnifiedAPI — chipset dispatch (Rx000 vs E710/E910)
```

### 7.3 Supported Readers

| Model | Transport | Chipset | Form Factor |
|---|---|---|---|
| CS108 | BLE | Rx000 | Handheld |
| CS468 | BLE | Rx000 | Fixed / integrated |
| CS710S | BLE | E710 | Handheld |
| CS203XL | BLE + TCP | E910 | Fixed / desktop |

---

## 8. Current System Behaviour

### 8.1 Happy Path — BLE Connect and Inventory

1. Consumer app calls `DeviceFinder.Initialize(adapter, bluetoothLe)`
2. Consumer app calls `DeviceFinder.StartDeviceSearch()` → `OnDeviceFound` fires with `DeviceInformation` (includes `NativeDevice`)
3. Consumer app calls `adapter.ConnectToDeviceAsync(device)` and obtains a connected `IDevice`
4. Consumer app calls `rfidReader.ConnectAsync(adapter, device, MODEL.CS108)`
5. `BLETransport` discovers GATT services/characteristics and subscribes to notifications
6. `RFIDReader.State` → `READYFORUSING`; `OnStateChanged` fires with `CONNECT_SUCESS`
7. Consumer app configures `rfidReader.Options` (power, antenna, algorithm)
8. Consumer app calls `rfidReader.StartInventory()` → `OnAsyncCallback` streams tag data
9. Consumer app calls `rfidReader.StopInventory()`
10. Consumer app calls `rfidReader.Disconnect()` → BLE disconnection and cleanup

### 8.2 Happy Path — TCP Connect (CS203XL)

1. Consumer app calls `rfidReader.ConnectAsync("192.168.1.100", 5000)`
2. `TCPTransport` opens a `CSLTCPSTREAM` socket
3. `RFIDReader.State` → `READYFORUSING`; `OnStateChanged` fires with `CONNECT_SUCESS`
4. Remainder of RFID operations are identical to BLE path from step 7 above

### 8.3 Known Current Limitations

- No unit tests exist — all validation is manual
- 472 compiler warnings (mostly unused private fields) reduce signal quality
- `net10.0` is a pre-release target; stability is unconfirmed
- No CI/CD pipeline — releases are manual
- No CHANGELOG — version history is not formally tracked
- `.bak` files exist in source tree (legacy artifacts)
- WinForms BLE via `Windows.Devices.Bluetooth` is planned but not implemented

---

## 9. Assumptions and Constraints

| # | Assumption / Constraint | Impact |
|---|---|---|
| AS-01 | Plugin.BLE 3.0.0 remains compatible with future Xamarin/MAUI releases | If Plugin.BLE is abandoned, a new BLE abstraction must be added to HAL |
| AS-02 | CSL does not change BLE service UUIDs or GATT characteristic UUIDs in future firmware | If UUIDs change, `BLETransport` and `DeviceFinder` must be updated |
| AS-03 | Consumer applications handle OS-level BLE permission requests before calling the library | Library does not request Bluetooth permissions |
| AS-04 | Readers are running firmware versions compatible with the current protocol specification | Older firmware may not support all API features |
| AS-05 | `netstandard2.0` consumers are using Xamarin or legacy .NET Framework ≥ 4.6.1 | WinForms on .NET Framework is not supported via netstandard2.0 |
| AS-06 | R1000/R2000 TCP protocol specification is not available to the development team | This hardware is explicitly out of scope (constitution §「R1000/R2000 TCP」) |
| C-01 | Build environment has .NET 10 SDK at `/home/node/.dotnet` | Cross-compilation to `net10.0` requires .NET 10 installed |
| C-02 | Physical reader hardware is required for integration testing | CI cannot run integration tests without hardware mocking or simulation |

---

## 10. Non-Functional Requirements

### 10.1 Performance

| NFR | Requirement |
|---|---|
| **NFR-P01** | Tag inventory callback events SHALL be delivered within 50ms of the reader transmitting the tag response under normal RF conditions |
| **NFR-P02** | `DeviceFinder.StartDeviceSearch()` SHALL complete a 5-second BLE scan without blocking the calling thread |
| **NFR-P03** | The library SHALL NOT allocate unbounded memory during sustained inventory sessions — inbound buffers are fixed-size |

### 10.2 Reliability

| NFR | Requirement |
|---|---|
| **NFR-R01** | A `CONNECTION_LOST` event SHALL be raised within 5 seconds of an unexpected BLE link loss |
| **NFR-R02** | Calling `Disconnect()` SHALL clean up all BLE subscriptions and characteristic notifications |
| **NFR-R03** | After `Disconnect()`, a subsequent `ConnectAsync()` to the same or a different device SHALL succeed without residual state errors |

### 10.3 Portability

| NFR | Requirement |
|---|---|
| **NFR-Po01** | The `netstandard2.0` build SHALL be loadable by .NET Framework 4.6.1+, .NET Core 2.0+, and .NET 5+ applications |
| **NFR-Po02** | The `net10.0` build SHALL run on Windows 10+ with BLE hardware or USB BLE dongle support |
| **NFR-Po03** | No platform-specific code SHALL exist in the public API surface |

### 10.4 Maintainability

| NFR | Requirement |
|---|---|
| **NFR-M01** | The public API surface SHALL NOT break between minor versions — breaking changes require a major version bump and migration guide (constitution §10.3) |
| **NFR-M02** | Every public type and member SHALL have XML documentation |
| **NFR-M03** | The build SHALL produce **0 errors** on both `netstandard2.0` and `net10.0` TFMs before any PR is merged |

### 10.5 Security

| NFR | Requirement |
|---|---|
| **NFR-S01** | No RF tag data, MAC addresses, or connection metadata SHALL be written to logs in release builds |
| **NFR-S02** | The library SHALL NOT store any shared secrets or credentials |
| **NFR-S03** | All inbound byte arrays from the reader SHALL be length-validated before array indexing |

---

## 11. Acceptance Criteria

### 11.1 Build and Release

| AC | Criterion | Verification |
|---|---|---|
| **AC-B01** | `dotnet build -c Release` produces 0 errors on both TFMs | CI runs `dotnet build` on every PR |
| **AC-B02** | `dotnet pack -c Release` produces a valid `.nupkg` | Manual `dotnet pack` smoke test |
| **AC-B03** | The NuGet package contains the correct assembly for `netstandard2.0` and `net10.0` | Inspect `.nupkg` with NuGet Package Explorer |

### 11.2 BLE Discovery and Connection

| AC | Criterion | Verification |
|---|---|---|
| **AC-BLE01** | `DeviceFinder.StartDeviceSearch()` discovers CS108 and CS710S devices advertising known service UUIDs | Manual test with physical readers |
| **AC-BLE02** | `rfidReader.ConnectAsync(adapter, device, MODEL.CS108)` returns `true` and fires `CONNECT_SUCESS` | Manual test |
| **AC-BLE03** | `rfidReader.Disconnect()` fires `CONNECTION_LOST` on the BLE adapter being disabled | Manual test |
| **AC-BLE04** | `rfidReader.State` transitions through `DICONNECT → IDLE → BUSY → READYFORDISCONNECT` correctly | Manual test |

### 11.3 TCP Connection (CS203XL)

| AC | Criterion | Verification |
|---|---|---|
| **AC-TCP01** | `rfidReader.ConnectAsync("192.168.1.100", 5000)` connects and fires `CONNECT_SUCESS` | Manual test on LAN |
| **AC-TCP02** | `rfidReader.IsConnected` returns `true` while connected and `false` after `Disconnect()` | Manual test |

### 11.4 RFID Operations

| AC | Criterion | Verification |
|---|---|---|
| **AC-RFID01** | `StartInventory()` produces `OnAsyncCallback` events with `CallbackType.TAG_SEARCHING` | Manual test with tags in field |
| **AC-RFID02** | `StopInventory()` stops the callback stream within 1 second | Manual test |
| **AC-RFID03** | `TagRead()` after `TagSelect()` produces `OnAccessCompleted` with correct data | Manual test |
| **AC-RFID04** | Inventory result is equivalent across CS108, CS710S (BLE), and CS203XL (TCP) for the same tag set | Comparative manual test |

### 11.5 Barcode

| AC | Criterion | Verification |
|---|---|---|
| **AC-BC01** | Barcode scan fires `BarcodeReader.OnBarcodeEvent` with correct barcode data | Manual test (reader with integrated scanner) |

### 11.6 Battery

| AC | Criterion | Verification |
|---|---|---|
| **AC-BAT01** | `Battery.GetBatteryLevel()` returns a value 0–100 | Manual test |

---

## 12. Success Metrics

| Metric | Current Baseline | Target | How Measured |
|---|---|---|---|
| Build errors | 0 (✅) | 0 | `dotnet build -c Release` |
| Compiler warnings | 472 | < 20 | `dotnet build -c Release` |
| Unit test coverage | 0% | ≥ 80% (Transport + BluetoothProtocol layers) | `dotnet test /p:CollectCoverage=true` |
| Public API XML docs | Partial | 100% | CI doc generation tool |
| CI pipeline | Not configured | Runs on every PR | GitHub Actions |
| NuGet package | 1.0.0-beta.1 (local) | Published stable on nuget.org | NuGet feed |
| GitHub issues response | N/A | < 5 business days | GitHub project board |
| Supported reader models | 4 (CS108, CS468, CS710S, CS203XL) | Stable across firmware updates | Regression test suite |
| CHANGELOG | Not maintained | Updated per release | Manual review |

---

## 13. Future Extensibility

The following directions are explicitly anticipated by the architecture and are permitted under the constitution without breaking changes:

### 13.1 New Reader Model Support

Adding a new CSL reader (e.g., CS469) requires:
1. Add `MODEL.CS469` to the enum
2. Add BLE UUIDs to `BLETransport` and `DeviceFinder`
3. Verify which chipset path it uses and confirm UnifiedAPI dispatch
4. Add to the README and CHANGELOG

No HAL changes required if BLE UUIDs are in the existing service ranges.

### 13.2 New Transport Backend

Per constitution §7, a new transport (e.g., USB CDC, or WinRT BLE for WinForms) requires:
1. New `Source/HAL/{Backend}/` folder with partial `HighLevelInterface` + `ITransport` implementation
2. `README.md` inside the folder
3. `.csproj` update to activate for the target TFM
4. Functional review PR

### 13.3 Windows.Devices.Bluetooth for WinForms (.NET 10 Windows)

This is the primary planned extensibility item. The WinFormsBLE HAL (`Source/HAL/WinFormsBLE/`) is excluded from the current build but is architecturally prepared for this activation.

Activation requires:
- Implement `ITransport` using `Windows.Devices.Bluetooth.Advertisement` APIs
- Add `#if NET10_0_OR_GREATER && WINDOWS` guards
- Update `.csproj` to include WinFormsBLE for `net10.0-windows`

### 13.4 R1000/R2000 TCP

Not currently planned because the TCP protocol specification for these models has not been sourced. If the specification becomes available, it can be implemented as a new `TCPTransport`-based path.

### 13.5 Expanded Sensor Tag Support

Currently excluded components (EM4325, FM13DT160, FM13DT160) can be re-enabled in `.csproj` when there is consumer demand. They are compilable but gated off.

---

## 14. Out of Scope (Explicitly Declined)

| Item | Reason |
|---|---|
| Firmware update / TFTP flash | Not needed for current consumer apps; adds significant complexity and risk |
| R1000/R2000 TCP | Protocol specification unavailable |
| Direct USB HID for CS108/468 | Not in current CSL USB API specification |
| iOS CoreBluetooth directly | Plugin.BLE provides this abstraction; direct CoreBluetooth would fragment the HAL |
| Android-specific BLE scanning | Same — Plugin.BLE is the established abstraction |
| Windows.Devices.Bluetooth for UWP | The UWP HAL is excluded; WinForms path is WinRT BLE |

---

## 15. Glossary

| Term | Definition |
|---|---|
| **BLE** | Bluetooth Low Energy — short-range wireless protocol used by all handheld CSL readers |
| **EPC** | Electronic Product Code — 96-bit or 128-bit identifier encoded in RFID tags |
| **GATT** | Generic Attribute Profile — the BLE protocol for exchanging data via services and characteristics |
| **GEN2** | ISO 18000-6C EPC tag air interface protocol — the standard all CSL UHF readers operate under |
| **HAL** | Hardware Abstraction Layer — per-backend partial class implementations that share the same `HighLevelInterface` |
| **ITransport** | Interface (`Source/Transport/ITransport.cs`) that abstracts `SendAsync`, `ConnectAsync`, `Disconnect`, and receive callbacks |
| **MAC** | Bluetooth Media Access Control address — the hardware identifier of a BLE device |
| **MTU** | Maximum Transmission Unit — the largest BLE packet payload (255 bytes requested for CS710S/CS203XL) |
| **RFID** | Radio-Frequency Identification — the technology CSL readers use to communicate with tags |
| **Rx000** | CSL's proprietary chipset/firmware for CS108, CS468, and related models — controlled via direct MAC register writes |
| **E710 / E910** | Integrated chip families used in CS710S and CS203XL — controlled via high-level command/response protocol |
| **TFTP** | Trivial File Transfer Protocol — used by NetFinder for firmware upgrades (present but not exposed publicly) |
| **TF-M** | Target Framework Moniker — e.g., `netstandard2.0`, `net10.0` |
| **UHF** | Ultra-High Frequency — the RF band (860–960 MHz) used by CSL RFID readers |
| **UUID** | 128-bit GUID used as BLE service and characteristic identifiers |

---

## Appendix A — Document History

| Version | Date | Author | Change |
|---|---|---|---|
| 0.1.0 | 2026-04-05 | GithubProject-CSLibrary2026 | Initial draft |

## Appendix B — Related Documents

| Document | Location | Purpose |
|---|---|---|
| Constitution | `Documents/constitution.md` | Immutable project principles, architecture rules, and technical debt register |
| README | `README.md` | Installation, quick-start, supported readers |
| Project Report | `PROJECT_REPORT.md` | Architecture deep-dive, folder structure, NuGet config |
| CHANGELOG | Not yet created | Per-release change log (TODO — see constitution §12) |

---

*End of specification. For questions or proposed changes, open a GitHub issue at `https://github.com/cslrfid/CSLibrary2026/issues`.*
