using System;

using CSLibrary.Barcode;
using CSLibrary.Barcode.Constants;
using CSLibrary.Barcode.Structures;

namespace CSLibrary
{
    /// <summary>
    /// Provides the notifications reported by the reader, such as the battery voltage and the state of the hardware key.
    /// </summary>
    /// <remarks>
    /// The instance is reached through <see cref="HighLevelInterface.notification"/>.
    /// </remarks>
    public partial class Notification
    {
        bool _currentAutoReportStatus = false;
        uint _batteryLevel = 0;

        // RFID event code
        private class DOWNLINKCMD
        {
            public static readonly byte[] GETVOLTAGE = { 0xA0, 0x00 };
            public static readonly byte[] GETTRIGGERSTATE = { 0xA0, 0x01 };
            public static readonly byte[] STARTAUTOREPORTING = { 0xA0, 0x02 };
            public static readonly byte[] STOPAUTOREPORTING = { 0xA0, 0x03 };
            public static readonly byte[] RFIDABORT = { 0xA0, 0x04 };
            public static readonly byte[] GETTRIGGER = { 0xA0, 0x05 };
            public static readonly byte[] SETFASTMODE = { 0xA0, 0x06 };
            public static readonly byte[] GETFASTMODE = { 0xA0, 0x07 };
            public static readonly byte[] STARTAUTOREIGGERREPORTING = { 0xA0, 0x08 };
            public static readonly byte[] STOPAUTOREIGGERREPORTING = { 0xA0, 0x09 };
        }

        private HighLevelInterface _deviceHandler;

        /// <summary>
        /// HotKey Event Argument
        /// </summary>
        public class HotKeyEventArgs : EventArgs
        {
            Key m_KeyCode = Key.BUTTON;
            bool m_KeyDown = false;

            /// <summary>
            /// Gets the key that has been pressed or released.
            /// </summary>
            /// <returns>
            /// The <see cref="Key"/> that generated the event.
            /// </returns>
            public Key KeyCode { get { return m_KeyCode; } }
            /// <summary>
            /// Gets the direction of the key event.
            /// </summary>
            /// <returns>
            /// <see langword="true"/> when the key has just been pressed, <see langword="false"/> when it has just been released.
            /// </returns>
            public bool KeyDown { get { return m_KeyDown; } }

            /// <summary>
            /// Initializes a new instance of the <see cref="HotKeyEventArgs"/> class.
            /// </summary>
            /// <param name="KeyCode">The key that has been pressed or released.</param>
            /// <param name="KeyDown"><see langword="true"/> when the key has just been pressed, <see langword="false"/> when it has just been released.</param>
            public HotKeyEventArgs(Key KeyCode, bool KeyDown)
            {
                m_KeyCode = KeyCode;
                m_KeyDown = KeyDown;
            }
        }

        /// <summary>
        /// Carries the battery voltage reported by the reader.
        /// </summary>
        public class VoltageEventArgs : EventArgs
        {
            uint m_Voltage = 0;

            /// <summary>
            /// Gets the battery voltage reported by the reader.
            /// </summary>
            /// <returns>
            /// The battery voltage in millivolts.
            /// </returns>
            public uint Voltage { get { return m_Voltage; } }

            /// <summary>
            /// Initializes a new instance of the <see cref="VoltageEventArgs"/> class.
            /// </summary>
            /// <param name="voltage">The battery voltage in millivolts.</param>
            public VoltageEventArgs(uint voltage)
            {
                m_Voltage = voltage;
            }
        }

        /// <summary>
        /// Occurs when the reader reports its battery voltage.
        /// </summary>
        /// <remarks>
        /// The event argument is a <see cref="VoltageEventArgs"/> whose <see cref="VoltageEventArgs.Voltage"/> property holds the measured voltage in millivolts.
        /// </remarks>
        public event EventHandler<VoltageEventArgs> OnVoltageEvent;
        /// <summary>
        /// Occurs when the hardware key of the reader is pressed or released.
        /// </summary>
        /// <remarks>
        /// The event argument is a <see cref="HotKeyEventArgs"/> that identifies the key and tells whether it has been pressed or released.
        /// </remarks>
        public event EventHandler<HotKeyEventArgs> OnKeyEvent;

        /// <summary>
        /// Current Supported Virtual Key
        /// </summary>
        public enum Key : uint
        {
            /// <summary>
            /// Button
            /// </summary>
            BUTTON,
        }

