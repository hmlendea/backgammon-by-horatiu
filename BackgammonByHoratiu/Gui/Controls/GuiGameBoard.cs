using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NuciXNA.Graphics.SpriteEffects;
using NuciXNA.Gui.Controls;
using NuciXNA.Primitives;

using BackgammonByHoratiu.GameLogic.GameManagers;
using BackgammonByHoratiu.Settings;

namespace BackgammonByHoratiu.Gui.Controls
{
    public class GuiGameBoard(IGameManager game) : GuiControl
    {
        public bool IsAnimating =>
            player1Pieces is not null && player1Pieces.Any(p => p.MovementEffect.IsActive) ||
            player2Pieces is not null && player2Pieces.Any(p => p.MovementEffect.IsActive);

        GuiImage[] boardBackgrounds;
        GuiImage[] frames;
        GuiImage[] columnImages;
        GuiImage targetColumn;
        GuiImage ghostPiece;
        GuiImage[] player1Pieces;
        GuiImage[] player2Pieces;
        GuiImage[] dice;
        GuiImage moveDiceIndicator;
        GameTime lastGameTime = new();

        Rectangle2D[] columnRectangles;
        Rectangle2D[] outColumns;
        Rectangle2D[] houses;

        public int SelectedColumn { get; set; } = -1;
        public int HoveredColumn { get; set; } = -1;

        public IReadOnlyList<int> ValidDestinations { get; set; } = [];

        public bool IsOnDice(int x, int y) => dice.Any(d => d.ClientRectangle.Contains(x, y));

        public bool IsOnHouse(int x, int y) => houses.Any(h => h.Contains(x, y));

        protected override void DoLoadContent()
        {
            BuildLayoutRectangles();

            frames = new GuiImage[2];
            boardBackgrounds = new GuiImage[2];
            dice = new GuiImage[2];

            frames[0] = new GuiImage
            {
                ContentFile = "Table/frame",
                Location = new Point2D(0, 0),
                Size = new Size2D(GameDefines.FrameSize.Width, GameDefines.FrameSize.Height)
            };
            frames[1] = new GuiImage
            {
                ContentFile = "Table/frame",
                Location = new Point2D(GameDefines.BarX, 0),
                Size = new Size2D(GameDefines.FrameSize.Width, GameDefines.FrameSize.Height)
            };

            boardBackgrounds[0] = new GuiImage
            {
                ContentFile = "Table/board",
                Location = new Point2D(GameDefines.FrameThickness, GameDefines.FrameThickness),
                Size = frames[0].Size - new Size2D(GameDefines.FrameThickness * 2)
            };
            boardBackgrounds[1] = new GuiImage
            {
                ContentFile = "Table/board",
                Location = new Point2D(
                    boardBackgrounds[0].Location.X + boardBackgrounds[0].Size.Width + GameDefines.FrameThickness * 2,
                    boardBackgrounds[0].Location.Y),
                Size = frames[1].Size - new Size2D(GameDefines.FrameThickness * 2)
            };

            dice[0] = new GuiImage
            {
                ContentFile = "Table/dice",
                Location = new Point2D(
                    boardBackgrounds[0].Location.X + boardBackgrounds[0].Size.Width * 3 / 4 - GameDefines.DieSize / 2,
                    boardBackgrounds[0].Location.Y + boardBackgrounds[0].Size.Height / 2 - GameDefines.DieSize / 2),
                Size = new Size2D(GameDefines.DieSize)
            };
            dice[1] = new GuiImage
            {
                ContentFile = "Table/dice",
                Location = new Point2D(
                    boardBackgrounds[1].Location.X + boardBackgrounds[1].Size.Width * 1 / 4 - GameDefines.DieSize / 2,
                    boardBackgrounds[1].Location.Y + boardBackgrounds[1].Size.Height / 2 - GameDefines.DieSize / 2),
                Size = new Size2D(GameDefines.DieSize)
            };

            foreach (GuiImage frame in frames)
            {
                frame.Hide();
            }

            foreach (GuiImage boardBackground in boardBackgrounds)
            {
                boardBackground.Hide();
            }

            columnImages = new GuiImage[GameDefines.TotalColumns];

            for (int columnIndex = 0; columnIndex < GameDefines.TotalColumns; columnIndex++)
            {
                bool isTopHalf = columnIndex < GameDefines.TotalColumns / 2;
                bool isYellow = isTopHalf ? columnIndex % 2 != 0 : columnIndex % 2 == 0;
                int sourceX = isYellow ? 0 : GameDefines.ColumnFrameSize.Width;

                columnImages[columnIndex] = new GuiImage
                {
                    ContentFile = "Table/columns",
                    Location = new Point2D(columnRectangles[columnIndex].X, columnRectangles[columnIndex].Y),
                    Size = new Size2D(columnRectangles[columnIndex].Width, columnRectangles[columnIndex].Height),
                    SourceRectangle = new Rectangle2D(sourceX, 0, GameDefines.ColumnFrameSize.Width, GameDefines.ColumnFrameSize.Height)
                };

                if (isTopHalf)
                {
                    columnImages[columnIndex].Rotation = MathHelper.Pi;
                }

                columnImages[columnIndex].Hide();
            }

            targetColumn = new GuiImage
            {
                ContentFile = "Table/columns",
                SourceRectangle = new Rectangle2D(GameDefines.ColumnFrameSize.Width * 2, 0, GameDefines.ColumnFrameSize.Width, GameDefines.ColumnFrameSize.Height)
            };
            targetColumn.Hide();

            moveDiceIndicator = new GuiImage
            {
                ContentFile = "Table/dice",
                Size = new Size2D(GameDefines.DiceIndicatorSize, GameDefines.DiceIndicatorSize)
            };
            moveDiceIndicator.Hide();

            player1Pieces = new GuiImage[GameDefines.TotalPiecesPerPlayer];
            player2Pieces = new GuiImage[GameDefines.TotalPiecesPerPlayer];

            for (int pieceIndex = 0; pieceIndex < GameDefines.TotalPiecesPerPlayer; pieceIndex++)
            {
                player1Pieces[pieceIndex] = new GuiImage
                {
                    ContentFile = "Table/pieces",
                    Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize),
                    MovementEffect = new MovementEffect { Speed = GameDefines.AnimationSpeed }
                };
                player2Pieces[pieceIndex] = new GuiImage
                {
                    ContentFile = "Table/pieces",
                    Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize),
                    MovementEffect = new MovementEffect { Speed = GameDefines.AnimationSpeed }
                };
                player1Pieces[pieceIndex].Hide();
                player2Pieces[pieceIndex].Hide();
            }

