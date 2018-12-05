using Rebex.Net;
using Rebex.TerminalEmulation;
using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Color = SFML.Graphics.Color;

namespace FressClient
{
    class Program
    {

        static void Main(string[] args)
        {
            Rebex.Licensing.Key = "==AKOz7Fgv0W1Kau2iJwlo61vuaQ1v05EfMcFXUgg6T5rQ==";
            new Program().Run(args);
        }

        public static Font Font;
        public static readonly uint FontSize = 18;

        public Buffer CommandBuffer, ErrorBuffer;
        public Buffer[] Buffers;
        public Buffer CurrentBuffer => Buffers[CurrentBufferIndex];
        public int CurrentBufferIndex;

        public List<Button> Buttons = new List<Button>();

        public WindowConfig CurrentConfig = WindowConfig.None;

        public enum WindowConfig
        {
            None,
            Config_1A,
            Config_1B, //Don't use
            Config_2A,
            Config_2B,
            Config_3A,
            Config_3B,
            Config_4A,
        }

        [Flags]
        public enum Flag1
        {
            InputMode = 0x1,
            TxWindowDimensions = 0x2,
            TxSpecialMessage = 0x4,
            TxBinaryData = 0x8,
        }

        [Flags]
        public enum Flag2
        {
            MsgLineInBuffer = 0x1,
            Unlightpennable = 0x2,
            StartsInItalic = 0x4,
        }

        public static float CharWidth { get; private set; }
        public static float CharHeight { get; private set; }

        class Sender
        {
            // EBCDIC to ASCII
            private string[] CP37 =
            {
                /*          0123456789ABCDEF  */
                /* 0- */ "                ",
                /* 1- */ "                ",
                /* 2- */ "                ",
                /* 3- */ "                ",
                /* 4- */ "           .<(+|",
                /* 5- */ "&         !$*);~",
                /* 6- */ "-/        |,%_>?",
                /* 7- */ "         `:#@'=\"",
                /* 8- */ " abcdefghi      ",
                /* 9- */ " jklmnopqr      ",
                /* A- */ " ~stuvwxyz      ",
                /* B- */ "^         []  ' ",
                /* C- */ "{ABCDEFGHI-     ",
                /* D- */ "}JKLMNOPQR      ",
                /* E- */ "\\ STUVWXYZ      ",
                /* F- */ "0123456789      "
            };

            private byte[] ASCIItoEBCDIC =
            {
                0x00, 0x01, 0x02, 0x03, 0x1A, 0x09, 0x1A, 0x7F,
                0x1A, 0x1A, 0x1A, 0x0B, 0x0C, 0x0D, 0x0E, 0x0F,
                0x10, 0x11, 0x12, 0x13, 0x3C, 0x3D, 0x32, 0x26,
                0x18, 0x19, 0x3F, 0x27, 0x1C, 0x1D, 0x1E, 0x1F,
                0x40, 0x4F, 0x7F, 0x7B, 0x5B, 0x6C, 0x50, 0x7D,
                0x4D, 0x5D, 0x5C, 0x4E, 0x6B, 0x60, 0x4B, 0x61,
                0xF0, 0xF1, 0xF2, 0xF3, 0xF4, 0xF5, 0xF6, 0xF7,
                0xF8, 0xF9, 0x7A, 0x5E, 0x4C, 0x7E, 0x6E, 0x6F,
                0x7C, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7,
                0xC8, 0xC9, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6,
                0xD7, 0xD8, 0xD9, 0xE2, 0xE3, 0xE4, 0xE5, 0xE6,
                0xE7, 0xE8, 0xE9, 0x4A, 0xE0, 0x5A, 0x5F, 0x6D,
                0x79, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87,
                0x88, 0x89, 0x91, 0x92, 0x93, 0x94, 0x95, 0x96,
                0x97, 0x98, 0x99, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6,
                0xA7, 0xA8, 0xA9, 0xC0, 0x6A, 0xD0, 0xA1, 0x07,
            };

            private byte ToASCII(byte b)
            {
                return (byte)CP37[b / 16][b % 16];
            }

            private byte ToEBCDIC(byte b)
            {
                return b < 0x80 ? ASCIItoEBCDIC[b] : (byte)0x3F;
            }

