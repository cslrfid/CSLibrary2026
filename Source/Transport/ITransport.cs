/*
Copyright (c) 2018-2026 Convergence Systems Limited

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
using System.Threading.Tasks;

namespace CSLibrary
{
    using static RFIDDEVICE;

    /// <summary>
    /// Transport layer abstraction — BLE and TCP/IP implement this interface.
    /// Allows HighLevelInterface to remain unaware of which transport is in use
    /// while keeping all existing public API overloads intact.
    /// </summary>
    public interface ITransport
    {
        /// <summary>Whether the transport is currently connected.</summary>
        bool IsConnected { get; }

        /// <summary>Connection metadata — BLE: MAC address, TCP: "ip:port".</summary>
        string ConnectionInfo { get; }

        /// <summary>
        /// Establish a connection.
        /// Concrete argument types differ per transport:
        ///   BLE: ConnectAsync((IAdapter adapter, IDevice device, MODEL model))
        ///   TCP: ConnectAsync((string ipAddress, int port))
        /// This is typed as object[] for interface neutrality — each
        /// transport casts and validates its own arguments.
        /// </summary>
        Task<bool> ConnectAsync(object[] args);

        /// <summary>Send raw bytes over the transport.</summary>
        Task<int> SendAsync(byte[] data);

        /// <summary>Close the connection.</summary>
        void Disconnect();

        /// <summary>
        /// Wire up the data-received callback from the transport
        /// into the HighLevelInterface receive handler.
        /// </summary>
        void SetReceiveCallback(Action<byte[]> callback);
    }
}
