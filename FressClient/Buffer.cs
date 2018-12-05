using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System;

namespace FressClient
{
    public class Buffer : Transformable, Drawable
    {
        private readonly Text _drawableText;
        private RectangleShape _cursor;
        private int _cursorIndex;

        public Vector2i CharacterSize { get; set; }

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

        private string _bufferText = "";

        public Buffer(Vector2i characterSize)
        {
            _drawableText = new Text(BufferText, Program.Font, Program.FontSize);
            _cursor = new RectangleShape() { FillColor = new Color(Color.White) };
            CharacterSize = characterSize;
        }

        public string BufferText
        {
            get => _bufferText;
            set
            {
                _bufferText = value;
                _cursorIndex = _bufferText.Length;
            }
        }

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
            foreach (string line in lines)
            {
                if (linesDrawn++ >= CharacterSize.Y)
                {
                    break;
                }
                string drawable = line;
                Vector2f? pos;
                while (drawable.Length > CharacterSize.X)
                {
                    string l = drawable.Substring(0, CharacterSize.X);
                    drawable = drawable.Substring(CharacterSize.X);
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

        private Vector2f? DrawString(string s, RenderTarget target, RenderStates states, Vector2f position, int cursorPos)
        {
            _drawableText.DisplayedString = s;
            _drawableText.Position = position;
            target.Draw(_drawableText, states);
            if (cursorPos <= s.Length && cursorPos >= 0)
            {
                Vector2f characterPos = _drawableText.FindCharacterPos((uint) cursorPos);
                return new Vector2f(characterPos.X, position.Y);
            }

            return null;
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
            BufferText += s.Replace("\r", "");
            GoToEnd();
        }
    }
}
