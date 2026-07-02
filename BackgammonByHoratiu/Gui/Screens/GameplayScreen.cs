using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

using NuciXNA.Gui;
using NuciXNA.Gui.Controls;
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
        GuiImage tableBackground;
        GuiButton undoButton;
        GuiButton resetButton;

        Stack<GameSnapshot> undoHistory;

        Point2D mousePosition;

        int dragBeginCol = -1;

        const int BarBrown = -2;
        const int BarWhite = -3;

        protected override void DoLoadContent()
        {
            AiGameManager aiManager = new();
            game = aiManager;
            game.LoadContent();

            tableBackground = new GuiImage
            {
                ContentFile = "Table/table",
                Location = Point2D.Empty,
                Size = new Size2D(GameDefines.WindowWidth, GameDefines.WindowHeight)
            };

            gameBoard = new GuiGameBoard(game)
            {
                Location = new Point2D(GameDefines.HouseWidth, 0),
                Size = new Size2D(GameDefines.WindowWidth, GameDefines.WindowHeight)
            };

            aiManager.AnimateMoveRequested += (fromCol, toCol, player, onComplete) =>
                gameBoard.BeginPieceMoveAnimation(fromCol, toCol, player, _ => onComplete?.Invoke());

            aiManager.IsExternallyAnimating = () => gameBoard.IsAnimating;

            int buttonSpacing = (GameDefines.HouseWidth - GameDefines.InGameButtonSize.Width) / 2;

            undoButton = new GuiButton
            {
                ContentFile = "interface/buttons_ingame",
                Icon = InGameButtonIcon.Undo,
                Location = new Point2D(buttonSpacing, buttonSpacing),
                Size = GameDefines.InGameButtonSize
            };
            resetButton = new GuiButton
            {
                ContentFile = "interface/buttons_ingame",
                Icon = InGameButtonIcon.Reset,
                Location = new Point2D(
                    undoButton.ClientRectangle.Left,
                    undoButton.ClientRectangle.Bottom + buttonSpacing),
                Size = GameDefines.InGameButtonSize
            };

            GuiManager.Instance.RegisterControls(
                tableBackground,
                gameBoard,
                undoButton,
                resetButton);

            undoHistory = [];
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

            if (dragBeginCol != -1 && leftButtonDown)
            {
                GameWindow.ActiveCursor = CursorType.HandOpen;
            }
            else
            {
                if (dragBeginCol != -1)
                {
                    GameWindow.ActiveCursor = CursorType.HandGrabbing;
                }
                else
                {
                    if (hoveringDice && hasNoValidMoves && game.ActivePlayer == 1)
                    {
                        GameWindow.ActiveCursor = CursorType.Dice;
                    }
                    else
                    {
                        if (gameBoard.IsHoveringOverWhitePiece(boardLocalMouseX, boardLocalMouseY))
                        {
                            GameWindow.ActiveCursor = CursorType.HandPicking;
                        }
                        else
                        {
                            GameWindow.ActiveCursor = CursorType.Pointer;
                        }
                    }
                }
            }

            if (dragBeginCol == BarWhite)
            {
                gameBoard.SelectedColumn = GameDefines.ColBarP1;
            }
            else
            {
                if (dragBeginCol == BarBrown)
                {
                    gameBoard.SelectedColumn = GameDefines.ColBarP2;
                }
                else
                {
                    gameBoard.SelectedColumn = dragBeginCol;
                }
            }

            if (!gameBoard.IsAnimating && dragBeginCol != -1)
            {
                int mappedFrom;
                if (dragBeginCol == BarWhite)
                {
                    mappedFrom = GameDefines.ColBarP1;
                }
                else
                {
                    if (dragBeginCol == BarBrown)
                    {
                        mappedFrom = GameDefines.ColBarP2;
                    }
                    else
                    {
                        mappedFrom = dragBeginCol;
                    }
                }

                gameBoard.ValidDestinations = game.GetValidDestinations(mappedFrom);

                int col = gameBoard.ColumnAt(boardLocalMouseX, boardLocalMouseY);

                if (col >= 0)
                {
                    gameBoard.HoveredColumn = col;
                }
                else if (gameBoard.IsOnHouse(boardLocalMouseX, boardLocalMouseY))
                {
                    if (game.ActivePlayer == 1)
                    {
                        gameBoard.HoveredColumn = GameDefines.ColHouseP1;
                    }
                    else
                    {
                        gameBoard.HoveredColumn = GameDefines.ColHouseP2;
                    }
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

            bool canUndo = undoHistory.Count > 0 && !gameBoard.IsAnimating && game.ActivePlayer == 1;

            undoButton.IsActive = canUndo;
        }

        protected override void DoDraw(SpriteBatch spriteBatch) { }

        void ChainMoveAnimation(
            int fromColumn,
            IReadOnlyList<int> intermediates,
            int finalColumn,
            int activePlayer,
            IReadOnlyList<Action> stepActions)
        {
            List<int> stops = [.. intermediates];
            stops.Add(finalColumn);

            void Continue(GuiImage piece, int index)
            {
                if (index >= stops.Count)
                {
                    return;
                }

                GameSnapshot stepSnapshot = game.CreateSnapshot();
                gameBoard.ContinuePieceMoveAnimation(piece, stops[index], activePlayer, () =>
                {
                    try
                    {
                        stepActions[index]();
                        undoHistory.Push(stepSnapshot);
                    }
                    catch (PieceMoveException ex)
                    {
                        Console.Error.WriteLine($"[Backgammon] {ex.Message}");
                    }

                    Continue(piece, index + 1);
                });
            }

            GameSnapshot firstSnapshot = game.CreateSnapshot();
            gameBoard.BeginPieceMoveAnimation(fromColumn, stops[0], activePlayer, piece =>
            {
                try
                {
                    stepActions[0]();
                    undoHistory.Push(firstSnapshot);
                }
                catch (PieceMoveException ex)
                {
                    Console.Error.WriteLine($"[Backgammon] {ex.Message}");
                }

                Continue(piece, 1);
            });
        }

        IReadOnlyList<Action> BuildBoardMoveStepActions(int fromColumn, IReadOnlyList<int> intermediates, int finalColumn)
        {
            List<int> allStops = [.. intermediates, finalColumn];
            List<Action> stepActions = [];
            int previousColumn = fromColumn;

            foreach (int stop in allStops)
            {
                int capturedFrom = previousColumn;
                int capturedDie = Math.Abs(stop - previousColumn);
                stepActions.Add(() => game.MovePiece(capturedFrom, capturedDie));
                previousColumn = stop;
            }

            return stepActions;
        }

        IReadOnlyList<Action> BuildBarMoveStepActions(int fromBarColumn, IReadOnlyList<int> intermediates, int finalColumn)
        {
            List<int> allStops = [.. intermediates, finalColumn];
            List<Action> stepActions = [];
            bool isPlayer1Bar = fromBarColumn.Equals(GameDefines.ColBarP1);
            int firstDie = GameDefines.TotalColumns - allStops[0];

            if (isPlayer1Bar)
            {
                firstDie = allStops[0] + 1;
            }

            int capturedFirstDie = firstDie;
            stepActions.Add(() => game.MoveOutedPiece(capturedFirstDie));

            for (int i = 1; i < allStops.Count; i++)
            {
                int capturedFrom = allStops[i - 1];
                int capturedDie = Math.Abs(allStops[i] - allStops[i - 1]);
                stepActions.Add(() => game.MovePiece(capturedFrom, capturedDie));
            }

            return stepActions;
        }

        void RegisterEvents()
        {
            InputManager.Instance.KeyboardKeyPressed += OnKeyboardKeyPressed;
            InputManager.Instance.MouseButtonPressed += OnMouseButtonPressed;
            InputManager.Instance.MouseButtonReleased += OnMouseButtonReleased;
            InputManager.Instance.MouseMoved += OnMouseMoved;
            resetButton.Clicked += OnResetButtonClicked;
            undoButton.Clicked += OnUndoButtonClicked;
        }

        void UnregisterEvents()
        {
            InputManager.Instance.KeyboardKeyPressed -= OnKeyboardKeyPressed;
            InputManager.Instance.MouseButtonPressed -= OnMouseButtonPressed;
            InputManager.Instance.MouseButtonReleased -= OnMouseButtonReleased;
            InputManager.Instance.MouseMoved -= OnMouseMoved;
            resetButton.Clicked -= OnResetButtonClicked;
            undoButton.Clicked -= OnUndoButtonClicked;
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

            for (int columnIndex = 0; columnIndex < GameDefines.TotalColumns; columnIndex++)
            {
                if (game.TableValues[columnIndex] > 0 &&
                    game.GetValidDestinations(columnIndex).Count > 0)
                {
                    return true;
                }
            }

            return false;
        }

        void OnResetButtonClicked(object sender, MouseButtonEventArgs e)
        {
            game.NewGame();
            undoHistory.Clear();
            dragBeginCol = -1;
        }

        void OnUndoButtonClicked(object sender, MouseButtonEventArgs e)
        {
            if (!undoButton.IsActive || gameBoard.IsAnimating || undoHistory.Count == 0)
            {
                return;
            }

            game.RestoreState(undoHistory.Pop());
            dragBeginCol = -1;
        }

        void OnKeyboardKeyPressed(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Keys.N || e.Key == Keys.F2)
            {
                game.NewGame();
                undoHistory.Clear();
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
                    undoHistory.Clear();
                    dragBeginCol = -1;
                }
                catch (PieceMoveException ex)
                {
                    Console.Error.WriteLine($"[Backgammon] {ex.Message}");
                }

                return;
            }

            int column = gameBoard.ColumnAt(x, y);

            if (column < 0 && gameBoard.IsOnHouse(x, y) && dragBeginCol >= 0)
            {
                int savedFrom = dragBeginCol;
                int toHouse = game.ActivePlayer == 1 ? GameDefines.ColHouseP1 : GameDefines.ColHouseP2;

                if (!game.GetValidDestinations(savedFrom).Contains(toHouse))
                {
                    dragBeginCol = -1;
                    return;
                }

                dragBeginCol = -1;

                GameSnapshot bearOffSnapshot = game.CreateSnapshot();
                gameBoard.BeginPieceMoveAnimation(savedFrom, toHouse, game.ActivePlayer, _ =>
                {
                    try
                    {
                        game.BearOffPiece(savedFrom);
                        undoHistory.Push(bearOffSnapshot);
                    }
                    catch (PieceMoveException ex)
                    {
                        Console.Error.WriteLine($"[Backgammon] {ex.Message}");
                    }
                });

                return;
            }

            if (column < 0 && gameBoard.IsInHumanBar(x, y))
            {
                if (game.Player1.OutedPieces > 0)
                {
                    dragBeginCol = BarWhite;
                }

                return;
            }

            if (column < 0 && gameBoard.IsInAiBar(x, y))
            {
                if (game.Player2.OutedPieces > 0)
                {
                    dragBeginCol = BarBrown;
                }

                return;
            }

            if (column < 0)
            {
                dragBeginCol = -1;
                return;
            }

            if (dragBeginCol == BarBrown || dragBeginCol == BarWhite)
            {
                int distance = column + 1;
                int fromBar = GameDefines.ColBarP1;

                if (dragBeginCol == BarBrown)
                {
                    distance = GameDefines.TotalColumns - column;
                    fromBar = GameDefines.ColBarP2;
                }

                if (!game.GetValidDestinations(fromBar).Contains(column))
                {
                    dragBeginCol = -1;
                    return;
                }

                int savedDist = distance;
                dragBeginCol = -1;

                IReadOnlyList<int> barMoveIntermediates = game.FindMoveOutedPieceIntermediates(savedDist);
                IReadOnlyList<Action> barStepActions = BuildBarMoveStepActions(fromBar, barMoveIntermediates, column);
                ChainMoveAnimation(fromBar, barMoveIntermediates, column, game.ActivePlayer, barStepActions);

                return;
            }

            if (dragBeginCol == -1)
            {
                bool isPlayer1Piece = game.ActivePlayer == 1 && game.TableValues[column] > 0;
                bool isPlayer2Piece = game.ActivePlayer == 2 && game.TableValues[column] < 0;

                if (isPlayer1Piece || isPlayer2Piece)
                {
                    dragBeginCol = column;
                }
            }
            else if (column == dragBeginCol)
            {
                dragBeginCol = -1;
            }
            else
            {
                int savedFrom = dragBeginCol;

                if (!game.GetValidDestinations(savedFrom).Contains(column))
                {
                    dragBeginCol = -1;

                    return;
                }

                dragBeginCol = -1;

                IReadOnlyList<int> moveIntermediates = game.FindMovePieceDirectIntermediates(savedFrom, column);
                IReadOnlyList<Action> moveStepActions = BuildBoardMoveStepActions(savedFrom, moveIntermediates, column);
                ChainMoveAnimation(savedFrom, moveIntermediates, column, game.ActivePlayer, moveStepActions);
            }
        }
    }
}
