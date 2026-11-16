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
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSLibrary
{
    using Constants;
    using System.Collections.Generic;
    using System.Linq;
    using static CSLibrary.HighLevelInterface;
    using static RFIDDEVICE;

    public partial class HighLevelInterface
    {
        private CSLibrary.Tools.CSLTCPSTREAM _tcpipClient;
        internal TCPTransport _tcpTransport = new TCPTransport();

        int TCP_Init()
        {
            _tcpipClient = new CSLibrary.Tools.CSLTCPSTREAM();
            _tcpipClient.DataReceived += OnTcpDataReceived;
            return 0;
        }

        private void OnTcpDataReceived(byte[] data)
        {
            CSLibrary.CSLDebug.WriteBytes("TCP receive data ", data);
            CharacteristicOnValueUpdated(data);
        }

        /// <summary>
        /// Connects to a reader over TCP/IP.
        /// </summary>
        /// <param name="ipAddress">The IP address of the reader.</param>
        /// <param name="port">The TCP port the reader listens on.</param>
        /// <returns>
        /// <see langword="true"/> when the connection has been established, <see langword="false"/> when it failed.
        /// </returns>
        /// <remarks>
        /// When the call returns <see langword="true"/> the reader is initialized and the RFID, barcode and notification subsystems are ready to be used; the connection is closed with <see cref="TCP_DisconnectAsync"/>.
        /// </remarks>
        public async Task<bool> ConnectAsync(string ipAddress, int port)
        {
            if (_tcpipClient == null)
                TCP_Init();

            _sp._ConnectionMode = CONNECTIONMODE.TCP;
            await _tcpipClient.ConnectAsync(ipAddress, port);

            if (!_tcpipClient.IsConnected)
                return false;

            // Wire _tcpipClient → HighLevelInterface packet processor
            _tcpipClient.DataReceived -= OnTcpDataReceived;
            _tcpipClient.DataReceived += OnTcpDataReceived;

            _transport = _tcpTransport;
            _readerState = READERSTATE.IDLE;
            BTTimer = new Timer(TimerFunc, this, 0, 1000);
            HardwareInit();

            return true;
        }

        /// <summary>
        /// Reconnects to the reader over TCP/IP after the connection has been lost.
        /// </summary>
        /// <remarks>
        /// The IP address and port of the previous connection are reused and the connection is retried up to ten times with a one second delay; the call does nothing when the reader has never been connected.
        /// </remarks>
        public async Task ReconnectAsync()
        {
            if (_tcpipClient != null)
                await _tcpipClient.ReconnectAsync();
        }

        /// <summary>
        /// Closes the TCP/IP connection to the reader.
        /// </summary>
        /// <remarks>
        /// The transport is released and the common disconnect handling is executed, so the reader is no longer usable until <see cref="ConnectAsync(string, int)"/> is called again.
        /// </remarks>
        public void TCP_DisconnectAsync()
        {
            _transport?.Disconnect();
            _transport = null;

            if (_tcpipClient != null)
                _tcpipClient.DisconnectAsync();

            DisconnectCommon();
        }

        /// <summary>
        /// Gets the state of the TCP/IP connection.
        /// </summary>
        /// <returns>
        /// <see langword="true"/> when the TCP/IP link to the reader is established, <see langword="false"/> otherwise.
        /// </returns>
        public bool IsConnected => _tcpipClient?.IsConnected ?? false;

        /// <summary>
        /// Occurs when the state of the TCP/IP connection changes.
        /// </summary>
        /// <remarks>
        /// The argument is a <see cref="CSLibrary.Tools.CSLTCPSTREAM.ConnectionStatus"/> value that describes the new state, for example <c>Connected</c>, <c>Disconnected</c> or <c>Reconnecting</c>; the handler is attached to the underlying TCP stream only when it exists, so the event must be subscribed after the first connection.
        /// </remarks>
        public event Action<CSLibrary.Tools.CSLTCPSTREAM.ConnectionStatus> ConnectionStatusChanged
        {
            add { if (_tcpipClient != null) _tcpipClient.ConnectionStatusChanged += value; }
            remove { if (_tcpipClient != null) _tcpipClient.ConnectionStatusChanged -= value; }
        }

    }
}
#endif
