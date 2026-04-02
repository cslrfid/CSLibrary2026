# CSLibrary2026

CSL RFID Reader Library for .NET — supporting Bluetooth LE communication with CSL RFID readers.

## Supported Readers

| Reader | Connection | Description |
|--------|-----------|-------------|
| **CS108** | Bluetooth LE | Handheld UHF RFID Reader |
| **CS710S** | Bluetooth LE | Fixed UHF RFID Reader |
| **CS203XL** | TCP/IP | Fixed UHF RFID Reader |

## Features

- Bluetooth LE (BLE) communication via [Plugin.BLE](https://github.com/xabre/xamarin-bluetooth-le)
- Multi-reader support (CS108, CS710S, CS203XL)
- Barcode scanning integration
- Battery & notification management
- Rich RFID operations: inventory, read/write, lock/kill, filtering

## Supported Platforms

- **.NET Standard 2.0** — Xamarin.Forms, .NET MAUI, and other .NET Standard compatible frameworks
- **.NET 10** — .NET 10+ applications

## Installation

```bash
dotnet add package CSLibrary2026
```

Or add to your `.csproj`:

```xml
<PackageReference Include="CSLibrary2026" Version="0.0.1" />
```

## Quick Start

### 1. Connect to a Reader

```csharp
using CSLibrary;

// Create the RFID reader instance
var reader = new CSLibrary.RFIDReader.ClassRFID();

// Discover devices via Bluetooth LE
var adapter = new Plugin.BLE.Adapter.Adapter();
var devices = await adapter.DiscoverDevicesAsync();

// Connect to the first device
await reader.ConnectAsync(adapter, devices[0]);
```

### 2. Subscribe to Events

```csharp
// RFID tag inventory callback
reader.OnAsyncCallback += (sender, e) =>
{
    if (e.type == CSLibrary.Constants.CallbackType.TAG_RANGING)
    {
        Console.WriteLine($"EPC: {e.info.epc}");
    }
};

// Reader state change callback
reader.OnStateChanged += (sender, e) =>
{
    Console.WriteLine($"State: {e.state}");
};
```

### 3. Start Inventory

```csharp
// Set power level (0–3000 = 0–30.00 dBm)
reader.SetPowerLevel(3000);

// Start inventory
reader.StartOperation(CSLibrary.RFIDReader.Operation.TAG_RANGING);

// Stop after some time
reader.StopOperation();
```

## API Overview

### Connection

| Method | Description |
|--------|-------------|
| `ConnectAsync(IAdapter, IDevice)` | Connect to a BLE reader |
| `DisconnectAsync()` | Disconnect from the reader |

### Battery & Notifications

| Method | Description |
|--------|-------------|
| `GetCurrentBatteryLevel()` | Get battery level |
| `ClearEventHandler()` | Clear all callback events |

**Events:**
- `OnVoltageEvent` — Battery level changes
- `OnKeyEvent` — Hotkey presses

### RFID Operations

**Start operations:**
```csharp
reader.StartOperation(Operation.TAG_RANGING);       // Inventory
reader.StartOperation(Operation.TAG_READ);           // Read tags
reader.StartOperation(Operation.TAG_READ_TID);       // Read TID bank
reader.StartOperation(Operation.TAG_READ_USER);      // Read USER bank
reader.StartOperation(Operation.TAG_WRITE);         // Write tags
reader.StartOperation(Operation.TAG_LOCK);           // Lock tag
reader.StartOperation(Operation.TAG_KILL);           // Kill tag
reader.StopOperation();                             // Stop continuous operation
```

**Power & Frequency:**
```csharp
reader.SetPowerLevel(uint pwrlevel);           // Set power (0–3000)
reader.GetActiveMaxPowerLevel();               // Get max power
reader.SetCurrentLinkProfile(uint profile);    // Set link profile
reader.SetCountry(string countryName);         // Set country frequency
reader.SetCountry(string countryName, int ch);// Set country + fixed channel
```

**Singulation Algorithms:**
```csharp
reader.SetCurrentSingulationAlgorithm(SingulationAlgorithm.FIXEDQ);
reader.SetFixedQParms(FixedQParms parms);
reader.SetDynamicQParms(DynamicQParms parms);
reader.SetTagGroup(TagGroup tagGroup);
reader.SetPostMatchCriteria(SingulationCriterion[] criteria);
```

**Callback Events:**
- `OnAsyncCallback` — Inventory / tag data (`CallbackType.TAG_RANGING`, `TAG_SEARCHING`)
- `OnAccessCompleted` — Read/write/lock result
- `OnStateChanged` — RFID reader state (`RFState.IDLE`, `RFState.BUSY`, `INITIALIZATION_COMPLETE`)

### Barcode Scanner

```csharp
var barcode = new CSLibrary.Barcode.ClassBarCode();
barcode.OnCapturedNotify += (sender, e) => Console.WriteLine($"Barcode: {e.Barcode}");
barcode.Start();
barcode.Stop();
barcode.FactoryReset();
```

**Events:**
- `OnCapturedNotify` — Captured barcode data
- `OnStateChanged` — Scanner state (`BarcodeState.IDLE`, `BUSY`)

### Reader Properties

```csharp
reader.SelectedChannel;           // Current channel
reader.SelectedRegionCode;        // Current region code
reader.IsHoppingChannelOnly;      // Hopping-only mode
reader.IsFixedChannelOnly;        // Fixed-channel-only mode
reader.DeviceType;                // Reader type (Machine enum)
reader.ChipSetID;                 // Chipset ID
```

## Project Structure

```
CSLibrary2026/
├── Source/
│   ├── CSLibrary.cs                     # Main library entry point
│   ├── BluetoothProtocol/               # BLE protocol (send/receive/connect)
│   ├── BluetoothIC/                     # Bluetooth IC wrapper
│   ├── BarcodeReader/                   # Barcode scanning (Class, Constants, Events, Structures)
│   ├── Battery/                         # Battery management
│   ├── Notification/                    # System notifications
│   ├── HAL/
│   │   ├── Plugin.BLE/                  # Primary BLE backend (active)
│   │   ├── MvvmCross.Plugin.BLE/        # MvvmCross BLE backend
│   │   └── btframework/                 # wclBluetoothFramework DLLs
│   └── RFIDReader/
│       └── CSLUnifiedAPI/
│           └── Basic_API/
│               ├── CS108/                # CS108 reader API
│               └── CS710S/              # CS710S reader API
└── Properties/
```

## License

MIT License. See [LICENSE](LICENSE) for details.

## Links

- **GitHub Repository:** https://github.com/cslrfid/CSLibrary2026
- **NuGet Package:** https://www.nuget.org/packages/CSLibrary2026
