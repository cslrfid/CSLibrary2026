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
- Multi-reader support across Rx000 and E710/E910 chipset families
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
| `DisconnectAsync()` | Disconnect from the reader |

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
│   ├── Transport/                       # Transport layer abstraction
│   │   ├── ITransport.cs                # Transport interface
│   │   ├── BLETransport.cs              # BLE transport (Plugin.BLE)
│   │   └── TCPTransport.cs              # TCP transport
│   ├── BluetoothProtocol/               # BLE protocol (send/receive/connect)
│   ├── HAL/
│   │   ├── Plugin.BLE/                  # BLE backend for MAUI
│   │   └── TCPIP/                       # TCP/IP backend for Windows desktop
│   ├── RFIDReader/
│   │   └── CSLUnifiedAPI/
│   │       └── Basic_API/
│   │           ├── CS108/               # CS108 / CS468 reader API (Rx000)
│   │           └── CS710S/              # CS710S / CS203XL reader API (E710/E910)
│   ├── BarcodeReader/                   # Barcode scanning
│   ├── Battery/                         # Battery management
│   └── Notification/                    # System notifications
└── Properties/
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

## License

MIT License. See [LICENSE](LICENSE) for details.

## Links

- **GitHub Repository:** https://github.com/cslrfid/CSLibrary2026
- **NuGet Package:** https://www.nuget.org/packages/CSLibrary2026
