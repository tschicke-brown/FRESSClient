using System;
using System.Collections.Generic;
using System.Text;
using SFML.Graphics;
using SFML.System;
using SFML.Window;

namespace FressClient
{
    public class Buffer : Transformable, Drawable
    {
        private readonly Text _drawableText;
        private RectangleShape _cursor;
        private int _cursorIndex;

        public int CharacterWidth { get; set; }

        public uint CurrentChar
        {
            get
            {
                if (_cursorIndex < BufferText.Length)
                {
                    var curChar = BufferText[_cursorIndex];
                    if (curChar == '\n' || curChar == '\t') return ' ';
                    return curChar;
                }

                return ' ';
            }
        }

        private readonly object _bufferLock = new object();

        public Buffer(int characterWidth)
        {
            _drawableText = new Text(BufferText, Program.Font, Program.FontSize);
            _cursor = new RectangleShape() { FillColor = new Color(Color.White) };
            CharacterWidth = characterWidth;
        }

        public string BufferText { get; private set; } = "";

        public void Draw(RenderTarget target, RenderStates states)
        {
            lock (_bufferLock)
            {
                states.Transform.Combine(Transform);
                var size = _drawableText.Font.GetGlyph('a', _drawableText.CharacterSize, false, 0).Bounds;
                float height = _drawableText.Font.GetLineSpacing(_drawableText.CharacterSize);
                _cursor.Size = new Vector2f(size.Width, height - 1);


                int cursorIndex = _cursorIndex;
                var lines = BufferText.Split("\n");
                Vector2f? cursorPos = null;
                Vector2f currentPosition = new Vector2f(0, 0);
                foreach (var line in lines)
                {
                    var drawable = line;
                    Vector2f? pos;
                    while (drawable.Length > CharacterWidth)
                    {
                        var l = drawable.Substring(0, CharacterWidth);
                        drawable = drawable.Substring(CharacterWidth);
                        pos = DrawString(l, target, states, currentPosition, cursorIndex);
                        cursorPos = cursorPos ?? pos;
                        cursorIndex -= l.Length;
                        currentPosition.Y += height;
                    }

                    pos = DrawString(drawable, target, states, currentPosition, cursorIndex);
                    cursorPos = cursorPos ?? pos;
                    cursorIndex -= drawable.Length + 1;
                    currentPosition.Y += height;
                }

                if (cursorPos == null)
                {
                    Vector2f p = _drawableText.FindCharacterPos((uint) _drawableText.DisplayedString.Length);
                    cursorPos = new Vector2f(p.X, currentPosition.Y - height);
                }

                _cursor.Position = new Vector2f(cursorPos.Value.X, cursorPos.Value.Y + 1);
                target.Draw(_cursor, states);
            }
        }

        private Vector2f? DrawString(string s, RenderTarget target, RenderStates states, Vector2f position, int cursorPos)
        {
            _drawableText.DisplayedString = s;
            _drawableText.Position = position;
            target.Draw(_drawableText, states);
            if (cursorPos <= s.Length && cursorPos >= 0)
            {
                var characterPos = _drawableText.FindCharacterPos((uint) cursorPos);
                return new Vector2f(characterPos.X, position.Y);
            }

            return null;
        }

        public void HandleText(TextEventArgs args)
        {
            lock (_bufferLock)
            {
                var newChar = args.Unicode;
                if (newChar == "\b")
                {
                    Backspace();
                    return;
                }

                if (newChar == "\r") newChar = "\n";
                BufferText = BufferText.Insert(_cursorIndex, newChar);
                CursorRight();
            }
        }

        public void Backspace()
        {
            var str = BufferText;
            if (str.Length > 0 && _cursorIndex > 0)
            {
                BufferText = str.Remove(_cursorIndex - 1, 1);
                CursorLeft();
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
            lock (_bufferLock)
            {
                BufferText += s.Replace("\r", "");
                GoToEnd();
            }
        }
    }
}
