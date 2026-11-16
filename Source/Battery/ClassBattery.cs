using System;
using System.Text;

using CSLibrary.Barcode;
using CSLibrary.Barcode.Constants;
using CSLibrary.Barcode.Structures;

namespace CSLibrary
{
	/// <summary>
	/// Provides the battery information of the connected reader.
	/// </summary>
	/// <remarks>
	/// The instance is reached through <see cref="HighLevelInterface.battery"/>.
	/// </remarks>
	public partial class Battery
	{
		HighLevelInterface _deviceHandler;
		uint _pollingTime = 300; // 5 second
		bool _autoBatteryLevel = false;
		DateTime _nextTime = DateTime.MaxValue;

		internal Battery(HighLevelInterface handler)
		{
			_deviceHandler = handler;
		}

		/// <summary>
		/// Gets the state of the automatic battery level reporting.
		/// </summary>
		/// <returns>
		/// <see langword="true"/> when the reader reports its battery level on its own, <see langword="false"/> otherwise.
		/// </returns>
		/// <remarks>
		/// The library does not currently switch the automatic reporting on, so the returned value is always <see langword="false"/>.
		/// </remarks>
		public bool GetCurrentAutoBattryStatus ()
		{
			return _autoBatteryLevel;
		}

		/// <summary>
		/// Sets the interval at which the battery voltage of the reader is polled.
		/// </summary>
		/// <param name="sec">The polling interval in seconds; <c>0</c> disables the polling. The default interval is 300 seconds.</param>
		public void SetPollingTime (uint sec)
		{
			_pollingTime = sec; 
		}

		internal void EnableAutoBatteryLevel ()
		{
//			_autoBatteryLevel = true;
//			_deviceHandler.notification.SetAutoReport(true);
		}

		internal void DisbleAutoBatteryLevel()
		{
//			_autoBatteryLevel = false;
//			_deviceHandler.notification.SetAutoReport(false);
		}

		internal void GetBatteryLevel()
		{
			_deviceHandler.notification.GetCurrentBatteryVoltage();
		}

		internal void Timer ()
		{
			if (_deviceHandler.Status != CSLibrary.HighLevelInterface.READERSTATE.DISCONNECT && !_autoBatteryLevel && _pollingTime != 0)
			{
				if (DateTime.Now >= _nextTime)
				{
					_nextTime = DateTime.Now.AddSeconds(_pollingTime);
					GetBatteryLevel();
				}
			}
		}

	}
}

/*
using System;
using System.Collections.Generic;
using System.Text;

namespace CSLibrary.Battery
{
	class ClassBattery
	{
	}
}
*/