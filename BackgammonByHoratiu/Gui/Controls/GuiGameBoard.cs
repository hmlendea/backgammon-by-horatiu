using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using NuciXNA.Gui.Controls;
using NuciXNA.Primitives;

using BackgammonByHoratiu.GameLogic.GameManagers;
using BackgammonByHoratiu.Settings;

namespace BackgammonByHoratiu.Gui.Controls
{
    public class GuiGameBoard(IGameManager game) : GuiControl
    {
        static readonly Color ColorPlayer1 = Color.White;
        static readonly Color ColorPlayer2 = new(139, 69, 19);
        GuiImage animPiece;
        Point2D animTargetLocation;
        bool isAnimating;
        int animationFromColumn;
        int pendingOutingColumn = -1;
        int pendingOutingPlayer = -1;
        Action pendingMoveAction;

        public bool IsAnimating => isAnimating;

        GuiImage leftBoardBackground;
        GuiImage rightBoardBackground;
        GuiImage leftFrame;
        GuiImage rightFrame;
        GuiImage[] columnImages;
        GuiImage targetColumn;
        GuiImage ghostPiece;
        GuiImage[] player1Pieces;
        GuiImage[] player2Pieces;
        GuiImage die1;
        GuiImage die2;
        GuiImage moveDiceIndicator;
        GameTime lastGameTime = new();

        Rectangle2D[] columnRectangles;
        Rectangle2D outColumnTop, outColumnBottom;
        Rectangle2D houseTop, houseBottom;

        public int SelectedColumn { get; set; } = -1;
        public int HoveredColumn { get; set; } = -1;

        public IReadOnlyList<int> ValidDestinations { get; set; } = [];

        public bool IsOnDice(int x, int y) => die1.ClientRectangle.Contains(x, y) || die2.ClientRectangle.Contains(x, y);

        public bool IsOnHouse(int x, int y) => houseTop.Contains(x, y) || houseBottom.Contains(x, y);

