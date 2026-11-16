/*
Copyright (c) 2018-2026 Convergence Systems Limited

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace CSLibrary
{
    using static RFIDDEVICE;
    using Constants;

    public partial class HighLevelInterface
    {
        // =====================================================================
        // WinFormsBLE implementation — Windows.Devices.Bluetooth async API
        // =====================================================================

        #region Fields

        private BluetoothLEDevice _bleDevice;
        private GattDeviceService _serviceWrite;
        private GattDeviceService _serviceUpdate;
        private GattDeviceService _serviceDeviceInfo;
        private GattCharacteristic _characteristicWrite;
        private GattCharacteristic _characteristicUpdate;
        private GattCharacteristic _characteristicDeviceInfoRead;

        // Note: this HAL talks to the reader through the WinRT GATT stack
        // (Windows.Devices.Bluetooth) and therefore does not use the Plugin.BLE based
        // BLETransport. Keep the WinRT handles above as the single source of truth.

        #endregion


        /// <summary>
        /// Connect via BLE using Windows.Devices.Bluetooth async API.
        /// Called by the WinForms app layer after device discovery.
        /// </summary>
        public async Task<bool> ConnectAsync(BluetoothLEDevice device, MODEL deviceType)
        {
            if (_readerState != READERSTATE.DISCONNECT)
                return false;

            if (device == null)
                return false;

            this._deviceType = deviceType;

            // Subscribe to connection status changes
            device.ConnectionStatusChanged += OnConnectionStatusChanged;

            // Discover primary service based on model
            switch (_deviceType)
            {
                case MODEL.CS108:
                case MODEL.CS468:
                    _serviceUpdate = device.GetGattService(Guid.Parse("00009800-0000-1000-8000-00805f9b34fb"));
                    break;

                case MODEL.CS710S:
                case MODEL.CS203XL:
                    // Note: the Windows Runtime GATT stack has no MTU request API
                    // (RequestMtuAsync is a Plugin.BLE extension), the negotiated MTU is
                    // handled by the stack itself.
                    _serviceUpdate = device.GetGattService(Guid.Parse("00009802-0000-1000-8000-00805f9b34fb"));
                    break;
            }

            if (_serviceUpdate == null)
            {
                Debug.WriteLine("[WinFormsBLE] Primary service not found");
                return false;
            }

            // Device Information service (standard BLE)
            _serviceDeviceInfo = device.GetGattService(Guid.Parse("0000180a-0000-1000-8000-00805f9b34fb"));

            // Discover characteristics
            try
            {
                // Write characteristic (host → device)
                var writeChars = _serviceUpdate.GetCharacteristics(
                    Guid.Parse("00009900-0000-1000-8000-00805f9b34fb"));
                if (writeChars.Count > 0)
                    _characteristicWrite = writeChars[0];

                // Update/notify characteristic (device → host)
                var updateChars = _serviceUpdate.GetCharacteristics(
                    Guid.Parse("00009901-0000-1000-8000-00805f9b34fb"));
                if (updateChars.Count > 0)
                    _characteristicUpdate = updateChars[0];

                // Device info - read MAC address
                if (_serviceDeviceInfo != null)
                {
                    var diChars = _serviceDeviceInfo.GetCharacteristics(
                        Guid.Parse("00002a23-0000-1000-8000-00805f9b34fb"));
                    if (diChars.Count > 0)
                        _characteristicDeviceInfoRead = diChars[0];
                }

                // Read MAC address from Device Information
                if (_characteristicDeviceInfoRead != null)
                {
                    var result = await _characteristicDeviceInfoRead.ReadValueAsync(
                        BluetoothCacheMode.Uncached);
                    if (result.Status == GattCommunicationStatus.Success &&
                        result.Value.Length == 8)
                    {
                        var bytes = BufferToArray(result.Value);
                        _MacAdd =
                            bytes[7].ToString("X2") +
                            bytes[6].ToString("X2") +
                            bytes[5].ToString("X2") +
                            bytes[2].ToString("X2") +
                            bytes[1].ToString("X2") +
                            bytes[0].ToString("X2");
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[WinFormsBLE] Cannot set characteristics: " + ex.Message);
            }

            // Subscribe for value changed notifications
            if (_characteristicUpdate != null)
            {
                _characteristicUpdate.ValueChanged -= BLE_Recv;
                _characteristicUpdate.ValueChanged += BLE_Recv;

                var notifyResult = await _characteristicUpdate.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.Notify);
            }

            _bleDevice = device;
            _readerState = READERSTATE.IDLE;

            // Start watchdog timer
            BTTimer = new Timer(TimerFunc, this, 0, 1000);

            HardwareInit();

            return true;
        }

        /// <summary>
        /// Disconnect BLE — cleans up all GATT resources.
        /// </summary>
        /// <remarks>
        /// Named BLE_DisconnectAsync() to match the other HALs: the shared core owns the
        /// unified DisconnectAsync() and routes to the HAL implementation.
        /// </remarks>
        public void BLE_DisconnectAsync()
        {
            try
            {
                if (_readerState != READERSTATE.DISCONNECT)
                    BARCODEPowerOff();

                if (_characteristicUpdate != null)
                {
                    _characteristicUpdate.ValueChanged -= BLE_Recv;
                    _ = _characteristicUpdate.WriteClientCharacteristicConfigurationDescriptorAsync(
                        GattClientCharacteristicConfigurationDescriptorValue.None);
                }

                if (_bleDevice != null)
                {
                    _bleDevice.ConnectionStatusChanged -= OnConnectionStatusChanged;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[WinFormsBLE] Disconnect error: " + ex.Message);
            }

            _characteristicWrite = null;
            _characteristicUpdate = null;
            _characteristicDeviceInfoRead = null;
            _serviceWrite = null;
            _serviceUpdate = null;
            _serviceDeviceInfo = null;

            try
            {
                if (_bleDevice != null)
                {
                    _bleDevice.Dispose();
                }
            }
            catch { }

            _bleDevice = null;
            _readerState = READERSTATE.DISCONNECT;
        }

        /// <summary>
        /// Copies a Windows Runtime buffer into a byte array.
        /// </summary>
        /// <remarks>
        /// Uses the WindowsRuntimeBufferExtensions of the Windows Runtime projection
        /// (System.Runtime.InteropServices.WindowsRuntime).
        /// </remarks>
        private static byte[] BufferToArray(Windows.Storage.Streams.IBuffer buffer)
        {
            if (buffer == null || buffer.Length == 0)
                return Array.Empty<byte>();

            return buffer.ToArray();
        }

        /// <summary>
        /// Write data to the BLE characteristic (send command to reader).
        /// </summary>
        private async Task<int> BLE_Send(byte[] data)
        {
            if (_characteristicWrite == null)
                return -1;

            try
            {
                // DataWriter.DetachBuffer() replaces the removed
                // Buffer.CreateFromArray() / DataWriter.FromBuffer() pair
                var writer = new Windows.Storage.Streams.DataWriter();
                writer.WriteBytes(data);
                var buffer = writer.DetachBuffer();

                var result = await _characteristicWrite.WriteValueAsync(
                    buffer,
                    GattWriteOption.WriteWithResponse);
                return result == GattCommunicationStatus.Success ? data.Length : -1;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[WinFormsBLE] BLE_Send error: " + ex.Message);
                return -1;
            }
        }

        /// <summary>
        /// Receive callback — called when BLE characteristic value changes (notification).
        /// </summary>
        private void BLE_Recv(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            try
            {
                var buffer = args.CharacteristicValue;
                if (buffer == null || buffer.Length == 0)
                    return;

                var data = BufferToArray(buffer);

                Debug.WriteBytes("WinFormsBLE recv", data);
                CharacteristicOnValueUpdated(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[WinFormsBLE] BLE_Recv error: " + ex.Message);
            }
        }

        /// <summary>
        /// Called when Bluetooth connection status changes.
        /// </summary>
        private void OnConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            if (sender == null || sender.ConnectionStatus != BluetoothConnectionStatus.Connected)
            {
                // Connection lost
                FireReaderStateChangedEvent(
                    new Events.OnReaderStateChangedEventArgs(null,
                        Constants.ReaderCallbackType.CONNECTION_LOST));
            }
        }

        private void ConnectLostAsync()
        {
            _readerState = READERSTATE.READYFORDISCONNECT;

            if (_characteristicUpdate != null)
                _characteristicUpdate.ValueChanged -= BLE_Recv;

            _characteristicWrite = null;
            _characteristicUpdate = null;
            _serviceWrite = null;
            _serviceUpdate = null;
            _serviceDeviceInfo = null;

            try
            {
                if (_bleDevice != null)
                {
                    _bleDevice.ConnectionStatusChanged -= OnConnectionStatusChanged;
                    _bleDevice.Dispose();
                }
            }
            catch { }

            _bleDevice = null;
            _readerState = READERSTATE.DISCONNECT;

            FireReaderStateChangedEvent(
                new Events.OnReaderStateChangedEventArgs(null,
                    Constants.ReaderCallbackType.CONNECTION_LOST));
        }

        async Task ClearConnection()
        {
            _readerState = READERSTATE.READYFORDISCONNECT;

            if (_characteristicUpdate != null)
            {
                _characteristicUpdate.ValueChanged -= BLE_Recv;
                await _characteristicUpdate.WriteClientCharacteristicConfigurationDescriptorAsync(
                    GattClientCharacteristicConfigurationDescriptorValue.None);
            }

            _characteristicWrite = null;
            _characteristicUpdate = null;
            _serviceWrite = null;
            _serviceUpdate = null;
            _serviceDeviceInfo = null;

            if (_bleDevice != null)
            {
                _bleDevice.ConnectionStatusChanged -= OnConnectionStatusChanged;
                _bleDevice.Dispose();
            }

            _bleDevice = null;
            _readerState = READERSTATE.DISCONNECT;
        }
    }
}