            public byte[] ToEBDIC(string s)
            {
                return Encoding.ASCII.GetBytes(s).Select(ToEBCDIC).ToArray();
            }

            public string ToASCII(string s)
            {
                return Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(s).Select(ToASCII).ToArray());
            }
        }

        void SetWindowConfig(WindowConfig config)
        {
            if (config == CurrentConfig)
            {
                return;
            }
            switch (config)
            {
                case WindowConfig.Config_1A:
                    Buffers = new[] { new Buffer(new Vector2i(130, 40)) };
                    break;
                case WindowConfig.Config_2A:
                    Buffers = new[] { new Buffer(new Vector2i(130, 20)), new Buffer(new Vector2i(130, 20)) { Position = new Vector2f(0, CharHeight * 20) } };
                    break;
                case WindowConfig.Config_2B:
                    Buffers = new[] { new Buffer(new Vector2i(65, 40)), new Buffer(new Vector2i(65, 40)) { Position = new Vector2f(CharWidth * 65, 0) } };
                    break;
                case WindowConfig.Config_3B:
                    Buffers = new[]{
                        new Buffer(new Vector2i(65, 40)),
                        new Buffer(new Vector2i(65, 20)){Position = new Vector2f(CharWidth * 65, 0)},
                        new Buffer(new Vector2i(65, 20)){Position = new Vector2f(CharWidth * 65, CharHeight * 20)}
                    };
                    break;
                case WindowConfig.Config_3A:
                    Buffers = new[]{
                        new Buffer(new Vector2i(130, 20)),
                        new Buffer(new Vector2i(65, 20)){Position = new Vector2f(0, CharHeight * 20)},
                        new Buffer(new Vector2i(65, 20)){Position = new Vector2f(CharWidth * 65, CharHeight * 20)}
                    };
                    break;
                case WindowConfig.Config_4A:
                    Buffers = new[]{
                        new Buffer(new Vector2i(65, 20)),
                        new Buffer(new Vector2i(65, 20)){Position = new Vector2f(CharWidth * 65, 0)},
                        new Buffer(new Vector2i(65, 20)){Position = new Vector2f(0, CharHeight * 20)},
                        new Buffer(new Vector2i(65, 20)){Position = new Vector2f(CharWidth * 65, CharHeight * 20)}
                    };
                    break;
                default:
                    return;
            }

            foreach (var buffer in Buffers)
            {
                var bufferPosition = buffer.Position;
                bufferPosition.Y += 2 * CharHeight;
                buffer.Position = bufferPosition;
            }

            CurrentBufferIndex = 0;

            CurrentConfig = config;
        }

        public void SetWindowConfig(string config)
        {
            if (Enum.TryParse($"config_{config}", false, out WindowConfig result))
            {
                SetWindowConfig(result);
            }
        }

        public void SetCurrentWindow(int index)
        {
            if (index >= 0 && index < Buffers.Length)
            {
                CurrentBuffer.Active = false;
                CurrentBufferIndex = index;
                CurrentBuffer.Active = true;
            }
        }

        private void ParseResponse(string res)
        {
            string[] contents = res.Split('|');
            foreach (string content in contents)
            {
                if (content.StartsWith('\\')) //Command
                {
                    int windowNumber = content[1] - '0';
                    int currentWindowNumber = content[2] - '0';
                    Flag1 flag1 = (Flag1)(content[3] - '0');
                    Flag2 flag2 = (Flag2)(content[4] - '0');

                    if (flag1.HasFlag(Flag1.TxWindowDimensions))
                    {
                        int windowConfig = content[5] - '0';
                        SetWindowConfig(((WindowConfig[])Enum.GetValues(typeof(WindowConfig)))[windowConfig]);
                    }

                    SetCurrentWindow(currentWindowNumber - 1);

                    Console.WriteLine($"Window Number: {windowNumber}, CurrentWindow: {currentWindowNumber}, Flag1: {flag1}, Flag2: {flag2}");

                    if (flag1.HasFlag(Flag1.TxSpecialMessage))
                    {
                        int space = content.IndexOf(' ');
                        if (space >= 0)
                        {
                            string text = content.Substring(space + 1);
                            //Console.WriteLine($"Data: {text}");
                            if (text.Any())
                            {
                                ErrorBuffer.BufferText = text.Replace("\r", "");
                            }
                        }
                    }
                    else if (!flag1.HasFlag(Flag1.TxBinaryData))
                    {
                        int space = content.IndexOf(' ');
                        if (space >= 0)
                        {
                            string text = content.Substring(space + 1);
                            //Console.WriteLine($"Data: {text}");
                            if (text.Any())
                            {
                                Buffers[windowNumber - 1].BufferText = text.Replace("\r", "");
                            }
                        }
                    }
                }
            }
        }

