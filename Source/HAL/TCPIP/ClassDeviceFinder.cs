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
    public partial class DeviceFinder
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
            public uint ID;

            public string deviceName;

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

        static public event EventHandler<DeviceFinderArgs> OnSearchCompleted;

        static public void SearchDevice()
        {
            _deviceDB.Clear();
            CSLibrary.NetFinder.NetFinder.ClearDeviceList();
            CSLibrary.NetFinder.NetFinder.SearchDevice();
        }

        static public void Stop()
        {
            CSLibrary.NetFinder.NetFinder.StopSearch();
        }

        static public void ClearDeviceList()
        {
            _deviceDB.Clear();
            CSLibrary.NetFinder.NetFinder.ClearDeviceList();
        }

        static public DeviceInfomation GetDeviceInformation(int id)
        {
            if (id < _deviceDB.Count)
                return _deviceDB[id];

            return null;
        }

        static public DeviceInfomation GetDeviceInformation(string readername)
        {
            foreach (DeviceInfomation item in _deviceDB)
            {
                if (item.deviceName == readername)
                    return item;
            }

            return null;
        }

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

        static DeviceFinder()
        {
            CSLibrary.NetFinder.NetFinder.OnSearchCompleted += OnNetFinderSearchCompleted;
        }
    }

}

#endif