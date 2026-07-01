using System;
using System.Linq;
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

        Point2D mousePosition;

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
            AiGameManager aiManager = new();
            game = aiManager;
            game.LoadContent();

            gameBoard = new GuiGameBoard(game)
            {
                Location = new Point2D(GameDefines.HouseWidth, 0),
                Size = new Size2D(GameDefines.WindowWidth, GameDefines.WindowHeight)
            };

            aiManager.AnimateMoveRequested += (fromCol, toCol, player, onComplete) =>
                gameBoard.BeginPieceMoveAnimation(fromCol, toCol, player, onComplete);

            aiManager.IsExternallyAnimating = () => gameBoard.IsAnimating;

            GuiManager.Instance.RegisterControls(gameBoard);
            RegisterEvents();
        }

        protected override void DoUnloadContent()
        {
            game.UnloadContent();
            UnregisterEvents();
        }

        protected override void DoUpdate(GameTime gameTime)
        {
            game.Update(gameTime.ElapsedGameTime.TotalMilliseconds);

            bool leftButtonDown = InputManager.Instance.IsMouseButtonDown(MouseButton.Left);
            bool noPieceSelected = dragBeginCol == -1;
            int boardLocalMouseX = mousePosition.X - gameBoard.Location.X;
            int boardLocalMouseY = mousePosition.Y - gameBoard.Location.Y;
            bool hoveringDice = noPieceSelected && gameBoard.IsOnDice(boardLocalMouseX, boardLocalMouseY);
            bool hasNoValidMoves = hoveringDice && !HasAnyValidMoveForPlayer1();

            GameWindow.ActiveCursor = dragBeginCol != -1 && leftButtonDown
                ? CursorType.HandOpen
                : dragBeginCol != -1
                    ? CursorType.HandGrabbing
                    : hoveringDice && hasNoValidMoves && game.ActivePlayer == 1
                        ? CursorType.Dice
                        : gameBoard.IsHoveringOverWhitePiece(boardLocalMouseX, boardLocalMouseY)
                            ? CursorType.HandPicking
                            : CursorType.Pointer;

            gameBoard.SelectedColumn = dragBeginCol == BarWhite ? GameDefines.ColBarP1
                                     : dragBeginCol == BarBrown ? GameDefines.ColBarP2
                                     : dragBeginCol;

            if (!gameBoard.IsAnimating && dragBeginCol != -1)
            {
                int mappedFrom = dragBeginCol == BarWhite ? GameDefines.ColBarP1
                               : dragBeginCol == BarBrown ? GameDefines.ColBarP2
                               : dragBeginCol;
                gameBoard.ValidDestinations = game.GetValidDestinations(mappedFrom);

                int col = gameBoard.ColumnAt(boardLocalMouseX, boardLocalMouseY);
                if (col >= 0)
                {
                    gameBoard.HoveredColumn = col;
                }
                else if (gameBoard.IsOnHouse(boardLocalMouseX, boardLocalMouseY))
                {
                    gameBoard.HoveredColumn = game.ActivePlayer == 1
                        ? GameDefines.ColHouseP1
                        : GameDefines.ColHouseP2;
                }
                else
                {
                    gameBoard.HoveredColumn = -1;
                }
            }
            else
            {
                gameBoard.ValidDestinations = [];
                gameBoard.HoveredColumn = -1;
            }
        }

        protected override void DoDraw(SpriteBatch spriteBatch) { }

        void RegisterEvents()
        {
            InputManager.Instance.KeyboardKeyPressed += OnKeyboardKeyPressed;
            InputManager.Instance.MouseButtonPressed += OnMouseButtonPressed;
            InputManager.Instance.MouseButtonReleased += OnMouseButtonReleased;
            InputManager.Instance.MouseMoved += OnMouseMoved;
        }

        void UnregisterEvents()
        {
            InputManager.Instance.KeyboardKeyPressed -= OnKeyboardKeyPressed;
            InputManager.Instance.MouseButtonPressed -= OnMouseButtonPressed;
            InputManager.Instance.MouseButtonReleased -= OnMouseButtonReleased;
            InputManager.Instance.MouseMoved -= OnMouseMoved;
        }

        bool HasAnyValidMoveForPlayer1()
        {
            if (game.ActivePlayer != 1)
            {
                return false;
            }

            if (game.Player1.OutedPieces > 0)
            {
                return game.GetValidDestinations(GameDefines.ColBarP1).Count > 0;
            }

            for (int i = 0; i < 24; i++)
            {
                if (game.TableValues[i] > 0 && game.GetValidDestinations(i).Count > 0)
                {
                    return true;
                }
            }

            return false;
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

        void OnMouseMoved(object sender, MouseEventArgs e)
        {
            mousePosition = e.Location;
        }

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

            int x = e.Location.X - gameBoard.Location.X;
            int y = e.Location.Y - gameBoard.Location.Y;

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
                int toHouse = game.ActivePlayer == 1 ? GameDefines.ColHouseP1 : GameDefines.ColHouseP2;

                if (!game.GetValidDestinations(savedFrom).Contains(toHouse))
                {
                    dragBeginCol = -1;
                    return;
                }

                dragBeginCol = -1;

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

            if (col < 0 && gameBoard.IsInHumanBar(x, y))
            {
                if (game.Player1.OutedPieces > 0)
                {
                    dragBeginCol = BarWhite;
                }

                return;
            }

            if (col < 0 && gameBoard.IsInAiBar(x, y))
            {
                if (game.Player2.OutedPieces > 0)
                {
                    dragBeginCol = BarBrown;
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

                if (!game.GetValidDestinations(fromBar).Contains(col))
                {
                    dragBeginCol = -1;
                    return;
                }

                int savedDist = distance;
                dragBeginCol = -1;

                int barIntermediate = game.FindMoveOutedPieceIntermediate(savedDist);

                if (barIntermediate >= 0)
                {
                    gameBoard.BeginPieceMoveAnimation(fromBar, barIntermediate, game.ActivePlayer, () =>
                    {
                        gameBoard.ContinuePieceMoveAnimation(col, game.ActivePlayer, () =>
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
                    });
                }
                else
                {
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
                }

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

                if (!game.GetValidDestinations(savedFrom).Contains(col))
                {
                    dragBeginCol = -1;
                    return;
                }

                dragBeginCol = -1;

                int directIntermediate = game.FindMovePieceDirectIntermediate(savedFrom, col);

                if (directIntermediate >= 0)
                {
                    gameBoard.BeginPieceMoveAnimation(savedFrom, directIntermediate, game.ActivePlayer, () =>
                    {
                        gameBoard.ContinuePieceMoveAnimation(col, game.ActivePlayer, () =>
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
                    });
                }
                else
                {
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
}
