# T-18 — Implement WinForms BLE HAL (Windows.Devices.Bluetooth)

**Status:** Pending
**Phase:** 4 — WinForms BLE HAL
**Priority:** 🟡 Medium
**Estimated:** 1–2 weeks
**Constitution:** §7 (HAL Extensibility Rules), §13.3 of spec
**Spec reference:** Future Extensibility §13.3 (Windows.Devices.Bluetooth for WinForms .NET 10 Windows)

---

## Objective

Activate `Source/HAL/WinFormsBLE/` for `net10.0-windows` by implementing `WinRTTransport.cs` using `Windows.Devices.Bluetooth` APIs. This unblocks WinForms/WPF desktop applications on Windows 10/11.

---

## Architecture Overview

```
Source/HAL/WinFormsBLE/
├── WinRTTransport.cs     ← NEW: ITransport implementation using WinRT BLE APIs
├── README.md            ← Platform requirements and activation instructions
├── DeviceFinder.cs       ← Already exists; may need WinRT adjustments
└── CodeFileBLE.cs        ← Already excluded; partial class to HighLevelInterface
```

The existing `CodeFileBLE.cs` in WinFormsBLE is excluded from the build. It will be replaced by `WinRTTransport.cs`.

---

## Step 1 — WinRTTransport.cs

Create `Source/HAL/WinFormsBLE/WinRTTransport.cs`:

```csharp
#if NET10_0_OR_GREATER && WINDOWS

using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace CSLibrary
{
    using static RFIDDEVICE;

    /// <summary>
    /// ITransport implementation using Windows.Devices.Bluetooth for WinForms/WPF
    /// desktop applications targeting .NET 10 on Windows 10 1809+.
    /// </summary>
    public class WinRTTransport : ITransport
    {
        private GattDeviceService? _primaryService;
        private GattCharacteristic? _characteristicWrite;
        private GattCharacteristic? _characteristicUpdate;
        private BluetoothLEDevice? _device;
        private MODEL _deviceType = MODEL.UNKNOWN;
        private Action<byte[]>? _receiveCallback;
        private bool _isConnected;

        public bool IsConnected => _isConnected;

        public string ConnectionInfo =>
            _device?.BluetoothAddress.ToString("X12") ?? string.Empty;

        public async Task<bool> ConnectAsync(object[] args)
        {
            if (args == null || args.Length < 3)
                throw new ArgumentException(
                    "WinRTTransport.ConnectAsync requires (ulong bluetoothAddress, MODEL model)");

            var bluetoothAddress = (ulong)args[0];
            var model = (MODEL)args[1];

            switch (model)
            {
                case MODEL.CS108:
                case MODEL.CS468:
                case MODEL.CS710S:
                case MODEL.CS203XL:
                    break;
                default:
                    return false;
            }

            _deviceType = model;
            _device = await BluetoothLEDevice.FromBluetoothAddressAsync(bluetoothAddress);
            if (_device == null) return false;

            // Determine service UUID based on model
            var serviceUuid = model switch
            {
                MODEL.CS108 or MODEL.CS468 =>
                    Guid.Parse("00009800-0000-1000-8000-00805f9b34fb"),
                MODEL.CS710S or MODEL.CS203XL =>
                    Guid.Parse("00009802-0000-1000-8000-00805f9b34fb"),
                _ => Guid.Empty
            };

            _primaryService = _device.GetGattService(serviceUuid);
            if (_primaryService == null) return false;

            // Discover characteristics
            var characteristics = _primaryService.GetCharacteristics(serviceUuid);
            foreach (var characteristic in characteristics)
            {
                if (characteristic.Uuid == Guid.Parse("00009900-0000-1000-8000-00805f9b34fb"))
                    _characteristicWrite = characteristic;
                else if (characteristic.Uuid == Guid.Parse("00009901-0000-1000-8000-00805f9b34fb"))
                    _characteristicUpdate = characteristic;
            }

            if (_characteristicUpdate == null) return false;

            // Subscribe to ValueChanged for notifications
            _characteristicUpdate.ValueChanged += OnCharacteristicValueChanged;
            await _characteristicUpdate.WriteClientCharacteristicConfigurationDescriptorAsync(
                GattClientCharacteristicConfigurationDescriptorValue.Notify);

            _isConnected = true;
            return true;
        }

        public async Task<int> SendAsync(byte[] data)
        {
            if (!_isConnected || _characteristicWrite == null)
                return -1;

            var result = await _characteristicWrite.WriteValueAsync(
                data.AsBuffer(),
                GattWriteOption.WriteWithoutResponse);
            return result == GattCommunicationStatus.Success ? data.Length : -1;
        }

        public void Disconnect()
        {
            CleanupConnection();
        }

        public void SetReceiveCallback(Action<byte[]> callback)
        {
            _receiveCallback = callback;
        }

        private void OnCharacteristicValueChanged(
            GattCharacteristic sender,
            GattValueChangedEventArgs args)
        {
            var data = args.CharacteristicValue.ToArray();
            _receiveCallback?.Invoke(data);
        }

        private void CleanupConnection()
        {
            if (_characteristicUpdate != null)
            {
                _characteristicUpdate.ValueChanged -= OnCharacteristicValueChanged;
            }
            _device?.Dispose();
            _primaryService?.Dispose();
            _device = null;
            _primaryService = null;
            _characteristicWrite = null;
            _characteristicUpdate = null;
            _isConnected = false;
        }
    }
}

#endif
```

