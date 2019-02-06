using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FressClient
{
    public class TelnetSocket
    {
        private TcpClient _client;
        private Stream _stream;
        public class ByteBuffer
        {
            private object _bufferLock = new object();

            Queue<byte[]> bList = new Queue<byte[]>();
            int len = 0;
            int scanpos = 0;


            public void Add(Byte[] buf)
            {
                lock (_bufferLock)
                {
                    len += buf.Length;
                    bList.Enqueue(buf);
                }
            }
            public int Length
            {
                get
                {
                    return len;
                }
            }
            public int Count
            {
                get
                {
                    lock (_bufferLock) return len - scanpos;
                }
            }

            public (bool, byte) Peek
            {
                get
                {
                    lock (_bufferLock) {
                        if (bList.First() == null)
                            return (false, 0);
                        else
                            return (true, bList.First()[scanpos]);
                    }
                }
            }

            public byte[] ReadAll()
            {
                lock (_bufferLock)
                {
                    if (Count == 0) return null;
                    byte[] result = new byte[Count];
                    byte[] first = bList.Dequeue();
                    Array.ConstrainedCopy(first, scanpos, result, 0, first.Length - scanpos);
                    int pos = first.Length - scanpos;
                    while (bList.Any())
                    {
                        byte[] buf = bList.Dequeue();
                        buf.CopyTo(result, pos);
                        pos += buf.Length;
                    }
                    scanpos = 0;
                    len = 0;
                    return result;
                }
            }

        }
        enum TelnetProtocol {
            IAC = 255,
            WILL = 251,
            WONT = 252,
            DO = 253,
            DONT = 254,
            SYNCH = 242
        }
        ByteBuffer _recBuf = new ByteBuffer();

        public TelnetSocket(string server, int port)
        {
            void SendCommand(byte com, byte com2)
            {
                byte[] o = new Byte[] { (byte)TelnetProtocol.IAC, com, com2 };
                Console.WriteLine($"sending {(TelnetProtocol)com}, {com2}!");
                _stream.Write(o, 0, 3);
                _stream.Flush();
            }

            _client = new TcpClient(server, port);
            _stream = _client.GetStream();

            // _stream = Stream.Synchronized(_client.GetStream());
            Task.Run(async () =>
            {
                byte[] termtype = new byte[] { 0xff, 0xfa, 0x18, 0x00, (byte)'a', 0xff, 0xf0 };
                byte[] scanbuf = new byte[256];
                while (true)
                {
                    bool saw_IAC = false;
                    byte open_option = 0;
                    byte sub_option = 0;
                    int skipCount = 0;
                    int bytesRead = await _stream.ReadAsync(scanbuf, 0, scanbuf.Length);
                    if (bytesRead != 0)
                    {
                        // Console.WriteLine($"Read {bytesRead} bytes");
                        int out_pos = 0;
                        for (int i = 0; i < bytesRead; i++)
                        {
                            byte cur = scanbuf[i];
                            if (skipCount > 0)
                            {
                                skipCount--;
                            } else if (sub_option != 0)
                            {
                                skipCount = 3;
                                Console.WriteLine("sending termtype");
                                Write(termtype);
                                sub_option = 0;
                            }
                            else if (!saw_IAC && open_option == 0)
                            {
                                if (cur == (byte)TelnetProtocol.IAC)
                                {
                                    saw_IAC = true;
                                }
                                else if (out_pos != i)
                                {
                                    scanbuf[out_pos++] = cur;
                                }
                                else
                                    out_pos++;
                            } else if (saw_IAC)
                            {
                                saw_IAC = false;
                                Debug.Assert(open_option == 0);
                                if (cur == (byte)TelnetProtocol.IAC) // escaped IAC
                                {
                                    scanbuf[out_pos++] = cur;
                                }
                                else if (cur == 241)
                                { // Command NOP, no PARAM
                                  // start option
                                }
                                else
                                {// not IN IAC, was option
                                    open_option = cur;
                                }
                            } else // Now we know we are in option processing: handle suboption
                            {
                                switch (open_option)
                                {
                                    case 241: // NOP
                                        break;
                                    case 242: // SYNCH
                                        break;
                                    case 250: // sub-negotiate, should skip to end of options, but we're punting
                                        sub_option = cur;
                                        break;
                                    case 251: //WILL
                                        break;
                                    case 252: //WONT
                                        break;
                                    case 253: //DO
                                        if (cur == 24 || cur == 1)
                                        {
                                            SendCommand((byte)TelnetProtocol.WILL, cur);
                                        } else
                                            SendCommand((byte)TelnetProtocol.WONT, cur);
                                        break;
                                    case 254: //DONT
                                        SendCommand((byte)TelnetProtocol.WONT, cur);
                                        break;
                                }
                                open_option = 0;
                            }
                        }
                        // we've processed buffer, but it may be smaller due to telnet specials
                        if (out_pos != 0)
                        {
                            //Console.WriteLine("data available");
                            _recBuf.Add(scanbuf.Take(out_pos).ToArray());
                        }
                    }
                    System.Threading.Thread.Yield();
                }
            });
        }

        private Encoding Latin1 = Encoding.GetEncoding("iso-8859-1");

        public string Read()
        {
            if (_recBuf.Count == 0)
                return null;
            byte[] bytes = _recBuf.ReadAll();
            if (bytes == null || bytes.Length == 0)
            {
                return null;
            }
            string result = Latin1.GetString(bytes, 0, bytes.Length);
            //Console.WriteLine($"Returning {bytes.Length} bytes. '{result}'");
            return result;
        }

        public void Write(byte[] buffer)
        {
            //Console.WriteLine($"Writing {buffer.Length} bytes.");
            _stream.Write(buffer, 0, buffer.Length);
        }

        public static byte[] CRLF = new byte[] { (byte)'\r', (byte)'\n' };
        public void WriteCommand(string buffer)
        {
            //Console.WriteLine($"Writing {buffer.Length} bytes.");
            _stream.Write(Latin1.GetBytes(buffer), 0, buffer.Length);
            _stream.Write(CRLF);
            _stream.Flush();
        }

        public void Write(string s)
        {
            Write(Latin1.GetBytes(s));
        }
    }
}