        internal Notification(HighLevelInterface handler)
        {
            _deviceHandler = handler;
        }

        internal void SetAutoReport(bool OnOff)
        {
            if (OnOff)
            {
//                if (!_currentAutoReportStatus)
                {
                    _deviceHandler.SendAsync(0, 2, DOWNLINKCMD.STARTAUTOREPORTING, null, HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE);
                    _currentAutoReportStatus = true;
                }
            }
            else
            {
  //              if (_currentAutoReportStatus)
                {
                    _deviceHandler.SendAsync(0, 2, DOWNLINKCMD.STOPAUTOREPORTING, null, HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE);
                    _currentAutoReportStatus = false;
                }
            }
        }

        internal void SetAutoTriggerReport(byte sec) // 0 = off
        {
            if (sec > 0)
            {
                {
                    _deviceHandler.SendAsync(0, 2, DOWNLINKCMD.STARTAUTOREIGGERREPORTING, new byte[1] { sec }, HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE);
                    _currentAutoReportStatus = true;
                }
            }
            else
            {
                {
                    _deviceHandler.SendAsync(0, 2, DOWNLINKCMD.STOPAUTOREIGGERREPORTING, null, HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE);
                    _currentAutoReportStatus = false;
                }
            }
        }


        /// <summary>
        /// Represents the command that starts the automatic reporting of the hardware key state.
        /// </summary>
        /// <remarks>
        /// The command carries the reporting interval in seconds as payload; the stop command switches the reporting off.
        /// </remarks>
        public static readonly byte[] STARTAUTOREIGGERREPORTING = { 0xA0, 0x08 };
        /// <summary>
        /// Represents the command that stops the automatic reporting of the hardware key state.
        /// </summary>
        public static readonly byte[] STOPAUTOREIGGERREPORTING = { 0xA0, 0x09 };



        internal void GetCurrentBatteryVoltage()
        {
            _deviceHandler.SendAsync(0, 2, DOWNLINKCMD.GETVOLTAGE, null, HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.NOWAIT);
        }


        internal void DeviceRecvVoltage(uint voltagemV)
        {
            _batteryLevel = voltagemV;

            if (OnVoltageEvent == null)
                return;

            OnVoltageEvent(_deviceHandler, new Notification.VoltageEventArgs(voltagemV));
        }

        bool _receiveOffWithin1s = false;
        bool _receiveOnWithinOffcycle = false;

        internal void DeviceRecvState(int value)
        {
            if (OnKeyEvent == null)
                return;

            try
            {
                switch (value)
                {
                    case 0: // button on
                        OnKeyEvent(_deviceHandler, new Notification.HotKeyEventArgs(Key.BUTTON, true));
                        break;

                    case 1: // button off
                        OnKeyEvent(_deviceHandler, new Notification.HotKeyEventArgs(Key.BUTTON, false));
                        break;
                }
            }
            catch (Exception ex)
            {
                CSLibrary.Debug.WriteLine("DeviceRecvState Error : " + ex.Message);
            }
        }

        /// <summary>
        /// Gets the last battery voltage reported by the reader.
        /// </summary>
        /// <returns>
        /// The battery voltage in millivolts, or <c>0</c> when no voltage has been reported yet.
        /// </returns>
        public uint GetCurrentBatteryLevel()
        {
            return _batteryLevel;
        }

        /// <summary>
        /// Configures whether the reader aborts the running RFID inventory when the trigger is released.
        /// </summary>
        /// <param name="enable"><see langword="true"/> to abort the inventory on trigger release, <see langword="false"/> to let it run until it is stopped.</param>
        public void SetTriggerReleaseAbortRFID(bool enable)
        {
            if (enable)
            {
                _deviceHandler.SendAsync(0, 2, DOWNLINKCMD.RFIDABORT, new byte[] { 0x01 }, HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE);
            }
            else
            {
                _deviceHandler.SendAsync(0, 2, DOWNLINKCMD.RFIDABORT, new byte[] { 0x00 }, HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE);
            }
        }

        /// <summary>
        /// Removes all subscribers of the <see cref="OnVoltageEvent"/> and <see cref="OnKeyEvent"/> events.
        /// </summary>
        public void ClearEventHandler()
        {
            //OnVoltageEvent = delegate { };
            //OnKeyEvent = delegate { };
            OnVoltageEvent = null;
            OnKeyEvent = null;
        }

    }
}
