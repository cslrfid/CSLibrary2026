namespace CSLibrary
{
    /// <summary>
    /// Debug output helper for the Bluetooth LE transport.
    /// </summary>
    public static class Debug
    {
        /// <summary>
        /// Writes a byte buffer to the debug output as hexadecimal text.
        /// </summary>
        /// <param name="header">A label written in front of the data.</param>
        /// <param name="data">The buffer to write.</param>
        public static void WriteBytes(string header, byte[] data)
        {
            string str = "";
            for (int cnt = 0; cnt < data.Length; cnt++)
                str += data[cnt].ToString("X2") + " ";
            WriteLine ("CSLibrary : " + header + " {0}:{1}", data.Length, str);
        }

        /// <summary>
        /// Writes a formatted line to the debug output.
        /// </summary>
        /// <param name="format">The composite format string.</param>
        /// <param name="args">The values to format.</param>
        public static void WriteLine(string format, params object[] args)
        {
            System.Console.WriteLine(string.Format(format, args));
            //Plugin.BLE.Abstractions.Trace.Message(string.Format(format, args));
        }

        /// <summary>
        /// Writes formatted text to the debug output.
        /// </summary>
        /// <param name="format">The composite format string.</param>
        /// <param name="args">The values to format.</param>
        public static void Write(string format, params object[] args)
        {
            System.Console.WriteLine(string.Format(format, args));
            //Plugin.BLE.Abstractions.Trace.Message(string.Format(format, args));
        }
    }
}
