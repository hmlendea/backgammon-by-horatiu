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

        const int BarBrown = -2;
        const int BarWhite = -3;

        public GameplayScreen()
        {
            BackgroundColour = Colour.Black;
            ForegroundColour = Colour.White;
        }

        protected override void DoLoadContent()
        {
            AiGameManager aiManager = new AiGameManager();
            game = aiManager;
            game.LoadContent();

            gameBoard = new GuiGameBoard(game)
            {
                Size = new Size2D(GameDefines.WindowWidth, GameDefines.WindowHeight)
            };

            aiManager.AnimateMoveRequested += (fromCol, toCol, player, onComplete) =>
                gameBoard.BeginPieceMoveAnimation(fromCol, toCol, player, onComplete);

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
                gameBoard.CancelAnimation();
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

            if (gameBoard.IsAnimating)
            {
                return;
            }

            int x = e.Location.X;
            int y = e.Location.Y;

            if (gameBoard.IsOnDice(x, y))
            {
                try
                {
                    game.NextTurn();
                    dragBeginCol = -1;
                }
                catch (PieceMoveException ex)
                {
                    Console.Error.WriteLine($"[Backgammon] {ex.Message}");
                }

                return;
            }

            int col = gameBoard.ColumnAt(x, y);

            if (col < 0 && gameBoard.IsOnHouse(x, y) && dragBeginCol >= 0)
            {
                int savedFrom = dragBeginCol;
                dragBeginCol = -1;
                int toHouse = game.ActivePlayer == 1 ? GameDefines.ColHouseP1 : GameDefines.ColHouseP2;

                gameBoard.BeginPieceMoveAnimation(savedFrom, toHouse, game.ActivePlayer, () =>
                {
                    try
                    {
                        game.BearOffPiece(savedFrom);
                    }
                    catch (PieceMoveException ex)
                    {
                        Console.Error.WriteLine($"[Backgammon] {ex.Message}");
                    }
                });

                return;
            }

            if (col < 0 && gameBoard.IsInOutColumnTop(x, y))
            {
                if (game.Player2.OutedPieces > 0)
                {
                    dragBeginCol = BarBrown;
                }

                return;
            }

            if (col < 0 && gameBoard.IsInOutColumnBottom(x, y))
            {
                if (game.Player1.OutedPieces > 0)
                {
                    dragBeginCol = BarWhite;
                }

                return;
            }

            if (col < 0)
            {
                dragBeginCol = -1;
                return;
            }

            if (dragBeginCol == BarBrown || dragBeginCol == BarWhite)
            {
                int distance = dragBeginCol == BarBrown ? 24 - col : col + 1;
                int fromBar = dragBeginCol == BarBrown ? GameDefines.ColBarP2 : GameDefines.ColBarP1;
                int savedDist = distance;
                dragBeginCol = -1;

                gameBoard.BeginPieceMoveAnimation(fromBar, col, game.ActivePlayer, () =>
                {
                    try
                    {
                        game.MoveOutedPiece(savedDist);
                    }
                    catch (PieceMoveException ex)
                    {
                        Console.Error.WriteLine($"[Backgammon] {ex.Message}");
                    }
                });

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
                int savedFrom = dragBeginCol;
                dragBeginCol = -1;

                gameBoard.BeginPieceMoveAnimation(savedFrom, col, game.ActivePlayer, () =>
                {
                    try
                    {
                        game.MovePieceDirect(savedFrom, col);
                    }
                    catch (PieceMoveException ex)
                    {
                        Console.Error.WriteLine($"[Backgammon] {ex.Message}");
                    }
                });
            }
        }
    }
}
