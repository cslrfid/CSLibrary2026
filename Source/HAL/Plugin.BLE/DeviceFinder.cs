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
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using Plugin.BLE.Abstractions;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Plugin.BLE.Abstractions.Exceptions;

using CSLibrary;
using static CSLibrary.RFIDDEVICE;

namespace CSLibrary
{
    public partial class DeviceFinder
    {
        // =====================================================================
        // Plugin.BLE implementation — this is the primary implementation for
        // Xamarin/MAUI platforms. The btframework/ and other HALs share the
        // same partial class but provide their own back-ends.
        // =====================================================================

        #region Fields

        private static IAdapter _adapter;
        private static IBluetoothLE _bluetoothLe;
        private static CancellationTokenSource _scanTokenSource;
        private static bool _isScanning;
        private static int _timeoutMs = 5000; // default 5 seconds
        private static ScanMode _scanMode = ScanMode.LowLatency;
        private static Timer _scanTimer;
        private static readonly List<DeviceInformation> _devices = new List<DeviceInformation>();

        // Events
        private static event EventHandler<DeviceFoundEventArgs> _onDeviceFound;
        private static event EventHandler<SearchCompletedEventArgs> _onSearchCompleted;

        #endregion

        #region Public API

        /// <summary>
        /// Start scanning for CSL RFID readers.
        /// </summary>
        /// <param name="scanMode">BLE scan mode (default: LowLatency)</param>
        /// <param name="timeoutMs">Scan duration in ms (default: 5000)</param>
        public static void StartDeviceSearch(ScanMode scanMode = ScanMode.LowLatency, int timeoutMs = 5000)
        {
            if (_isScanning)
                StopDeviceSearch();

            _scanMode = scanMode;
            _timeoutMs = timeoutMs;
            _devices.Clear();

            if (_adapter == null || _bluetoothLe == null)
            {
                throw new InvalidOperationException(
                    "DeviceFinder has not been initialized. " +
                    "Call Initialize(IAdapter adapter, IBluetoothLE bluetoothLe) first.");
            }

            _scanTokenSource = new CancellationTokenSource();
            _isScanning = true;

            _adapter.ScanMode = _scanMode;
            _adapter.DeviceAdvertised += OnDeviceAdvertised;

            _scanTimer = new Timer(StopSearchOnTimeout, null, _timeoutMs, Timeout.Infinite);

            try
            {
                _adapter.StartScanningForDevicesAsync();
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
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceFinder] StopScanning failed: {ex.Message}");
            }
            finally
            {
                CleanupScan();
                FireSearchCompleted(timedOut: false);
            }
        }

        /// <summary>
        /// Initialize DeviceFinder with Plugin.BLE adapter and BluetoothLE instances.
        /// Must be called before StartDeviceSearch.
        /// </summary>
        public static void Initialize(IAdapter adapter, IBluetoothLE bluetoothLe)
        {
            if (_adapter != null && _bluetoothLe != null)
                return; // already initialized, safe to call multiple times
            _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
            _bluetoothLe = bluetoothLe ?? throw new ArgumentNullException(nameof(bluetoothLe));
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
        /// Delivereds on the main thread.
        /// </summary>
        public static event EventHandler<DeviceFoundEventArgs> OnDeviceFound
        {
            add => _onDeviceFound += value;
            remove => _onDeviceFound -= value;
        }

        /// <summary>
        /// Fires when the search completes (timeout or user stop).
        /// Delivereds on the main thread.
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

        private static void OnDeviceAdvertised(object sender, DeviceEventArgs args)
        {
            try
            {
                var device = args.Device;
                var model = DetectDeviceModel(device);

                if (model == MODEL.UNKNOWN)
                    return; // not a CSL device

                var info = new DeviceInformation
                {
                    Id = device.Id.ToString(),
                    Name = device.Name ?? "Unknown",
                    DeviceType = model,
                    NativeDevice = device,
                    Rssi = device.Rssi
                };

                lock (_devices)
                {
                    if (_devices.Any(d => d.Id == info.Id))
                        return; // already seen
                    _devices.Add(info);
                }

                FireDeviceFoundOnMainThread(info);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[DeviceFinder] OnDeviceAdvertised error: {ex.Message}");
            }
        }

        /// <summary>
        /// Detects whether a discovered BLE device is a CSL reader based on
        /// its advertisement records and service UUIDs.
        /// </summary>
        private static MODEL DetectDeviceModel(IDevice device)
        {
            foreach (var record in device.AdvertisementRecords)
            {
                // CS108 / CS710S use CSL-specific service UUIDs in advertisement
                if (record.Data.Length == 2)
                {
                    // 2-byte service UUID
                    // Android / iOS byte order: MSB first
                    if (record.Data[0] == 0x98 && record.Data[1] == 0x00)
                        return MODEL.CS108;

                    if ((record.Data[0] == 0x98 && record.Data[1] == 0x02) || // iOS/Android alt
                        (record.Data[0] == 0x53 && record.Data[1] == 0x50))   // Android
                        return MODEL.CS710S;
                }
                else if (record.Data.Length == 16)
                {
                    // 128-bit service UUID
                    var uuid = new Guid(record.Data);
                    if (uuid == Guid.Parse("00009800-0000-1000-8000-00805f9b34fb"))
                        return MODEL.CS108;
                    if (uuid == Guid.Parse("00009802-0000-1000-8000-00805f9b34fb"))
                        return MODEL.CS710S;
                }
            }

            // Fallback: check by MAC address prefix (CSL OUI-based)
            string address = device.NativeDevice.ToString().ToUpperInvariant();
            if (address.StartsWith("3C:A3:08") || // CS108 OUI
                address.StartsWith("6C:79:B8") ||
                address.StartsWith("7C:01:0A") ||
                address.StartsWith("C8:FD:19"))
                return MODEL.CS108;
            if (address.StartsWith("84:C6:92")) // CS710S OUI
                return MODEL.CS710S;

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
            if (_adapter != null)
            {
                _adapter.DeviceAdvertised -= OnDeviceAdvertised;
            }

            _scanTimer?.Dispose();
            _scanTimer = null;
            _scanTokenSource?.Dispose();
            _scanTokenSource = null;
            _isScanning = false;
        }

        private static void FireDeviceFoundOnMainThread(DeviceInformation info)
        {
            _onDeviceFound?.Invoke(null, new DeviceFoundEventArgs(info));
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
    }

    /// <summary>
    /// Represents a discovered BLE device.
    /// </summary>
    public class DeviceInformation
    {
        /// <summary>Device unique identifier (GUID string).</summary>
        public string Id { get; set; }

        /// <summary>Device display name.</summary>
        public string Name { get; set; }

        /// <summary>CSL model type (CS108 or CS710S).</summary>
        public MODEL DeviceType { get; set; }

        /// <summary>
        /// The underlying Plugin.BLE IDevice. Use this to call
        /// adapter.ConnectToDeviceAsync() in the MAUI app layer.
        /// </summary>
        public IDevice NativeDevice { get; set; }

        /// <summary>Received signal strength (dBm).</summary>
        public int Rssi { get; set; }
    }
}