        protected override void DoLoadContent()
        {
            BuildLayoutRectangles();

            leftFrame = new GuiImage
            {
                ContentFile = "Table/frame",
                Location = new Point2D(0, 0),
                Size = new Size2D(GameDefines.FrameWidth, GameDefines.FrameHeight)
            };
            rightFrame = new GuiImage
            {
                ContentFile = "Table/frame",
                Location = new Point2D(GameDefines.BarX, 0),
                Size = new Size2D(GameDefines.FrameWidth, GameDefines.FrameHeight)
            };

            leftBoardBackground = new GuiImage
            {
                ContentFile = "Table/board",
                Location = new Point2D(GameDefines.FrameBorder, GameDefines.FrameBorder),
                Size = leftFrame.Size - new Size2D(GameDefines.FrameBorder * 2)
            };
            rightBoardBackground = new GuiImage
            {
                ContentFile = "Table/board",
                Location = new Point2D(
                    leftBoardBackground.Location.X + leftBoardBackground.Size.Width + GameDefines.FrameBorder * 2,
                    leftBoardBackground.Location.Y),
                Size = rightFrame.Size - new Size2D(GameDefines.FrameBorder * 2)
            };

            die1 = new()
            {
                ContentFile = "Table/dice",
                Location = new Point2D(
                    leftBoardBackground.Location.X + leftBoardBackground.Size.Width * 3 / 4 - GameDefines.DieSize / 2,
                    leftBoardBackground.Location.Y + leftBoardBackground.Size.Height / 2 - GameDefines.DieSize / 2),
                Size = new Size2D(GameDefines.DieSize)
            };
            die2 = new()
            {
                ContentFile = "Table/dice",
                Location = new Point2D(
                    rightBoardBackground.Location.X + rightBoardBackground.Size.Width * 1 / 4 - GameDefines.DieSize / 2,
                    rightBoardBackground.Location.Y + rightBoardBackground.Size.Height / 2 - GameDefines.DieSize / 2),
                Size = new Size2D(GameDefines.DieSize)
            };

            leftBoardBackground.Hide();
            rightBoardBackground.Hide();
            leftFrame.Hide();
            rightFrame.Hide();

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


            animPiece = new GuiImage
            {
                ContentFile = "Table/pieces",
                Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize)
            };
            animPiece.Hide();

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
                    Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize)
                };
                player2Pieces[pieceIndex] = new GuiImage
                {
                    ContentFile = "Table/pieces",
                    Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize)
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

            RegisterChildren(leftFrame, rightFrame, leftBoardBackground, rightBoardBackground);
            RegisterChildren(die1, die2, ghostPiece, targetColumn);
            RegisterChildren(player1Pieces);
            RegisterChildren(player2Pieces);
            RegisterChildren(columnImages);
            RegisterChildren(moveDiceIndicator);
            RegisterChildren(animPiece);
        }

        protected override void DoUnloadContent()
        {
        }

        protected override void DoUpdate(GameTime gameTime)
        {
            lastGameTime = gameTime;

            if (isAnimating)
            {
                StepAnimation();
            }

            int diceRowY = GameDefines.DieFrameSize.Height;

            if (game.ActivePlayer == 1)
            {
                diceRowY = 0;
            }

            die1.SourceRectangle = new Rectangle2D(
                (game.Dice1 - 1) * GameDefines.DieFrameSize.Width,
                diceRowY,
                GameDefines.DieFrameSize);

            die2.SourceRectangle = new Rectangle2D(
                (game.Dice2 - 1) * GameDefines.DieFrameSize.Width,
                diceRowY,
                GameDefines.DieFrameSize);
        }

        protected override void DoDraw(SpriteBatch spriteBatch)
        {
            leftBoardBackground.Draw(spriteBatch);
            rightBoardBackground.Draw(spriteBatch);

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

            leftFrame.Draw(spriteBatch);
            rightFrame.Draw(spriteBatch);

            DrawAllBoardPieces(spriteBatch);
            DrawGhostPiece(spriteBatch);
            DrawMoveDiceIndicators(spriteBatch);
        }

        void DrawGhostPiece(SpriteBatch spriteBatch)
        {
            if (SelectedColumn < 0 ||
                !ValidDestinations.Contains(HoveredColumn))
            {
                return;
            }

            Color pieceColor = game.ActivePlayer == 1 ? ColorPlayer1 : ColorPlayer2;
            ghostPiece.SourceRectangle = new Rectangle2D(pieceColor == ColorPlayer2 ? GameDefines.PieceFrameSize : 0, 0, GameDefines.PieceFrameSize, GameDefines.PieceFrameSize);

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
                int centerX = houseBottom.Left + (houseBottom.Width - GameDefines.PieceSize) / 2;
                int ghostPiecePixelY = GetBottomHalfSlotPixelY(game.Player1.CompletedPieces, houseBottom.Bottom);

                destination = new Rectangle2D(centerX, ghostPiecePixelY, GameDefines.PieceSize, GameDefines.PieceSize);
            }
            else if (HoveredColumn == GameDefines.ColHouseP2)
            {
                int centerX = houseTop.Left + (houseTop.Width - GameDefines.PieceSize) / 2;
                int ghostPiecePixelY = GetTopHalfSlotPixelY(game.Player2.CompletedPieces, houseTop.Top);

                destination = new Rectangle2D(centerX, ghostPiecePixelY, GameDefines.PieceSize, GameDefines.PieceSize);
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
                int totalDiceWidth = diceUsed.Count * GameDefines.DiceIndicatorSize + (diceUsed.Count - 1) * GameDefines.DiceIndicatorSpacing;
                int startX = columnRectangles[destination].Left + (GameDefines.PieceSize - totalDiceWidth) / 2;
                int indicatorY = isTopHalf
                    ? columnRectangles[destination].Top - GameDefines.DiceIndicatorSize - GameDefines.DiceIndicatorSpacing
                    : columnRectangles[destination].Bottom + GameDefines.DiceIndicatorSpacing;

                for (int diceIndex = 0; diceIndex < diceUsed.Count; diceIndex++)
                {
                    int dieValue = diceUsed[diceIndex];
                    int indicatorX = startX + diceIndex * (GameDefines.DiceIndicatorSize + GameDefines.DiceIndicatorSpacing);

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

        void DrawAllBoardPieces(SpriteBatch spriteBatch)
        {
            int[] tableValues = game.TableValues;
            int suppressedColumn = isAnimating ? animationFromColumn : int.MinValue;
            int player1Index = 0;
            int player2Index = 0;

            for (int columnIndex = 0; columnIndex < GameDefines.TotalColumns; columnIndex++)
            {
                int pieceCount = Math.Abs(tableValues[columnIndex]);

                if (suppressedColumn == columnIndex && pieceCount > 0)
                {
                    pieceCount -= 1;
                }

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
                    piece.Location = new Point2D(columnRectangles[columnIndex].Left, pixelY);
                    piece.Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize);
                    piece.Update(lastGameTime);
                    piece.Draw(spriteBatch);
                }
            }

            int player1OutedPieces = game.Player1.OutedPieces;
            int player2OutedPieces = game.Player2.OutedPieces;

            if (suppressedColumn == GameDefines.ColBarP1)
            {
                player1OutedPieces = Math.Max(0, player1OutedPieces - 1);
            }

            if (suppressedColumn == GameDefines.ColBarP2)
            {
                player2OutedPieces = Math.Max(0, player2OutedPieces - 1);
            }

            int centerTopX = outColumnTop.Left + (outColumnTop.Width - GameDefines.PieceSize) / 2;

            for (int barIndex = 0; barIndex < player1OutedPieces; barIndex++)
            {
                int layer = barIndex / GameDefines.PiecesPerColumnLayer;
                int indexInLayer = barIndex % GameDefines.PiecesPerColumnLayer;
                int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / GameDefines.PieceFrameSize;
                bool isSelected = barIndex == player1OutedPieces - 1 && SelectedColumn == GameDefines.ColBarP1;
                int sourceX = isSelected ? GameDefines.PieceFrameSize * 2 : 0;
                int pixelY = outColumnTop.Top - layerOffset + indexInLayer * GameDefines.PieceSize;
                GuiImage piece = player1Pieces[player1Index++];

                piece.SourceRectangle = new Rectangle2D(sourceX, 0, GameDefines.PieceFrameSize, GameDefines.PieceFrameSize);
                piece.Location = new Point2D(centerTopX, pixelY);
                piece.Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize);
                piece.Update(lastGameTime);
                piece.Draw(spriteBatch);
            }

            int centerBottomX = outColumnBottom.Left + (outColumnBottom.Width - GameDefines.PieceSize) / 2;

            for (int barIndex = 0; barIndex < player2OutedPieces; barIndex++)
            {
                int layer = barIndex / GameDefines.PiecesPerColumnLayer;
                int indexInLayer = barIndex % GameDefines.PiecesPerColumnLayer;
                int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / GameDefines.PieceFrameSize;
                bool isSelected = barIndex == player2OutedPieces - 1 && SelectedColumn == GameDefines.ColBarP2;
                int sourceX = isSelected ? GameDefines.PieceFrameSize * 2 : GameDefines.PieceFrameSize;
                int pixelY = outColumnBottom.Bottom - GameDefines.PieceSize - layerOffset - indexInLayer * GameDefines.PieceSize;
                GuiImage piece = player2Pieces[player2Index++];

                piece.SourceRectangle = new Rectangle2D(sourceX, 0, GameDefines.PieceFrameSize, GameDefines.PieceFrameSize);
                piece.Location = new Point2D(centerBottomX, pixelY);
                piece.Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize);
                piece.Update(lastGameTime);
                piece.Draw(spriteBatch);
            }

            int completedPiecesPlayer2 = game.Player2.CompletedPieces;
            int completedPiecesPlayer1 = game.Player1.CompletedPieces;
            int houseP2CenterX = houseTop.Left + (houseTop.Width - GameDefines.PieceSize) / 2;

            for (int stackIndex = 0; stackIndex < completedPiecesPlayer2; stackIndex++)
            {
                int layer = stackIndex / GameDefines.PiecesPerColumnLayer;
                int indexInLayer = stackIndex % GameDefines.PiecesPerColumnLayer;
                int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / GameDefines.PieceFrameSize;
                int pixelY = houseTop.Top - layerOffset + indexInLayer * GameDefines.PieceSize;
                GuiImage piece = player2Pieces[player2Index++];

                piece.SourceRectangle = new Rectangle2D(GameDefines.PieceFrameSize, 0, GameDefines.PieceFrameSize, GameDefines.PieceFrameSize);
                piece.Location = new Point2D(houseP2CenterX, pixelY);
                piece.Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize);
                piece.Update(lastGameTime);
                piece.Draw(spriteBatch);
            }

            int houseP1CenterX = houseBottom.Left + (houseBottom.Width - GameDefines.PieceSize) / 2;

            for (int stackIndex = 0; stackIndex < completedPiecesPlayer1; stackIndex++)
            {
                int layer = stackIndex / GameDefines.PiecesPerColumnLayer;
                int indexInLayer = stackIndex % GameDefines.PiecesPerColumnLayer;
                int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / GameDefines.PieceFrameSize;
                int pixelY = houseBottom.Bottom - GameDefines.PieceSize - layerOffset - indexInLayer * GameDefines.PieceSize;
                GuiImage piece = player1Pieces[player1Index++];

                piece.SourceRectangle = new Rectangle2D(0, 0, GameDefines.PieceFrameSize, GameDefines.PieceFrameSize);
                piece.Location = new Point2D(houseP1CenterX, pixelY);
                piece.Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize);
                piece.Update(lastGameTime);
                piece.Draw(spriteBatch);
            }
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

        public bool IsInHumanBar(int x, int y) => outColumnTop.Contains(x, y);
        public bool IsInAiBar(int x, int y) => outColumnBottom.Contains(x, y);

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
                    outColumnTop.Left + (outColumnTop.Width - GameDefines.PieceSize) / 2,
                    outColumnTop.Top + stackIndex * GameDefines.PieceSize,
                    GameDefines.PieceSize,
                    GameDefines.PieceSize);

                if (destination.Contains(x, y))
                {
                    return true;
                }
            }

            return false;
        }

        public void BeginPieceMoveAnimation(int fromColumn, int toColumn, int activePlayer, Action onComplete)
        {
            if (isAnimating)
            {
                onComplete?.Invoke();

                return;
            }

            animationFromColumn = fromColumn;
            pendingMoveAction = onComplete;
            SetPendingOuting(toColumn, activePlayer);

            Point2D sourcePixel = GetAnimSourcePixel(fromColumn);
            Point2D destinationPixel = GetAnimDestPixel(toColumn, activePlayer);

            animPiece.SourceRectangle = new Rectangle2D(activePlayer == 2 ? GameDefines.PieceFrameSize : 0, 0, GameDefines.PieceFrameSize, GameDefines.PieceFrameSize);
            animPiece.Location = sourcePixel;
            animTargetLocation = destinationPixel;
            animPiece.Show();

            isAnimating = true;
        }

        public void CancelAnimation()
        {
            isAnimating = false;
            animPiece.Hide();
            pendingMoveAction = null;
            pendingOutingColumn = -1;
            pendingOutingPlayer = -1;
        }

        public void ContinuePieceMoveAnimation(int toColumn, int activePlayer, Action onComplete)
        {
            pendingMoveAction = onComplete;
            SetPendingOuting(toColumn, activePlayer);

            animPiece.SourceRectangle = new Rectangle2D(
                activePlayer == 2 ? GameDefines.PieceFrameSize : 0,
                0,
                GameDefines.PieceFrameSize,
                GameDefines.PieceFrameSize);

            animPiece.Location = animTargetLocation;
            animTargetLocation = GetAnimDestPixel(toColumn, activePlayer);
            animPiece.Show();

            isAnimating = true;
        }

        // Returns the pixel Y for a given slot index in a top-half column/bar/house
        // (pieces grow downward from the column's top edge).
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
                int centerX = outColumnTop.Left + (outColumnTop.Width - GameDefines.PieceSize) / 2;
                int sourceSlotIndex = game.Player1.OutedPieces - 1;

                return new Point2D(centerX, GetTopHalfSlotPixelY(sourceSlotIndex, outColumnTop.Top));
            }

            int player2BarCenterX = outColumnBottom.Left + (outColumnBottom.Width - GameDefines.PieceSize) / 2;
            int player2SourceSlotIndex = game.Player2.OutedPieces - 1;

            return new Point2D(player2BarCenterX, GetBottomHalfSlotPixelY(player2SourceSlotIndex, outColumnBottom.Bottom));
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
                int centerX = houseBottom.Left + (houseBottom.Width - GameDefines.PieceSize) / 2;

                return new Point2D(centerX, GetBottomHalfSlotPixelY(game.Player1.CompletedPieces, houseBottom.Bottom));
            }

            int houseCenterX = houseTop.Left + (houseTop.Width - GameDefines.PieceSize) / 2;

            return new Point2D(houseCenterX, GetTopHalfSlotPixelY(game.Player2.CompletedPieces, houseTop.Top));
        }

        void SetPendingOuting(int toColumn, int activePlayer)
        {
            int playerSign = activePlayer == 1 ? 1 : -1;

            if (toColumn >= 0 && toColumn < GameDefines.TotalColumns &&
                game.TableValues[toColumn] * playerSign < 0 &&
                Math.Abs(game.TableValues[toColumn]) == 1)
            {
                pendingOutingColumn = toColumn;
                pendingOutingPlayer = activePlayer == 1 ? 2 : 1;
            }
            else
            {
                pendingOutingColumn = -1;
                pendingOutingPlayer = -1;
            }
        }

        void BeginOutingAnimation(int hitColumn, int hitPlayer)
        {
            Point2D sourcePixel = new(columnRectangles[hitColumn].Left, columnRectangles[hitColumn].Bottom - GameDefines.PieceSize);

            if (hitColumn < GameDefines.TotalColumns / 2)
            {
                sourcePixel = new Point2D(columnRectangles[hitColumn].Left, columnRectangles[hitColumn].Top);
            }

            Point2D destinationPixel;

            if (hitPlayer == 1)
            {
                int centerX = outColumnTop.Left + (outColumnTop.Width - GameDefines.PieceSize) / 2;
                int destinationSlotIndex = game.Player1.OutedPieces - 1;
                destinationPixel = new Point2D(centerX, GetTopHalfSlotPixelY(destinationSlotIndex, outColumnTop.Top));
                animationFromColumn = GameDefines.ColBarP1;
            }
            else
            {
                int centerX = outColumnBottom.Left + (outColumnBottom.Width - GameDefines.PieceSize) / 2;
                int destinationSlotIndex = game.Player2.OutedPieces - 1;
                destinationPixel = new Point2D(centerX, GetBottomHalfSlotPixelY(destinationSlotIndex, outColumnBottom.Bottom));
                animationFromColumn = GameDefines.ColBarP2;
            }

            animPiece.SourceRectangle = new Rectangle2D(hitPlayer == 1 ? 0 : GameDefines.PieceFrameSize, 0, GameDefines.PieceFrameSize, GameDefines.PieceFrameSize);
            animPiece.Location = sourcePixel;
            animTargetLocation = destinationPixel;
            animPiece.Show();

            pendingMoveAction = null;
            isAnimating = true;
        }

        void StepAnimation()
        {
            Point2D current = animPiece.Location;
            Point2D delta = animTargetLocation - current;
            double distance = Math.Sqrt((double)delta.X * delta.X + (double)delta.Y * delta.Y);

            if (distance <= GameDefines.AnimationSpeed)
            {
                animPiece.Location = animTargetLocation;
                OnAnimationComplete();
            }
            else
            {
                float stepX = (float)(delta.X / distance * GameDefines.AnimationSpeed);
                float stepY = (float)(delta.Y / distance * GameDefines.AnimationSpeed);
                animPiece.Location = new Point2D(current.X + (int)Math.Round(stepX), current.Y + (int)Math.Round(stepY));
            }
        }

        void OnAnimationComplete()
        {
            isAnimating = false;
            animPiece.Hide();

            int outingColumn = pendingOutingColumn;
            int outingPlayer = pendingOutingPlayer;
            pendingOutingColumn = -1;
            pendingOutingPlayer = -1;

            Action completionAction = pendingMoveAction;
            pendingMoveAction = null;
            completionAction?.Invoke();

            if (outingColumn >= 0 && !isAnimating)
            {
                BeginOutingAnimation(outingColumn, outingPlayer);
            }
        }

        void BuildLayoutRectangles()
        {
            columnRectangles = new Rectangle2D[GameDefines.TotalColumns];
            columnRectangles[11] = new Rectangle2D(
                GameDefines.FrameBorder,
                GameDefines.FrameBorder,
                GameDefines.PieceSize,
                GameDefines.ColumnHeight);

            for (int columnIndex = 10; columnIndex >= 0; columnIndex--)
            {
                int topY = GameDefines.FrameBorder;

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

            int bottomY = GameDefines.FrameBorder + GameDefines.BoardHalfHeight - GameDefines.ColumnHeight;

            columnRectangles[12] = new Rectangle2D(
                GameDefines.FrameBorder,
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

            int outColumnX = GameDefines.BarX - GameDefines.FrameBorder;
            int outColumnWidth = columnRectangles[5].X - outColumnX;
            int boardMidY = GameDefines.RightFrameTopY + GameDefines.BoardHalfHeight / 2;

            outColumnTop = new Rectangle2D(
                outColumnX,
                GameDefines.RightFrameTopY,
                outColumnWidth,
                boardMidY - GameDefines.RightFrameTopY);

            outColumnBottom = new Rectangle2D(
                outColumnX,
                boardMidY,
                outColumnWidth,
                GameDefines.RightFrameTopY + GameDefines.BoardHalfHeight - boardMidY);

            houseTop = new Rectangle2D(
                GameDefines.HouseX,
                GameDefines.FrameBorder,
                GameDefines.HouseWidth,
                GameDefines.BoardHalfHeight / 2);

            houseBottom = new Rectangle2D(
                GameDefines.HouseX,
                GameDefines.FrameBorder + GameDefines.BoardHalfHeight / 2,
                GameDefines.HouseWidth,
                GameDefines.BoardHalfHeight / 2);
        }
    }
}
