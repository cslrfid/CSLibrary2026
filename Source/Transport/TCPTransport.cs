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

THE SOFTWARE IS "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.Threading.Tasks;
using CSLibrary.Tools;

namespace CSLibrary
{
    using static RFIDDEVICE;

    /// <summary>
    /// TCP/IP transport using CSLTCPSTREAM.
    /// Wraps the TCP connection previously embedded in HighLevelInterface
    /// via CodeFileTCPIP.cs (guarded by #if TCP).
    /// </summary>
    public class TCPTransport : ITransport
    {
        private readonly CSLTCPSTREAM _tcpClient;
        private Action<byte[]> _receiveCallback;

        public string ConnectionInfo => _tcpClient != null
            ? $"{_tcpClient.IPAddress}:{_tcpClient.Port}"
            : string.Empty;

        public bool IsConnected => _tcpClient?.IsConnected ?? false;

        public event Action<CSLTCPSTREAM.ConnectionStatus> ConnectionStatusChanged
        {
            add { _tcpClient.ConnectionStatusChanged += value; }
            remove { _tcpClient.ConnectionStatusChanged -= value; }
        }

        public TCPTransport()
        {
            _tcpClient = new CSLTCPSTREAM();
            _tcpClient.DataReceived += OnDataReceived;
        }

        public async Task<bool> ConnectAsync(object[] args)
        {
            if (args == null || args.Length < 2)
                throw new ArgumentException("TCPTransport.ConnectAsync requires (string ipAddress, int port)");

            var ipAddress = args[0] as string;
            var port = (int)args[1];

            if (string.IsNullOrEmpty(ipAddress) || port <= 0)
                throw new ArgumentException("TCPTransport: invalid IP address or port");

            await _tcpClient.ConnectAsync(ipAddress, port);
            return _tcpClient.IsConnected;
        }

        public async Task<int> SendAsync(byte[] data)
        {
            if (_tcpClient == null || !_tcpClient.IsConnected)
                return -1;

            await _tcpClient.SendAsync(data);
            return data.Length;
        }

        public void Disconnect()
        {
            _tcpClient?.DisconnectAsync();
        }

        public void SetReceiveCallback(Action<byte[]> callback)
        {
            _receiveCallback = callback;
        }

        private void OnDataReceived(byte[] data)
        {
            Debug.WriteBytes("TCP recv", data);
            _receiveCallback?.Invoke(data);
        }
    }
}
