using System;
using System.Collections.Generic;
using System.Text;

using CSLibrary;
using CSLibrary.Constants;
using CSLibrary.Structures;
using CSLibrary.Events;
using CSLibrary.Tools;

namespace CSLibrary
{
	/// <summary>
	/// Provides access to the EM4325 passive sensor module built into the reader.
	/// </summary>
	/// <remarks>
	/// The instance is reached through <see cref="RFIDReader.EM4325"/>; the parameters of a request are set in <see cref="Options"/> before the operation is started.
	/// </remarks>
	public class ClassEM4325
	{
		/// <summary>
		/// Defines the operations that can be performed on the EM4325 sensor module.
		/// </summary>
		public enum Operation
		{
			/// <summary>
			/// Reads the temperature measured by the EM4325 sensor.
			/// </summary>
			GETSENSORDATA
		}

		/// <summary>
		/// Holds the parameters of the <see cref="Operation.GETSENSORDATA"/> request and the measurement it returns.
		/// </summary>
		public class GETSENSORDATAPARAMETERS
		{
			/// <summary>
			/// Specifies whether the tag identifier is transmitted together with the sensor data.
			/// </summary>
			public bool sendUID = false;
			/// <summary>
			/// Specifies whether the module takes a new temperature measurement instead of returning the last stored value.
			/// </summary>
			public bool newSample = false;
			/// <summary>
			/// Represents the temperature measured by the sensor, in degrees Celsius.
			/// </summary>
			/// <remarks>
			/// The value is filled in with a resolution of 0.25 degree when the operation completes and is <c>0</c> until then.
			/// </remarks>
			public double temperatureC = 0;
		}

		/// <summary>
		/// Groups the parameters of the EM4325 module operations.
		/// </summary>
		public class EM4325Paras
		{
			/// <summary>
			/// Represents the parameters of the <see cref="Operation.GETSENSORDATA"/> request.
			/// </summary>
			public GETSENSORDATAPARAMETERS GetSensorData = new GETSENSORDATAPARAMETERS();
		}

		/// <summary>
		/// Carries the result of a completed EM4325 module operation.
		/// </summary>
		public class OnAccessCompletedEventArgs : EventArgs
		{
			/// <summary>
			/// Represents the operation the event belongs to.
			/// </summary>
			public Operation operation;
			/// <summary>
			/// Represents the outcome of the operation.
			/// </summary>
			/// <remarks>
			/// The value is <see langword="true"/> when the operation completed successfully.
			/// </remarks>
			public bool success;

			/// <summary>
			/// Initializes a new instance of the <see cref="OnAccessCompletedEventArgs"/> class.
			/// </summary>
			/// <param name="operation">The operation the event belongs to.</param>
			/// <param name="success"><see langword="true"/> when the operation completed successfully, <see langword="false"/> otherwise.</param>
			public OnAccessCompletedEventArgs(Operation operation, bool success)
			{
				this.operation = operation;
				this.success = success;
			}
		}


		/// <summary>
		/// Occurs when an EM4325 module operation has completed.
		/// </summary>
		/// <remarks>
		/// The event argument tells which operation has finished and whether it succeeded; the measurement itself is read back from <see cref="Options"/>.
		/// </remarks>
		public event EventHandler<OnAccessCompletedEventArgs> OnAccessCompleted;
		/// <summary>
		/// Represents the parameters of the EM4325 module operations.
		/// </summary>
		/// <remarks>
		/// Set the parameters before calling <see cref="StartOperation(Operation)"/> and read the results from the same object once the operation has completed.
		/// </remarks>
		public EM4325Paras Options = new EM4325Paras();

		private HighLevelInterface _deviceHandler;

		internal ClassEM4325(HighLevelInterface handler)
		{
			_deviceHandler = handler;
		}

		/// <summary>
		/// Removes all subscribers of the <see cref="OnAccessCompleted"/> event.
		/// </summary>
		public void ClearEventHandler()
		{
			OnAccessCompleted = null;
		}

		// call by Application
		/// <summary>
		/// Starts the given operation on the EM4325 sensor module.
		/// </summary>
		/// <param name="operation">The operation to run.</param>
		/// <returns>
		/// <c>0</c> when the operation has been started, <c>-1</c> when the operation is not supported.
		/// </returns>
		/// <remarks>
		/// The operation runs asynchronously and its result is reported through the <see cref="OnAccessCompleted"/> event.
		/// </remarks>
		public int StartOperation(Operation operation)
		{
			switch (operation)
			{
				case Operation.GETSENSORDATA:
					GetSenseDataProc();
					return 0;
			}

			return -1;
		}

		private void GetSensorData(bool sendUID, bool newSample)
		{
			UInt32 value = 0;

			if (sendUID)
				value |= 0x01;

			if (newSample)
				value |= 0x02;

			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.EM4325_CFG, value);
			_deviceHandler.SendAsync(0, 0, RFIDReader.DOWNLINKCMD.RFIDCMD, _deviceHandler.rfid.PacketData(0xf000, (UInt32)RFIDReader.HST_CMD.CUSTOMEMGETSENSORDATA), HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE_COMMANDENDRESPONSE, (uint)CSLibrary.Constants.Operation.EM_GetSensorData);
		}

		private bool GetSenseDataProc()
		{
			GetSensorData(Options.GetSensorData.sendUID, Options.GetSensorData.newSample);
			return true;
		}


		// call by Library core
		/// <summary>
		/// System Operation
		///	Subsystem Operation
		///	Sequence
		/// </summary>
		/// <param name="operation"></param>
		/// <param name="TagAccessPacket"></param>
		/// <returns></returns>
		//internal int TagAccessProc(CSLibrary.Constants.Operation mainOperation, Operation subOperation, int sqr, byte[] TagAccessPacket)
		internal bool TagAccessProc(CSLibrary.Constants.Operation mainOperation, byte[] TagAccessPacket)
		{
			switch (mainOperation)
			{
				case CSLibrary.Constants.Operation.EM_GetSensorData:
					{
						var SensorDataMsw = (TagAccessPacket[32] << 0x08 | TagAccessPacket[33]);
						var Temp = (SensorDataMsw & 0x00ff);
						if ((SensorDataMsw & 0x0100) != 00)
							Temp -= 256;

						Options.GetSensorData.temperatureC = Temp * 0.25;
					}
					return true;
			}

			return false;
		}

		// call by Library core
		internal bool CommandEndProc(CSLibrary.Constants.Operation mainOperation, bool success)
		{
			switch (mainOperation)
			{
				case CSLibrary.Constants.Operation.EM_GetSensorData:
					if (OnAccessCompleted != null)
						OnAccessCompleted(this, new OnAccessCompletedEventArgs(Operation.GETSENSORDATA, success));
					return true;
			}

			return false;
		}
	}
}
