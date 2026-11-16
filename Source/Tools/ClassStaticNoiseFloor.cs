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

using System.Collections.Generic;

namespace CSLibrary.Tools
{
    /// <summary>
    /// A single row of the CS710S estimated noise floor table.
    /// Each row describes one RF mode (link profile).
    /// </summary>
    public class ClassCS710SNoiseFloorEntry
    {
        /// <summary>
        /// Estimated static noise floor of the RF mode, in dBm.
        /// </summary>
        public decimal NoiseFloor { get; set; }

        /// <summary>
        /// Uplink modulation Miller code of the RF mode.
        /// A value of <c>1</c> means FM0.
        /// </summary>
        public int UplinkModulationMiller { get; set; }

        /// <summary>
        /// Theoretical SNR requirement of the uplink Miller code, in dB.
        /// </summary>
        public int TheoreticalSnrRequirementOfMiller { get; set; }
    }

    /// <summary>
    /// Static noise floor reference for the CS710S RFID reader.
    /// When no tag is present, the receiver reports a baseline noise level
    /// depending on the selected RF mode (link profile).
    ///
    /// <para>
    /// <b>Source of the data:</b> <i>Estimated Noise Floor CS710S-2,
    /// RF modes based on sensitivity</i>.
    /// </para>
    ///
    /// <para>
    /// <b>Unit of measurement:</b> all power values returned by this class
    /// (both the static noise floor entries in <see cref="Table"/> and the
    /// values returned by <see cref="GetNoiseFloor(int)"/>) are expressed in
    /// <b>dBm</b> (decibels relative to one milliwatt).
    /// </para>
    ///
    /// <para>
    /// The values below are baseline reference values and should be treated
    /// as a calibration table. In practice, the actual floor can vary with
    /// antenna, environment, cable loss, and regional configuration.
    /// </para>
    /// </summary>
    public static class ClassCS710SStaticNoiseFloor
    {
        /// <summary>
        /// Unit of measurement for all power values exposed by this class.
        /// Always <c>"dBm"</c> (decibels relative to one milliwatt).
        /// </summary>
        public const string PowerUnit = "dBm";

