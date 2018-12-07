using System;
using System.Collections.Generic;
using System.Text;
using SFML.Graphics;
using SFML.System;

namespace FressClient
{
    public class Button : Transformable, Drawable
    {
        private Text _renderableText;
        private RectangleShape _rectangleShape;
        public Button(string text)
        {
            Text = text;
            _renderableText = new Text(text, Program.Font, Program.MenuFontSize){FillColor = new Color(0, 0, 0)};
            _rectangleShape = new RectangleShape(new Vector2f(200, 100)){FillColor = new Color(0xa0, 0xa0, 0xa0)};
        }

        public string Text { get; set; }
        public Vector2f Size { get; set; }

        public void Draw(RenderTarget target, RenderStates states)
        {
            states.Transform.Combine(Transform);
            _renderableText.DisplayedString = Text;
            _rectangleShape.Size = Size;
            target.Draw(_rectangleShape, states);
            var size = _renderableText.GetLocalBounds();
            _renderableText.Origin = new Vector2f(size.Width / 2, size.Height / 2);
            _renderableText.Position = new Vector2f(_rectangleShape.Size.X / 2, _rectangleShape.Size.Y / 4);
            target.Draw(_renderableText, states);
        }

        public void TestTapped(float x, float y)
        {
            var oldPosition = _rectangleShape.Position;
            _rectangleShape.Position = Position;
            var contains = _rectangleShape.GetGlobalBounds().Contains(x, y);
            _rectangleShape.Position = oldPosition;
            if (contains)
            {
                OnTapped(this);
            }

        }

        public delegate void TappedHandler(Button b);

        public event TappedHandler Tapped;

        protected virtual void OnTapped(Button b)
        {
            Tapped?.Invoke(b);
        }
    }
}