            ghostPiece = new GuiImage
            {
                ContentFile = "Table/pieces",
                Opacity = 0.5f
            };
            ghostPiece.Hide();

            RegisterChildren(frames);
            RegisterChildren(boardBackgrounds);
            RegisterChildren(dice);
            RegisterChildren(ghostPiece, targetColumn);
            RegisterChildren(player1Pieces);
            RegisterChildren(player2Pieces);
            RegisterChildren(columnImages);
            RegisterChildren(moveDiceIndicator);
        }

        protected override void DoUnloadContent()
        {
        }

        protected override void DoUpdate(GameTime gameTime)
        {
            lastGameTime = gameTime;

            int diceRowY = GameDefines.DieFrameSize.Height;

            if (game.ActivePlayer == 1)
            {
                diceRowY = 0;
            }

            dice[0].SourceRectangle = new Rectangle2D(
                (game.Dice1 - 1) * GameDefines.DieFrameSize.Width,
                diceRowY,
                GameDefines.DieFrameSize);

            dice[1].SourceRectangle = new Rectangle2D(
                (game.Dice2 - 1) * GameDefines.DieFrameSize.Width,
                diceRowY,
                GameDefines.DieFrameSize);
        }

        protected override void DoDraw(SpriteBatch spriteBatch)
        {
            foreach (GuiImage bg in boardBackgrounds)
            {
                bg.Draw(spriteBatch);
            }

            foreach (GuiImage columnImage in columnImages)
            {
                columnImage.Draw(spriteBatch);
            }

            foreach (int destination in ValidDestinations)
            {
                if (destination >= 0 && destination < GameDefines.TotalColumns)
                {
                    DrawTargetColumn(spriteBatch, destination);
                }
            }

            foreach (GuiImage frame in frames)
            {
                frame.Draw(spriteBatch);
            }

            List<GuiImage> animatingPieces = CollectBoardPieces(spriteBatch);

            foreach (GuiImage die in dice)
            {
                die.Update(lastGameTime);
                die.Draw(spriteBatch);
            }

            DrawGhostPiece(spriteBatch);
            DrawMoveDiceIndicators(spriteBatch);

            foreach (GuiImage piece in animatingPieces)
            {
                piece.Draw(spriteBatch);
            }
        }

        void DrawGhostPiece(SpriteBatch spriteBatch)
        {
            if (SelectedColumn < 0 ||
                !ValidDestinations.Contains(HoveredColumn))
            {
                return;
            }

            if (game.ActivePlayer == 1)
            {
                ghostPiece.SourceRectangle = new Rectangle2D(0, 0, GameDefines.PieceFrameSize, GameDefines.PieceFrameSize);
            }
            else
            {
                ghostPiece.SourceRectangle = new Rectangle2D(GameDefines.PieceFrameSize, 0, GameDefines.PieceFrameSize, GameDefines.PieceFrameSize);
            }

            Rectangle2D destination;

            if (HoveredColumn >= 0 &&
                HoveredColumn < GameDefines.TotalColumns)
            {
                int[] tableValues = game.TableValues;
                int playerSign = game.ActivePlayer == 1 ? 1 : -1;
                int existingCount = tableValues[HoveredColumn] * playerSign > 0 ? Math.Abs(tableValues[HoveredColumn]) : 0;
                int ghostPiecePixelY = GetBottomHalfSlotPixelY(existingCount, columnRectangles[HoveredColumn].Bottom);

                if (HoveredColumn < GameDefines.TotalColumns / 2)
                {
                    ghostPiecePixelY = GetTopHalfSlotPixelY(existingCount, columnRectangles[HoveredColumn].Top);
                }

                destination = new Rectangle2D(
                    columnRectangles[HoveredColumn].Left,
                    ghostPiecePixelY,
                    GameDefines.PieceSize,
                    GameDefines.PieceSize);
            }
            else if (HoveredColumn == GameDefines.ColHouseP1)
            {
                int centerX = houses[0].Left + (houses[0].Width - GameDefines.PieceSize) / 2;
                int ghostPiecePixelY = GetBottomHalfSlotPixelY(
                    game.Player1.CompletedPieces,
                    houses[0].Bottom);

                destination = new Rectangle2D(
                    centerX,
                    ghostPiecePixelY,
                    GameDefines.PieceSize,
                    GameDefines.PieceSize);
            }
            else if (HoveredColumn == GameDefines.ColHouseP2)
            {
                int centerX = houses[1].Left + (houses[1].Width - GameDefines.PieceSize) / 2;
                int ghostPiecePixelY = GetTopHalfSlotPixelY(
                    game.Player2.CompletedPieces,
                    houses[1].Top);

                destination = new Rectangle2D(
                    centerX,
                    ghostPiecePixelY,
                    GameDefines.PieceSize,
                    GameDefines.PieceSize);
            }
            else
            {
                return;
            }

            ghostPiece.Location = new Point2D(destination.X, destination.Y);
            ghostPiece.Size = new Size2D(destination.Width, destination.Height);
            ghostPiece.Update(lastGameTime);
            ghostPiece.Draw(spriteBatch);
        }

        void DrawMoveDiceIndicators(SpriteBatch spriteBatch)
        {
            if (SelectedColumn < 0)
            {
                return;
            }

            int diceRowSourceY = game.ActivePlayer == 1 ? 0 : GameDefines.DieFrameSize.Height;

            foreach (int destination in ValidDestinations)
            {
                if (destination < 0 || destination >= GameDefines.TotalColumns)
                {
                    continue;
                }

                IReadOnlyList<int> diceUsed = game.GetDiceForDestination(SelectedColumn, destination);

                if (diceUsed.Count == 0)
                {
                    continue;
                }

                bool isTopHalf = destination < GameDefines.TotalColumns / 2;
                int dicePerRow = Math.Min(diceUsed.Count, 2);
                int rowCount = (diceUsed.Count + 1) / 2;
                int rowStep = GameDefines.DiceIndicatorSize + GameDefines.DiceIndicatorSpacing;
                int totalHeight = rowCount * GameDefines.DiceIndicatorSize + (rowCount - 1) * GameDefines.DiceIndicatorSpacing;
                int totalDiceWidth = dicePerRow * GameDefines.DiceIndicatorSize + (dicePerRow - 1) * GameDefines.DiceIndicatorSpacing;
                int startX = columnRectangles[destination].Left + (GameDefines.PieceSize - totalDiceWidth) / 2;
                int firstRowY = columnRectangles[destination].Bottom + GameDefines.DiceIndicatorSpacing;

                if (isTopHalf)
                {
                    firstRowY = columnRectangles[destination].Top - totalHeight - GameDefines.DiceIndicatorSpacing;
                }

                for (int diceIndex = 0; diceIndex < diceUsed.Count; diceIndex++)
                {
                    int dieValue = diceUsed[diceIndex];
                    int col = diceIndex % 2;
                    int row = diceIndex / 2;
                    int indicatorX = startX + col * (GameDefines.DiceIndicatorSize + GameDefines.DiceIndicatorSpacing);
                    int indicatorY = firstRowY + row * rowStep;

                    moveDiceIndicator.SourceRectangle = new Rectangle2D(
                        (dieValue - 1) * GameDefines.DieFrameSize.Width,
                        diceRowSourceY,
                        GameDefines.DieFrameSize);
                    moveDiceIndicator.Location = new Point2D(indicatorX, indicatorY);
                    moveDiceIndicator.Size = new Size2D(GameDefines.DiceIndicatorSize, GameDefines.DiceIndicatorSize);
                    moveDiceIndicator.Update(lastGameTime);
                    moveDiceIndicator.Draw(spriteBatch);
                }
            }
        }

        void DrawTargetColumn(SpriteBatch spriteBatch, int columnIndex)
        {
            Rectangle2D rectangle = columnRectangles[columnIndex];
            bool isTopHalf = columnIndex < GameDefines.TotalColumns / 2;

            targetColumn.Location = new Point2D(rectangle.X, rectangle.Y);
            targetColumn.Size = new Size2D(rectangle.Width, rectangle.Height);
            targetColumn.Rotation = isTopHalf ? MathHelper.Pi : 0f;
            targetColumn.Update(lastGameTime);
            targetColumn.Draw(spriteBatch);
        }

        List<GuiImage> CollectBoardPieces(SpriteBatch spriteBatch)
        {
            int[] tableValues = game.TableValues;
            int player1Index = 0;
            int player2Index = 0;
            List<GuiImage> animatingPieces = [];

            for (int columnIndex = 0; columnIndex < GameDefines.TotalColumns; columnIndex++)
            {
                int pieceCount = Math.Abs(tableValues[columnIndex]);

                if (pieceCount == 0)
                {
                    continue;
                }

                bool isPlayer2 = tableValues[columnIndex] < 0;

                for (int stackIndex = 0; stackIndex < pieceCount; stackIndex++)
                {
                    int layer = stackIndex / GameDefines.PiecesPerColumnLayer;
                    int indexInLayer = stackIndex % GameDefines.PiecesPerColumnLayer;
                    int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / GameDefines.PieceFrameSize;
                    int pixelY;

                    if (columnIndex < GameDefines.TotalColumns / 2)
                    {
                        pixelY = columnRectangles[columnIndex].Top - layerOffset + indexInLayer * GameDefines.PieceSize;
                    }
                    else
                    {
                        pixelY = columnRectangles[columnIndex].Bottom - GameDefines.PieceSize - layerOffset - indexInLayer * GameDefines.PieceSize;
                    }

                    bool isTopPiece = stackIndex == pieceCount - 1 && columnIndex == SelectedColumn;
                    int sourceX = isTopPiece ? GameDefines.PieceFrameSize * 2 : (isPlayer2 ? GameDefines.PieceFrameSize : 0);
                    GuiImage piece = isPlayer2 ? player2Pieces[player2Index++] : player1Pieces[player1Index++];

                    piece.SourceRectangle = new Rectangle2D(sourceX, 0, GameDefines.PieceFrameSize, GameDefines.PieceFrameSize);
                    piece.Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize);

                    if (piece.MovementEffect.IsActive)
                    {
                        animatingPieces.Add(piece);
                        continue;
                    }

                    piece.Location = new Point2D(columnRectangles[columnIndex].Left, pixelY);
                    piece.Update(lastGameTime);
                    piece.Draw(spriteBatch);
                }
            }

            int player1OutedPieces = game.Player1.OutedPieces;
            int player2OutedPieces = game.Player2.OutedPieces;
            int centerTopX = outColumns[0].Left + (outColumns[0].Width - GameDefines.PieceSize) / 2;

            for (int barIndex = 0; barIndex < player1OutedPieces; barIndex++)
            {
                int layer = barIndex / GameDefines.PiecesPerColumnLayer;
                int indexInLayer = barIndex % GameDefines.PiecesPerColumnLayer;
                int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / GameDefines.PieceFrameSize;
                bool isSelected = barIndex == player1OutedPieces - 1 && SelectedColumn == GameDefines.ColBarP1;
                int sourceX = isSelected ? GameDefines.PieceFrameSize * 2 : 0;
                int pixelY = outColumns[0].Top - layerOffset + indexInLayer * GameDefines.PieceSize;
                GuiImage piece = player1Pieces[player1Index++];

                piece.SourceRectangle = new Rectangle2D(sourceX, 0, GameDefines.PieceFrameSize, GameDefines.PieceFrameSize);
                piece.Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize);

                if (piece.MovementEffect.IsActive)
                {
                    animatingPieces.Add(piece);
                    continue;
                }

                piece.Location = new Point2D(centerTopX, pixelY);
                piece.Update(lastGameTime);
                piece.Draw(spriteBatch);
            }

            int centerBottomX = outColumns[1].Left + (outColumns[1].Width - GameDefines.PieceSize) / 2;

            for (int barIndex = 0; barIndex < player2OutedPieces; barIndex++)
            {
                int layer = barIndex / GameDefines.PiecesPerColumnLayer;
                int indexInLayer = barIndex % GameDefines.PiecesPerColumnLayer;
                int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / GameDefines.PieceFrameSize;
                bool isSelected = barIndex == player2OutedPieces - 1 && SelectedColumn == GameDefines.ColBarP2;
                int sourceX = isSelected ? GameDefines.PieceFrameSize * 2 : GameDefines.PieceFrameSize;
                int pixelY = outColumns[1].Bottom - GameDefines.PieceSize - layerOffset - indexInLayer * GameDefines.PieceSize;
                GuiImage piece = player2Pieces[player2Index++];

                piece.SourceRectangle = new Rectangle2D(sourceX, 0, GameDefines.PieceFrameSize, GameDefines.PieceFrameSize);
                piece.Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize);

                if (piece.MovementEffect.IsActive)
                {
                    animatingPieces.Add(piece);
                    continue;
                }

                piece.Location = new Point2D(centerBottomX, pixelY);
                piece.Update(lastGameTime);
                piece.Draw(spriteBatch);
            }

            int completedPiecesPlayer2 = game.Player2.CompletedPieces;
            int completedPiecesPlayer1 = game.Player1.CompletedPieces;
            int houseP2CenterX = houses[1].Left + (houses[1].Width - GameDefines.PieceSize) / 2;

            for (int stackIndex = 0; stackIndex < completedPiecesPlayer2; stackIndex++)
            {
                int layer = stackIndex / GameDefines.PiecesPerColumnLayer;
                int indexInLayer = stackIndex % GameDefines.PiecesPerColumnLayer;
                int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / GameDefines.PieceFrameSize;
                int pixelY = houses[1].Top - layerOffset + indexInLayer * GameDefines.PieceSize;
                GuiImage piece = player2Pieces[player2Index++];

                piece.SourceRectangle = new Rectangle2D(GameDefines.PieceFrameSize, 0, GameDefines.PieceFrameSize, GameDefines.PieceFrameSize);
                piece.Location = new Point2D(houseP2CenterX, pixelY);
                piece.Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize);
                piece.Update(lastGameTime);
                piece.Draw(spriteBatch);
            }

            int houseP1CenterX = houses[0].Left + (houses[0].Width - GameDefines.PieceSize) / 2;

            for (int stackIndex = 0; stackIndex < completedPiecesPlayer1; stackIndex++)
            {
                int layer = stackIndex / GameDefines.PiecesPerColumnLayer;
                int indexInLayer = stackIndex % GameDefines.PiecesPerColumnLayer;
                int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / GameDefines.PieceFrameSize;
                int pixelY = houses[0].Bottom - GameDefines.PieceSize - layerOffset - indexInLayer * GameDefines.PieceSize;
                GuiImage piece = player1Pieces[player1Index++];

                piece.SourceRectangle = new Rectangle2D(0, 0, GameDefines.PieceFrameSize, GameDefines.PieceFrameSize);
                piece.Location = new Point2D(houseP1CenterX, pixelY);
                piece.Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize);
                piece.Update(lastGameTime);
                piece.Draw(spriteBatch);
            }

            return animatingPieces;
        }

        public int ColumnAt(int x, int y)
        {
            for (int columnIndex = 0; columnIndex < GameDefines.TotalColumns; columnIndex++)
            {
                if (columnRectangles[columnIndex].Contains(x, y))
                {
                    return columnIndex;
                }
            }

            return -1;
        }

        public bool IsInHumanBar(int x, int y) => outColumns[0].Contains(x, y);
        public bool IsInAiBar(int x, int y) => outColumns[1].Contains(x, y);

        public bool IsHoveringOverWhitePiece(int x, int y)
        {
            int[] tableValues = game.TableValues;

            for (int columnIndex = 0; columnIndex < GameDefines.TotalColumns; columnIndex++)
            {
                if (tableValues[columnIndex] <= 0)
                {
                    continue;
                }

                int pieceCount = Math.Min(tableValues[columnIndex], GameDefines.PiecesPerColumnLayer);

                for (int stackIndex = 0; stackIndex < pieceCount; stackIndex++)
                {
                    Rectangle2D destination = new(
                            columnRectangles[columnIndex].Left,
                            columnRectangles[columnIndex].Bottom - (stackIndex + 1) * GameDefines.PieceSize,
                            GameDefines.PieceSize,
                            GameDefines.PieceSize);

                    if (columnIndex < GameDefines.TotalColumns / 2)
                    {
                        destination = new Rectangle2D(
                            columnRectangles[columnIndex].Left,
                            columnRectangles[columnIndex].Top + stackIndex * GameDefines.PieceSize,
                            GameDefines.PieceSize,
                            GameDefines.PieceSize);
                    }

                    if (destination.Contains(x, y))
                    {
                        return true;
                    }
                }
            }

            for (int stackIndex = 0; stackIndex < Math.Min(game.Player1.OutedPieces, GameDefines.PiecesPerColumnLayer); stackIndex++)
            {
                Rectangle2D destination = new(
                    outColumns[0].Left + (outColumns[0].Width - GameDefines.PieceSize) / 2,
                    outColumns[0].Top + stackIndex * GameDefines.PieceSize,
                    GameDefines.PieceSize,
                    GameDefines.PieceSize);

                if (destination.Contains(x, y))
                {
                    return true;
                }
            }

            return false;
        }

        public void BeginPieceMoveAnimation(int fromColumn, int toColumn, int activePlayer, Action<GuiImage> onComplete)
        {
            if (IsAnimating)
            {
                onComplete?.Invoke(null);

                return;
            }

            int playerSign = activePlayer == 1 ? 1 : -1;
            int outingColumn = toColumn >= 0 && toColumn < GameDefines.TotalColumns &&
                               game.TableValues[toColumn] * playerSign < 0 &&
                               Math.Abs(game.TableValues[toColumn]) == 1 ? toColumn : -1;
            int outingPlayer = outingColumn >= 0 ? (activePlayer == 1 ? 2 : 1) : -1;

            GuiImage piece = FindTopPieceImageAt(fromColumn);
            piece.SourceRectangle = new Rectangle2D(activePlayer == 2 ? GameDefines.PieceFrameSize : 0, 0, GameDefines.PieceFrameSize, GameDefines.PieceFrameSize);
            piece.Location = GetAnimSourcePixel(fromColumn);

            StartMovementAnimation(piece, GetAnimDestPixel(toColumn, activePlayer), () =>
            {
                if (outingColumn >= 0)
                {
                    BeginOutingAnimation(outingColumn, outingPlayer);
                }

                onComplete?.Invoke(piece);
            });
        }

        public void ContinuePieceMoveAnimation(GuiImage piece, int toColumn, int activePlayer, Action onComplete)
        {
            int playerSign = activePlayer == 1 ? 1 : -1;
            int outingColumn = toColumn >= 0 && toColumn < GameDefines.TotalColumns &&
                               game.TableValues[toColumn] * playerSign < 0 &&
                               Math.Abs(game.TableValues[toColumn]) == 1 ? toColumn : -1;
            int outingPlayer = -1;

            if (outingColumn >= 0)
            {
                outingPlayer = activePlayer == 1 ? 2 : 1;
            }

            piece.SourceRectangle = new Rectangle2D(
                activePlayer == 2 ? GameDefines.PieceFrameSize : 0,
                0,
                GameDefines.PieceFrameSize,
                GameDefines.PieceFrameSize);

            StartMovementAnimation(piece, GetAnimDestPixel(toColumn, activePlayer), () =>
            {
                if (outingColumn >= 0)
                {
                    BeginOutingAnimation(outingColumn, outingPlayer);
                }

                onComplete?.Invoke();
            });
        }
        static int GetTopHalfSlotPixelY(int slotIndex, int columnTop)
        {
            int layer = slotIndex / GameDefines.PiecesPerColumnLayer;
            int indexInLayer = slotIndex % GameDefines.PiecesPerColumnLayer;
            int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / GameDefines.PieceFrameSize;

            return columnTop - layerOffset + indexInLayer * GameDefines.PieceSize;
        }

        // Returns the pixel Y for a given slot index in a bottom-half column/bar/house
        // (pieces grow upward from the column's bottom edge).
        static int GetBottomHalfSlotPixelY(int slotIndex, int columnBottom)
        {
            int layer = slotIndex / GameDefines.PiecesPerColumnLayer;
            int indexInLayer = slotIndex % GameDefines.PiecesPerColumnLayer;
            int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / GameDefines.PieceFrameSize;

            return columnBottom - GameDefines.PieceSize - layerOffset - indexInLayer * GameDefines.PieceSize;
        }

        Point2D GetAnimSourcePixel(int fromColumn)
        {
            if (fromColumn >= 0 && fromColumn < GameDefines.TotalColumns)
            {
                int sourceSlotIndex = Math.Abs(game.TableValues[fromColumn]) - 1;

                if (fromColumn < GameDefines.TotalColumns / 2)
                {
                    return new Point2D(
                        columnRectangles[fromColumn].Left,
                        GetTopHalfSlotPixelY(sourceSlotIndex, columnRectangles[fromColumn].Top));
                }

                return new Point2D(
                    columnRectangles[fromColumn].Left,
                    GetBottomHalfSlotPixelY(sourceSlotIndex, columnRectangles[fromColumn].Bottom));
            }

            if (fromColumn == GameDefines.ColBarP1)
            {
                int centerX = outColumns[0].Left + (outColumns[0].Width - GameDefines.PieceSize) / 2;
                int sourceSlotIndex = game.Player1.OutedPieces - 1;

                return new Point2D(centerX, GetTopHalfSlotPixelY(sourceSlotIndex, outColumns[0].Top));
            }

            int player2BarCenterX = outColumns[1].Left + (outColumns[1].Width - GameDefines.PieceSize) / 2;
            int player2SourceSlotIndex = game.Player2.OutedPieces - 1;

            return new Point2D(player2BarCenterX, GetBottomHalfSlotPixelY(player2SourceSlotIndex, outColumns[1].Bottom));
        }

        Point2D GetAnimDestPixel(int toColumn, int activePlayer)
        {
            if (toColumn >= 0 && toColumn < GameDefines.TotalColumns)
            {
                int playerSign = -1;
                int existingCount = 0;

                if (activePlayer == 1)
                {
                    playerSign = 1;
                }

                if (game.TableValues[toColumn] * playerSign > 0)
                {
                    existingCount = Math.Abs(game.TableValues[toColumn]);
                }

                if (toColumn < GameDefines.TotalColumns / 2)
                {
                    return new Point2D(
                        columnRectangles[toColumn].Left,
                        GetTopHalfSlotPixelY(existingCount, columnRectangles[toColumn].Top));
                }

                return new Point2D(
                    columnRectangles[toColumn].Left,
                    GetBottomHalfSlotPixelY(existingCount, columnRectangles[toColumn].Bottom));
            }

            if (toColumn == GameDefines.ColHouseP1)
            {
                int centerX = houses[0].Left + (houses[0].Width - GameDefines.PieceSize) / 2;

                return new Point2D(
                    centerX,
                    GetBottomHalfSlotPixelY(game.Player1.CompletedPieces, houses[0].Bottom));
            }

            int houseCenterX = houses[1].Left + (houses[1].Width - GameDefines.PieceSize) / 2;

            return new Point2D(
                houseCenterX,
                GetTopHalfSlotPixelY(game.Player2.CompletedPieces, houses[1].Top));
        }

        void BeginOutingAnimation(int hitColumn, int hitPlayer)
        {
            Point2D sourcePixel = new(
                columnRectangles[hitColumn].Left,
                columnRectangles[hitColumn].Bottom - GameDefines.PieceSize);

            if (hitColumn < GameDefines.TotalColumns / 2)
            {
                sourcePixel = new Point2D(
                    columnRectangles[hitColumn].Left,
                    columnRectangles[hitColumn].Top);
            }

            Point2D destinationPixel;

            if (hitPlayer == 1)
            {
                destinationPixel = new Point2D(
                    outColumns[0].Left + (outColumns[0].Width - GameDefines.PieceSize) / 2,
                    GetTopHalfSlotPixelY(game.Player1.OutedPieces, outColumns[0].Top));
            }
            else
            {
                destinationPixel = new Point2D(
                    outColumns[1].Left + (outColumns[1].Width - GameDefines.PieceSize) / 2,
                    GetBottomHalfSlotPixelY(game.Player2.OutedPieces, outColumns[1].Bottom));
            }

            int barColumn = GameDefines.ColBarP2;

            if (hitPlayer == 1)
            {
                barColumn = GameDefines.ColBarP1;
            }

            GuiImage piece = FindTopPieceImageAt(barColumn);
            piece.SourceRectangle = new Rectangle2D(
                hitPlayer == 1 ? 0 : GameDefines.PieceFrameSize,
                0,
                GameDefines.PieceFrameSize,
                GameDefines.PieceFrameSize);
            piece.Location = sourcePixel;

            StartMovementAnimation(piece, destinationPixel, () => { });
        }

        void StartMovementAnimation(GuiImage piece, Point2D destination, Action onComplete)
        {
            EventHandler handler = null!;
            handler = (_, _) =>
            {
                piece.MovementEffect.Deactivated -= handler;
                piece.Location = destination;
                onComplete();
            };

            piece.MovementEffect.Deactivated += handler;
            piece.MovementEffect.TargetLocation = destination + ScreenLocation;
            piece.MovementEffect.Activate();
        }

        GuiImage FindTopPieceImageAt(int fromColumn)
        {
            int[] tableValues = game.TableValues;
            int player1Index = 0;
            int player2Index = 0;

            for (int columnIndex = 0; columnIndex < GameDefines.TotalColumns; columnIndex++)
            {
                int count = Math.Abs(tableValues[columnIndex]);
                bool isPlayer2 = tableValues[columnIndex] < 0;

                if (columnIndex == fromColumn)
                {
                    if (isPlayer2)
                    {
                        return player2Pieces[player2Index + count - 1];
                    }

                    return player1Pieces[player1Index + count - 1];
                }

                if (count == 0)
                {
                    continue;
                }

                if (isPlayer2)
                {
                    player2Index += count;
                }
                else
                {
                    player1Index += count;
                }
            }

            if (fromColumn == GameDefines.ColBarP1)
            {
                return player1Pieces[player1Index + game.Player1.OutedPieces - 1];
            }

            return player2Pieces[player2Index + game.Player2.OutedPieces - 1];
        }

        void BuildLayoutRectangles()
        {
            columnRectangles = new Rectangle2D[GameDefines.TotalColumns];
            columnRectangles[11] = new Rectangle2D(
                GameDefines.FrameThickness,
                GameDefines.FrameThickness,
                GameDefines.PieceSize,
                GameDefines.ColumnHeight);

            for (int columnIndex = 10; columnIndex >= 0; columnIndex--)
            {
                int topY = GameDefines.FrameThickness;

                if (columnIndex < 6)
                {
                    topY = GameDefines.RightFrameTopY;
                }

                if (columnIndex == 5)
                {
                    columnRectangles[columnIndex] = new Rectangle2D(
                        columnRectangles[columnIndex + 1].Right + GameDefines.HalfSeparatorWidth,
                        topY,
                        GameDefines.PieceSize,
                        GameDefines.ColumnHeight);
                }
                else
                {
                    columnRectangles[columnIndex] = new Rectangle2D(
                        columnRectangles[columnIndex + 1].Right,
                        topY,
                        GameDefines.PieceSize,
                        GameDefines.ColumnHeight);
                }
            }

            int bottomY = GameDefines.FrameThickness + GameDefines.BoardHalfHeight - GameDefines.ColumnHeight;

            columnRectangles[12] = new Rectangle2D(
                GameDefines.FrameThickness,
                bottomY,
                GameDefines.PieceSize,
                GameDefines.ColumnHeight);

            for (int columnIndex = 13; columnIndex < GameDefines.TotalColumns; columnIndex++)
            {
                int colBottomY = bottomY;

                if (columnIndex == 18)
                {
                    columnRectangles[columnIndex] = new Rectangle2D(
                        columnRectangles[columnIndex - 1].Right + GameDefines.HalfSeparatorWidth,
                        colBottomY,
                        GameDefines.PieceSize,
                        GameDefines.ColumnHeight);
                }
                else
                {
                    columnRectangles[columnIndex] = new Rectangle2D(
                        columnRectangles[columnIndex - 1].Right,
                        colBottomY,
                        GameDefines.PieceSize,
                        GameDefines.ColumnHeight);
                }
            }

            int outColumnX = GameDefines.BarX - GameDefines.FrameThickness;
            int outColumnWidth = columnRectangles[5].X - outColumnX;
            int boardMidY = GameDefines.RightFrameTopY + GameDefines.BoardHalfHeight / 2;

            outColumns = new Rectangle2D[2];
            outColumns[0] = new Rectangle2D(
                outColumnX,
                GameDefines.RightFrameTopY,
                outColumnWidth,
                boardMidY - GameDefines.RightFrameTopY);

            outColumns[1] = new Rectangle2D(
                outColumnX,
                boardMidY,
                outColumnWidth,
                GameDefines.RightFrameTopY + GameDefines.BoardHalfHeight - boardMidY);

            houses = new Rectangle2D[2];
            houses[1] = new Rectangle2D(
                GameDefines.HouseX,
                GameDefines.FrameThickness,
                GameDefines.HouseWidth,
                GameDefines.BoardHalfHeight / 2);

            houses[0] = new Rectangle2D(
                GameDefines.HouseX,
                GameDefines.FrameThickness + GameDefines.BoardHalfHeight / 2,
                GameDefines.HouseWidth,
                GameDefines.BoardHalfHeight / 2);
        }
    }
}
