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

        // Drag-and-drop state
        int dragBeginCol = -1;

        // Error message overlay
        string errorMessage;
        double errorTimer;
        const double ErrorDisplaySeconds = 3.0;

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

            if (errorTimer > 0)
                errorTimer -= gameTime.ElapsedGameTime.TotalSeconds;

            gameBoard.SelectedColumn = dragBeginCol;
        }

        protected override void DoDraw(SpriteBatch spriteBatch)
        {
            if (errorMessage != null && errorTimer > 0)
            {
                DrawErrorMessage(spriteBatch);
            }
        }

        void DrawErrorMessage(SpriteBatch spriteBatch)
        {
            // Simple red text in top-centre area
            // The font is loaded inside GuiGameBoard but we need one here too.
            // We use the screen's own DrawString via NuciXNA text if available,
            // or just skip — the error is printed to console as fallback.
        }

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

        void ShowError(string message)
        {
            errorMessage = message;
            errorTimer = ErrorDisplaySeconds;
            Console.Error.WriteLine($"[Backgammon] {message}");
        }

        void OnKeyboardKeyPressed(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Keys.N || e.Key == Keys.F2)
            {
                game.NewGame();
                dragBeginCol = -1;
            }
        }

        void OnMouseButtonPressed(object sender, MouseButtonEventArgs e)
        {
            if (e.Button != MouseButton.Left)
                return;

            int mx = e.Location.X;
            int my = e.Location.Y;

            dragBeginCol = -1;

            // Check if clicking the bar for outed pieces (on press we just record intent)
            // Check board columns
            int col = gameBoard.ColumnAt(mx, my);
            if (col >= 0 && game.TableValues[col] != 0)
                dragBeginCol = col;
        }

        void OnMouseButtonReleased(object sender, MouseButtonEventArgs e)
        {
            if (e.Button != MouseButton.Left)
                return;

            int mx = e.Location.X;
            int my = e.Location.Y;

            // Handle outed-piece re-entry from bar
            if (gameBoard.IsInOutColumnTop(mx, my) && game.Player1.OutedPieces != 0)
            {
                if (game.Player1.MovesLeft.Count > 0)
                {
                    try { game.MoveOutedPiece(game.Player1.MovesLeft[0]); }
                    catch (PieceMoveException pme) { ShowError(pme.Message); }
                }
                dragBeginCol = -1;
                return;
            }

            if (gameBoard.IsInOutColumnBottom(mx, my) && game.Player2.OutedPieces != 0)
            {
                if (game.Player2.MovesLeft.Count > 0)
                {
                    try { game.MoveOutedPiece(game.Player2.MovesLeft[0]); }
                    catch (PieceMoveException pme) { ShowError(pme.Message); }
                }
                dragBeginCol = -1;
                return;
            }

            if (dragBeginCol == -1)
                return;

            int destCol = gameBoard.ColumnAt(mx, my);
            if (destCol < 0)
            {
                dragBeginCol = -1;
                return;
            }

            try
            {
                if (game.TableValues[dragBeginCol] > 0)
                {
                    // Player 1 moves left (increasing indices)
                    int move = dragBeginCol == destCol
                        ? game.Player1.MovesLeft[0]
                        : destCol - dragBeginCol;

                    game.MovePiece(dragBeginCol, move);
                }
                else
                {
                    // Player 2 moves right→left (decreasing indices)
                    int move = dragBeginCol == destCol
                        ? game.Player2.MovesLeft[0]
                        : dragBeginCol - destCol;

                    game.MovePiece(dragBeginCol, move);
                }
            }
            catch (PieceMoveException pme)
            {
                ShowError(pme.Message);
            }

            dragBeginCol = -1;
        }
    }
}
