using System;
using System.Collections.Generic;
using System.Text;

namespace CSLibrary
{
    using static RFIDDEVICE;

    public partial class RFIDReader
    {
        public decimal GetNoiseFloor()
        {
            decimal noiselvl = 0;
            UInt32 freq;

            switch (_deviceType)
            {
                case MODEL.CS710S:
                    noiselvl = GetNoiseFloot_CS710S(922750) / 100; // 922.75MHz
                    return noiselvl;
            }

            return decimal.MinValue;
        }
    }
}
