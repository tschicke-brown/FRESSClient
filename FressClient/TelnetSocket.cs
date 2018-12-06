using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FressClient
{
    public class TelnetSocket
    {
        private TcpClient _client;
        private NetworkStream _stream;
        public TelnetSocket(string server, int port)
        {
            _client = new TcpClient(server, port);
            _stream = _client.GetStream();
        }

        public async Task<int> Read(byte[] buffer)
        {
            if (_stream.DataAvailable)
            {
                return await _stream.ReadAsync(buffer, 0, buffer.Length);
            }

            return -1;
        }

        public async Task<bool> Write(byte[] buffer)
        {
            await _stream.WriteAsync(buffer, 0, buffer.Length);
            return true;
        }
    }
}
