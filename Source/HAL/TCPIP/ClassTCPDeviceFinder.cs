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

#if TCP

using System;
using System.Collections.Generic;

namespace CSLibrary
{
    /// <summary>
    /// Device finder for readers reachable over TCP/IP (CS203XL).
    /// <para>
    /// This is the network counterpart of <see cref="DeviceFinder"/>, which discovers
    /// BLE readers. Both used to be named <c>DeviceFinder</c>, which collided as soon as
    /// the TCP HAL and the Plugin.BLE HAL were compiled into the same assembly.
    /// </para>
    /// <para>
    /// The underlying UDP broadcast search is implemented by
    /// <see cref="CSLibrary.NetFinder.NetFinder"/>.
    /// </para>
    /// </summary>
    public class TCPDeviceFinder
    {
        /// <summary>
        /// DeviceFinder Argument
        /// </summary>
        public class DeviceFinderArgs : EventArgs
        {
            private CSLibrary.NetFinder.DeviceInfo _data;

            /// <summary>
            /// Device Finder 
            /// </summary>
            /// <param name="data"></param>
            public DeviceFinderArgs(CSLibrary.NetFinder.DeviceInfo data)
            {
                _data = data;
            }

            /// <summary>
            /// Device finder information
            /// </summary>
            public CSLibrary.NetFinder.DeviceInfo Found
            {
                get { return _data; }
                set { _data = value; }
            }
        }

        /// <summary>
        /// Netfinder information return from device
        /// </summary>
        public class DeviceInfomation
        {
            /// <summary>
            /// Represents the zero-based index of the device within the current search result list.
            /// </summary>
            public uint ID;

            /// <summary>
            /// Represents the name reported by the discovered device.
            /// </summary>
            public string deviceName;

            /// <summary>
            /// Represents the underlying <see cref="CSLibrary.NetFinder.DeviceInfo"/> object of the discovered device.
            /// </summary>
            public object nativeDeviceInformation;

            /*
                    /// <summary>
                    /// Reserved for future use
                    /// </summary>
                    public Mode Mode = Mode.Unknown; 
                    /// <summary>
                    /// Total time on network
                    /// </summary>
                    public TimeEvent TimeElapsedNetwork = new TimeEvent();
                    /// <summary>
                    /// Total Power on time
                    /// </summary>
                    public TimeEvent TimeElapsedPowerOn = new TimeEvent();
                    /// <summary>
                    /// MAC address
                    /// </summary>
                    public MAC MACAddress = new MAC();//[6];
                    /// <summary>
                    /// IP address
                    /// </summary>
                    public IP IPAddress = new IP();
                    /// <summary>
                    /// Subnet Mask
                    /// </summary>
                    public IP SubnetMask = new IP();
                    /// <summary>
                    /// Gateway
                    /// </summary>
                    public IP Gateway = new IP();
                    /// <summary>
                    /// Trusted hist IP
                    /// </summary>
                    public IP TrustedServer = new IP();
                    /// <summary>
                    /// Inducated trusted server enable or not.
                    /// </summary>
                    public Boolean TrustedServerEnabled = false;
                    /// <summary>
                    /// UDP Port
                    /// </summary>
                    public ushort Port; // Get port from UDP header
                    /// <summary>
                    /// Reserved for future use, Server mode ip
                    /// </summary>
                    public byte[] serverip = new byte[4];
                    /// <summary>
                    /// enable or disable DHCP
                    /// </summary>
                    public bool DHCPEnabled;
                    /// <summary>
                    /// Reserved for future use, Server mode port
                    /// </summary>
                    public ushort serverport;
                    /// <summary>
                    /// DHCP retry
                    /// </summary>
                    public byte DHCPRetry;
                    /// <summary>
                    /// Device name, user can change it.
                    /// </summary>
                    public string DeviceName;
                    /// <summary>
                    /// Mode discription
                    /// </summary>
                    public string Description;
                    /// <summary>
                    /// Connect Mode
                    /// </summary>        
                    public byte ConnectMode;
                    /// <summary>
                    /// Gateway check reset mode
                    /// </summary>
                    public int GatewayCheckResetMode;
            */
        }

