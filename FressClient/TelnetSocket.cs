using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace FressClient
{
    public class TelnetSocket
    {
        private TcpClient _client;
        private NetworkStream _stream;

        private Queue<byte> _buffer;
        public TelnetSocket(string server, int port)
        {
            _client = new TcpClient(server, port);
            _stream = _client.GetStream();
            _buffer = new Queue<byte>();
            Task.Run(async () =>
            {
                byte[] buffer = new byte[4096];
                List<byte> bList = new List<byte>();
                List<byte> outBuffer = new List<byte>();
                while (true)
                {
                    //if (!bList.Any() || _stream.DataAvailable)
                    {
                        int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead != 0)
                        {
                            for (int i = 0; i < bytesRead; ++i)
                            {
                                bList.Add(buffer[i]);
                            }
                            Debug.WriteLine($"Read {bytesRead} bytes");
                        }
                    }
                    //else
                    {
                        lock (_bufferLock)
                        {
                            for (int i = 0; i < bList.Count;)
                            {
                                if (bList[i] == 0xFF) //IAC
                                {
                                    byte command = bList[i + 1];
                                    if (command == 0xFF)
                                    {
                                        _buffer.Enqueue(0xFF);
                                        i += 2;
                                    }
                                    else if (command == 0xfa)//suboption
                                    {
                                        byte suboption = bList[i + 2];
                                        byte suboptionOption = bList[i + 3];
                                        if (suboption == 0x18 && suboptionOption == 0x01)
                                        {
                                            outBuffer.Add(0xff);
                                            outBuffer.Add(0xfa);
                                            outBuffer.Add(0x18);
                                            outBuffer.Add(0x00);
                                            outBuffer.AddRange(Encoding.ASCII.GetBytes("XTERM"));
                                            outBuffer.Add(0xFF);
                                            outBuffer.Add(0xF0);
                                        }

                                        i += 6;
                                    }
                                    else
                                    {
                                        byte type = bList[i + 2];
                                        outBuffer.Add(0xFF);
                                        if (command == 253 && type == 0x18)
                                        {
                                            outBuffer.Add(251);
                                        }
                                        else
                                        {
                                            outBuffer.Add(command == 253 ? (byte) 252 : (byte) 254);
                                        }

                                        outBuffer.Add(type);
                                        i += 3;
                                    }
                                }
                                else //Normal data
                                {
                                    _buffer.Enqueue(bList[i]);

                                    i++;
                                }

                            }
                        }

                        bList.Clear();
                        if (outBuffer.Any())
                        {
                            Write(outBuffer.ToArray());
                            outBuffer.Clear();
                        }

                        if (_buffer.Any())
                        {
                            DataAvailable?.Invoke();
                        }
                    }
                }
            });
        }

        public event Action DataAvailable;

        private object _bufferLock = new object();
        public int Read(byte[] buffer)
        {
            lock (_bufferLock)
            {
                int count = Math.Min(_buffer.Count, buffer.Length);

                for (int i = 0; i < count; i++)
                {
                    buffer[i] = _buffer.Dequeue();
                }

                return count;
            }
        }

        public string Read()
        {
            lock (_bufferLock)
            {
                byte[] bytes = new byte[_buffer.Count];
                int bytesRead = Read(bytes);
                if (bytesRead == 0)
                {
                    return null;
                }
                return Encoding.ASCII.GetString(bytes, 0, bytesRead);
            }
        }

        public void Write(byte[] buffer)
        {
            _stream.Write(buffer, 0, buffer.Length);
        }

        public void Write(string s)
        {
            Write(Encoding.ASCII.GetBytes(s));
        }
    }
}
