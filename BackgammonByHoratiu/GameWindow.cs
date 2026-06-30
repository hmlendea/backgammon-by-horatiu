using System.Collections.Generic;

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

        const int CursorTargetHeight = 48;

        static readonly Dictionary<CursorType, string> CursorContentFiles = new()
        {
            [CursorType.Pointer]      = "Cursors/pointer",
            [CursorType.HandPicking]  = "Cursors/hand_picking",
            [CursorType.HandGrabbing] = "Cursors/hand_holding",
            [CursorType.HandOpen]     = "Cursors/hand_open",
            [CursorType.Dice]         = "Cursors/dice",
        };

        readonly Dictionary<CursorType, Scale2D> cursorScales = [];

        readonly FpsIndicator fpsIndicator;
        readonly Cursor cursor;

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
                ContentFile = CursorContentFiles[CursorType.Pointer]
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

            foreach (KeyValuePair<CursorType, string> entry in CursorContentFiles)
            {
                Texture2D texture = NuciContentManager.Instance.LoadTexture2D(entry.Value);
                float scale = (float)CursorTargetHeight / texture.Height;
                cursorScales[entry.Key] = new Scale2D(scale);
            }

            cursor.Scale = cursorScales[CursorType.Pointer];
            cursor.LoadContent();
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

            cursor.ContentFile = CursorContentFiles[ActiveCursor];
            cursor.Scale = cursorScales[ActiveCursor];

            fpsIndicator.Update(gameTime);
            cursor.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.AnisotropicClamp);

            ScreenManager.Instance.Draw(spriteBatch);

            fpsIndicator.Draw(spriteBatch);

            cursor.Draw(spriteBatch);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
