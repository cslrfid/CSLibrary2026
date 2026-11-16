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
	/// Provides access to the FM13DT160 passive tag module built into the reader.
	/// </summary>
	/// <remarks>
	/// The instance is reached through <see cref="RFIDReader.FM13DT160"/>; the parameters of a request are set in <see cref="Options"/> before the operation is started.
	/// </remarks>
	public class ClassFM13DT160
	{
		/// <summary>
		/// Defines the operations that can be performed on the FM13DT160 tag.
		/// </summary>
		public enum Operation
		{
			/// <summary>
			/// Reads a block of memory from the tag.
			/// </summary>
			READMEMORY,		// Read memory
			/// <summary>
			/// Writes a block of memory to the tag.
			/// </summary>
			WRITEMEMORY,	// Write memory
			/// <summary>
			/// Reads a configuration register of the tag.
			/// </summary>
			READREGISTER,	// Read Register
			/// <summary>
			/// Writes a configuration register of the tag.
			/// </summary>
			WRITEREGISTER,	// Write Register
			/// <summary>
			/// Authenticates the tag so that its protected content can be accessed.
			/// </summary>
			AUTH,			// Auth
			/// <summary>
			/// Reads the temperature measured by the tag.
			/// </summary>
			GETTEMP,		// Get Temp
			/// <summary>
			/// Starts the logging of the sensor measurements in the memory of the tag.
			/// </summary>
			STARTLOG,		// Start Log
			/// <summary>
			/// Stops the logging of the sensor measurements in the memory of the tag.
			/// </summary>
			STOPLOG,		// Stop Log
			/// <summary>
			/// Puts the tag into its deep sleep mode.
			/// </summary>
			DEEPSLEEP,		// Reset to sleep mode
			/// <summary>
			/// Reads the operating mode status of the tag.
			/// </summary>
			OPMODECHK,		// Operation
			/// <summary>
			/// Initializes the register file of the tag.
			/// </summary>
			INITIALREGFILE,	// Init Tag
			/// <summary>
			/// Turns the LED of the tag on for one second.
			/// </summary>
			LEDCTRL			// Tuen on LED 1s
		}

		/// <summary>
		/// offset = Starting address
		/// count = Read size (byte)
		/// data = Result
		/// </summary>
		public class ReadMemoryParms
		{
			/// <summary>
			/// Represents the start address of the memory block to read.
			/// </summary>
			public UInt16 offset;
			/// <summary>
			/// Represents the number of bytes to read; the value must be a multiple of four.
			/// </summary>
			public uint count;		// Byte size (divide by 4)
			/// <summary>
			/// Represents the memory content returned by the reader.
			/// </summary>
			/// <remarks>
			/// The array holds <c>count</c> bytes once the read operation has completed and is <see langword="null"/> before that.
			/// </remarks>
			public byte[] data;
		}

		/// <summary>
		/// offset = Starting address
		/// count = Write size (byte)
		/// data = Data
		/// </summary>
		public class WriteMemoryParms
		{
			/// <summary>
			/// Represents the start address of the memory block to write.
			/// </summary>
			public UInt16 offset;
			/// <summary>
			/// Represents the number of bytes to write; the value must be even and must not exceed four.
			/// </summary>
			public uint count;		// Byte size (max 4)
			/// <summary>
			/// Represents the data written to the tag; the first four bytes of the array are sent to the reader.
			/// </summary>
			public byte[] data;
		}

		/// <summary>
		/// offset = Starting address
		/// count = Write size (byte)
		/// data = Data
		/// </summary>
		public class ReadRegisterParms
		{
			/// <summary>
			/// Represents the address of the register to read; it must be in the range 0xC000 to 0xC0FF.
			/// </summary>
			public UInt16 address;
			/// <summary>
			/// Represents the value of the register returned by the reader.
			/// </summary>
			public UInt16 value;
		}

		/// <summary>
		/// offset = Starting address
		/// count = Read size (byte)
		/// data = Result
		/// </summary>
		public class WriteRegisterParms
		{
			/// <summary>
			/// Represents the address of the register to write.
			/// </summary>
			public UInt16 address;
			/// <summary>
			/// Represents the value written to the register.
			/// </summary>
			public UInt16 value;
		}

		/// <summary>
		/// </summary>
		public class AuthParms
		{
		}

		/// <summary>
		/// </summary>
		public class GetTempParms
		{
			/// <summary>
			/// Defines the kind of measurement requested from the tag.
			/// </summary>
			public enum CMDCFG : UInt32
			{
				/// <summary>
				/// Requests the temperature measured by the tag.
				/// </summary>
				TEMP = 0x86,
				/// <summary>
				/// Requests the battery voltage of the tag.
				/// </summary>
				BATTERY = 0x92
			}

			/// <summary>
			/// Specifies the measurement requested from the tag; it defaults to <see cref="CMDCFG.TEMP"/>.
			/// </summary>
			public UInt32 cmd_cfg = (UInt32)CMDCFG.TEMP;
			/// <summary>
			/// Specifies the address of the tag memory block where the measurement is stored.
			/// </summary>
			public UInt32 ewblock_addr = 0x00;
		}

		/// <summary>
		/// </summary>
		public class StartLogParms
		{
		}

		/// <summary>
		/// </summary>
		public class StopLogParms
		{
			/// <summary>
			/// Represents the password required to stop the logging.
			/// </summary>
			public UInt32 password;
		}

		/// <summary>
		/// Holds the parameters of the <see cref="Operation.DEEPSLEEP"/> request.
		/// </summary>
		public class DeepSleepParms
		{
			/// <summary>
			/// Specifies whether the tag is put into its deep sleep mode.
			/// </summary>
			public bool enable;
		}

		/// <summary>
		/// Holds the parameters and the results of the <see cref="Operation.OPMODECHK"/> request.
		/// </summary>
		public class OpModeChkParms
		{
			/// <summary>
			/// Specifies whether the operating mode check is started.
			/// </summary>
			public bool enable;
			/// <summary>
			/// Represents whether the user access rights of the tag are valid; the value is filled in when the check completes.
			/// </summary>
			public bool user_access_en;
			/// <summary>
			/// Represents whether the tag is currently performing an RTC temperature measurement.
			/// </summary>
			public bool RTC_logging;
			/// <summary>
			/// Represents whether a temperature measurement has been interrupted.
			/// </summary>
			public bool vdet_process_flag;
			/// <summary>
			/// Represents whether the detected light intensity exceeds the configured threshold.
			/// </summary>
			public bool light_chk_flag;
			/// <summary>
			/// Represents whether the battery voltage of the tag is above 0.9 V.
			/// </summary>
			public bool vbat_pwr_flag;
		}

		/// <summary>
		/// Holds the parameters of the <see cref="Operation.LEDCTRL"/> request.
		/// </summary>
		public class LedCtrlParms
		{
			/// <summary>
			/// Specifies whether the LED of the tag is switched on.
			/// </summary>
			public bool enable;
		}

		/// <summary>
		/// Groups the parameters of all FM13DT160 module operations.
		/// </summary>
		public class FM13DT160Paras
		{
			/// <summary>
			/// Represents the parameters of the <see cref="Operation.READMEMORY"/> request.
			/// </summary>
			public ReadMemoryParms ReadMemory = new ReadMemoryParms();
			/// <summary>
			/// Represents the parameters of the <see cref="Operation.WRITEMEMORY"/> request.
			/// </summary>
			public WriteMemoryParms WriteMemory = new WriteMemoryParms();
			/// <summary>
			/// Represents the parameters of the <see cref="Operation.READREGISTER"/> request.
			/// </summary>
			public ReadRegisterParms ReadRegister = new ReadRegisterParms();
			/// <summary>
			/// Represents the parameters of the <see cref="Operation.WRITEREGISTER"/> request.
			/// </summary>
			public WriteRegisterParms WriteRegister = new WriteRegisterParms();
			/// <summary>
			/// Represents the parameters of the <see cref="Operation.AUTH"/> request.
			/// </summary>
			public AuthParms Auth = new AuthParms();
			/// <summary>
			/// Represents the parameters of the <see cref="Operation.GETTEMP"/> request.
			/// </summary>
			public GetTempParms GetTemp = new GetTempParms();
			/// <summary>
			/// Represents the parameters of the <see cref="Operation.STARTLOG"/> request.
			/// </summary>
			public StartLogParms StartLog = new StartLogParms();
			/// <summary>
			/// Represents the parameters of the <see cref="Operation.STOPLOG"/> request.
			/// </summary>
			public StopLogParms StopLog = new StopLogParms();
			/// <summary>
			/// Represents the parameters of the <see cref="Operation.DEEPSLEEP"/> request.
			/// </summary>
			public DeepSleepParms DeepSleep = new DeepSleepParms();
			/// <summary>
			/// Represents the parameters of the <see cref="Operation.OPMODECHK"/> request.
			/// </summary>
			public OpModeChkParms OpModeChk = new OpModeChkParms();
			/// <summary>
			/// Represents the parameters of the <see cref="Operation.LEDCTRL"/> request.
			/// </summary>
			public LedCtrlParms LedCtrl = new LedCtrlParms();
		}

		/// <summary>
		/// Carries the result of a completed FM13DT160 module operation.
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
		/// Occurs when an FM13DT160 module operation has completed.
		/// </summary>
		/// <remarks>
		/// The event argument tells which operation has finished and whether it succeeded; the result of the operation itself is read back from <see cref="Options"/>.
		/// </remarks>
		public event EventHandler<OnAccessCompletedEventArgs> OnAccessCompleted;
		/// <summary>
		/// Represents the parameters of the FM13DT160 module operations.
		/// </summary>
		/// <remarks>
		/// Set the parameters before calling <see cref="StartOperation(Operation)"/> and read the results from the same object once the operation has completed.
		/// </remarks>
		public FM13DT160Paras Options = new FM13DT160Paras();

		private HighLevelInterface _deviceHandler;

		internal ClassFM13DT160(HighLevelInterface handler)
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
		/// Starts the given operation on the FM13DT160 tag.
		/// </summary>
		/// <param name="operation">The operation to run.</param>
		/// <returns>
		/// The value <c>-1</c> in every case, because the operation runs asynchronously and reports its outcome through the <see cref="OnAccessCompleted"/> event.
		/// </returns>
		public int StartOperation(Operation operation)
		{
			switch (operation)
			{
				case Operation.READMEMORY:
					FM13DTReadMemoryThreadProc();
					break;

				case Operation.WRITEMEMORY:
					FM13DTWriteMemoryThreadProc();
					break;

				case Operation.READREGISTER:
					FM13DTReadRegThreadProc();
					break;

				case Operation.WRITEREGISTER:
					FM13DTWriteRegThreadProc();
					break;

				case Operation.AUTH:
					FM13DTAuthThreadProc();
					break;

				case Operation.GETTEMP:
					FM13DTGetTempThreadProc();
					break;

				case Operation.STARTLOG:
					FM13DTStartLogThreadProc();
					break;

				case Operation.STOPLOG:
					FM13DTStopLogChkThreadProc();
					break;

				case Operation.DEEPSLEEP:
					FM13DTDeepSleepThreadProc();
					break;

				case Operation.OPMODECHK:
					FM13DTOpModeChkThreadProc();
					break;

				case Operation.INITIALREGFILE:
					FM13DTInitialRegFileThreadProc();
					break;

				case Operation.LEDCTRL:
					FM13DTLedCtrlThreadProc();
					break;
			}

			return -1;
		}

		private bool FM13DTReadMemoryThreadProc()
		{
			if (Options.ReadMemory.offset > 0xffff)
				return false;

			if ((Options.ReadMemory.count & 0x3) != 0)
				return false;

			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_STARTADDRPAR, (uint)Options.ReadMemory.offset);
			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_READWRITELENPAR, (uint)Options.ReadMemory.count);
			_deviceHandler.SendAsync(0, 0, RFIDReader.DOWNLINKCMD.RFIDCMD, _deviceHandler.rfid.PacketData(0xf000, (UInt32)RFIDReader.HST_CMD.CUSTOMMFM13DTREADMEMORY), HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE_COMMANDENDRESPONSE, (uint)CSLibrary.Constants.Operation.FM13DT_READMEMORY);

			return true;
		}

		private bool FM13DTWriteMemoryThreadProc()
		{
			if (Options.WriteMemory.offset > 0xffff)
				return false;

			if ((Options.WriteMemory.count & 0x1) != 0)
				return false;

			UInt32 value = (UInt32)((Options.WriteMemory.data[0] << 24) | (Options.WriteMemory.data[1] << 16) | (Options.WriteMemory.data[2] << 8) | Options.WriteMemory.data[3]);

			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_STARTADDRPAR, (uint)Options.WriteMemory.offset);
			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_READWRITELENPAR, (uint)Options.WriteMemory.count);
			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_DATAPAR, value);
			//_deviceHandler.SendAsync(0, 0, RFIDReader.DOWNLINKCMD.RFIDCMD, _deviceHandler.rfid.PacketData(0xf000, (UInt32)RFIDReader.HST_CMD.CUSTOMMFM13DTWRITEMEMORY), HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE_COMMANDENDRESPONSE, (uint)CSLibrary.Constants.Operation.FM13DT_WRITEMEMORY);

			return true;
		}

		private bool FM13DTReadRegThreadProc()
		{
			if (Options.ReadRegister.address < 0xc000 || Options.ReadRegister.address > 0xc0ff)
				return false;

			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_REGADDRPAR, (uint)Options.ReadRegister.address);
			_deviceHandler.SendAsync(0, 0, RFIDReader.DOWNLINKCMD.RFIDCMD, _deviceHandler.rfid.PacketData(0xf000, (UInt32)RFIDReader.HST_CMD.CUSTOMMFM13DTREADREG), HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE_COMMANDENDRESPONSE, (uint)CSLibrary.Constants.Operation.FM13DT_READREGISTER);
			return true;
		}

		private bool FM13DTWriteRegThreadProc()
		{
			if (Options.ReadRegister.address < 0xc000 || Options.ReadRegister.address > 0xcff)
				return false;

			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_REGADDRPAR, (uint)Options.WriteRegister.address);
			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_WRITEPAR, (uint)Options.WriteRegister.value);
			_deviceHandler.SendAsync(0, 0, RFIDReader.DOWNLINKCMD.RFIDCMD, _deviceHandler.rfid.PacketData(0xf000, (UInt32)RFIDReader.HST_CMD.CUSTOMMFM13DTWRITEREG), HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE_COMMANDENDRESPONSE, (uint)CSLibrary.Constants.Operation.FM13DT_WRITEREGISTER);
			return true;
		}

		private bool FM13DTAuthThreadProc()
		{
//			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_CMDCFGPAR, (uint)mode);
//			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_PWDPAR, password);

			_deviceHandler.SendAsync(0, 0, RFIDReader.DOWNLINKCMD.RFIDCMD, _deviceHandler.rfid.PacketData(0xf000, (UInt32)RFIDReader.HST_CMD.CUSTOMMFM13DTAUTH), HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE_COMMANDENDRESPONSE, (uint)CSLibrary.Constants.Operation.FM13DT_AUTH);
			return true;
		}

		private bool FM13DTGetTempThreadProc()
		{
			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_CMDCFGPAR, Options.GetTemp.cmd_cfg);
			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_STOBLOCKADDPAR, Options.GetTemp.ewblock_addr);
			_deviceHandler.SendAsync(0, 0, RFIDReader.DOWNLINKCMD.RFIDCMD, _deviceHandler.rfid.PacketData(0xf000, (UInt32)RFIDReader.HST_CMD.CUSTOMMFM13DTGETTEMP), HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE_COMMANDENDRESPONSE, (uint)CSLibrary.Constants.Operation.FM13DT_GETTEMP);
			return true;
		}
		private bool FM13DTStartLogThreadProc()
		{
			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_CMDCFGPAR, 0x00);