        static private CSLibrary.NetFinder.NetFinder _netFinder;
        static private DeviceFinderArgs _lastArgs;
        static List<DeviceInfomation> _deviceDB = new List<DeviceInfomation>();

        /// <summary>
        /// Occurs each time a reader is found on the network.
        /// </summary>
        /// <remarks>
        /// The event argument is a <see cref="DeviceFinderArgs"/> whose <see cref="DeviceFinderArgs.Found"/> property holds the information of the reader that has just been found, including its IP address, MAC address, name and UDP port.
        /// </remarks>
        static public event EventHandler<DeviceFinderArgs> OnSearchCompleted;

        /// <summary>
        /// Starts a search for readers on the network.
        /// </summary>
        /// <remarks>
        /// The device list is cleared before the search starts and every reader that answers the UDP broadcast raises <see cref="OnSearchCompleted"/>.
        /// </remarks>
        static public void SearchDevice()
        {
            _deviceDB.Clear();
            CSLibrary.NetFinder.NetFinder.ClearDeviceList();
            CSLibrary.NetFinder.NetFinder.SearchDevice();
        }

        /// <summary>
        /// Stops the network search for readers.
        /// </summary>
        static public void Stop()
        {
            CSLibrary.NetFinder.NetFinder.StopSearch();
        }

        /// <summary>
        /// Removes all readers from the search result list.
        /// </summary>
        static public void ClearDeviceList()
        {
            _deviceDB.Clear();
            CSLibrary.NetFinder.NetFinder.ClearDeviceList();
        }

        /// <summary>
        /// Gets the information of the reader at the given position of the search result list.
        /// </summary>
        /// <param name="id">The zero-based index of the reader in the search result list.</param>
        /// <returns>
        /// The <see cref="DeviceInfomation"/> of the reader, or <see langword="null"/> when the index is out of range.
        /// </returns>
        static public DeviceInfomation GetDeviceInformation(int id)
        {
            if (id < _deviceDB.Count)
                return _deviceDB[id];

            return null;
        }

        /// <summary>
        /// Gets the information of the reader with the given name.
        /// </summary>
        /// <param name="readername">The name of the reader to look for.</param>
        /// <returns>
        /// The <see cref="DeviceInfomation"/> of the first reader with that name, or <see langword="null"/> when no reader matches.
        /// </returns>
        static public DeviceInfomation GetDeviceInformation(string readername)
        {
            foreach (DeviceInfomation item in _deviceDB)
            {
                if (item.deviceName == readername)
                    return item;
            }

            return null;
        }

        /// <summary>
        /// Gets the list of the readers found during the last search.
        /// </summary>
        /// <returns>
        /// The list of <see cref="DeviceInfomation"/> objects collected during the search.
        /// </returns>
        static public List<DeviceInfomation> GetAllDeviceInformation()
        {
            return _deviceDB;
        }

        static private void OnNetFinderSearchCompleted(object sender, CSLibrary.NetFinder.DeviceFinderArgs e)
        {
            CSLibrary.NetFinder.DeviceInfo deviceInfo = e.Device;

            DeviceInfomation di = new DeviceInfomation();
            di.deviceName = deviceInfo.DeviceName;
            di.ID = (uint)_deviceDB.Count;
            di.nativeDeviceInformation = (object)deviceInfo;

            _deviceDB.Add(di);

            _lastArgs = new DeviceFinderArgs(deviceInfo);
            RaiseEvent<DeviceFinderArgs>(OnSearchCompleted, _lastArgs);
        }

        static private void RaiseEvent<T>(EventHandler<T> eventHandler, T e)
            where T : EventArgs
        {
            if (eventHandler != null)
            {
                eventHandler(null, e);
            }
            return;
        }

        static TCPDeviceFinder()
        {
            CSLibrary.NetFinder.NetFinder.OnSearchCompleted += OnNetFinderSearchCompleted;
        }
    }

}

#endif