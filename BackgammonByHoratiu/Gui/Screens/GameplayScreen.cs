using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using NuciXNA.Gui;
using NuciXNA.Gui.Screens;
using NuciXNA.Input;
using NuciXNA.Primitives;

using BackgammonByHoratiu.Entities;
using BackgammonByHoratiu.GameLogic.GameManagers;
using BackgammonByHoratiu.Gui.Controls;
using BackgammonByHoratiu.Settings;

namespace BackgammonByHoratiu.Gui.Screens
{
    public class GameplayScreen : Screen
    {
        IGameManager game;
        GuiGameBoard gameBoard;

        int dragBeginCol = -1;

        public GameplayScreen()
        {
            BackgroundColour = Colour.Black;
            ForegroundColour = Colour.White;
        }

        protected override void DoLoadContent()
        {
            game = new GameManager();
            game.LoadContent();

            gameBoard = new GuiGameBoard(game)
            {
                Size = new Size2D(GameDefines.WindowWidth, GameDefines.WindowHeight)
            };

            GuiManager.Instance.RegisterControls(gameBoard);
            RegisterEvents();
            SetChildrenProperties();
        }

        protected override void DoUnloadContent()
        {
            game.UnloadContent();
            UnregisterEvents();
        }

        protected override void DoUpdate(GameTime gameTime)
        {
            game.Update(gameTime.ElapsedGameTime.TotalMilliseconds);
            gameBoard.SelectedColumn = dragBeginCol;
        }

        protected override void DoDraw(SpriteBatch spriteBatch) { }

        void SetChildrenProperties()
        {
            gameBoard.Location = Point2D.Empty;
        }

        void RegisterEvents()
        {
            InputManager.Instance.KeyboardKeyPressed += OnKeyboardKeyPressed;
            InputManager.Instance.MouseButtonPressed += OnMouseButtonPressed;
            InputManager.Instance.MouseButtonReleased += OnMouseButtonReleased;
        }

        void UnregisterEvents()
        {
            InputManager.Instance.KeyboardKeyPressed -= OnKeyboardKeyPressed;
            InputManager.Instance.MouseButtonPressed -= OnMouseButtonPressed;
            InputManager.Instance.MouseButtonReleased -= OnMouseButtonReleased;
        }

        void OnKeyboardKeyPressed(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Keys.N || e.Key == Keys.F2)
            {
                game.NewGame();
                dragBeginCol = -1;
            }
        }

        void OnMouseButtonPressed(object sender, MouseButtonEventArgs e) { }

        void OnMouseButtonReleased(object sender, MouseButtonEventArgs e)
        {
            if (e.Button != MouseButton.Left)
            {
                return;
            }

            int x = e.Location.X;
            int y = e.Location.Y;

            if (gameBoard.IsOnDice(x, y))
            {
                game.NextTurn();
                dragBeginCol = -1;
                return;
            }

            int col = gameBoard.ColumnAt(x, y);
            if (col < 0)
            {
                dragBeginCol = -1;
                return;
            }

            if (dragBeginCol == -1)
            {
                if (game.TableValues[col] != 0)
                {
                    dragBeginCol = col;
                }
            }
            else if (col == dragBeginCol)
            {
                dragBeginCol = -1;
            }
            else
            {
                try
                {
                    game.MovePieceDirect(dragBeginCol, col);
                }
                catch (PieceMoveException ex)
                {
                    Console.Error.WriteLine($"[Backgammon] {ex.Message}");
                }

                dragBeginCol = -1;
            }
        }
    }
}
