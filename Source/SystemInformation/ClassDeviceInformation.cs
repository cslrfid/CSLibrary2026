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

/// <summary>
/// Provides static device information for Convergence Systems Limited (CSL) RFID readers
/// Includes model numbers, antenna counts and basic specifications
/// </summary>

using System;
using System.Collections.Generic;
using System.Text;

namespace CSLibrary
{
    using Constants;

    /// <summary>
    /// Provides the model and connection information of the CSL RFID readers.
    /// </summary>
    public static class RFIDDEVICE
    {
        /// <summary>
        /// Identifies the transport used to communicate with the reader.
        /// </summary>
        public enum CONNECTIONMODE
        {
            /// <summary>
            /// The reader is connected over TCP/IP.
            /// </summary>
            TCP,
            /// <summary>
            /// The reader is connected over Bluetooth.
            /// </summary>
            BLUETOOTH,
            /// <summary>
            /// The reader is connected over a serial (RS-232) port.
            /// </summary>
            RS232,
            /// <summary>
            /// The reader is connected over USB.
            /// </summary>
            USB,
            /// <summary>
            /// The connection mode could not be determined.
            /// </summary>
            UNKNOWN
        }

        //public static CSLibrary.RFIDDEVICE.CONNECTIONMODE ConnectionMode = RFIDDEVICE.CONNECTIONMODE.UNKNOWN;

        /// <summary>
        /// CSL RFID Reader model (OEM model code)
        /// </summary>
        public enum MODEL
        {
            /// <summary>
            /// The CS101 handheld UHF RFID reader.
            /// </summary>
            CS101 = 0,
            /// <summary>
            /// The CS203 fixed UHF RFID reader.
            /// </summary>
            CS203 = 1,
            /// <summary>
            /// The CS333 fixed UHF RFID reader.
            /// </summary>
            CS333 = 2,
            /// <summary>
            /// The CS468 fixed UHF RFID reader.
            /// </summary>
            CS468 = 3,
            /// <summary>
            /// The CS468INT fixed UHF RFID reader, international version.
            /// </summary>
            CS468INT = 5,
            /// <summary>
            /// The CS463 fixed UHF RFID reader.
            /// </summary>
            CS463 = 6,
            /// <summary>
            /// The CS469 fixed UHF RFID reader.
            /// </summary>
            CS469 = 7,
            /// <summary>
            /// The CS208 fixed UHF RFID reader.
            /// </summary>
            CS208 = 8,
            /// <summary>
            /// The CS209 fixed UHF RFID reader.
            /// </summary>
            CS209 = 9,
            /// <summary>
            /// The CS103 handheld UHF RFID reader.
            /// </summary>
            CS103 = 10,
            /// <summary>
            /// The CS108 handheld UHF RFID reader.
            /// </summary>
            CS108 = 11,
            /// <summary>
            /// The CS206 fixed UHF RFID reader.
            /// </summary>
            CS206 = 12,
            /// <summary>
            /// The CS468X fixed UHF RFID reader, enhanced version.
            /// </summary>
            CS468X = 13,
            /// <summary>
            /// The CS203X fixed UHF RFID reader.
            /// </summary>
            CS203X = 14,
            /// <summary>
            /// The CS468XJ fixed UHF RFID reader, Japan version.
            /// </summary>
            CS468XJ = 15,
            /// <summary>
            /// The CS710S handheld sled UHF RFID reader.
            /// </summary>
            CS710S = 32,
            /// <summary>
            /// The CS203XL fixed UHF RFID reader.
            /// </summary>
            CS203XL = 33,
            /// <summary>
            /// The reader model could not be determined.
            /// </summary>
            UNKNOWN = 0xff
        }

        // Hardware configuration has moved to CSL_HARDWARE_TABLE
        // See: Source/SystemInformation/ClassReaderHardwareConfiguration.cs
    }
}
