using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NuciXNA.Gui.Controls;
using NuciXNA.Input;
using NuciXNA.Primitives;

namespace BackgammonByHoratiu.Gui.Controls
{
    public class GuiButton : GuiControl, IGuiControl
    {
        public string ContentFile { get; set; }

        public InGameButtonIcon Icon { get; set; }

        const int FrameWidth = 280;
        const int FrameHeight = 280;

        GuiImage image;

        protected override void DoLoadContent()
        {
            image = new GuiImage
            {
                Id = $"{Id}_{nameof(image)}",
                ContentFile = ContentFile,
                Size = Size,
                SamplerState = SamplerState.LinearClamp
            };

            RegisterChildren(image);
            SetChildrenProperties();
        }

        protected override void DoUnloadContent() { }

        protected override void DoUpdate(GameTime gameTime)
        {
            SetChildrenProperties();
        }

        protected override void DoDraw(SpriteBatch spriteBatch) { }

        void SetChildrenProperties()
        {
            int frameX;

            if (!IsEnabled)
            {
                frameX = FrameWidth * 3;
            }
            else if (IsHovered && InputManager.Instance.IsMouseButtonDown(MouseButton.Left))
            {
                frameX = FrameWidth * 2;
            }
            else if (IsHovered)
            {
                frameX = FrameWidth;
            }
            else
            {
                frameX = 0;
            }

            int frameY = (int)Icon * FrameHeight;

            image.SourceRectangle = new Rectangle2D(frameX, frameY, FrameWidth, FrameHeight);
            image.Size = Size;
        }
    }
}
