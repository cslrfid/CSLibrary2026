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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSLibrary
{
    using static FrequencyBand;
    using Constants;


    public partial class RFIDReader
    {
        private uint m_oem_hipower = 0;

        /// <summary>
        /// Available Maximum Power you can set on specific region
        /// </summary>
        public uint GetActiveMaxPowerLevel_CS710S()
        {
            return 320;
        }

        /// <summary>
        /// Get current power level
        /// </summary>
        public uint SelectedPowerLevel_CS710S
        {
            get
            {
                uint pwrlvl = 0;
                GetPowerLevel_CS710S(ref pwrlvl);
                return pwrlvl;
            }
        }

        /// <summary>
        /// Get Power Level
        /// </summary>
        public Result GetPowerLevel_CS710S(ref uint pwrlvl)
        {
            pwrlvl = RFIDRegister.AntennaPortConfig.GetPower();
            return Result.OK;
        }

        /// <summary>
        /// CS710S-specific implementation of <see cref="SetPowerLevel(uint, int)"/>.
        /// </summary>
        /// <param name="pwrlevel">The transmit power level in 0.1 dBm units, for example 300 for 30.0 dBm; values above 330 are clamped to 330.</param>
        /// <param name="port">The logical antenna port index the power level is applied to, or -1 to apply it to every enabled antenna port.</param>
        public Result SetPowerLevel_CS710S(uint pwrlevel, int port = -1)
        {
            if (pwrlevel < 0)
                return Result.INVALID_PARAMETER;

            if (pwrlevel > 330)
                pwrlevel = 330;

            RFIDRegister.AntennaPortConfig.SetPower((UInt16)(pwrlevel * 10), port);
            return Result.OK;
        }

/*
        public Result SetPowerLevel_CS710S(int pwrlevel, uint port = 0)
        {
            return SetPowerLevel_CS710S((uint)pwrlevel, port);
        }
*/

        /// <summary>
        /// CS710S-specific implementation of <see cref="SetPowerLevel(uint[])"/>.
        /// </summary>
        /// <param name="pwrlevel">The transmit power level of each antenna port in 0.1 dBm units; the array index is the antenna port index.</param>
        public Result SetPowerLevel_CS710S(UInt32 [] pwrlevel)
        {
            Result r;

            for (int cnt = 0; cnt < pwrlevel.Length; cnt++)
                if ((r = SetPowerLevel_CS710S(pwrlevel[cnt], cnt)) != Result.OK)
                    return r;

            return Result.OK;
        }

        /// <summary>
        /// Available Maximum Power you can set on specific region
        /// </summary>
        private uint GetSoftwareMaxPowerLevel_CS710S(RegionCode region)
        {
            // MAX Power 32dB
            return 300;
        }

        /// <summary>
        /// Set Power Sequencing (only for CS108)
        /// </summary>
        /// <param name="numberofPower">The number of antenna ports in the power sequence.</param>
        /// <param name="power">The output power of each antenna port, in units of 0.01 dBm.</param>
        /// <param name="dwell">The dwell time of each antenna port, in milliseconds.</param>
        /// <param name="CloneAntenna0Setting">Whether the antenna 0 settings are copied to the other ports.</param>
        /// <returns>The result of the operation.</returns>
        public Result SetPowerSequencing_CS710S(int numberofPower, uint[] power = null, uint[] dwell = null, bool CloneAntenna0Setting = true)
        {
            int i;

            if (numberofPower == 0)
            {
                try
                {
                    AntennaPortSetState(0, AntennaPortState.ENABLED);

                    for (i = 1; i < 16; i++)
                        AntennaPortSetState_CS710S((uint)i, AntennaPortState.DISABLED);
                }
                catch (Exception ex)
                {
                    CSLibrary.Debug.WriteLine("Set Antenna Configuration Fail : " + ex.Message);
                }
                return Result.OK;
            }

            if (power == null || dwell == null || power.Length < numberofPower || dwell.Length < numberofPower)
                return Result.INVALID_PARAMETER;

            for (i = 0; i < numberofPower; i++)
            {
                AntennaPortSetState_CS710S((uint)i, AntennaPortState.ENABLED);
                SetPowerLevel_CS710S(power[i], i);
                SetInventoryDuration_CS710S(dwell[i], (uint)i);
            }

            if (CloneAntenna0Setting && numberofPower >= 2)
                RFIDRegister.AntennaPortConfig.CloneAntenna0Setting();

            for (; i < 16; i++)
                AntennaPortSetState_CS710S((uint)i, AntennaPortState.DISABLED);
            //AntennaPortSetState_CS710S(i, 15, AntennaPortState.DISABLED);

            return Result.OK;
        }
    }
}