---

## Step 2 — Create WinFormsBLE README.md

```markdown
# WinFormsBLE HAL — Windows.Devices.Bluetooth Backend

## Platform Requirements

- Windows 10 version 1809 (Build 17763) or later
- .NET 10.0 targeting `net10.0-windows`
- `<UseWindowsForms>true</UseWindowsForms>` in the consuming application's `.csproj`

## Activation

This HAL is activated automatically when building for `net10.0-windows`.

The `WinRTTransport.cs` uses `#if NET10_0_OR_GREATER && WINDOWS` to ensure it only
compiles on the Windows .NET 10 target.

## BLE Service UUIDs

Same as Plugin.BLE HAL:
- CS108/CS468: `00009800-0000-1000-8000-00805f9b34fb`
- CS710S/CS203XL: `00009802-0000-1000-8000-00805f9b34fb`

## Usage from WinForms

```csharp
// WinForms app (.csproj targets net10.0-windows)
var reader = new RFIDReader();

// WinRTTransport uses BluetoothAddress instead of IDevice
ulong btAddress = 0x3CA308123456; // reader's Bluetooth address
await reader.ConnectAsync(btAddress, MODEL.CS108);
```

## Architecture Notes

- Uses `BluetoothLEDevice.FromBluetoothAddressAsync()` for connection
- Uses `GattCharacteristic.WriteValueAsync()` for outbound commands
- Uses `GattCharacteristic.ValueChanged` event for inbound notifications
- Connection metadata is the Bluetooth address as a 12-digit hex string
```

---

## Step 3 — Update .csproj

```xml
<!-- In CSLibrary2026.csproj -->

<!-- For net10.0-windows: remove WinFormsBLE from the exclusion list -->
<ItemGroup Condition="'$(TargetFramework)' == 'net10.0-windows'">
  <!-- Only exclude HALs that should never activate -->
  <Compile Remove="Source/HAL/Acr.ble/**" />
  <Compile Remove="Source/HAL/btframework/**" />
  <Compile Remove="Source/HAL/UWP/**" />
  <Compile Remove="Source/HAL/MvvmCross.Plugin.BLE/**" />
  <!-- WinFormsBLE is included for net10.0-windows -->
  <Compile Remove="Source/HAL/WinFormsBLE/**" Condition="'$(TargetFramework)' != 'net10.0-windows'" />
</ItemGroup>
```

Note: WinFormsBLE may need a WinRT reference added. If the project does not already reference WinRT types, add:

```xml
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0-windows'">
  <EnableWindowsTargeting>true</EnableWindowsTargeting>
</PropertyGroup>
```

---

## Step 4 — Commit

```bash
git add Source/HAL/WinFormsBLE/WinRTTransport.cs
git add Source/HAL/WinFormsBLE/README.md
git add CSLibrary2026.csproj
git commit -m "feat(winforms): implement WinRTTransport for Windows.Devices.Bluetooth

- New WinRTTransport : ITransport using Windows.Devices.Bluetooth
  - ConnectAsync(bluetoothAddress, MODEL) — WinForms consumer use
  - SendAsync, Disconnect, SetReceiveCallback
- WinFormsBLE/README.md documents platform requirements and usage
- .csproj activates WinFormsBLE for net10.0-windows

Constitutes: constitution §7 (HAL extensibility), plan T-18"
```

---

## Definition of Done

- [ ] `Source/HAL/WinFormsBLE/WinRTTransport.cs` exists and implements `ITransport`
- [ ] `Source/HAL/WinFormsBLE/README.md` documents platform requirements
- [ ] `.csproj` is updated to include WinFormsBLE for `net10.0-windows`
- [ ] `dotnet build -c Release` passes for `net10.0-windows`
- [ ] Functional review PR approved with manual test notes
