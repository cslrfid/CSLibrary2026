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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CSLibrary.Constants;

namespace CSLibrary
{
    using static FrequencyBand;
    using static FrequencyBand_CS710S;

    public partial class RFIDReader
    {
        /// <summary>
        /// Gets the model name of the connected reader.
        /// </summary>
        /// <returns>
        /// The reader model name, for example <c>CS108</c> or <c>CS710S</c>.
        /// </returns>
        public string GetModelName()
        {
            return m_oem_machine.ToString();
        }

        /// <summary>
        /// Gets the country and regulatory version part of the reader model name.
        /// </summary>
        /// <returns>
        /// The country part of the model name, for example <c>-2 RW</c>.
        /// </returns>
        public string GetModelCountry()
        {
            return DEVICE.GetModelName((int)m_oem_country_code, (int)m_oem_special_country_version);
        }

        /// <summary>
        /// Gets the full model name of the connected reader, including the country and regulatory version part.
        /// </summary>
        /// <returns>
        /// The model name followed by the country part, for example <c>CS108-2 RW</c>.
        /// </returns>
        public string GetFullModelName()
        {
            return m_oem_machine.ToString() + DEVICE.GetModelName((int)m_oem_country_code, (int)m_oem_special_country_version);
        }

        // Get Active Country Name List
        private string[] GetActiveRegionNameList_CS710S()
        {
            string SubModel = GetModelCountry();

            List<string> ActiveCountryNameList = new List<string>();

            foreach (var i in FrequencyBand_CS710S.frequencySet)
                //if (i.modelCode.Equals(SubModel))
                if (i.modelCode == SubModel)
                    ActiveCountryNameList.Add(i.name);

            if (ActiveCountryNameList.Count == 0)
                return null;

            return ActiveCountryNameList.ToArray();
        }

        /// <summary>
        /// CS710S-specific implementation that reports whether the country with the given name uses frequency hopping.
        /// </summary>
        public bool IsHopping_CS710S(string CountryName)
        {
            var item = FrequencyBand_CS710S.frequencySet.Find(i => i.name.Equals(CountryName));

            if (item == null)
            {
                return false;
            }

            return (item.hopping == "Hop");
        }

        /// <summary>
        /// CS710S-specific implementation that reports whether the country at the given index uses frequency hopping.
        /// </summary>
        public bool IsHopping_CS710S(int index)
        {
            return (FrequencyBand_CS710S.frequencySet[index].hopping == "Hop");
        }

        /// <summary>
        /// CS710S-specific implementation that reports whether the country with the given name uses a fixed frequency channel instead of channel hopping.
        /// </summary>
        public bool IsFixed_CS710S(string CountryName)
        {
            var item = FrequencyBand_CS710S.frequencySet.Find(i => i.name.Equals(CountryName));

            if (item == null)
            {
                return false;
            }

            return (item.hopping == "Fixed");
        }

        /// <summary>
        /// CS710S-specific implementation that reports whether the country at the given index uses a fixed frequency channel instead of channel hopping.
        /// </summary>
        public bool IsFixed_CS710S(int index)
        {
            return (FrequencyBand_CS710S.frequencySet[index].hopping == "Fixed");
        }

        internal Result SetRegion_CS710S(string CountryName, int Channel = 0)                                        // Select Country Frequency with channel if fixed
        {
            var item = FrequencyBand_CS710S.frequencySet.Find(i => i.name.Equals(CountryName));

            if (item == null)
                return Result.FAILURE;

            return SetCountry(item.index, Channel);
        }

        /// <summary>
        /// CS710S-specific implementation that selects the country and the frequency channel used by the reader.
        /// </summary>
        /// <param name="CountryIndex">The index of the country in the reader country table.</param>
        /// <param name="Channel">The channel to use when the country is a fixed channel country; it is ignored for hopping countries.</param>
        public Result SetCountry_CS710S(int CountryIndex, int Channel = 0)                                        // Select Country Frequency with channel if fixed
        {
            RFIDRegister.CountryEnum.Set((UInt16)CountryIndex);

            if (IsFixed_CS710S(CountryIndex))
                RFIDRegister.FrequencyChannelIndex.Set((byte)(Channel + 1));
            else
                RFIDRegister.FrequencyChannelIndex.Set(0);

            return Result.OK;
        }

        /// <summary>
        /// CS710S-specific implementation that gets the frequencies available in the country with the given name.
        /// </summary>
        /// <param name="CountryName">The name of the country as listed in the reader country table.</param>
        /// <returns>
        /// The available frequencies in MHz, or <c>null</c> when the country name is not found.
        /// </returns>
        public double[] GetAvailableFrequencyTable_CS710S(string CountryName)									// Get Available frequency table with country code
        {
            var item = FrequencyBand_CS710S.frequencySet.Find(i => i.name.Equals(CountryName));

            if (item == null)
                return null;

            return GetAvailableFrequencyTable_CS710S(item.index);
        }

        internal double[] GetAvailableFrequencyTable_CS710S(int index)									// Get Available frequency table with country code
        {
            double firstChannel = FrequencyBand_CS710S.frequencySet[index].firstChannel;
            double lastChannel = FrequencyBand_CS710S.frequencySet[index].lastChannel + 0.1; // Fix double value compare error (C# bug)
            int totalFrequencyChannel = FrequencyBand_CS710S.frequencySet[index].totalFrequencyChannel;
            double channelSepatration = (double)(FrequencyBand_CS710S.frequencySet[index].channelSepatration) / 1000;
            double[] FreqTable = new double[totalFrequencyChannel];

            if (index == 74)
            {
                FreqTable[0] = 916.8;
                FreqTable[1] = 918;
                FreqTable[2] = 919.2;
                FreqTable[3] = 920.4;
                FreqTable[4] = 920.6;
                FreqTable[5] = 920.8;
            }
            else
            {
                double freq = firstChannel;
                for (int i = 0;
                    freq <= lastChannel && i < totalFrequencyChannel;
                    freq += channelSepatration)
                    FreqTable[i++] = freq;
            }

            return FreqTable;
        }

        /// <summary>
        /// CS710S-specific implementation that gets the frequencies available in the currently selected country.
        /// </summary>
        /// <returns>
        /// The available frequencies in MHz, or <c>null</c> when the frequency table is not available.
        /// </returns>
        /// <remarks>
        /// The current implementation always returns <c>null</c>.
        /// </remarks>
        public List<double> GetCurrentFrequencyTable_CS710S()														// Get frequency table on current selected region
        {
            //return GetAvailableFrequencyTable_CS710S(1m_save_region);

            return null;
        }

        /// <summary>
        /// CS710S-specific implementation that gets the name of the currently selected country.
        /// </summary>
        /// <returns>
        /// The name of the country selected in the reader country register.
        /// </returns>
        public string GetCurrentCountry_CS710S()
        {
            return FrequencyBand_CS710S.frequencySet[RFIDRegister.CountryEnum.Get()].name;
        }

        /// <summary>
        /// CS710S-specific implementation that gets the index of the currently selected country.
        /// </summary>
        /// <returns>
        /// The country index currently selected in the reader country register.
        /// </returns>
        public int GetCurrentCountryIndex_CS710S()
        {
            return RFIDRegister.CountryEnum.Get();
        }

        int GetCurrentFrequencyChannel_CS710S()
        {
            return RFIDRegister.FrequencyChannelIndex.Get();
        }
    }
}
