# T-15 — Expand README.md

**Status:** Pending
**Phase:** 2 — Documentation
**Priority:** 🟡 Medium
**Estimated:** 2 hours
**Constitution:** §10.2 (Required Documents — README.md)
**Spec reference:** Success Metrics (README: currently minimal → complete)

---

## Objective

Replace the current minimal `CSLibrary2026/README.md` with a complete README covering installation, supported readers, quick-start code snippets, and links to all governance documents.

---

## Current State

```
# CSLibrary2026
```

---

## Deliverable: README.md

```markdown
# CSLibrary2026

[![NuGet](https://img.shields.io/nuget/v/CSLibrary2026.svg)](https://www.nuget.org/packages/CSLibrary2026/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

CSL RFID Reader Library for .NET — Supports Bluetooth LE and TCP/IP communication with CSL RFID readers.

## Supported Readers

| Model | Transport | Chipset | Form Factor |
|---|---|---|---|
| CS108 | Bluetooth LE | Rx000 | Handheld |
| CS468 | Bluetooth LE | Rx000 | Fixed / integrated |
| CS710S | Bluetooth LE | E710 | Handheld |
| CS203XL | Bluetooth LE + TCP/IP | E910 | Fixed / desktop |

For details on supported firmware versions, see [the specification](Documents/specs/spec.md).

## Requirements

- **.NET Standard 2.0:** Xamarin.Forms, .NET Framework 4.6.1+, .NET Core 2.0+, .NET 5+
- **.NET 10:** Windows 10 1809+ (for WinForms/WinRT BLE support — see [WinForms BLE HAL](Source/HAL/WinFormsBLE/README.md))
- **Plugin.BLE 3.0.0** — included via NuGet dependency

## Installation

### NuGet (recommended)

```bash
dotnet add package CSLibrary2026
```

Or add to your `.csproj`:

```xml
<PackageReference Include="CSLibrary2026" Version="1.0.0-beta.2" />
```

### From source

```bash
git clone https://github.com/cslrfid/CSLibrary2026.git
cd CSLibrary2026
dotnet pack -c Release -o ./nupkg
```

## Quick Start

### 1. Discover Readers (BLE)

```csharp
using CSLibrary;

// Initialize DeviceFinder with Plugin.BLE instances
DeviceFinder.Initialize(adapter, bluetoothLe);

// Start scanning — fires OnDeviceFound for each discovered reader
DeviceFinder.StartDeviceSearch(scanMode: Plugin.BLE.Abstractions.ScanMode.LowLatency, timeoutMs: 5000);
DeviceFinder.OnDeviceFound += (sender, args) =>
{
    Console.WriteLine($"Found: {args.Device.Name} ({args.Device.DeviceType})");
};
```

### 2. Connect (BLE)

```csharp
using CSLibrary;

var reader = new RFIDReader();

// Connect to a discovered device
await reader.ConnectAsync(adapter, device, MODEL.CS108);

// Subscribe to events
reader.OnStateChanged += (sender, args) =>
{
    if (args.State == RFState.RF_STATE_ON)
        Console.WriteLine("Reader ready — antenna on");
};
reader.OnAsyncCallback += (sender, args) =>
{
    if (args.Type == CallbackType.TAG_SEARCHING)
        Console.WriteLine($"Tag: {args.Info:EPC}");
};
```

### 3. Connect (TCP/IP — CS203XL only)

```csharp
var reader = new RFIDReader();
await reader.ConnectAsync("192.168.1.100", 5000); // IP, port
Console.WriteLine($"Connected: {reader.IsConnected}");
```

### 4. Run Inventory

```csharp
reader.Options.TagRanging flags = TagRanging flags.None;
reader.Options.QswitchQuery = new QswitchQuery { Q = 4 };

reader.StartInventory();
await Task.Delay(5000); // collect for 5 seconds
reader.StopInventory();
```

## Documentation

| Document | Purpose |
|---|---|
| [Constitution](Documents/constitution.md) | Immutable project principles, architecture rules, technical debt |
| [Specification](Documents/specs/spec.md) | What to build — goals, requirements, acceptance criteria |
| [Technical Plan](Documents/plan.md) | How to build it — phases, tasks, risk register |
| [API Reference](Documents/API.md) | Full public API reference |
| [CHANGELOG](CHANGELOG.md) | Per-release change log |

## Architecture

```
Consumer App
    └── CSLibrary.RFIDReader          ← public API
            └── HighLevelInterface     ← internal coordinator (partial class)
                    ├── ITransport     ← BLETransport | TCPTransport
                    └── BluetoothProtocol
```

See [the Constitution](Documents/constitution.md) for full architecture details.

## License

MIT — see [LICENSE](LICENSE)

## Links

- **NuGet Package:** https://www.nuget.org/packages/CSLibrary2026/
- **GitHub Repository:** https://github.com/cslrfid/CSLibrary2026
- **Demo MAUI App:** https://github.com/cslrfid/CSLRFIDReader-C-Sharp-MAUIAPP
- **Issues:** https://github.com/cslrfid/CSLibrary2026/issues
```

---

## Step-by-Step Commands

```bash
# 1. Write the expanded README
# (use write tool to overwrite CSLibrary2026/README.md)

# 2. Commit
git add README.md
git commit -m "docs: expand README.md with installation, supported readers, quick-start

Added:
- NuGet badge and license badge
- Supported readers table with transport and chipset columns
- Requirements section
- dotnet add package instruction
- Quick-start code for BLE discover, BLE connect, TCP connect, inventory
- Documentation links table (constitution, spec, plan, API, CHANGELOG)
- Architecture diagram
- Links section

Constitutes: constitution §10.2, plan T-15"
```

---

## Definition of Done

- [ ] README.md has supported readers table
- [ ] README.md has installation snippet (`dotnet add package`)
- [ ] README.md has ≥ 1 BLE quick-start code block
- [ ] README.md has ≥ 1 TCP quick-start code block
- [ ] README.md links to constitution.md, spec.md, plan.md, CHANGELOG.md
- [ ] README.md has NuGet badge pointing to correct package URL