//			_deviceHandler.SendAsync(0, 0, RFIDReader.DOWNLINKCMD.RFIDCMD, PacketData(0xf000, (UInt32)RFIDReader.HST_CMD.CUSTOMMFM13DTSTARTLOG), HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE_COMMANDENDRESPONSE, (uint)CSLibrary.Constants.Operation.FM13DT_STARTLOG);
			return true;
		}

		private bool FM13DTStopLogChkThreadProc()
		{
			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_CMDCFGPAR, 0x50);
			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_PWDPAR, Options.StopLog.password);
			_deviceHandler.SendAsync(0, 0, RFIDReader.DOWNLINKCMD.RFIDCMD, _deviceHandler.rfid.PacketData(0xf000, (UInt32)RFIDReader.HST_CMD.CUSTOMMFM13DTSTOPLOG), HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE_COMMANDENDRESPONSE, (uint)CSLibrary.Constants.Operation.FM13DT_STOPLOG);
			return true;
		}
		private bool FM13DTDeepSleepThreadProc()
		{
			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_CMDCFGPAR, 1U);
			_deviceHandler.SendAsync(0, 0, RFIDReader.DOWNLINKCMD.RFIDCMD, _deviceHandler.rfid.PacketData(0xf000, (UInt32)RFIDReader.HST_CMD.CUSTOMMFM13DTDEEPSLEEP), HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE_COMMANDENDRESPONSE, (uint)CSLibrary.Constants.Operation.FM13DT_DEEPSLEEP);
			return true;
		}
		private bool FM13DTOpModeChkThreadProc()
		{
			uint value = 0;

			if (Options.OpModeChk.enable)
				value = 0x01;

			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_CMDCFGPAR, value);
			_deviceHandler.SendAsync(0, 0, RFIDReader.DOWNLINKCMD.RFIDCMD, _deviceHandler.rfid.PacketData(0xf000, (UInt32)RFIDReader.HST_CMD.CUSTOMMFM13DTOPMODECHK), HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE_DATA1, (uint)CSLibrary.Constants.Operation.FM13DT_OPMODECHK);

			return true;
		}

		private bool FM13DTInitialRegFileThreadProc()
		{
			_deviceHandler.SendAsync(0, 0, RFIDReader.DOWNLINKCMD.RFIDCMD, _deviceHandler.rfid.PacketData(0xf000, (UInt32)RFIDReader.HST_CMD.CUSTOMMFM13DTINITIALREGFILE), HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE_COMMANDENDRESPONSE, (uint)CSLibrary.Constants.Operation.FM13DT_INITIALREGFILE);
			return true;
		}

		private bool FM13DTLedCtrlThreadProc()
		{
			uint value = 0;

			if (Options.LedCtrl.enable)
				value = 0x02;

			_deviceHandler.rfid.MacWriteRegister(RFIDReader.MACREGISTER.FM13DT160_CMDCFGPAR, value);
			_deviceHandler.SendAsync(0, 0, RFIDReader.DOWNLINKCMD.RFIDCMD, _deviceHandler.rfid.PacketData(0xf000, (UInt32)RFIDReader.HST_CMD.CUSTOMMFM13DTLEDCTRL), HighLevelInterface.BTWAITCOMMANDRESPONSETYPE.WAIT_BTAPIRESPONSE_DATA1, (uint)CSLibrary.Constants.Operation.FM13DT_LEDCTRL);

			return true;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="operation">The tag operation to perform.</param>
		/// <param name="TagAccessPacket"></param>
		/// <returns></returns>
		internal bool TagAccessProc(CSLibrary.Constants.Operation operation, byte[] TagAccessPacket)
		{
			var a = 10;

			switch (operation)
			{
				case CSLibrary.Constants.Operation.FM13DT_AUTH:

					break;

				case CSLibrary.Constants.Operation.FM13DT_DEEPSLEEP:
					break;

				case CSLibrary.Constants.Operation.FM13DT_GETTEMP:
					{
						int returnvalue = (TagAccessPacket[TagAccessPacket.Length - 4] << 8) | TagAccessPacket[TagAccessPacket.Length - 3];

						switch (returnvalue)
						{
							case 0x0f:
								break;

							case 0xfffa: // 场能量足够
								return true;

							case 0xfff5: // 场能量不足
								return true;

							case 0xfff0: // 未启动场能量检测
								return true;
						}
					}
					break;

				case CSLibrary.Constants.Operation.FM13DT_INITIALREGFILE:
					{
						var returnvalue = TagAccessPacket[TagAccessPacket.Length - 4];
						if (TagAccessPacket[TagAccessPacket.Length - 4] != 00 || TagAccessPacket[TagAccessPacket.Length - 3] != 00)
							break;
					}
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_LEDCTRL:
					{
						var returnvalue = TagAccessPacket[TagAccessPacket.Length - 4];
					}
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_OPMODECHK:
					{
						/*
												result[15:14]：= 00 RFU
												result[13]：= 1 user_access_en，高有效，表示当前用户权限是否有效
												result[12]：= 0 RTC logging，高有效，表示当前处于rtc测温流程
												result[11]：= 0 vdet_process_flag，高有效，表示一次测温流程被打断
												result[10]：= 0 RFU
												result[9]：= 0 light_chk_flag，高表示光强超过预设值
												result[8]：= 1 vbat_pwr_flag，高表示电池电压高于0.9V

												result[7:4]：= 0000 RFU
												result[3:0]: = 0001 RFU
						*/
						var returnvalue = TagAccessPacket[TagAccessPacket.Length - 4];

						Options.OpModeChk.user_access_en = (returnvalue >> 5 & 0x01) != 0;
						Options.OpModeChk.RTC_logging = (returnvalue >> 4 & 0x01) != 0;
						Options.OpModeChk.vdet_process_flag = (returnvalue >> 3 & 0x01) != 0;
						Options.OpModeChk.light_chk_flag = (returnvalue >> 1 & 0x01) != 0;
						Options.OpModeChk.vbat_pwr_flag = (returnvalue & 0x01) != 0;
					}
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_READMEMORY:
					{
						if (TagAccessPacket.Length != (Options.ReadMemory.count + 20))
							break;

						Options.ReadMemory.data = new byte[Options.ReadMemory.count];
						Buffer.BlockCopy(TagAccessPacket, 20, Options.ReadMemory.data, 0, (int)(Options.ReadMemory.count));
					}
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_READREGISTER:
					Options.ReadRegister.value = (UInt16)((TagAccessPacket[20] << 8) | TagAccessPacket[21]);
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_STARTLOG:
					break;

				case CSLibrary.Constants.Operation.FM13DT_STOPLOG:
					break;

				case CSLibrary.Constants.Operation.FM13DT_WRITEMEMORY:
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_WRITEREGISTER:
					return true;
					break;
			}

			return false;
		}

		internal bool CommandEndPProc(CSLibrary.Constants.Operation operation, bool success)
		{
			switch (operation)
			{
				case CSLibrary.Constants.Operation.FM13DT_AUTH:
					if (OnAccessCompleted != null)
						OnAccessCompleted(this, new OnAccessCompletedEventArgs(Operation.AUTH, success));
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_DEEPSLEEP:
					if (OnAccessCompleted != null)
						OnAccessCompleted(this, new OnAccessCompletedEventArgs(Operation.DEEPSLEEP, success));
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_GETTEMP:
					if (OnAccessCompleted != null)
						OnAccessCompleted(this, new OnAccessCompletedEventArgs(Operation.GETTEMP, success));
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_INITIALREGFILE:
					if (OnAccessCompleted != null)
						OnAccessCompleted(this, new OnAccessCompletedEventArgs(Operation.INITIALREGFILE, success));
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_LEDCTRL:
					if (OnAccessCompleted != null)
						OnAccessCompleted(this, new OnAccessCompletedEventArgs(Operation.LEDCTRL, success));
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_OPMODECHK:
					if (OnAccessCompleted != null)
						OnAccessCompleted(this, new OnAccessCompletedEventArgs(Operation.OPMODECHK, success));
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_READMEMORY:
					if (OnAccessCompleted != null)
						OnAccessCompleted(this, new OnAccessCompletedEventArgs(Operation.READMEMORY, success));
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_READREGISTER:
					if (OnAccessCompleted != null)
						OnAccessCompleted(this, new OnAccessCompletedEventArgs(Operation.READREGISTER, success));
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_STARTLOG:
					if (OnAccessCompleted != null)
						OnAccessCompleted(this, new OnAccessCompletedEventArgs(Operation.STARTLOG, success));
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_STOPLOG:
					if (OnAccessCompleted != null)
						OnAccessCompleted(this, new OnAccessCompletedEventArgs(Operation.STOPLOG, success));
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_WRITEMEMORY:
					if (OnAccessCompleted != null)
						OnAccessCompleted(this, new OnAccessCompletedEventArgs(Operation.WRITEMEMORY, success));
					return true;
					break;

				case CSLibrary.Constants.Operation.FM13DT_WRITEREGISTER:
					if (OnAccessCompleted != null)
						OnAccessCompleted(this, new OnAccessCompletedEventArgs(Operation.WRITEREGISTER, success));
					return true;
					break;

				default:
					break;
			}

			return false;
		}
	}
}
