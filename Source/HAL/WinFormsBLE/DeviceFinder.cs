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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace CSLibrary
{
    using static RFIDDEVICE;

    /// <summary>
    /// DeviceFinder implementation for WinForms/WPF using Windows.Devices.Bluetooth
    /// and Windows.Devices.Enumeration (WinRT APIs).
    /// Provides async device discovery for CSL RFID readers via BLE.
    /// </summary>
    public partial class DeviceFinder
    {
        // =====================================================================
        // WinFormsBLE implementation — uses Windows.Devices.Bluetooth + async
        // =====================================================================

        #region Fields

        private static DeviceWatcher _deviceWatcher;
        private static CancellationTokenSource _scanTokenSource;
        private static bool _isScanning;
        private static int _timeoutMs = 5000;
        private static Timer _scanTimer;
        private static readonly List<DeviceInformation> _devices = new List<DeviceInformation>();

        // Events
        private static event EventHandler<DeviceFoundEventArgs> _onDeviceFound;
        private static event EventHandler<SearchCompletedEventArgs> _onSearchCompleted;

        #endregion

        #region Public API

        /// <summary>
        /// Start scanning for CSL RFID readers using Windows Bluetooth LE API.
        /// </summary>
        /// <param name="timeoutMs">Scan duration in ms (default: 5000)</param>
        public static void StartDeviceSearch(int timeoutMs = 5000)
        {
            if (_isScanning)
                StopDeviceSearch();

            _timeoutMs = timeoutMs;
            _devices.Clear();

            _scanTokenSource = new CancellationTokenSource();
            _isScanning = true;

            // Use DeviceWatcher to find all Bluetooth LE devices
            string aqsFilter = "System.Devices.Aep.ProtocolId:={bb7bb05e-5972-42b5-94ff-8465b1f172ea} AND System.Devices.Aep.IsConnected:=False";
            _deviceWatcher = DeviceInformation.CreateWatcher(aqsFilter, null, DeviceWatcherStatus鞋.None);

            _deviceWatcher.Added += OnDeviceAdded;
            _deviceWatcher.Updated += OnDeviceUpdated;
            _deviceWatcher.Removed += OnDeviceRemoved;
            _deviceWatcher.EnumerationCompleted += OnEnumerationCompleted;

            _scanTimer = new Timer(StopSearchOnTimeout, null, _timeoutMs, Timeout.Infinite);

            try
            {
                _deviceWatcher.Start();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceFinder] StartScanning failed: {ex.Message}");
                CleanupScan();
            }
        }

        /// <summary>
        /// Stop the current device search.
        /// </summary>
        public static void StopDeviceSearch()
        {
            if (!_isScanning)
                return;

            try
            {
                _scanTokenSource?.Cancel();
                _deviceWatcher?.Stop();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceFinder] StopScanning failed: {ex.Message}");
            }
            finally
            {
                CleanupScan();
            }
        }

        /// <summary>
        /// Returns a read-only list of all devices found in the current search.
        /// </summary>
        public static IReadOnlyList<DeviceInformation> GetDevices() => _devices.AsReadOnly();

        /// <summary>
        /// Clears the list of found devices.
        /// </summary>
        public static void ClearDevices() => _devices.Clear();

        /// <summary>
        /// Fires when a CSL RFID reader is discovered.
        /// </summary>
        public static event EventHandler<DeviceFoundEventArgs> OnDeviceFound
        {
            add => _onDeviceFound += value;
            remove => _onDeviceFound -= value;
        }

        /// <summary>
        /// Fires when the search completes (timeout or user stop).
        /// </summary>
        public static event EventHandler<SearchCompletedEventArgs> OnSearchCompleted
        {
            add => _onSearchCompleted += value;
            remove => _onSearchCompleted -= value;
        }

        /// <summary>
        /// Whether a scan is currently in progress.
        /// </summary>
        public static bool IsScanning => _isScanning;

        #endregion

        #region Private Methods

        private static async void OnDeviceAdded(DeviceWatcher sender, DeviceInformation args)
        {
            try
            {
                var deviceInfo = args;

                // Try to get the Bluetooth LE device to inspect services
                BluetoothLEDevice bleDevice = null;
                try
                {
                    bleDevice = await BluetoothLEDevice.FromIdAsync(deviceInfo.Id);
                }
                catch
                {
                    // May fail if device is not accessible
                }

                if (bleDevice != null)
                {
                    var model = DetectDeviceModel(bleDevice);

                    if (model != MODEL.UNKNOWN)
                    {
                        var info = new DeviceInformation
                        {
                            Id = deviceInfo.Id,
                            Name = deviceInfo.Name ?? "Unknown",
                            DeviceType = model,
                            NativeDevice = bleDevice,
                            Rssi = 0  // RSSI requires separate inquiry
                        };

                        lock (_devices)
                        {
                            if (_devices.Any(d => d.Id == info.Id))
                                return;
                            _devices.Add(info);
                        }

                        _onDeviceFound?.Invoke(null, new DeviceFoundEventArgs(info));
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceFinder] OnDeviceAdded error: {ex.Message}");
            }
        }

        private static void OnDeviceUpdated(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            // Update device properties if needed
        }

        private static void OnDeviceRemoved(DeviceWatcher sender, DeviceInformationUpdate args)
        {
            lock (_devices)
            {
                _devices.RemoveAll(d => d.Id == args.Id);
            }
        }

        private static void OnEnumerationCompleted(DeviceWatcher sender, object args)
        {
            if (_isScanning)
            {
                // Continue watching for new devices until timeout
            }
        }

        /// <summary>
        /// Detects whether a discovered BLE device is a CSL reader based on
        /// its advertised service UUIDs.
        /// </summary>
        private static MODEL DetectDeviceModel(BluetoothLEDevice device)
        {
            if (device == null)
                return MODEL.UNKNOWN;

            // Check by MAC address prefix (CSL OUI-based)
            string address = device.BluetoothAddress.ToString("X").ToUpperInvariant();

            // CSL OUIs
            if (address.StartsWith("3CA308") ||
                address.StartsWith("6C79B8") ||
                address.StartsWith("7C010A") ||
                address.StartsWith("C8FD19"))
                return MODEL.CS108;

            if (address.StartsWith("84C692"))
                return MODEL.CS710S;

            // Check GattServices for CSL service UUIDs
            foreach (var service in device.GattServices)
            {
                string svcUuid = service.Uuid.ToString().ToUpperInvariant();

                if (svcUuid == "00009800-0000-1000-8000-00805F9B34FB")
                    return MODEL.CS108;

                if (svcUuid == "00009802-0000-1000-8000-00805F9B34FB")
                    return MODEL.CS710S;
            }

            return MODEL.UNKNOWN;
        }

        private static void StopSearchOnTimeout(object state)
        {
            if (_isScanning)
            {
                CleanupScan();
                FireSearchCompleted(timedOut: true);
            }
        }

        private static void CleanupScan()
        {
            if (_deviceWatcher != null)
            {
                _deviceWatcher.Added -= OnDeviceAdded;
                _deviceWatcher.Updated -= OnDeviceUpdated;
                _deviceWatcher.Removed -= OnDeviceRemoved;
                _deviceWatcher.EnumerationCompleted -= OnEnumerationCompleted;
            }

            _scanTimer?.Dispose();
            _scanTimer = null;
            _scanTokenSource?.Dispose();
            _scanTokenSource = null;
            _isScanning = false;
        }

        private static void FireSearchCompleted(bool timedOut)
        {
            _onSearchCompleted?.Invoke(null, new SearchCompletedEventArgs(timedOut));
        }

        #endregion

        #region Event Args

        /// <summary>
        /// Event args passed when a CSL device is found during scan.
        /// </summary>
        public class DeviceFoundEventArgs : EventArgs
        {
            public DeviceInformation Device { get; }

            public DeviceFoundEventArgs(DeviceInformation device)
            {
                Device = device;
            }
        }

        /// <summary>
        /// Event args passed when a device search completes.
        /// </summary>
        public class SearchCompletedEventArgs : EventArgs
        {
            /// <summary>
            /// True if the search ended due to timeout; false if stopped manually.
            /// </summary>
            public bool TimedOut { get; }

            public SearchCompletedEventArgs(bool timedOut)
            {
                TimedOut = timedOut;
            }
        }

        #endregion

        /// <summary>
        /// Represents a discovered BLE device for WinFormsBLE.
        /// </summary>
        public class DeviceInformation
        {
            /// <summary>Device instance ID (from Windows.Devices.Enumeration).</summary>
            public string Id { get; set; }

            /// <summary>Device display name.</summary>
            public string Name { get; set; }

            /// <summary>CSL model type (CS108 or CS710S).</summary>
            public MODEL DeviceType { get; set; }

            /// <summary>
            /// The underlying BluetoothLEDevice. Use this to connect
            /// in the WinForms app layer.
            /// </summary>
            public BluetoothLEDevice NativeDevice { get; set; }

            /// <summary>Received signal strength (dBm). Currently always 0 on WinFormsBLE.</summary>
            public int Rssi { get; set; }
        }
    }
}
