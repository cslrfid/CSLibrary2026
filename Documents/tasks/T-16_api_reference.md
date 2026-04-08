# T-16 — Create Documents/API.md

**Status:** Pending
**Phase:** 2 — Documentation
**Priority:** 🟡 Medium
**Estimated:** 1 day
**Constitution:** §10.2 (Required Documents — API.md)
**Spec reference:** Success Metrics (Public API XML docs: partial → 100%)

---

## Objective

Create `CSLibrary2026/Documents/API.md` as a full public API reference. Each of the 6 public entry points is documented with its key members.

---

## Deliverable: API.md

```markdown
# CSLibrary2026 — Public API Reference

> **Status:** Generated from source analysis — 2026-04-05
> **Constitution:** §5.1 defines the official public surface

---

## Table of Contents

1. [RFIDReader](#1-rfidreader)
2. [BarcodeReader](#2-barcodereader)
3. [Battery](#3-battery)
4. [DeviceFinder](#4-devicefinder)
5. [Notification](#5-notification)
6. [Debug](#6-debug)
7. [Constants](#7-constants)
8. [Events](#8-events)
9. [Structures](#9-structures)

---

## 1. RFIDReader

Primary entry point for all RFID operations. Instantiate once per connected reader.

```csharp
var reader = new RFIDReader();
```

### Properties

| Property | Type | Description |
|---|---|---|
| `State` | `RFState` | Current reader state (DISCONNECT, IDLE, BUSY, READYFORDISCONNECT) |
| `IsConnected` | `bool` | True when the reader is connected (BLE or TCP) |
| `Options` | `CSLibraryOperationParms` | Global operation parameters (power, antenna, algorithm) |

### Connect Methods

| Method | Description |
|---|---|
| `Task<bool> ConnectAsync(IAdapter adapter, IDevice device, MODEL model)` | Connect over BLE (MAUI/Xamarin) |
| `Task<bool> ConnectAsync(string ipAddress, int port)` | Connect over TCP (CS203XL only) |
| `void Disconnect()` | Disconnect and clean up all BLE subscriptions |

### Inventory Methods

| Method | Description |
|---|---|
| `Result StartInventory()` | Begin continuous inventory. Tags fire `OnAsyncCallback`. |
| `Result StopInventory()` | Stop inventory. |
| `Result StartRanging()` | Begin tag ranging (with RSSI distance). |
| `Result StopRanging()` | Stop ranging. |

### Tag Access Methods

| Method | Description |
|---|---|
| `Result TagSelect(TagSelected select)` | Select a specific tag by EPC or handle |
| `Result TagRead(ushort bank, ushort offset, ushort count, TagCallbackInfo callback)` | Read tag memory bank |
| `Result TagWrite(ushort bank, ushort offset, ushort count, byte[] data)` | Write to tag memory bank |
| `Result TagLock(TagLockParms parms)` | Lock/unlock tag memory regions |
| `Result TagKill(TagKillParms parms)` | Kill the tag |

### Configuration Methods

| Method | Description |
|---|---|
| `Result SetPowerLevel(uint level)` | Set TX power (dBm) |
| `Result SetAntennaPower(uint port, uint power)` | Set per-port antenna power |
| `Result SetFrequencyBand(FREQENCY_MODE mode)` | Set frequency band mode |
| `Result SetCountry(COUNTRIES code)` | Set country regulation |

### Events

| Event | Event Args | Description |
|---|---|---|
| `OnStateChanged` | `OnReaderStateChangedEventArgs` | Reader state transitions |
| `OnAsyncCallback` | `OnAsyncCallbackEventArgs` | Tag inventory/ranging results |
| `OnAccessCompleted` | `OnAccessCompletedEventArgs` | Tag read/write/kill/lock completion |
| `OnInventoryTagRateCallback` | `OnInventoryTagRateCallbackEventArgs` | Real-time tag read rate |

---

## 2. BarcodeReader

Integrated barcode scanner on supported readers.

```csharp
var barcode = new BarcodeReader();
```

### Properties

| Property | Type | Description |
|---|---|---|
| `Enabled` | `bool` | Scanner power state |

### Methods

| Method | Description |
|---|---|
| `Result SetPower(bool on)` | Power the scanner on or off |
| `Result TriggerScan()` | Software-trigger a barcode scan |

### Events

| Event | Event Args | Description |
|---|---|---|
| `OnBarcodeEvent` | `BarcodeEventArgs` | Barcode scan data received |

---

## 3. Battery

Battery monitoring for handheld readers.

```csharp
var battery = new Battery();
```

### Properties

| Property | Type | Description |
|---|---|---|
| `Level` | `int` | Battery percentage (0–100), -1 if unavailable |

### Methods

| Method | Description |
|---|---|
| `int GetBatteryLevel()` | Returns battery level as 0–100, or -1 |

---

## 4. DeviceFinder

Static BLE scanner for discovering CSL readers.

```csharp
DeviceFinder.Initialize(adapter, bluetoothLe);
DeviceFinder.StartDeviceSearch(timeoutMs: 5000);
```

### Methods

| Method | Description |
|---|---|
| `void Initialize(IAdapter adapter, IBluetoothLE bluetoothLe)` | Initialize scanner. Call before StartDeviceSearch. |
| `void StartDeviceSearch(ScanMode scanMode = LowLatency, int timeoutMs = 5000)` | Start BLE scan |
| `void StopDeviceSearch()` | Stop BLE scan early |
| `IReadOnlyList<DeviceInformation> GetDevices()` | Get list of found devices |
| `void ClearDevices()` | Clear the device list |

### Properties

| Property | Type | Description |
|---|---|---|
| `IsScanning` | `bool` | True if scan is in progress |

### Events

| Event | Event Args | Description |
|---|---|---|
| `OnDeviceFound` | `DeviceFoundEventArgs` | Fires for each discovered CSL reader |
| `OnSearchCompleted` | `SearchCompletedEventArgs` | Fires when scan times out or is stopped |

---

## 5. Notification

Reader notification subsystem (GPI triggers, auto-reporting, etc.).

```csharp
var notification = new Notification(reader);
```

### Methods

| Method | Description |
|---|---|
| `Result StartAutoReporting()` | Enable automatic status reporting |
| `Result StopAutoReporting()` | Disable automatic status reporting |
| `Result EnableGPIEvent(int gpiPort, bool enable)` | Enable/disable GPI trigger events |

### Events

| Event | Event Args | Description |
|---|---|---|
| `OnGPIChanged` | `GPIEventArgs` | GPI pin state changed |

---

## 6. Debug

Static debug logging utility — output is only produced in DEBUG builds.

```csharp
Debug.WriteLine("Reader connected");
Debug.WriteBytes("TX", data);
```

### Methods

| Method | Description |
|---|---|
| `void WriteLine(string format, params object[] args)` | Log a formatted line (DEBUG only) |
| `void Write(string format, params object[] args)` | Log a formatted string (DEBUG only) |
| `void WriteBytes(string header, byte[] data)` | Log a hex dump of bytes (DEBUG only) |

---

## 7. Constants

| Namespace | Key Types |
|---|---|
| `CSLibrary.Constants` | `MODEL` (enum — CS108, CS468, CS710S, CS203XL), `Result` (enum), `ReaderCallbackType` (enum), `CallbackType` (enum), `RFState` (enum), `COUNTRIES` (enum) |

### MODEL Enum

```csharp
public enum MODEL
{
    UNKNOWN = 0,
    CS108   = 1,
    CS468   = 2,
    CS710S  = 3,
    CS203XL = 4,
    // ...
}
```

---

## 8. Events

| Class | Namespace | Description |
|---|---|---|
| `OnReaderStateChangedEventArgs` | `CSLibrary.Events` | Reader state change |
| `OnAsyncCallbackEventArgs` | `CSLibrary.Events` | Tag inventory/ranging data |
| `OnAccessCompletedEventArgs` | `CSLibrary.Events` | Tag access completion |
| `OnInventoryTagRateCallbackEventArgs` | `CSLibrary.Events` | Tag rate metrics |
| `BarcodeEventArgs` | `CSLibrary.BarcodeReader.Events` | Barcode scan result |

---

## 9. Structures

| Class | Namespace | Description |
|---|---|---|
| `TagCallbackInfo` | `CSLibrary.Structures` | Tag data from inventory (EPC, RSSI, PC) |
| `TagSelected` | `CSLibrary.Structures` | Tag selection criteria |
| `INVENTORY_PKT` | `CSLibrary.Structures` | Raw inventory packet data |
| `TAG_ACCESS_PKT` | `CSLibrary.Structures` | Raw tag access packet data |

---

## Adding New Public Members

When adding a new public method or property:

1. Add `/// <summary>` XML documentation (constitution §10.1)
2. Add the member to this document in the correct section
3. Add an entry in [Unreleased] of `CHANGELOG.md` under `### Added`
4. If adding a new public class, add it to the table of contents and its section

---

*End of API Reference*
```

---

## Step-by-Step Commands

```bash
# 1. Write the API reference
# (use write tool to create CSLibrary2026/Documents/API.md)

# 2. Commit
git add Documents/API.md
git commit -m "docs: create Documents/API.md public API reference

Documents all 6 public entry points:
- RFIDReader (connect, inventory, tag access, config, events)
- BarcodeReader
- Battery
- DeviceFinder
- Notification
- Debug

Includes: Constants (MODEL, Result, CallbackType, RFState),
Events (On*EventArgs), Structures (TagCallbackInfo, INV_PKT, ACCESS_PKT)

Constitutes: constitution §10.2, plan T-16"
```

---

## Definition of Done

- [ ] `Documents/API.md` exists
- [ ] All 6 public entry points have a section with key methods/properties listed
- [ ] `RFIDReader` has ≥ 5 method groups documented
- [ ] `DeviceFinder` static API is documented
- [ ] Constants namespace lists key enums
- [ ] Events and Structures sections list key types
