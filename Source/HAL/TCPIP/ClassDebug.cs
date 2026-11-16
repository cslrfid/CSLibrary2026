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


#if TCP
using System;
using System.Net;

namespace CSLibrary
{
    /// <summary>
    /// Debug output helper for the TCP/IP transport.
    /// </summary>
    public static class CSLDebug
    {
        /// <summary>
        /// Writes a byte buffer to the debug output as hexadecimal text.
        /// </summary>
        /// <param name="header">A label written in front of the data.</param>
        /// <param name="data">The buffer to write.</param>
        /// <param name="len">The number of bytes to write, or -1 to write the whole buffer.</param>
        public static void WriteBytes(string header, byte[] data, int len = -1)
        {
            if (len == -1)
                len = data.Length;
            string str = "";
            for (int cnt = 0; cnt < len; cnt++)
                str += data[cnt].ToString("X2") + " ";
            WriteLine("CSLibrary : " + header + " {0}:{1}", len, str);
        }

        /// <summary>
        /// Writes a formatted line to the debug output.
        /// </summary>
        /// <param name="format">The composite format string.</param>
        /// <param name="args">The values to format.</param>
        public static void WriteLine(string format, params object[] args)
        {
            System.Diagnostics.Debug.WriteLine(string.Format(format, args));
        }

        /// <summary>
        /// Writes formatted text to the debug output.
        /// </summary>
        /// <param name="format">The composite format string.</param>
        /// <param name="args">The values to format.</param>
        public static void Write(string format, params object[] args)
        {
            System.Diagnostics.Debug.Write(string.Format(format, args));
        }
    }
}
#endif