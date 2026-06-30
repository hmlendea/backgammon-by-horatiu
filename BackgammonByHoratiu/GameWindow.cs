using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NuciXNA.DataAccess.Content;
using NuciXNA.Graphics;
using NuciXNA.Gui;
using NuciXNA.Gui.Screens;
using NuciXNA.Input;
using NuciXNA.Primitives;

using BackgammonByHoratiu.Gui;
using BackgammonByHoratiu.Gui.Screens;
using BackgammonByHoratiu.Settings;

namespace BackgammonByHoratiu
{
    public class GameWindow : Game
    {
        readonly GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        readonly FpsIndicator fpsIndicator;
        readonly Cursor cursor;

        Texture2D handPickingTexture;
        Texture2D handGrabbingTexture;
        Texture2D handOpenTexture;
        Texture2D diceTexture;

        public static CursorType ActiveCursor { get; set; }

        public GameWindow()
        {
            graphics = new GraphicsDeviceManager(this)
            {
                PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8,
                PreferredBackBufferWidth = GameDefines.WindowWidth,
                PreferredBackBufferHeight = GameDefines.WindowHeight
            };
            Content.RootDirectory = "Content";
            IsMouseVisible = false;

            fpsIndicator = new FpsIndicator();
            cursor = new Cursor
            {
                ContentFile = "Cursors/pointer",
                SpriteSize = new Size2D(442, 409),
                Scale = new Scale2D(28.0f / 409.0f)
            };
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);

            GraphicsManager.Instance.SpriteBatch = spriteBatch;
            GraphicsManager.Instance.Graphics = graphics;

            NuciContentManager.Instance.LoadContent(Content, GraphicsDevice);
            SettingsManager.Instance.LoadContent();

            ScreenManager.Instance.SpriteBatch = spriteBatch;
            ScreenManager.Instance.StartingScreenType = typeof(SplashScreen);
            ScreenManager.Instance.LoadContent();

            fpsIndicator.LoadContent();
            cursor.LoadContent();
            handPickingTexture = NuciContentManager.Instance.LoadTexture2D("Cursors/hand_picking");
            handGrabbingTexture = NuciContentManager.Instance.LoadTexture2D("Cursors/hand_holding");
            handOpenTexture = NuciContentManager.Instance.LoadTexture2D("Cursors/hand_open");
            diceTexture = NuciContentManager.Instance.LoadTexture2D("Cursors/dice");
        }

        protected override void UnloadContent()
        {
            ScreenManager.Instance.UnloadContent();
            FpsIndicator.UnloadContent();
            cursor.UnloadContent();
        }

        protected override void Update(GameTime gameTime)
        {
            SettingsManager.Instance.Update();
            ScreenManager.Instance.Update(gameTime);

            if (IsActive)
            {
                InputManager.Instance.Update(Window);
            }
            else
            {
                InputManager.Instance.ResetInputStates();
            }

            fpsIndicator.Update(gameTime);
            cursor.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin();

            ScreenManager.Instance.Draw(spriteBatch);

            fpsIndicator.Draw(spriteBatch);

            if (ActiveCursor == CursorType.HandOpen)
            {
                Point2D mousePosition = cursor.Location;
                float scale = 28.0f / 409.0f;
                int drawWidth = (int)(handOpenTexture.Width * scale);
                int drawHeight = (int)(handOpenTexture.Height * scale);
                spriteBatch.Draw(handOpenTexture, new Rectangle(mousePosition.X, mousePosition.Y, drawWidth, drawHeight), Color.White);
            }
            else if (ActiveCursor == CursorType.HandGrabbing)
            {
                Point2D mousePosition = cursor.Location;
                float scale = 28.0f / 409.0f;
                int drawWidth = (int)(handGrabbingTexture.Width * scale);
                int drawHeight = (int)(handGrabbingTexture.Height * scale);
                spriteBatch.Draw(handGrabbingTexture, new Rectangle(mousePosition.X, mousePosition.Y, drawWidth, drawHeight), Color.White);
            }
            else if (ActiveCursor == CursorType.HandPicking)
            {
                Point2D mousePosition = cursor.Location;
                float scale = 28.0f / 409.0f;
                int drawWidth = (int)(handPickingTexture.Width * scale);
                int drawHeight = (int)(handPickingTexture.Height * scale);
                spriteBatch.Draw(handPickingTexture, new Rectangle(mousePosition.X, mousePosition.Y, drawWidth, drawHeight), Color.White);
            }
            else if (ActiveCursor == CursorType.Dice)
            {
                Point2D mousePosition = cursor.Location;
                float scale = 28.0f / 409.0f;
                int drawWidth = (int)(diceTexture.Width * scale);
                int drawHeight = (int)(diceTexture.Height * scale);
                spriteBatch.Draw(diceTexture, new Rectangle(mousePosition.X, mousePosition.Y, drawWidth, drawHeight), Color.White);
            }
            else
            {
                cursor.Draw(spriteBatch);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
