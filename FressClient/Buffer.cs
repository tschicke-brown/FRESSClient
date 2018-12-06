using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace FressClient
{
    public class Buffer : Transformable, Drawable
    {
        private readonly Text _drawableText;
        private RectangleShape _cursor;
        private RectangleShape _border;
        private int _cursorIndex;
        private string _bufferText = "";

        public bool DisplayCursor { get; set; } = false;

        public Vector2i CharacterSize { get; set; }

        public bool Active
        {
            set => _border.OutlineColor = value ? new Color(0xff, 0, 0) : new Color(0x7f, 0x7f, 0x7f);
        }

        public uint CurrentChar
        {
            get
            {
                if (_cursorIndex < BufferText.Length)
                {
                    char curChar = BufferText[_cursorIndex];
                    if (curChar == '\n' || curChar == '\t') return ' ';
                    return curChar;
                }

                return ' ';
            }
        }

        public Buffer(Vector2i characterSize)
        {
            _drawableText = new Text(BufferText, Program.Font, Program.FontSize);
            _cursor = new RectangleShape() { FillColor = new Color(Color.White) };
            _border = new RectangleShape() { FillColor = new Color(0, 0, 0, 0), OutlineColor = new Color(0x7f, 0x7f, 0x7f), OutlineThickness = 1 };
            CharacterSize = characterSize;
        }

        public string BufferText
        {
            get => _bufferText;
            set
            {
                _bufferText = value;
                _cursorIndex = Math.Min(_cursorIndex, _bufferText.Length);
            }
        }

        public event Action<string> TextClicked;

        public void Draw(RenderTarget target, RenderStates states)
        {
            states.Transform.Combine(Transform);
            FloatRect size = _drawableText.Font.GetGlyph('a', _drawableText.CharacterSize, false, 0).Bounds;
            float height = _drawableText.Font.GetLineSpacing(_drawableText.CharacterSize);
            _cursor.Size = new Vector2f(size.Width, height - 1);


            int cursorIndex = _cursorIndex;
            string[] lines = BufferText.Split("\n");
            Vector2f? cursorPos = null;
            Vector2f currentPosition = new Vector2f(0, 0);
            int linesDrawn = 0;
            Text.Styles style = 0;
            foreach (string line in lines)
            {
                if (linesDrawn++ >= CharacterSize.Y)
                {
                    break;
                }
                string drawable = line;
                Vector2f? pos;
                int charsDrawn;
                while (drawable.Length > CharacterSize.X)
                {
                    string l = drawable.Substring(0, CharacterSize.X);
                    drawable = drawable.Substring(CharacterSize.X);
                    (pos, style, charsDrawn) = DrawString(l, target, states, style, currentPosition, cursorIndex);
                    cursorPos = cursorPos ?? pos;
                    cursorIndex -= charsDrawn;
                    currentPosition.Y += height;
                }

                (pos, style, charsDrawn) = DrawString(drawable, target, states, style, currentPosition, cursorIndex);
                cursorPos = cursorPos ?? pos;
                cursorIndex -= charsDrawn + 1;// +1 for new line
                currentPosition.Y += height;
            }

            if (cursorPos == null)
            {
                Vector2f p = _drawableText.FindCharacterPos((uint) _drawableText.DisplayedString.Length);
                cursorPos = new Vector2f(p.X, currentPosition.Y - height);
            }

            if (DisplayCursor)
            {
                _cursor.Position = new Vector2f(cursorPos.Value.X, cursorPos.Value.Y + 1);
                target.Draw(_cursor, states);
            }

            _border.Size = new Vector2f(CharacterSize.X * Program.CharWidth, CharacterSize.Y * Program.CharHeight);
            target.Draw(_border, states);
        }

        private (Vector2f?, Text.Styles, int) DrawString(string s, RenderTarget target, RenderStates states, Text.Styles style, Vector2f position, int cursorPos)
        {
            List<(string, Text.Styles)> SplitString(string str, Text.Styles initialStyle)
            {
                //return new List<(string, Text.Styles)>{(str, 0)};
                List<(string, Text.Styles)> strings = new List<(string, Text.Styles)>();
                int openIndex = -2, closeIndex = -2;
                int start = 0;
                for (int i = 0; i < str.Length;)
                {
                    if (openIndex == -2)
                    {
                        openIndex = str.IndexOf("!(1", i);
                    }

                    if (closeIndex == -2)
                    {
                        closeIndex = str.IndexOf("!)", i);
                    }

                    if (i == openIndex)
                    {
                        if (start != i)
                        {
                            string segment = str.Substring(start, i - start);
                            strings.Add((segment, initialStyle));
                        }
                        initialStyle = Text.Styles.Italic;

                        i += 3;
                        start = i;
                        continue;
                    }

                    if (i == closeIndex)
                    {
                        if (start != i)
                        {
                            string segment = str.Substring(start, i - start);
                            strings.Add((segment, initialStyle));
                        }
                        initialStyle = 0;
                        i += 2;
                        start = i;
                        continue;
                    }

                    ++i;
                }

                if (start != str.Length)
                {
                    string segment = str.Substring(start);
                    strings.Add((segment, initialStyle));
                }

                return strings;
            }

            List<(string, Text.Styles)> splits = SplitString(s, style);
            Vector2f? characterPos = null;
            int count = 0;
            foreach ((string subStr, Text.Styles subStyle) in splits)
            {

                _drawableText.DisplayedString = subStr;
                _drawableText.CharacterSize = style == Text.Styles.Bold ? Program.FontSize - 2 : Program.FontSize;
                _drawableText.Position = position;
                _drawableText.Style = subStyle;
                target.Draw(_drawableText, states);
                if (cursorPos <= subStr.Length && cursorPos >= 0)
                {
                    characterPos = _drawableText.FindCharacterPos((uint)cursorPos);
                    characterPos = new Vector2f(characterPos.Value.X, position.Y);
                }

                position.X += _drawableText.GetLocalBounds().Width;
                count += subStr.Length;
                cursorPos -= subStr.Length;
            }


            return (characterPos, style, count);
        }

        public void HandleText(TextEventArgs args)
        {
            string newChar = args.Unicode;
            if (newChar == "\b")
            {
                Backspace();
                return;
            }

            if (newChar == "\r") newChar = "\n";
            BufferText = BufferText.Insert(_cursorIndex, newChar);
            CursorRight();
        }

        public void Backspace()
        {
            string str = BufferText;
            if (str.Length > 0 && _cursorIndex > 0)
            {
                CursorLeft();
                BufferText = str.Remove(_cursorIndex, 1);
            }
        }

        public void CursorLeft()
        {
            _cursorIndex = Math.Max(0, _cursorIndex - 1);
        }

        public void CursorRight()
        {
            _cursorIndex = Math.Min(BufferText.Length, _cursorIndex + 1);
        }

        public void GoToEnd()
        {
            _cursorIndex = BufferText.Length;
        }

        public void Append(string s)
        {
            BufferText += s.Replace("\r", "");
            GoToEnd();
        }

        public void HandleMouse(float x, float y)
        {
            FloatRect rect = new FloatRect(Position, new Vector2f(Program.CharWidth, Program.CharHeight));
            int col = 0;
            for (int i = 0; i < BufferText.Length; ++i)
            {
                if (col == CharacterSize.X || BufferText[i] == '\n')
                {
                    col = 0;
                    rect.Left = Position.X;
                    rect.Top += rect.Height;
                    if (BufferText[i] == '\n') continue;
                }

                if (rect.Contains(x, y))
                {
                    int end = Math.Min(BufferText.Length, i + 3);
                    int start = end - 3;
                    string str = BufferText.Substring(start, 3);
                    OnTextClicked(str);
                }

                col++;
                rect.Left += rect.Width;
            }
        }

        protected virtual void OnTextClicked(string str)
        {
            Debug.WriteLine(str);
            TextClicked?.Invoke(str);
        }
    }
}
