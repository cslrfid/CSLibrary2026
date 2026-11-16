using System;
using System.Collections.Generic;
using System.Text;

namespace CSLibrary
{
    using static FrequencyBand;

    /// <summary>
    /// Provides the country and region data of the CSL RFID readers.
    /// </summary>
    public static class DEVICE
    {
        class COUNTRY
        {
            public int countryCode;
            public int specialCountryCode;
            public string countryName;
            public RegionCode defaultRegion;     // for old API

            public COUNTRY (int countryCode, int specialCountryCode, string countryName, RegionCode defaultRegion)
            {
                this.countryCode = countryCode;
                this.specialCountryCode = specialCountryCode;
                this.countryName = countryName;
                this.defaultRegion = defaultRegion;
            }
        }

        static List<COUNTRY> country;

        static DEVICE ()
        {
            country = new List<COUNTRY>();

            country.Add(new COUNTRY(1, 0x0, "", RegionCode.ETSI));
            country.Add(new COUNTRY(2, 0x0, "", RegionCode.FCC));
            country.Add(new COUNTRY(2, 0x2A2A5257, " RW", RegionCode.FCC));
            country.Add(new COUNTRY(2, 0x4F464341, " OFCA", RegionCode.HK));
            country.Add(new COUNTRY(2, 0x2A2A4153, " AS", RegionCode.AU));
            country.Add(new COUNTRY(2, 0x2A2A4E5A, " NZ", RegionCode.NZ));
            country.Add(new COUNTRY(2, 0x20937846, " ZA", RegionCode.ZA));
            country.Add(new COUNTRY(2, 0x2A2A5347, " SG", RegionCode.SG));
            country.Add(new COUNTRY(2, 0x2A2A5448, " TH", RegionCode.TH));
            country.Add(new COUNTRY(4, 0x0, "", RegionCode.TW));
            country.Add(new COUNTRY(6, 0x0, "", RegionCode.KR));
            country.Add(new COUNTRY(7, 0x0, "", RegionCode.CN));
            country.Add(new COUNTRY(8, 0x0, " JP4", RegionCode.JP));
            country.Add(new COUNTRY(8, 0x2A4A5036, " JP6", RegionCode.JP));
            country.Add(new COUNTRY(9, 0x0, "", RegionCode.ETSIUPPERBAND));
        }

        /// <summary>
        /// Returns the country suffix of the reader model name.
        /// </summary>
        /// <param name="countryCode">The country code reported by the reader.</param>
        /// <param name="specialCountryCode">The special country code reported by the reader.</param>
        /// <returns>
        /// The country suffix of the model name, for example <c>-2 RW</c>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when the country code pair is not known.
        /// </exception>
        public static string GetModelName (int countryCode, int specialCountryCode)
        {
            foreach (var i in country)
                if (i.countryCode == countryCode && i.specialCountryCode == specialCountryCode)
                    return ("-" + countryCode.ToString() + i.countryName);

            throw new ArgumentOutOfRangeException("Country not found.");
        }

        // for old API
        /// <summary>
        /// Returns the default frequency region of the reader country.
        /// </summary>
        /// <param name="countryCode">The country code reported by the reader.</param>
        /// <param name="specialCountryCode">The special country code reported by the reader.</param>
        /// <returns>
        /// The default region code, or <see cref="FrequencyBand.RegionCode.UNKNOWN"/> when the country code pair is not known.
        /// </returns>
        public static RegionCode GetDefauleRegion(int countryCode, int specialCountryCode)
        {
            foreach (var i in country)
                if (i.countryCode == countryCode && i.specialCountryCode == specialCountryCode)
                    return (i.defaultRegion);

            return RegionCode.UNKNOWN;
        }
    }
}