        /// <summary>
        /// Calibration table mapping RF mode (link profile) id to its
        /// estimated static noise floor data.
        ///
        /// <para>
        /// Source: <i>Estimated Noise Floor CS710S-2, RF modes based on sensitivity</i>.
        /// </para>
        ///
        /// <para>
        /// Each row holds the noise floor (dBm), the uplink modulation Miller code
        /// (1 = FM0) and the theoretical SNR requirement of that Miller code (dB).
        /// </para>
        ///
        /// <para>
        /// <b>TODO:</b> <see cref="ClassCS710SNoiseFloorEntry.UplinkModulationMiller"/> and
        /// <see cref="ClassCS710SNoiseFloorEntry.TheoreticalSnrRequirementOfMiller"/> are still
        /// placeholders (0) and have to be filled in manually.
        /// </para>
        /// </summary>
        public static readonly Dictionary<int, ClassCS710SNoiseFloorEntry> Table = new Dictionary<int, ClassCS710SNoiseFloorEntry>
        {
            { 302, new ClassCS710SNoiseFloorEntry { NoiseFloor = -88.74m, UplinkModulationMiller = 1, TheoreticalSnrRequirementOfMiller = 11 } },
            { 103, new ClassCS710SNoiseFloorEntry { NoiseFloor = -88.55m, UplinkModulationMiller = 1, TheoreticalSnrRequirementOfMiller = 11 } },
            { 104, new ClassCS710SNoiseFloorEntry { NoiseFloor = -90.85m, UplinkModulationMiller = 1, TheoreticalSnrRequirementOfMiller = 11 } },
            { 120, new ClassCS710SNoiseFloorEntry { NoiseFloor = -92.55m, UplinkModulationMiller = 2, TheoreticalSnrRequirementOfMiller = 8 } },
            { 324, new ClassCS710SNoiseFloorEntry { NoiseFloor = -91.85m, UplinkModulationMiller = 2, TheoreticalSnrRequirementOfMiller = 8 } },
            { 4324, new ClassCS710SNoiseFloorEntry { NoiseFloor = -98.05m, UplinkModulationMiller = 2, TheoreticalSnrRequirementOfMiller = 8 } },
            { 323, new ClassCS710SNoiseFloorEntry { NoiseFloor = -91.94m, UplinkModulationMiller = 2, TheoreticalSnrRequirementOfMiller = 8 } },
            { 4323, new ClassCS710SNoiseFloorEntry { NoiseFloor = -94.01m, UplinkModulationMiller = 2, TheoreticalSnrRequirementOfMiller = 8 } },
            { 325, new ClassCS710SNoiseFloorEntry { NoiseFloor = -92.74m, UplinkModulationMiller = 4, TheoreticalSnrRequirementOfMiller = 3 } },
            { 326, new ClassCS710SNoiseFloorEntry { NoiseFloor = -90.3m, UplinkModulationMiller = 4, TheoreticalSnrRequirementOfMiller = 3 } },
            { 342, new ClassCS710SNoiseFloorEntry { NoiseFloor = -89.25m, UplinkModulationMiller = 4, TheoreticalSnrRequirementOfMiller = 3 } },
            { 4342, new ClassCS710SNoiseFloorEntry { NoiseFloor = -92.35m, UplinkModulationMiller = 4, TheoreticalSnrRequirementOfMiller = 3 } },
            { 343, new ClassCS710SNoiseFloorEntry { NoiseFloor = -88.68m, UplinkModulationMiller = 4, TheoreticalSnrRequirementOfMiller = 3 } },
            { 4343, new ClassCS710SNoiseFloorEntry { NoiseFloor = -92.82m, UplinkModulationMiller = 4, TheoreticalSnrRequirementOfMiller = 3 } },
            { 344, new ClassCS710SNoiseFloorEntry { NoiseFloor = -89.01m, UplinkModulationMiller = 4, TheoreticalSnrRequirementOfMiller = 3 } },
            { 345, new ClassCS710SNoiseFloorEntry { NoiseFloor = -88.49m, UplinkModulationMiller = 4, TheoreticalSnrRequirementOfMiller = 3 } },
            { 4345, new ClassCS710SNoiseFloorEntry { NoiseFloor = -93.05m, UplinkModulationMiller = 4, TheoreticalSnrRequirementOfMiller = 3 } },
            { 382, new ClassCS710SNoiseFloorEntry { NoiseFloor = -93.44m, UplinkModulationMiller = 8, TheoreticalSnrRequirementOfMiller = 0 } },
            { 4382, new ClassCS710SNoiseFloorEntry { NoiseFloor = -96.4m, UplinkModulationMiller = 8, TheoreticalSnrRequirementOfMiller = 0 } },
            { 385, new ClassCS710SNoiseFloorEntry { NoiseFloor = -88.31m, UplinkModulationMiller = 8, TheoreticalSnrRequirementOfMiller = 0 } },
            { 4385, new ClassCS710SNoiseFloorEntry { NoiseFloor = -98.51m, UplinkModulationMiller = 8, TheoreticalSnrRequirementOfMiller = 0 } },
            { 386, new ClassCS710SNoiseFloorEntry { NoiseFloor = -89.91m, UplinkModulationMiller = 8, TheoreticalSnrRequirementOfMiller = 0 } },
            { 4386, new ClassCS710SNoiseFloorEntry { NoiseFloor = -94.56m, UplinkModulationMiller = 8, TheoreticalSnrRequirementOfMiller = 0 } },
            { 202, new ClassCS710SNoiseFloorEntry { NoiseFloor = -88.97m, UplinkModulationMiller = 1, TheoreticalSnrRequirementOfMiller = 11 } },
            { 203, new ClassCS710SNoiseFloorEntry { NoiseFloor = -88.08m, UplinkModulationMiller = 1, TheoreticalSnrRequirementOfMiller = 11 } },
            { 205, new ClassCS710SNoiseFloorEntry { NoiseFloor = -88.69m, UplinkModulationMiller = 1, TheoreticalSnrRequirementOfMiller = 11 } },
            { 225, new ClassCS710SNoiseFloorEntry { NoiseFloor = -90.77m, UplinkModulationMiller = 2, TheoreticalSnrRequirementOfMiller = 8 } },
            { 226, new ClassCS710SNoiseFloorEntry { NoiseFloor = -88.79m, UplinkModulationMiller = 3, TheoreticalSnrRequirementOfMiller = 8 } },
        };

        /// <summary>
        /// Returns the complete table row of the given RF mode, or <c>null</c>
        /// when the RF mode is not part of the table.
        /// </summary>
        public static ClassCS710SNoiseFloorEntry GetEntry(int rfMode)
        {
            if (Table.TryGetValue(rfMode, out var entry))
                return entry;

            return null;
        }

        /// <summary>
        /// Returns the estimated static noise floor of the given RF mode, in dBm,
        /// or <see cref="double.NaN"/> when the RF mode is not part of the table.
        /// </summary>
        public static decimal GetNoiseFloor(int rfMode)
        {
            if (Table.TryGetValue(rfMode, out var entry))
                return entry.NoiseFloor;

            return decimal.MinValue;
        }

        /// <summary>
        /// Returns the uplink modulation Miller code of the given RF mode
        /// (1 = FM0), or <see cref="int.MinValue"/> when the RF mode is not
        /// part of the table.
        /// </summary>
        public static int GetUplinkModulationMiller(int rfMode)
        {
            if (Table.TryGetValue(rfMode, out var entry))
                return entry.UplinkModulationMiller;

            return int.MinValue;
        }

        /// <summary>
        /// Returns the theoretical SNR requirement of the uplink Miller code of
        /// the given RF mode, in dB, or <see cref="double.NaN"/> when the RF
        /// mode is not part of the table.
        /// </summary>
        public static double GetTheoreticalSnrRequirementOfMiller(int rfMode)
        {
            if (Table.TryGetValue(rfMode, out var entry))
                return entry.TheoreticalSnrRequirementOfMiller;

            return double.NaN;
        }
    }
}
