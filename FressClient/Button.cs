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
            _renderableText = new Text(text, Program.Font, Program.FontSize){FillColor = new Color(0, 0, 0)};
            _rectangleShape = new RectangleShape(new Vector2f(200, 100)){FillColor = new Color(0xa0, 0xa0, 0xa0)};
        }

        public string Text { get; set; }

        public void Draw(RenderTarget target, RenderStates states)
        {
            states.Transform.Combine(Transform);
            _renderableText.DisplayedString = Text;
            var textBounds = _renderableText.GetLocalBounds();
            _rectangleShape.Size = new Vector2f(textBounds.Width + 30, textBounds.Height + 10);
            target.Draw(_rectangleShape, states);
            _renderableText.Position = new Vector2f(15, 5);
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