        public Scripting Scripting;
        private void SubmitCommand(string command)
        {
            if (!string.IsNullOrWhiteSpace(command))
            {
                Scripting.Send(command + "\r\n");
            }

            string res = Scripting.ReadUntil(ScriptEvent.Timeout);
            if (res != null)
            {
                ParseResponse(res);
            }
        }

        private Button AddButton(string name, string command)
        {
            var button = new Button(name);
            button.Tapped += b => CommandBuffer.Append(command);
            return button;
        }

        private void AddButtons()
        {
            var patterns = new[]
            {
                ("Left end defer", "="),
                ("Right end defer", "=?"),
                ("Resolve deferred", "?/"),
                ("Choose word", "-W"),
                ("Choose line", "-L"),
                ("Choose order", "-O"),
            };

            var editing = new[]
            {
                ("Delete text", "d"),
                ("Insert text", "i"),
                ("Insert annotation", "ia"),
                ("Move text", "m"),
                ("Move from workspace", "mws"),
                ("Move to label", "mt"),
                ("Revert", "rev"),
            };

            var viewing = new[]
            {
                ("Change window", "cw/"),
                ("Display space", "ds/"),
                ("Display viewspecs", "dv/"),
                ("Set viewspaces", "sv/"),
                ("Set keyword display request", "skd/"),
                ("Query all files", "q/f"),
            };

            var structure = new[]
            {
                ("Block trail continuous", "bt/"),
                ("Block trail discrete", "btd/"),
                ("Insert block", "ib"),
                ("Insert decimal block", "idb"),
                ("Make decimal block", "mdb"),
                ("Make decimal reference", "mdr"),
                ("Insert Annotation", "ia"),
                ("Make annotation", "ma"),
                ("Refer to annotation", "rta"),
                ("Make jump", "mj"),
                ("Make label", "ml"),
                ("Make splice", "ms"),
                ("Split editing area", "sa"),
            };

            var navigation = new[]
            {
                ("Jump", "j"),
                ("Locate", "l"),
                ("Get label", "gl"),
                ("Get decimal label", "gdl"),
                ("Return", "r"),
                ("Ring forwards", "r/f"),
                ("Ring backwards", "r/b"),
                ("Trail forwards", "tr/f"),
                ("Trail backwards", "tr/b"),
            };

            var menus = new[]
            {
                ("Pattern", patterns),
                ("Editing", editing),
                ("Viewing", viewing),
                ("Structure", structure),
                ("Navigation", navigation),
            };

            var yOff = 0;
            foreach (var menu in menus)
            {
                foreach (var menuItem in menu.Item2)
                {
                    var button = AddButton(menuItem.Item1, menuItem.Item2);
                    yOff += 12;
                    button.Position = new Vector2f(20, yOff);
                    Buttons.Add(button);
                }
            }
        }

        private void Run(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Not enough arguments");
                Console.Read();
                return;
            }

            Font = new Font("resources/Inconsolata-Regular.ttf");
            CharWidth = Font.GetGlyph('a', FontSize, false, 0).Advance;
            CharHeight = Font.GetLineSpacing(FontSize);

            RenderWindow window = new RenderWindow(new VideoMode((uint) (CharWidth * 65 * 2), (uint) (CharHeight * 43)), "FRESS");
            window.KeyPressed += WindowOnKeyPressed;
            window.TextEntered += Window_TextEntered;
            window.MouseButtonReleased += Window_MouseButtonReleased;
            window.Closed += WindowOnClosed;

            RenderWindow commandWindow = new RenderWindow(new VideoMode(400, 700), "Commands");
            commandWindow.MouseButtonReleased += CommandWindowOnMouseButtonReleased;

