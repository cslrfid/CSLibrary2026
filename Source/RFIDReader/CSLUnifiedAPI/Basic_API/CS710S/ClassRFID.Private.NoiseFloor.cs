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
using System.Text;

namespace CSLibrary
{
    public partial class RFIDReader
    {
        internal Decimal GetNoiseFloot_CS710S(UInt32 FreqHz)
        {
            Int16 pwrlvl = 1;
            decimal pwrlve = 0;

            // Set NoiseFloorFrequency 0x3956
            RFIDRegister.NoiseFloorFrequency.Set(FreqHz);

            //per run inventory
            //RFIDStartSimpleInventory();
            //RFIDStopOperation();

            // Send commnad 0x10D8 to reader
            RFIDGetNoiseFloor();
            

            // Get the noise floor value from register 0x395a
            pwrlvl = RFIDRegister.NoiseFloorValue.Get();
            pwrlve = 1 * (pwrlvl / 100) + 1;

            if (pwrlve < -82)
                pwrlve = CSLibrary.Tools.ClassCS710SStaticNoiseFloor.GetNoiseFloor(RFIDRegister.AntennaPortConfig.GetRfMode(0));

            return pwrlve;
        }
    }
}
