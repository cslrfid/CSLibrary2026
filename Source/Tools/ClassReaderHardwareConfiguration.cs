/*
Copyright (c) 2026 Convergence Systems Limited

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
/// CSL RFID Reader Hardware Configuration Table
/// Contains antenna configuration and communication interface details for all supported models.
/// </summary>

using System.Collections.Generic;

namespace CSLibrary.Tools
{
    using ChipSet = CSLibrary.Constants.ChipSetID;

    /// <summary>
    /// CSL RFID Reader Hardware Configuration Table
    /// Contains antenna configuration and communication interface details for all supported models.
    /// </summary>
    public static class CSL_HARDWARE_TABLE
    {
        /// <summary>
        /// Communication interface type
        /// </summary>
        public enum COMMUNICATION_INTERFACE
        {
            BLUETOOTH,
            USB,
            TCPIP,
            SERIAL
        }

        /// <summary>
        /// The CSL Bluetooth/USB API specification used by each interface.
        /// CSL CS108 API = Rx000 command set (R1000/R2000 chipsets)
        /// CSL CS710 API = E710/E910 command set
        /// </summary>
        public enum API_SPEC
        {
            CSL_CS108_API,  // Rx000 command set
            CSL_CS710_API,  // E710/E910 command set
            NONE            // No protocol (direct register access)
        }

        /// <summary>
        /// Antenna type classification
        /// </summary>
        public enum ANTENNA_TYPE
        {
            INTERNAL,
            EXTERNAL,
            BOTH
        }

        /// <summary>
        /// Hardware configuration for a single antenna port
        /// </summary>
        public class HwAntennaPort
        {
            /// <summary>0-based antenna port index</summary>
            public int PortIndex;
            /// <summary>Human-readable name</summary>
            public string Name;
            /// <summary>Internal or external</summary>
            public ANTENNA_TYPE Type;
            /// <summary>Maximum RF power in 0.01 dBm units (e.g. 3000 = 30.00 dBm)</summary>
            public int MaxPower;

            public HwAntennaPort(int portIndex, string name, ANTENNA_TYPE type, int maxPower)
            {
                PortIndex = portIndex;
                Name = name;
                Type = type;
                MaxPower = maxPower;
            }
        }

        /// <summary>
        /// Communication interface configuration for a reader model
        /// </summary>
        public class InterfaceConfig
        {
            public COMMUNICATION_INTERFACE Interface;
            public API_SPEC ApiSpec;
            public string Notes;

            public InterfaceConfig(COMMUNICATION_INTERFACE intf, API_SPEC api, string notes = "")
            {
                Interface = intf;
                ApiSpec = api;
                Notes = notes;
            }
        }

        /// <summary>
        /// Complete hardware configuration for a reader model
        /// </summary>
        public class HardwareConfiguration
        {
            public RFIDDEVICE.MODEL Model;
            public ChipSet ChipSetID;
            public string ModelName;
            /// <summary>First antenna port index (zero-based)</summary>
            public int FirstAntenna;
            /// <summary>Last antenna port index (zero-based)</summary>
            public int LastAntenna;
            /// <summary>Default antenna port index (zero-based)</summary>
            public int DefaultAntenna;
            /// <summary>True = fixed reader, false = handheld</summary>
            public bool FixedReader;
            public List<HwAntennaPort> AntennaPorts;
            public List<InterfaceConfig> Interfaces;

            public HardwareConfiguration(RFIDDEVICE.MODEL model, ChipSet chipSetId, string modelName,
                HwAntennaPort[] antennas, InterfaceConfig[] interfaces,
                int firstAntenna = 0, int lastAntenna = 0, int defaultAntenna = 0, bool fixedReader = false)
            {
                Model = model;
                ChipSetID = chipSetId;
                ModelName = modelName;
                FirstAntenna = firstAntenna;
                LastAntenna = lastAntenna;
                DefaultAntenna = defaultAntenna;
                FixedReader = fixedReader;
                AntennaPorts = new List<HwAntennaPort>(antennas);
                Interfaces = new List<InterfaceConfig>(interfaces);
            }
        }

        /// <summary>
        /// Hardware configuration table — one entry per supported reader model
        /// </summary>
        public static List<HardwareConfiguration> HARDWARE_TABLE = new List<HardwareConfiguration>
        {
            // =====================================================================
            // CS101 — Handheld UHF RFID Reader
            // Chipset: Rx000 (R1000)
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS101,
                ChipSet.R1000,
                "CS101",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(0, "Internal Antenna", ANTENNA_TYPE.INTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.BLUETOOTH, API_SPEC.CSL_CS108_API),
                },
                firstAntenna: 0, lastAntenna: 0, defaultAntenna: 0, fixedReader: false
            ),

            // =====================================================================
            // CS108 — Handheld UHF RFID Reader
            // Chipset: Rx000 (R2000)
            // Notes: Single internal antenna. Bluetooth + USB use CSL CS108 API.
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS108,
                ChipSet.R2000,
                "CS108",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(0, "Internal Antenna", ANTENNA_TYPE.INTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.BLUETOOTH, API_SPEC.CSL_CS108_API),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.USB,         API_SPEC.CSL_CS108_API),
                },
                firstAntenna: 0, lastAntenna: 0, defaultAntenna: 0, fixedReader: false
            ),

            // =====================================================================
            // CS203 — Fixed UHF RFID Reader
            // Chipset: Rx000 (R1000)
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS203,
                ChipSet.R1000,
                "CS203",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(2, "Antenna Port 2", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(3, "Antenna Port 3", ANTENNA_TYPE.EXTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.BLUETOOTH, API_SPEC.CSL_CS108_API),
                },
                firstAntenna: 2, lastAntenna: 3, defaultAntenna: 3, fixedReader: true
            ),

            // =====================================================================
            // CS208 — Fixed UHF RFID Reader
            // Chipset: Rx000 (R1000)
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS208,
                ChipSet.R1000,
                "CS208",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(0, "Antenna Port 0", ANTENNA_TYPE.EXTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.USB, API_SPEC.CSL_CS108_API),
                },
                firstAntenna: 0, lastAntenna: 0, defaultAntenna: 0, fixedReader: true
            ),

            // =====================================================================
            // CS209 — Fixed UHF RFID Reader
            // Chipset: Rx000 (R2000)
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS209,
                ChipSet.R2000,
                "CS209",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(0, "Antenna Port 0", ANTENNA_TYPE.EXTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.USB, API_SPEC.CSL_CS108_API),
                },
                firstAntenna: 0, lastAntenna: 0, defaultAntenna: 0, fixedReader: true
            ),

            // =====================================================================
            // CS333 — Fixed UHF RFID Reader
            // Chipset: Rx000 (R2000)
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS333,
                ChipSet.R2000,
                "CS333",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(0, "Antenna Port 0", ANTENNA_TYPE.EXTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.USB, API_SPEC.CSL_CS108_API),
                },
                firstAntenna: 0, lastAntenna: 0, defaultAntenna: 0, fixedReader: true
            ),

            // =====================================================================
            // CS463 — Fixed UHF RFID Reader
            // Chipset: Rx000 (R2000)
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS463,
                ChipSet.R2000,
                "CS463",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(0, "Antenna Port 0", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(1, "Antenna Port 1", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(2, "Antenna Port 2", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(3, "Antenna Port 3", ANTENNA_TYPE.EXTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.BLUETOOTH, API_SPEC.CSL_CS108_API),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.USB,         API_SPEC.CSL_CS108_API),
                },
                firstAntenna: 0, lastAntenna: 3, defaultAntenna: 0, fixedReader: true
            ),

            // =====================================================================
            // CS468 — Fixed UHF RFID Reader
            // Chipset: Rx000 (R2000)
            // Notes: Bluetooth uses CSL CS108 API with Rx000 commands.
            //        USB, TCP/IP and Serial bypass the protocol layer — direct Rx000 register access.
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS468,
                ChipSet.R2000,
                "CS468",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(0, "Antenna Port 0", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(1, "Antenna Port 1", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(2, "Antenna Port 2", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(3, "Antenna Port 3", ANTENNA_TYPE.EXTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.BLUETOOTH, API_SPEC.CSL_CS108_API),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.USB,         API_SPEC.NONE,           "Direct Rx000 register commands (no CSL protocol)"),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.TCPIP,      API_SPEC.NONE,           "Direct Rx000 register commands (no CSL protocol)"),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.SERIAL,    API_SPEC.NONE,           "Direct Rx000 register commands (no CSL protocol)"),
                },
                firstAntenna: 0, lastAntenna: 3, defaultAntenna: 0, fixedReader: true
            ),

            // =====================================================================
            // CS468INT — Fixed UHF RFID Reader (International Version)
            // Chipset: Rx000 (R1000)
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS468INT,
                ChipSet.R1000,
                "CS468INT",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(0, "Antenna Port 0", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(1, "Antenna Port 1", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(2, "Antenna Port 2", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(3, "Antenna Port 3", ANTENNA_TYPE.EXTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.BLUETOOTH, API_SPEC.CSL_CS108_API),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.USB,         API_SPEC.NONE, "Direct Rx000 register commands (no CSL protocol)"),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.TCPIP,      API_SPEC.NONE, "Direct Rx000 register commands (no CSL protocol)"),
                },
                firstAntenna: 0, lastAntenna: 3, defaultAntenna: 0, fixedReader: true
            ),

            // =====================================================================
            // CS468X — Fixed UHF RFID Reader (Enhanced Version)
            // Chipset: Rx000 (R2000)
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS468X,
                ChipSet.R2000,
                "CS468X",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(0, "Antenna Port 0", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(1, "Antenna Port 1", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(2, "Antenna Port 2", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(3, "Antenna Port 3", ANTENNA_TYPE.EXTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.BLUETOOTH, API_SPEC.CSL_CS108_API),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.USB,         API_SPEC.NONE, "Direct Rx000 register commands (no CSL protocol)"),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.TCPIP,      API_SPEC.NONE, "Direct Rx000 register commands (no CSL protocol)"),
                },
                firstAntenna: 0, lastAntenna: 3, defaultAntenna: 0, fixedReader: true
            ),

            // =====================================================================
            // CS468XJ — Fixed UHF RFID Reader (Japan Version)
            // Chipset: Rx000 (R2000)
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS468XJ,
                ChipSet.R2000,
                "CS468XJ",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(0, "Antenna Port 0", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(1, "Antenna Port 1", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(2, "Antenna Port 2", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(3, "Antenna Port 3", ANTENNA_TYPE.EXTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.BLUETOOTH, API_SPEC.CSL_CS108_API),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.USB,         API_SPEC.NONE, "Direct Rx000 register commands (no CSL protocol)"),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.TCPIP,      API_SPEC.NONE, "Direct Rx000 register commands (no CSL protocol)"),
                },
                firstAntenna: 0, lastAntenna: 3, defaultAntenna: 0, fixedReader: true
            ),

            // =====================================================================
            // CS469 — Fixed UHF RFID Reader
            // Chipset: Rx000 (R1000)
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS469,
                ChipSet.R1000,
                "CS469",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(0, "Antenna Port 0", ANTENNA_TYPE.EXTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.BLUETOOTH, API_SPEC.CSL_CS108_API),
                },
                firstAntenna: 0, lastAntenna: 0, defaultAntenna: 0, fixedReader: true
            ),

            // =====================================================================
            // CS103 — Handheld UHF RFID Reader
            // Chipset: Rx000 (R2000)
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS103,
                ChipSet.R2000,
                "CS103",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(0, "Internal Antenna", ANTENNA_TYPE.INTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.BLUETOOTH, API_SPEC.CSL_CS108_API),
                },
                firstAntenna: 0, lastAntenna: 0, defaultAntenna: 0, fixedReader: false
            ),

            // =====================================================================
            // CS206 — Fixed UHF RFID Reader
            // Chipset: Rx000 (R2000)
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS206,
                ChipSet.R2000,
                "CS206",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(0, "Antenna Port 0", ANTENNA_TYPE.EXTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.USB, API_SPEC.CSL_CS108_API),
                },
                firstAntenna: 0, lastAntenna: 0, defaultAntenna: 0, fixedReader: true
            ),

            // =====================================================================
            // CS710S — Handheld Sled RFID Reader
            // Chipset: E710
            // Notes: All interfaces use CSL CS710 API with E710 commands.
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS710S,
                ChipSet.E710,
                "CS710S",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(0, "Internal Antenna", ANTENNA_TYPE.INTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.BLUETOOTH, API_SPEC.CSL_CS710_API),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.USB,         API_SPEC.CSL_CS710_API),
                },
                firstAntenna: 0, lastAntenna: 0, defaultAntenna: 0, fixedReader: false
            ),

            // =====================================================================
            // CS203X — Fixed UHF RFID Reader
            // Chipset: Rx000 (R2000)
            // Notes: Antenna 2 external, Antenna 3 internal.
            //        USB/TCP use direct Rx000 register access.
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS203X,
                ChipSet.R2000,
                "CS203X",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(2, "Antenna Port 2", ANTENNA_TYPE.EXTERNAL, 3000),
                    new HwAntennaPort(3, "Antenna Port 3", ANTENNA_TYPE.INTERNAL,  3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.BLUETOOTH, API_SPEC.CSL_CS108_API),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.USB,         API_SPEC.NONE, "Direct Rx000 register commands (no CSL protocol)"),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.TCPIP,      API_SPEC.NONE, "Direct Rx000 register commands (no CSL protocol)"),
                },
                firstAntenna: 2, lastAntenna: 3, defaultAntenna: 3, fixedReader: true
            ),

            // =====================================================================
            // CS203XL — Fixed UHF RFID Reader
            // Chipset: E910
            // Notes: Antenna 0 internal, Antenna 1 external.
            //        All interfaces (BT/USB/TCP) use CSL CS710 API with E910 commands.
            // =====================================================================
            new HardwareConfiguration(
                RFIDDEVICE.MODEL.CS203XL,
                ChipSet.E910,
                "CS203XL",
                new HwAntennaPort[]
                {
                    new HwAntennaPort(0, "Antenna Port 0", ANTENNA_TYPE.INTERNAL,  3000),
                    new HwAntennaPort(1, "Antenna Port 1", ANTENNA_TYPE.EXTERNAL, 3000),
                },
                new InterfaceConfig[]
                {
                    new InterfaceConfig(COMMUNICATION_INTERFACE.BLUETOOTH, API_SPEC.CSL_CS710_API),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.USB,         API_SPEC.CSL_CS710_API),
                    new InterfaceConfig(COMMUNICATION_INTERFACE.TCPIP,      API_SPEC.CSL_CS710_API),
                },
                firstAntenna: 0, lastAntenna: 1, defaultAntenna: 1, fixedReader: true
            ),
        };

        /// <summary>
        /// Looks up hardware configuration by reader MODEL.
        /// Returns null if the model is not in the table.
        /// </summary>
        public static HardwareConfiguration GetHardwareConfiguration(RFIDDEVICE.MODEL model)
        {
            return HARDWARE_TABLE.Find(item => item.Model == model);
        }

        /// <summary>
        /// Returns the total number of antenna ports for a given model.
        /// Returns 0 if model is not found.
        /// </summary>
        public static int GetTotalAntenna(RFIDDEVICE.MODEL model)
        {
            var cfg = GetHardwareConfiguration(model);
            return cfg != null ? cfg.AntennaPorts.Count : 0;
        }
    }
}
