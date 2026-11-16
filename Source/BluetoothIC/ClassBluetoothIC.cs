using System;
using System.Text;

using CSLibrary.Barcode;
using CSLibrary.Barcode.Constants;
using CSLibrary.Barcode.Structures;

namespace CSLibrary
{
    /// <summary>
    /// Provides access to the Bluetooth controller of the connected reader.
    /// </summary>
    /// <remarks>
    /// The instance is reached through <see cref="HighLevelInterface.bluetoothIC"/>.
    /// </remarks>
    public partial class BluetoothIC
    {
        string _deviceName;
        uint _firmwareVersion;

        // RFID event code
        private class DOWNLINKCMD
        {
            public static readonly byte[] GETVERSION = { 0xC0, 0x00 };
            public static readonly byte[] SETDEVICENAME = { 0xC0, 0x03 };
            public static readonly byte[] GETDEVICENAME = { 0xC0, 0x04 };
        }

        private HighLevelInterface _deviceHandler;

        internal BluetoothIC(HighLevelInterface handler)
        {
            _deviceHandler = handler;
        }

        internal bool BluetoothICPacket(byte [] recvData)
        {
            UInt16 eventCode = (UInt16)((UInt16)recvData[8] << 8 | (UInt16)recvData[9]);

            switch (eventCode)
            {
                case 0xc000:
                    if (recvData.Length == 13)
                    {
                        _firmwareVersion = (uint)((recvData[10] << 16) | (recvData[11] << 8) | (recvData[12]));
                        return true;
                    } else if (recvData.Length == 11)
                    {
                        _firmwareVersion = 0;
                        return true;
                    }
                    CSLibrary.Debug.WriteLine("BluetoothIC Get Version error!");
                    break;

                case 0xc001:
                    break;

                case 0xc002:
                    break;

                case 0xc003:
                    if (recvData.Length == 11)
                        if (recvData[10] == 0x00)
                            return true;
                    CSLibrary.Debug.WriteLine("Set Device Name Fail!");
                    break;

                case 0xc004:
                    if (recvData[2] == 0x17)
                        _deviceName = Encoding.UTF8.GetString(recvData, 10, 21).TrimEnd((Char)0);
                    else
                        _deviceName = "";
                    return true;
            }

            return false;
        }

        internal void Connect()
        {
            // Get Firmware Version
            _deviceHandler.SendAsync(0, 4, DOWNLINKCMD.GETVERSION, null, HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE);

            // Get Device Name
            _deviceName = null;
            _deviceHandler.SendAsync(0, 4, DOWNLINKCMD.GETDEVICENAME, null, HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE);
        }

        /// <summary>
        /// Gets the firmware version of the Bluetooth controller of the reader.
        /// </summary>
        /// <returns>
        /// The firmware version as a 24-bit value, or <c>0</c> when the reader did not report one.
        /// </returns>
        public uint GetFirmwareVersion()
        {
            return _firmwareVersion;
        }

        /// <summary>
        /// Gets the Bluetooth device name of the reader.
        /// </summary>
        /// <returns>
        /// The device name, or <see langword="null"/> when the reader has not reported it yet.
        /// </returns>
        public string GetDeviceName ()
        {
            return _deviceName;
        }

        /// <summary>
        /// Sets the Bluetooth device name of the reader.
        /// </summary>
        /// <param name="deviceName">The new device name; it must not be longer than 20 characters.</param>
        /// <returns>
        /// <see langword="true"/> when the name has been sent to the reader, <see langword="false"/> when the name is longer than 20 characters.
        /// </returns>
        public bool SetDeviceName (string deviceName)
        {
            if (deviceName.Length > 20)
                return false;

            byte[] bDeviceName = Encoding.UTF8.GetBytes(deviceName + new String('\0', 21 - deviceName.Length));
            _deviceHandler.SendAsync(0, 4, DOWNLINKCMD.SETDEVICENAME, bDeviceName, HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE);
            _deviceName = deviceName;

            return true;
        }
    }
}
