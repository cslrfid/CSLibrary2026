/*
Copyright (c) 2018 Convergence Systems Limited

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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;

namespace CSLibrary
{
    using static RFIDDEVICE;
    using Constants;
    using System.Linq;

    public partial class HighLevelInterface
    {
        // for bluetooth Connection
        // for bluetooth Connectiond
        IAdapter _adapter;
        IDevice _device;
        IService _service;
        IService _serviceDeviceInfo;
        ICharacteristic _characteristicWrite;
        ICharacteristic _characteristicUpdate;
        ICharacteristic _characteristicDeviceInfoRead;
        // BLE transport — encapsulates BLE connection state
        private readonly BLETransport _bleTransport = new BLETransport();

        /// <summary>
        /// return error code
        /// </summary>
        /// <returns></returns>
        /// <summary>
        /// Returns the BLE transport instance for this connection.
        /// </summary>
        BLETransport BLE_Init()
        {
            return _bleTransport;
        }

        /// <summary>
        /// Connect via BLE — delegates to BLETransport.
        /// </summary>
        public async Task<bool> ConnectAsync(IAdapter adapter, IDevice device, MODEL deviceType)
        {
            if (_readerState != READERSTATE.DISCONNECT)
                return false;

            this._deviceType = deviceType;
            var args = new object[] { adapter, device, deviceType };
            var success = await _bleTransport.ConnectAsync(args);

            if (!success)
                return false;

            _transport = _bleTransport;
            _MacAdd = _bleTransport.ConnectionInfo;
            _sp._ConnectionMode = CONNECTIONMODE.BLUETOOTH;
            _readerState = READERSTATE.IDLE;

            // Wire BLE receive → HighLevelInterface packet processor
            _bleTransport.SetReceiveCallback(data => CharacteristicOnValueUpdated(data));

            BTTimer = new Timer(TimerFunc, this, 0, 1000);
            HardwareInit();

            return true;
        }

        /// <summary>
        /// Disconnect BLE — delegates to BLETransport.
        /// </summary>
        public void BLE_DisconnectAsync()
        {
            try
            {
                if (_readerState != READERSTATE.DISCONNECT)
                    BARCODEPowerOff();

                _bleTransport.Disconnect();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Disconnect error " + ex.Message);
            }
        }

        /// <summary>
        /// return error code
        /// </summary>
        /// <returns></returns>
        private async Task<int> BLE_Send (byte[] data)
        {
            return await _characteristicWrite.WriteAsync(data);
        }

        private async void BLE_Recv(object sender, CharacteristicUpdatedEventArgs characteristicUpdatedEventArgs)
        {
            try
            {
                byte[] data = characteristicUpdatedEventArgs.Characteristic.Value;
                if (data == null)
                    return;
 
                CSLibrary.Debug.WriteBytes("BT data received", data);
                CharacteristicOnValueUpdated(data);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Program execption error, please check!!! error message : " + ex.Message);
            }
        }

        private void CharacteristicOnWriteUpdated(object sender, CharacteristicUpdatedEventArgs characteristicUpdatedEventArgs)
        {
            CSLibrary.Debug.WriteBytes("BT: Write data success updated", characteristicUpdatedEventArgs.Characteristic.Value);
        }

        private void OnStateChanged(object sender, BluetoothStateChangedArgs e)
        {
        }

        private void OnDeviceConnectionLost(object sender, DeviceErrorEventArgs e)
        {
            if (e.Device.Id == _device.Id)
            {
                //DisconnectAsync();
                ConnectLostAsync();
            }
        }

        public async void ConnectLostAsync()
        {
            _readerState = READERSTATE.READYFORDISCONNECT;

            _characteristicUpdate.ValueUpdated -= BLE_Recv;
            _adapter.DeviceConnectionLost -= OnDeviceConnectionLost;

            _characteristicUpdate = null;
            _characteristicWrite = null;
            _service = null;

            try
            {

                if (_device.State == DeviceState.Connected)
                {
                    await _adapter.DisconnectDeviceAsync(_device);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Program execption error, please check!!! error message : " + ex.Message);
            }
            _device = null;

            _readerState = READERSTATE.DISCONNECT;

            FireReaderStateChangedEvent(new Events.OnReaderStateChangedEventArgs(null, Constants.ReaderCallbackType.CONNECTION_LOST));
        }

        async Task ClearConnection()
        {
            _readerState = READERSTATE.READYFORDISCONNECT;
            // Stop Timer;
            await _characteristicUpdate.StopUpdatesAsync();

            _characteristicUpdate.ValueUpdated -= BLE_Recv;
            _adapter.DeviceConnectionLost -= OnDeviceConnectionLost;

            _characteristicUpdate = null;
            _characteristicWrite = null;
            _service = null;

            try
            {
                if (_device.State == DeviceState.Connected)
                {
                    await _adapter.DisconnectDeviceAsync(_device);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Program execption error, please check!!! error message : " + ex.Message);
            }
            _device = null;

            _readerState = READERSTATE.DISCONNECT;
        }

    }
}
