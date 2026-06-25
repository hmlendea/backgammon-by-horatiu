using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NuciXNA.Gui;
using NuciXNA.Gui.Controls;
using NuciXNA.Gui.Screens;
using NuciXNA.Input;
using NuciXNA.Primitives;

using BackgammonByHoratiu.Settings;

namespace BackgammonByHoratiu.Gui.Screens
{
    public class SplashScreen : Screen
    {
        public float Delay { get; set; }

        public GuiImage LogoImage { get; set; }

        public SplashScreen()
        {
            Delay = 2;
            BackgroundColour = Colour.Black;
        }

        protected override void DoLoadContent()
        {
            LogoImage = new GuiImage { ContentFile = "SplashScreen/Logo" };

            GuiManager.Instance.RegisterControls(LogoImage);
            RegisterEvents();
            SetChildrenProperties();
        }

        protected override void DoUnloadContent() => UnregisterEvents();

        protected override void DoUpdate(GameTime gameTime)
        {
            if (Delay <= 0)
            {
                ChangeScreen();
            }

            Delay -= (float)gameTime.ElapsedGameTime.TotalSeconds;

            SetChildrenProperties();
        }

        protected override void DoDraw(SpriteBatch spriteBatch) { }

        void RegisterEvents()
        {
            InputManager.Instance.KeyboardKeyPressed += OnKeyboardKeyPressed;
            InputManager.Instance.MouseButtonPressed += OnMouseButtonPressed;
        }

        void UnregisterEvents()
        {
            InputManager.Instance.KeyboardKeyPressed -= OnKeyboardKeyPressed;
            InputManager.Instance.MouseButtonPressed -= OnMouseButtonPressed;
        }

        void SetChildrenProperties()
        {
            LogoImage.Location = Point2D.Empty;
            LogoImage.Size = new Size2D(GameDefines.WindowWidth, GameDefines.WindowHeight);
        }

        void ChangeScreen() => ScreenManager.Instance.ChangeScreens<GameplayScreen>();

        void OnKeyboardKeyPressed(object sender, KeyboardKeyEventArgs e) => ChangeScreen();

        void OnMouseButtonPressed(object sender, MouseButtonEventArgs e) => ChangeScreen();
    }
}
