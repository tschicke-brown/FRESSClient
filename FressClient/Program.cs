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

        static async Task Main(string[] args)
        {
            Rebex.Licensing.Key = "==AKOz7Fgv0W1Kau2iJwlo61vuaQ1v05EfMcFXUgg6T5rQ==";
            await Run(args);
        }

        public static Font Font;
        public static readonly uint FontSize = 14;

        public static Buffer MainBuffer;

        static async Task Run(string[] args)
        {
            var window = new RenderWindow(new VideoMode(640, 480), "FRESS");
            window.KeyPressed += WindowOnKeyPressed;
            window.TextEntered += Window_TextEntered;
            window.Closed += WindowOnClosed;
            Font = new Font("resources/courier.ttf");

            MainBuffer = new Buffer(30);

            while (window.IsOpen)
            {
                window.Clear(new SFML.Graphics.Color(0, 0, 30));
                window.DispatchEvents();

                window.Draw(MainBuffer);

                window.Display();
            }

            if (args.Length < 2)
            {
                Console.WriteLine("Not enough arguments");
                Console.Read();
                return;
            }

            var ip = args[0];
            var port = int.Parse(args[1]);
            var server = new Telnet(ip, port);
            var scripting = server.StartScripting();
            scripting.Timeout = 300;

            var buffer = new byte[1024];
            Console.WriteLine("Hello World!");
            string line;
            while ((line = Console.ReadLine()) != null)
            {
                if(!string.IsNullOrWhiteSpace(line))
                    scripting.Send(line);
                var res = scripting.ReadUntil(ScriptEvent.Timeout);
                if (!string.IsNullOrEmpty(res))
                {
                    Console.WriteLine(res);
                }
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
            MainBuffer.HandleText(e);
        }

        private static void WindowOnClosed(object sender, EventArgs eventArgs)
        {
            (sender as Window)?.Close();
        }
    }
}
