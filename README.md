# CSLibrary2026

CSL RFID Reader Library for .NET — supporting Bluetooth LE and TCP/IP communication with CSL RFID readers.

## Supported Readers

| Reader | Connection |
|--------|-----------|
| **CS108** | Bluetooth LE |
| **CS468** | Bluetooth LE |
| **CS710S** | Bluetooth LE |
| **CS203XL** | Bluetooth LE / TCP/IP |

## Supported Platforms

- **.NET Standard 2.0** — Xamarin.Forms, .NET MAUI, and other .NET Standard compatible frameworks
- **.NET 10** — .NET 10+ applications
- **.NET 10 Windows** — Windows desktop applications (TCP/IP support)

## Features

- Bluetooth LE (BLE) communication via [Plugin.BLE](https://github.com/xabre/xamarin-bluetooth-le)
- TCP/IP connectivity for fixed readers (Windows desktop)
- Multi-reader support across CSL RFID readers
- Rich RFID operations: inventory, read/write, lock/kill, filtering
- Barcode scanning integration
- Battery & notification management

## Installation

```bash
dotnet add package CSLibrary2026
```

Or add to your `.csproj`:

```xml
<PackageReference Include="CSLibrary2026" Version="1.0.0-beta.1" />
```

## Quick Start

### 1. Connect to a Reader

#### Bluetooth LE (CS108, CS468, CS710S, CS203XL)

```csharp
using CSLibrary;

// Create the reader instance
var reader = new HighLevelInterface();

// Discover BLE devices
var deviceFinder = new DeviceFinder();
deviceFinder.OnDeviceFound += (sender, device) =>
{
    Console.WriteLine($"Found: {device.Name}");
};
await deviceFinder.StartScanAsync();

// Connect to a BLE reader
await reader.ConnectAsync(adapter, device, MODEL.CS108);
```

#### TCP/IP (CS203XL — Network API)

```csharp
using CSLibrary;

// Create the reader instance
var reader = new HighLevelInterface();

// Connect via TCP/IP (CS203XL Network API)
await reader.ConnectAsync("192.168.1.100", 2000);
```

### 2. Subscribe to Events

```csharp
// RFID tag inventory callback
reader.rfid.OnAsyncCallback += (sender, e) =>
{
    if (e.type == Constants.CallbackType.TAG_RANGING)
    {
        Console.WriteLine($"EPC: {e.info.epc}");
    }
};

// Reader state change callback
reader.OnReaderStateChanged += (sender, e) =>
{
    Console.WriteLine($"State: {e.state}");
};
```

### 3. Start Inventory

```csharp
// Set power level (0–3000 = 0–30.00 dBm)
reader.rfid.SetPowerLevel(3000);

// Start inventory
reader.rfid.StartOperation(RFIDReader.Operation.TAG_RANGING);

// Stop after some time
reader.rfid.StopOperation();
```

## API Overview

### Connection

| Method | Description |
|--------|-------------|
| `ConnectAsync(IAdapter, IDevice, MODEL)` | Connect to a BLE reader |
| `ConnectAsync(string ipAddress, int port)` | Connect to a TCP/IP reader |
| `DisconnectAsync()` | Disconnect from the reader (BLE or TCP, decided internally) |

> **Migrating from the previous `CSLibrary` / `CSLibrary2026`:** this package is the unified
> API, so a few signatures differ from the older per-HAL implementations. The most common
> one is `DisconnectAsync()`, which used to be `async Task<bool>` and is now `void`
> (the transport is selected internally). Drop the discard when updating an existing app:
>
> ```csharp
> -_ = reader.DisconnectAsync();   // previous CSLibrary2026 (Task<bool>)
> +reader.DisconnectAsync();        // CSLibrary2026 (unified API)
> ```
>
> Applications are expected to follow the library; if an app fails to compile, adjust the
> app rather than the library signature.

### RFID Operations

**Start operations:**
```csharp
reader.rfid.StartOperation(Operation.TAG_RANGING);       // Inventory
reader.rfid.StartOperation(Operation.TAG_READ);           // Read tags
reader.rfid.StartOperation(Operation.TAG_READ_TID);       // Read TID bank
reader.rfid.StartOperation(Operation.TAG_READ_USER);      // Read USER bank
reader.rfid.StartOperation(Operation.TAG_WRITE);         // Write tags
reader.rfid.StartOperation(Operation.TAG_LOCK);           // Lock tag
reader.rfid.StartOperation(Operation.TAG_KILL);           // Kill tag
reader.rfid.StopOperation();                             // Stop continuous operation
```

**Power & Frequency:**
```csharp
reader.rfid.SetPowerLevel(uint pwrlevel);           // Set power (0–3000)
reader.rfid.GetActiveMaxPowerLevel();               // Get max power
reader.rfid.SetCurrentLinkProfile(uint profile);    // Set link profile
reader.rfid.SetCountry(string countryName);         // Set country frequency
reader.rfid.SetCountry(string countryName, int ch);// Set country + fixed channel
```

**Singulation Algorithms:**
```csharp
reader.rfid.SetCurrentSingulationAlgorithm(SingulationAlgorithm.FIXEDQ);
reader.rfid.SetFixedQParms(FixedQParms parms);
reader.rfid.SetDynamicQParms(DynamicQParms parms);
reader.rfid.SetTagGroup(TagGroup tagGroup);
reader.rfid.SetPostMatchCriteria(SingulationCriterion[] criteria);
```

**Callback Events:**
- `reader.rfid.OnAsyncCallback` — Inventory / tag data (`CallbackType.TAG_RANGING`, `TAG_SEARCHING`)
- `reader.rfid.OnAccessCompleted` — Read/write/lock result
- `reader.OnReaderStateChanged` — RFID reader state

### Battery & Notifications

```csharp
reader.GetCurrentBatteryLevel();              // Get battery level
reader.ClearEventHandler();                   // Clear all callback events

reader.notification.OnVoltageEvent += ...     // Battery level changes
reader.notification.OnKeyEvent += ...         // Hotkey presses
```

### Barcode Scanner

```csharp
reader.barcode.OnCapturedNotify += (sender, e) => Console.WriteLine($"Barcode: {e.Barcode}");
reader.barcode.Start();
reader.barcode.Stop();
```

**Events:**
- `OnCapturedNotify` — Captured barcode data
- `OnStateChanged` — Scanner state

## Project Structure

```
CSLibrary2026/
├── Source/
│   ├── CSLibrary.cs                     # Main library entry point
│   ├── Transport/                       # Transport layer abstraction (ITransport, BLE/TCP)
│   ├── BluetoothProtocol/               # BLE protocol (send/receive/connect)
│   ├── HAL/
│   │   ├── Plugin.BLE/                  # BLE backend (Plugin.BLE)
│   │   └── TCPIP/                       # TCP/IP backend
│   ├── RFIDReader/
│   │   ├── Comm_Protocol/               # Low-level command protocols (Ex10Commands, RX000Commands)
│   │   └── CSLUnifiedAPI/               # Unified API and reader-specific implementations
│   │       └── Basic_API/
│   │           ├── CS108/               # CS108 / CS468 (Rx-family) implementation
│   │           └── CS710S/              # CS710S / CS203XL (Ex10-family) implementation
│   ├── BarcodeReader/                   # Barcode scanning
│   ├── Battery/                         # Battery management
│   ├── BluetoothIC/                     # Bluetooth IC control
│   ├── Notification/                    # System notifications
│   ├── SiliconLabIC/                    # Silicon Lab IC support
│   ├── SystemInformation/               # Device/country/frequency information
│   └── Tools/                           # Utilities
└── Documents/                           # Hardware/protocol specs and internal notes
```

## Architecture

CSLibrary2026 uses a **transport abstraction layer** (`ITransport`) to support both Bluetooth LE and TCP/IP connections:

```
HighLevelInterface
├── rfid          — RFID operations (inventory, read/write, lock/kill)
├── barcode       — Barcode scanning
├── notification  — Battery, key events
└── _transport    — ITransport (BLETransport or TCPTransport)

ITransport
├── ConnectAsync()  — Connect via BLE or TCP
├── SendAsync()     — Send command data
├── Disconnect()    — Close connection
└── SetReceiveCallback() — Handle incoming data
```

Reader discovery is currently per interface (`DeviceFinder` for BLE, `NetFinder` for
TCP/IP). A unified search façade covering BLE, TCP/IP, Serial and USB is proposed in
`Documents/READER_DISCOVERY.md` (design proposal, not yet implemented).

## BLE HAL selection

The BLE communication layer is implemented by three interchangeable HALs. They provide the
same functionality and are mutually exclusive — only one of them is compiled into a given
binary, because they define the same `HighLevelInterface` partial members and the same
`CSLibrary.DeviceFinder` API.

| Application | Target framework | BLE HAL | Available in NuGet? |
|---|---|---|---|
| WinForms | .NET Framework 4.8 (`net48`) | `Source/HAL/btframework` (WCL / 32feet) | **No** — build from source |
| Xamarin (Android / iOS / UWP) | `netstandard2.0` / `netstandard2.1` | `Source/HAL/Plugin.BLE` (Plugin.BLE 3.0.0) | Yes |
| MAUI (Android / iOS / Windows) | `net10.0` | `Source/HAL/Plugin.BLE` (Plugin.BLE 3.0.0) | Yes |
| WinForms | .NET 10 (`net10.0-windows10.0.19041.0`) | `Source/HAL/WinFormsBLE` (Windows Runtime GATT) | **No** — build from source |

The published NuGet package therefore contains the Plugin.BLE flavours only:

```
lib/netstandard2.0/   lib/netstandard2.1/   lib/net10.0/
lib/net10.0-android36.0/   lib/net10.0-ios26.0/
lib/net10.0-windows10.0.19041/     (Plugin.BLE - for the Windows head of a MAUI app)
```

The `net48` flavour is excluded from the package (`IncludeBuildOutput=false` and
`SuppressDependenciesWhenPacking=true` for that target framework), so the package
contains no `lib/net48` folder and no `.NETFramework4.8` dependency group.

## Publishing to NuGet

The publishing assets live in [`nuget/`](nuget/README.md):

```powershell
# Build the package only
pwsh -File nuget/scripts/Publish-NuGet.ps1 -Version 2.0.0-beta.3 -SkipPush

# Build and push to NuGet.org
pwsh -File nuget/scripts/Publish-NuGet.ps1 -Version 2.0.0-beta.3 -ApiKey $env:NUGET_API_KEY
```

```bash
./nuget/scripts/publish-nuget.sh 2.0.0-beta.3
```

The GitHub Actions workflow [`.github/workflows/publish-nuget.yml`](.github/workflows/publish-nuget.yml)
publishes automatically on a GitHub release: it packs the Linux and macOS target
frameworks in two jobs (Apple targets only build on macOS), merges them into one
package and pushes it to NuGet.org. It needs a `NUGET_API_KEY` repository secret.

### Building the WinForms flavours from source

Both WinForms flavours are used by downloading the source and switching the HAL in
`CSLibrary2026.csproj`, then rebuilding:

**WinForms on .NET Framework 4.8** — `btframework` (already the default for `net48`):

```xml
<ItemGroup Condition=" '$(TargetFramework)' == 'net48' ">
  <Compile Remove="Source/HAL/Plugin.BLE/**" />
  <Compile Include="Source/HAL/btframework/**/*.cs" />
  <Compile Remove="Source/Transport/BLETransport.cs" />
  <!-- WCL references: Source\HAL\btframework\wclBluetoothFramework.dll, wclCommon.dll,
       wclCommunication.dll -->
</ItemGroup>
```

**WinForms on .NET 10** — `WinFormsBLE` (Windows Runtime GATT, no third party dependency):

```xml
<ItemGroup Condition=" '$(TargetFramework)' == 'net10.0-windows10.0.19041.0' ">
  <Compile Remove="Source/HAL/Plugin.BLE/**" />
  <Compile Include="Source/HAL/WinFormsBLE/**/*.cs" />
  <Compile Remove="Source/Transport/BLETransport.cs" />
</ItemGroup>
```

Note that a MAUI application and a WinForms application on .NET 10 resolve to the same
`net10.0-windows*` target framework, so a single NuGet package cannot serve the
`WinFormsBLE` HAL to WinForms while serving `Plugin.BLE` to MAUI. WinForms on .NET 10
therefore builds the library from source.

## License

MIT License. See [LICENSE](LICENSE) for details.

## Links

- **GitHub Repository:** https://github.com/cslrfid/CSLibrary2026
- **NuGet Package:** https://www.nuget.org/packages/CSLibrary2026
