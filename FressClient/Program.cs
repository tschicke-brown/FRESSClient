using System;
using System.Collections.Specialized;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Rebex.Net;
using Rebex.TerminalEmulation;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace FressClient
{
    class Program
    {

        static void Main(string[] args)
        {
            Rebex.Licensing.Key = "==AKOz7Fgv0W1Kau2iJwlo61vuaQ1v05EfMcFXUgg6T5rQ==";
            Run(args);
        }

        public static Font Font;
        public static readonly uint FontSize = 14;

        public static Buffer MainBuffer, OtherBuffer;

        static void Run(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Not enough arguments");
                Console.Read();
                return;
            }

            Font = new Font("resources/courier.ttf");
            var charWidth = Font.GetGlyph('a', FontSize, false, 0).Bounds.Width;

            var window = new RenderWindow(new VideoMode((uint) (charWidth * 66 * 2), 800), "FRESS");
            window.KeyPressed += WindowOnKeyPressed;
            window.TextEntered += Window_TextEntered;
            window.Closed += WindowOnClosed;

            MainBuffer = new Buffer(65);
            OtherBuffer = new Buffer(65){Position = new Vector2f(charWidth * 65, 0)};

            Task.Run(() =>
            {
                var ip = args[0];
                var port = int.Parse(args[1]);
                var server = new Telnet(ip, port);
                var scripting = server.StartScripting();
                scripting.Timeout = 300;

                string line;
                while ((line = Console.ReadLine()) != null)
                {
                    if (!string.IsNullOrWhiteSpace(line))
                        scripting.Send(line);
                    var res = scripting.ReadUntil(ScriptEvent.Timeout);
                    if (!string.IsNullOrEmpty(res))
                    {
                        Console.WriteLine(res);
                        MainBuffer.Append(res);
                        OtherBuffer.Append(res);
                    }
                }
            });

            while (window.IsOpen)
            {
                window.Clear(new SFML.Graphics.Color(0, 0, 30));
                window.DispatchEvents();

                window.Draw(MainBuffer);
                window.Draw(OtherBuffer);

                window.Display();
            }
        }

        private static void WindowOnKeyPressed(object sender, KeyEventArgs keyEventArgs)
        {
            switch (keyEventArgs.Code)
            {
                case Keyboard.Key.Left:
                    MainBuffer.CursorLeft();
                    break;
                case Keyboard.Key.Right:
                    MainBuffer.CursorRight();
                    break;
                default:
                    break;
            }
        }

        private static void Window_TextEntered(object sender, TextEventArgs e)
        {
            if (e.Unicode == "\t")
            {
                var buffer = MainBuffer;
                MainBuffer = OtherBuffer;
                OtherBuffer = buffer;
            }
            else
            {
                MainBuffer.HandleText(e);
            }
        }

        private static void WindowOnClosed(object sender, EventArgs eventArgs)
        {
            (sender as Window)?.Close();
        }
    }
}
