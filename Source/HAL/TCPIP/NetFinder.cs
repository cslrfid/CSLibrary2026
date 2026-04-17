/*
Copyright (c) 2025 Convergence Systems Limited

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

#if TCP
using System;
using System.Collections.Generic;

namespace CSLibrary.NetFinder
{
    /// <summary>
    /// Device information returned from NetFinder search
    /// </summary>
    public class DeviceInfo
    {
        /// <summary>
        /// IP address of the device
        /// </summary>
        public string IPAddress { get; set; }

        /// <summary>
        /// MAC address of the device
        /// </summary>
        public string MacAddress { get; set; }

        /// <summary>
        /// Name of the device
        /// </summary>
        public string DeviceName { get; set; }

        /// <summary>
        /// UDP port of the device
        /// </summary>
        public ushort Port { get; set; }
    }

    /// <summary>
    /// Event args for device found during NetFinder search
    /// </summary>
    public class DeviceFinderArgs : EventArgs
    {
        /// <summary>
        /// Device information
        /// </summary>
        public DeviceInfo Device { get; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="device">Device information</param>
        public DeviceFinderArgs(DeviceInfo device)
        {
            Device = device;
        }

        /// <summary>
        /// IP address of the discovered device
        /// </summary>
        public string IPAddress => Device?.IPAddress;

        /// <summary>
        /// MAC address of the discovered device
        /// </summary>
        public string MacAddress => Device?.MacAddress;

        /// <summary>
        /// Name of the discovered device
        /// </summary>
        public string DeviceName => Device?.DeviceName;

        /// <summary>
        /// Port of the discovered device
        /// </summary>
        public ushort Port => Device?.Port ?? 0;
    }

    /// <summary>
    /// Cross-platform NetFinder for discovering CS203XL devices on the network.
    /// Uses UDP broadcast on port 3000 for device discovery.
    /// Works on iOS, Android, and Windows.
    /// </summary>
    public class NetFinder : IDisposable
    {
        private static NetFinder s_instance;
        private static readonly object s_lock = new object();
        private CSLibrary.NetFinder.CS203XL.NetFinder _impl;
        private bool _disposed = false;

        /// <summary>
        /// Static list of discovered devices
        /// </summary>
        public static List<DeviceInfo> Devices { get; } = new List<DeviceInfo>();

        /// <summary>
        /// Event raised when a device is found during search
        /// </summary>
        public static event EventHandler<DeviceFinderArgs> OnSearchCompleted;

        private NetFinder()
        {
            _impl = new CSLibrary.NetFinder.CS203XL.NetFinder();
            _impl.OnSearchCompleted += OnImplSearchCompleted;
        }

        private void OnImplSearchCompleted(object sender, CSLibrary.NetFinder.CS203XL.DeviceFinderArgs e)
        {
            var deviceInfo = new DeviceInfo
            {
                IPAddress = e.Found.IPAddress?.ToString(),
                MacAddress = e.Found.MACAddress?.ToString(),
                DeviceName = e.Found.DeviceName,
                Port = e.Found.Port
            };

            lock (s_lock)
            {
                // Avoid duplicates by MAC address
                bool exists = false;
                foreach (var existing in Devices)
                {
                    if (existing.MacAddress == deviceInfo.MacAddress)
                    {
                        exists = true;
                        break;
                    }
                }
                if (!exists)
                {
                    Devices.Add(deviceInfo);
                }
            }

            OnSearchCompleted?.Invoke(null, new DeviceFinderArgs(deviceInfo));
        }

        /// <summary>
        /// Gets the singleton instance of NetFinder
        /// </summary>
        private static NetFinder Instance
        {
            get
            {
                if (s_instance == null)
                {
                    lock (s_lock)
                    {
                        if (s_instance == null)
                        {
                            s_instance = new NetFinder();
                        }
                    }
                }
                return s_instance;
            }
        }

        /// <summary>
        /// Start searching for devices on the network.
        /// Uses UDP broadcast on port 3000.
        /// </summary>
        public static void SearchDevice()
        {
            lock (s_lock)
            {
                Devices.Clear();
            }
            Instance._impl.ClearDeviceList();
            Instance._impl.SearchDevice();
        }

        /// <summary>
        /// Stop searching for devices
        /// </summary>
        public static void StopSearch()
        {
            Instance._impl.Stop();
        }

        /// <summary>
        /// Clear the device list
        /// </summary>
        public static void ClearDeviceList()
        {
            lock (s_lock)
            {
                Devices.Clear();
            }
            Instance._impl.ClearDeviceList();
        }

        /// <summary>
        /// Dispose the NetFinder resources
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _impl.OnSearchCompleted -= OnImplSearchCompleted;
                _impl.Dispose();
                _disposed = true;
            }
        }

        /// <summary>
        /// Static dispose for cleanup
        /// </summary>
        public static void DisposeStatic()
        {
            if (s_instance != null)
            {
                s_instance.Dispose();
                s_instance = null;
            }
        }
    }
}
#endif