            CommandBuffer = new Buffer(new Vector2i(65, 1)) {Position = new Vector2f(0, CharHeight)};
            ErrorBuffer = new Buffer(new Vector2i(65, 1)) {Position = new Vector2f(CharWidth * 65, CharHeight)};
            SetWindowConfig(WindowConfig.Config_1A);
            SetCurrentWindow(0);

            AddButtons();

            string ip = args[0];
            int port = int.Parse(args[1]);
            Telnet server = new Telnet(ip, port);
            Scripting = server.StartScripting(new TerminalOptions() {TerminalType = TerminalType.Ansi, NewLineSequence = NewLineSequence.CRLF});
            var scripting = Scripting;
            scripting.Timeout = 300;

            string res = scripting.ReadUntil(ScriptEvent.Timeout);
            Console.WriteLine(res);
            scripting.Send(ConsoleKey.Enter, 0);
            res = scripting.ReadUntil(ScriptEvent.Timeout);
            Console.WriteLine(res);
            if (res.StartsWith("CMS"))
            {

            }
            else
            {
                scripting.Send("l dgd plasmate");
                Console.WriteLine(scripting.ReadUntil(ScriptEvent.Timeout));
                scripting.Send("b");
                Console.WriteLine(scripting.ReadUntil(ScriptEvent.Timeout));
                scripting.Send(ConsoleKey.Enter, 0);
                res = scripting.ReadUntil(ScriptEvent.Timeout);
                Console.WriteLine(res);
                if (!res.StartsWith("CMS"))
                {
                    //Console.WriteLine("Error connecting to 370");
                    //return;
                }
            }

            var initialCommand = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(initialCommand))
            {
                SubmitCommand(initialCommand);
            }

            while (window.IsOpen)
            {
                window.Clear(new Color(0, 0, 50));
                commandWindow.Clear(new Color(0xc0, 0xc0, 0xc0));
                window.DispatchEvents();
                commandWindow.DispatchEvents();

                foreach (Buffer buffer in Buffers)
                {
                    window.Draw(buffer);
                }

                window.Draw(CommandBuffer);
                window.Draw(ErrorBuffer);

                foreach (var rectangleShape in Buttons)
                {
                    commandWindow.Draw(rectangleShape);
                }

                commandWindow.Display();
                window.Display();
            }
        }

        private void CommandWindowOnMouseButtonReleased(object sender, MouseButtonEventArgs mouseButtonEventArgs)
        {
            if (mouseButtonEventArgs.Button != Mouse.Button.Left)
            {
                return;
            }
            foreach (var button in Buttons)
            {
                button.TestTapped(mouseButtonEventArgs.X, mouseButtonEventArgs.Y);
            }
        }

        private void Window_MouseButtonReleased(object sender, MouseButtonEventArgs e)
        {
            if (e.Button != Mouse.Button.Left)
            {
                return;
            }
            foreach (var buffer in Buffers)
            {
                var bounds = new FloatRect(buffer.Position, new Vector2f(buffer.CharacterSize.X * CharWidth, buffer.CharacterSize.Y * CharHeight));
                if (bounds.Contains(e.X, e.Y))
                {
                    buffer.HandleMouse(e.X, e.Y);
                    break;
                }
            }
        }

        private void WindowOnKeyPressed(object sender, KeyEventArgs keyEventArgs)
        {
            switch (keyEventArgs.Code)
            {
                case Keyboard.Key.Left:
                    CommandBuffer.CursorLeft();
                    break;
                case Keyboard.Key.Right:
                    CommandBuffer.CursorRight();
                    break;
                default:
                    break;
            }
        }

        private void Window_TextEntered(object sender, TextEventArgs e)
        {
            ErrorBuffer.BufferText = "";
            if (e.Unicode == "\r")
            {
                var command = CommandBuffer.BufferText;
                CommandBuffer.BufferText = "";
                SubmitCommand(command);
            }
            else
            {
                CommandBuffer.HandleText(e);
            }

        }

        private void WindowOnClosed(object sender, EventArgs eventArgs)
        {
            (sender as Window)?.Close();
        }
    }
}
