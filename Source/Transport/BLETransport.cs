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
using System.Threading.Tasks;
using Plugin.BLE.Abstractions.Contracts;

namespace CSLibrary
{
    using static RFIDDEVICE;

    /// <summary>
    /// Bluetooth LE transport using Plugin.BLE.
    /// Wraps the BLE connection, GATT service discovery, and read/write characteristics
    /// previously embedded in HighLevelInterface via CodeFileBLE.cs.
    /// </summary>
    public class BLETransport : ITransport
    {
        private IAdapter _adapter;
        private IDevice _device;
        private IService _service;
        private IService _serviceDeviceInfo;
        private ICharacteristic _characteristicWrite;
        private ICharacteristic _characteristicUpdate;
        private ICharacteristic _characteristicDeviceInfoRead;
        private MODEL _deviceType = MODEL.UNKNOWN;
        private Action<byte[]> _receiveCallback;
        private bool _isConnected;

        // Connection metadata: BLE MAC address when connected
        public string ConnectionInfo { get; private set; }

        public bool IsConnected => _isConnected;

        public async Task<bool> ConnectAsync(object[] args)
        {
            if (args == null || args.Length < 3)
                throw new ArgumentException("BLETransport.ConnectAsync requires (IAdapter, IDevice, MODEL)");

            var adapter = args[0] as IAdapter;
            var device = args[1] as IDevice;
            var model = (MODEL)args[2];

            if (adapter == null || device == null)
                throw new ArgumentException("BLETransport: invalid argument types");

            _adapter = adapter;
            _device = device;
            _deviceType = model;
            _isConnected = false;

            // Discover the primary service UUID based on model
            // CS108 and CS468 both use the CSL CS108 BLE service UUID
            switch (_deviceType)
            {
                case MODEL.CS108:
                case MODEL.CS468:
                    _service = await device.GetServiceAsync(Guid.Parse("00009800-0000-1000-8000-00805f9b34fb"));
                    break;

                case MODEL.CS710S:
                case MODEL.CS203XL:
                    try { await device.RequestMtuAsync(255); } catch { /* ignore MTU failure */ }
                    _service = await device.GetServiceAsync(Guid.Parse("00009802-0000-1000-8000-00805f9b34fb"));
                    break;
            }

            if (_service == null)
                return false;

            // Device Information service (standard BLE)
            _serviceDeviceInfo = await device.GetServiceAsync(Guid.Parse("0000180a-0000-1000-8000-00805f9b34fb"));

            // Set up connection-loss handler
            _adapter.DeviceConnectionLost -= OnDeviceConnectionLost;
            _adapter.DeviceConnectionLost += OnDeviceConnectionLost;

            // Discover characteristics
            try
            {
                _characteristicWrite = await _service.GetCharacteristicAsync(Guid.Parse("00009900-0000-1000-8000-00805f9b34fb"));
                _characteristicUpdate = await _service.GetCharacteristicAsync(Guid.Parse("00009901-0000-1000-8000-00805f9b34fb"));

                if (_serviceDeviceInfo != null)
                {
                    try
                    {
                        _characteristicDeviceInfoRead = await _serviceDeviceInfo.GetCharacteristicAsync(
                            Guid.Parse("00002a23-0000-1000-8000-00805f9b34fb"));

                        await _characteristicDeviceInfoRead.ReadAsync();

                        if (_characteristicDeviceInfoRead?.Value != null && _characteristicDeviceInfoRead.Value.Length == 8)
                        {
                            ConnectionInfo =
                                _characteristicDeviceInfoRead.Value[7].ToString("X2") +
                                _characteristicDeviceInfoRead.Value[6].ToString("X2") +
                                _characteristicDeviceInfoRead.Value[5].ToString("X2") +
                                _characteristicDeviceInfoRead.Value[2].ToString("X2") +
                                _characteristicDeviceInfoRead.Value[1].ToString("X2") +
                                _characteristicDeviceInfoRead.Value[0].ToString("X2");
                        }
                    }
                    catch { /* Device info not available on this device */ }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BLETransport: Cannot set characteristics: " + ex.Message);
            }

            // Wire up receive callback
            _characteristicUpdate.ValueUpdated -= OnCharacteristicValueUpdated;
            _characteristicUpdate.ValueUpdated += OnCharacteristicValueUpdated;

            // Subscribe for notifications
            await _characteristicUpdate.StartUpdatesAsync();

            _isConnected = true;
            return true;
        }

        public async Task<int> SendAsync(byte[] data)
        {
            if (!_isConnected || _characteristicWrite == null)
                return -1;

            return await _characteristicWrite.WriteAsync(data);
        }

        public void Disconnect()
        {
            CleanupConnection();
        }

        public void SetReceiveCallback(Action<byte[]> callback)
        {
            _receiveCallback = callback;
        }

        private void OnCharacteristicValueUpdated(object sender, Plugin.BLE.Abstractions.EventArgs.CharacteristicUpdatedEventArgs e)
        {
            try
            {
                var data = e.Characteristic.Value;
                if (data == null)
                    return;

                Debug.WriteBytes("BLE recv", data);
                _receiveCallback?.Invoke(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("BLETransport recv error: " + ex.Message);
            }
        }

        private void OnDeviceConnectionLost(object sender, Plugin.BLE.Abstractions.EventArgs.DeviceErrorEventArgs e)
        {
            if (e.Device.Id == _device?.Id)
            {
                CleanupConnection();
                _receiveCallback?.Invoke(null); // signal connection lost
            }
        }

        private async void CleanupConnection()
        {
            try
            {
                if (_characteristicUpdate != null)
                {
                    _characteristicUpdate.ValueUpdated -= OnCharacteristicValueUpdated;
                    await _characteristicUpdate.StopUpdatesAsync();
                }

                if (_adapter != null)
                    _adapter.DeviceConnectionLost -= OnDeviceConnectionLost;
            }
            catch { }

            _characteristicWrite = null;
            _characteristicUpdate = null;
            _service = null;
            _serviceDeviceInfo = null;
            _characteristicDeviceInfoRead = null;

            try
            {
                if (_device?.State == Plugin.BLE.Abstractions.DeviceState.Connected)
                {
                    await _adapter?.DisconnectDeviceAsync(_device);
                }
            }
            catch { }

            _device = null;
            _isConnected = false;
        }
    }
}
