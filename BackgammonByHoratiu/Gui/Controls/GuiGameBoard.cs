using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NuciXNA.DataAccess.Content;
using NuciXNA.Graphics;
using NuciXNA.Graphics.Drawing;
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

        Texture2D pixelTexture;
        GuiImage leftBoardBackground;
        GuiImage rightBoardBackground;
        GuiImage leftFrame;
        GuiImage rightFrame;
        GuiImage[] columnImages;
        GuiImage targetColumn;
        GuiImage ghostPiece;
        GuiImage[] pieces;
        GuiImage[] topBar;
        GuiImage[] bottomBar;
        int pieceFrameSize;
        GuiImage die1;
        GuiImage die2;
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
            pixelTexture = new Texture2D(GraphicsManager.Instance.Graphics.GraphicsDevice, 1, 1);
            pixelTexture.SetData([Color.White]);

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


            TextureSprite sizeProbe = new() { ContentFile = "Table/pieces" };
            sizeProbe.LoadContent();
            pieceFrameSize = sizeProbe.TextureSize.Height;
            sizeProbe.UnloadContent();

            animPiece = new GuiImage
            {
                ContentFile = "Table/pieces",
                Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize)
            };
            animPiece.Hide();

            pieces =
            [
                new GuiImage
                {
                    ContentFile = "Table/pieces",
                    SourceRectangle = new Rectangle2D(0, 0, pieceFrameSize, pieceFrameSize),
                    Size = new Size2D(pieceFrameSize, pieceFrameSize)
                },
                new GuiImage
                {
                    ContentFile = "Table/pieces",
                    SourceRectangle = new Rectangle2D(pieceFrameSize, 0, pieceFrameSize, pieceFrameSize),
                    Size = new Size2D(pieceFrameSize, pieceFrameSize)
                },
                new GuiImage
                {
                    ContentFile = "Table/pieces",
                    SourceRectangle = new Rectangle2D(pieceFrameSize * 2, 0, pieceFrameSize, pieceFrameSize),
                    Size = new Size2D(pieceFrameSize, pieceFrameSize)
                }
            ];

            foreach (var piece in pieces)
            {
                piece.Hide();
            }

            ghostPiece = new GuiImage
            {
                ContentFile = "Table/pieces",
                Opacity = 0.5f
            };
            ghostPiece.Hide();

            topBar = new GuiImage[GameDefines.TotalPiecesPerPlayer];
            bottomBar = new GuiImage[GameDefines.TotalPiecesPerPlayer];

            for (int i = 0; i < GameDefines.TotalPiecesPerPlayer; i++)
            {
                topBar[i] = new GuiImage
                {
                    ContentFile = "Table/pieces",
                    SourceRectangle = new Rectangle2D(0, 0, pieceFrameSize, pieceFrameSize),
                    Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize)
                };
                topBar[i].Hide();

                bottomBar[i] = new GuiImage
                {
                    ContentFile = "Table/pieces",
                    SourceRectangle = new Rectangle2D(pieceFrameSize, 0, pieceFrameSize, pieceFrameSize),
                    Size = new Size2D(GameDefines.PieceSize, GameDefines.PieceSize)
                };
                bottomBar[i].Hide();
            }

            RegisterChildren(leftFrame, rightFrame, leftBoardBackground, rightBoardBackground);
            RegisterChildren(die1, die2, ghostPiece, targetColumn);
            RegisterChildren(pieces);
            RegisterChildren(columnImages);
            RegisterChildren(topBar);
            RegisterChildren(bottomBar);
            RegisterChildren(animPiece);
        }

        protected override void DoUnloadContent()
        {
            pixelTexture?.Dispose();
        }

        protected override void DoUpdate(GameTime gameTime)
        {
            lastGameTime = gameTime;

            if (isAnimating)
            {
                StepAnimation();
            }

            UpdateOutedPieceSlots();

            int diceRowY = game.ActivePlayer == 1 ? 0 : GameDefines.DieFrameSize.Height;

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
                else if (destination == GameDefines.ColHouseP1)
                {
                    DrawBorder(spriteBatch, houseBottom, Color.Cyan, 3);
                }
                else if (destination == GameDefines.ColHouseP2)
                {
                    DrawBorder(spriteBatch, houseTop, Color.Cyan, 3);
                }
            }

            DrawPieces(spriteBatch);
            DrawCompletedPieces(spriteBatch);
            DrawGhostPiece(spriteBatch);
        }

        void DrawGhostPiece(SpriteBatch spriteBatch)
        {
            if (SelectedColumn < 0 || !ValidDestinations.Contains(HoveredColumn))
            {
                return;
            }

            Color pieceColor = game.ActivePlayer == 1 ? ColorPlayer1 : ColorPlayer2;
            ghostPiece.SourceRectangle = new Rectangle2D(pieceColor == ColorPlayer2 ? pieceFrameSize : 0, 0, pieceFrameSize, pieceFrameSize);

            Rectangle2D destination;

            if (HoveredColumn >= 0 &&
                HoveredColumn < GameDefines.TotalColumns)
            {
                int[] tableValues = game.TableValues;
                int playerSign = game.ActivePlayer == 1 ? 1 : -1;
                int existingCount = tableValues[HoveredColumn] * playerSign > 0 ? Math.Abs(tableValues[HoveredColumn]) : 0;
                int targetSlot = Math.Min(existingCount, GameDefines.PiecesPerColumnLayer - 1);

                if (HoveredColumn < GameDefines.TotalColumns / 2)
                {
                    destination = new Rectangle2D(
                        columnRectangles[HoveredColumn].Left,
                        columnRectangles[HoveredColumn].Top + targetSlot * GameDefines.PieceSize,
                        GameDefines.PieceSize,
                        GameDefines.PieceSize);
                }
                else
                {
                    destination = new Rectangle2D(
                        columnRectangles[HoveredColumn].Left,
                        columnRectangles[HoveredColumn].Bottom - (targetSlot + 1) * GameDefines.PieceSize,
                        GameDefines.PieceSize,
                        GameDefines.PieceSize);
                }
            }
            else if (HoveredColumn == GameDefines.ColHouseP1)
            {
                int existingCount = Math.Min(game.Player1.CompletedPieces, GameDefines.PiecesPerColumnLayer - 1);
                int centerX = houseBottom.Left + (houseBottom.Width - GameDefines.PieceSize) / 2;

                destination = new Rectangle2D(
                    centerX,
                    houseBottom.Bottom - (existingCount + 1) * GameDefines.PieceSize,
                    GameDefines.PieceSize,
                    GameDefines.PieceSize);
            }
            else if (HoveredColumn == GameDefines.ColHouseP2)
            {
                int existingCount = Math.Min(game.Player2.CompletedPieces, GameDefines.PiecesPerColumnLayer - 1);
                int centerX = houseTop.Left + (houseTop.Width - GameDefines.PieceSize) / 2;

                destination = new Rectangle2D(
                    centerX,
                    houseTop.Top + existingCount * GameDefines.PieceSize,
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

        void DrawPieces(SpriteBatch spriteBatch)
        {
            int[] tableValues = game.TableValues;
            int suppressedColumn = isAnimating ? animationFromColumn : int.MinValue;

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

                Color pieceColor = tableValues[columnIndex] > 0 ? ColorPlayer1 : ColorPlayer2;

                for (int stackIndex = 0; stackIndex < pieceCount; stackIndex++)
                {
                    int layer = stackIndex / GameDefines.PiecesPerColumnLayer;
                    int indexInLayer = stackIndex % GameDefines.PiecesPerColumnLayer;
                    int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / pieceFrameSize;
                    Rectangle2D destination;

                    if (columnIndex < GameDefines.TotalColumns / 2)
                    {
                        destination = new Rectangle2D(
                            columnRectangles[columnIndex].Left,
                            columnRectangles[columnIndex].Top - layerOffset + indexInLayer * GameDefines.PieceSize,
                            GameDefines.PieceSize,
                            GameDefines.PieceSize);
                    }
                    else
                    {
                        destination = new Rectangle2D(
                            columnRectangles[columnIndex].Left,
                            columnRectangles[columnIndex].Bottom - GameDefines.PieceSize - layerOffset - indexInLayer * GameDefines.PieceSize,
                            GameDefines.PieceSize,
                            GameDefines.PieceSize);
                    }

                    bool isTopPiece = stackIndex == pieceCount - 1 && columnIndex == SelectedColumn;
                    DrawCircle(spriteBatch, destination, pieceColor, isTopPiece);
                }
            }

        }

        void UpdateOutedPieceSlots()
        {
            int suppressedColumn = isAnimating ? animationFromColumn : int.MinValue;

            int player1OutedPieces = suppressedColumn == GameDefines.ColBarP1
                ? Math.Max(0, game.Player1.OutedPieces - 1)
                : game.Player1.OutedPieces;
            int player2OutedPieces = suppressedColumn == GameDefines.ColBarP2
                ? Math.Max(0, game.Player2.OutedPieces - 1)
                : game.Player2.OutedPieces;

            int centerTopX = outColumnTop.Left + (outColumnTop.Width - GameDefines.PieceSize) / 2;
            int centerBottomX = outColumnBottom.Left + (outColumnBottom.Width - GameDefines.PieceSize) / 2;

            for (int i = 0; i < topBar.Length; i++)
            {
                if (i < player1OutedPieces)
                {
                    int layer = i / GameDefines.PiecesPerColumnLayer;
                    int indexInLayer = i % GameDefines.PiecesPerColumnLayer;
                    int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / pieceFrameSize;
                    bool isSelected = i == player1OutedPieces - 1 && SelectedColumn == GameDefines.ColBarP1;
                    topBar[i].SourceRectangle = new Rectangle2D(isSelected ? pieceFrameSize * 2 : 0, 0, pieceFrameSize, pieceFrameSize);
                    topBar[i].Location = new Point2D(centerTopX, outColumnTop.Top - layerOffset + indexInLayer * GameDefines.PieceSize);
                    topBar[i].Show();
                }
                else
                {
                    topBar[i].Hide();
                }

                if (i < player2OutedPieces)
                {
                    int layer = i / GameDefines.PiecesPerColumnLayer;
                    int indexInLayer = i % GameDefines.PiecesPerColumnLayer;
                    int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / pieceFrameSize;
                    bool isSelected = i == player2OutedPieces - 1 && SelectedColumn == GameDefines.ColBarP2;
                    bottomBar[i].SourceRectangle = new Rectangle2D(isSelected ? pieceFrameSize * 2 : pieceFrameSize, 0, pieceFrameSize, pieceFrameSize);
                    bottomBar[i].Location = new Point2D(centerBottomX, outColumnBottom.Bottom - GameDefines.PieceSize - layerOffset - indexInLayer * GameDefines.PieceSize);
                    bottomBar[i].Show();
                }
                else
                {
                    bottomBar[i].Hide();
                }
            }
        }

        void DrawCompletedPieces(SpriteBatch spriteBatch)
        {
            int completedPiecesPlayer2 = game.Player2.CompletedPieces;
            int completedPiecesPlayer1 = game.Player1.CompletedPieces;

            for (int stackIndex = 0; stackIndex < completedPiecesPlayer2; stackIndex++)
            {
                int layer = stackIndex / GameDefines.PiecesPerColumnLayer;
                int indexInLayer = stackIndex % GameDefines.PiecesPerColumnLayer;
                int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / pieceFrameSize;
                int centerX = houseTop.Left + (houseTop.Width - GameDefines.PieceSize) / 2;

                Rectangle2D destination = new(
                    centerX,
                    houseTop.Top - layerOffset + indexInLayer * GameDefines.PieceSize,
                    GameDefines.PieceSize,
                    GameDefines.PieceSize);

                DrawCircle(spriteBatch, destination, ColorPlayer2);
            }

            for (int stackIndex = 0; stackIndex < completedPiecesPlayer1; stackIndex++)
            {
                int layer = stackIndex / GameDefines.PiecesPerColumnLayer;
                int indexInLayer = stackIndex % GameDefines.PiecesPerColumnLayer;
                int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * GameDefines.PieceSize / pieceFrameSize;
                int centerX = houseBottom.Left + (houseBottom.Width - GameDefines.PieceSize) / 2;

                Rectangle2D destination = new(
                    centerX,
                    houseBottom.Bottom - GameDefines.PieceSize - layerOffset - indexInLayer * GameDefines.PieceSize,
                    GameDefines.PieceSize,
                    GameDefines.PieceSize);

                DrawCircle(spriteBatch, destination, ColorPlayer1);
            }
        }

        void DrawCircle(SpriteBatch spriteBatch, Rectangle2D destination, Color fill, bool isSelected = false)
        {
            int spriteIndex = isSelected ? 2 : (fill == ColorPlayer2 ? 1 : 0);

            pieces[spriteIndex].Location = new Point2D(destination.X, destination.Y);
            pieces[spriteIndex].Size = new Size2D(destination.Width, destination.Height);
            pieces[spriteIndex].Update(lastGameTime);
            pieces[spriteIndex].Draw(spriteBatch);
        }

        void DrawBorder(SpriteBatch spriteBatch, Rectangle2D rectangle, Color color, int thickness)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle2D(rectangle.Left, rectangle.Top, rectangle.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle2D(rectangle.Left, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle2D(rectangle.Left, rectangle.Top, thickness, rectangle.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle2D(rectangle.Right - thickness, rectangle.Top, thickness, rectangle.Height), color);
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

        public bool IsInOutColumnTop(int x, int y) => outColumnTop.Contains(x, y);
        public bool IsInOutColumnBottom(int x, int y) => outColumnBottom.Contains(x, y);

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
                    Rectangle2D destination;
                    if (columnIndex < GameDefines.TotalColumns / 2)
                    {
                        destination = new Rectangle2D(
                            columnRectangles[columnIndex].Left,
                            columnRectangles[columnIndex].Top + stackIndex * GameDefines.PieceSize,
                            GameDefines.PieceSize,
                            GameDefines.PieceSize);
                    }
                    else
                    {
                        destination = new Rectangle2D(
                            columnRectangles[columnIndex].Left,
                            columnRectangles[columnIndex].Bottom - (stackIndex + 1) * GameDefines.PieceSize,
                            GameDefines.PieceSize,
                            GameDefines.PieceSize);
                    }

                    if (destination.Contains(x, y))
                    {
                        return true;
                    }
                }
            }

            int player1OutedPieces = game.Player1.OutedPieces;
            int centerX = outColumnTop.Left + (outColumnTop.Width - GameDefines.PieceSize) / 2;

            for (int stackIndex = 0; stackIndex < Math.Min(player1OutedPieces, GameDefines.PiecesPerColumnLayer); stackIndex++)
            {
                Rectangle2D destination = new(
                    centerX,
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

            animPiece.SourceRectangle = new Rectangle2D(activePlayer == 2 ? pieceFrameSize : 0, 0, pieceFrameSize, pieceFrameSize);
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

            animPiece.SourceRectangle = new Rectangle2D(activePlayer == 2 ? pieceFrameSize : 0, 0, pieceFrameSize, pieceFrameSize);
            animPiece.Location = animTargetLocation;
            animTargetLocation = GetAnimDestPixel(toColumn, activePlayer);
            animPiece.Show();

            isAnimating = true;
        }

        Point2D GetAnimSourcePixel(int fromColumn)
        {
            if (fromColumn >= 0 && fromColumn < GameDefines.TotalColumns)
            {
                int pieceCount = Math.Min(Math.Abs(game.TableValues[fromColumn]), GameDefines.PiecesPerColumnLayer);

                if (fromColumn < GameDefines.TotalColumns / 2)
                {
                    return new Point2D(
                        columnRectangles[fromColumn].Left,
                        columnRectangles[fromColumn].Top + (pieceCount - 1) * GameDefines.PieceSize);
                }
                else
                {
                    return new Point2D(
                        columnRectangles[fromColumn].Left,
                        columnRectangles[fromColumn].Bottom - pieceCount * GameDefines.PieceSize);
                }
            }

            if (fromColumn == GameDefines.ColBarP1)
            {
                int centerX = outColumnTop.Left + (outColumnTop.Width - GameDefines.PieceSize) / 2;
                int pieceCount = Math.Min(game.Player1.OutedPieces, GameDefines.PiecesPerColumnLayer);

                return new Point2D(centerX, outColumnTop.Top + (pieceCount - 1) * GameDefines.PieceSize);
            }

            int player2BarCenterX = outColumnBottom.Left + (outColumnBottom.Width - GameDefines.PieceSize) / 2;
            int player2OutedCount = Math.Min(game.Player2.OutedPieces, GameDefines.PiecesPerColumnLayer);

            return new Point2D(player2BarCenterX, outColumnBottom.Bottom - player2OutedCount * GameDefines.PieceSize);
        }

        Point2D GetAnimDestPixel(int toColumn, int activePlayer)
        {
            if (toColumn >= 0 && toColumn < GameDefines.TotalColumns)
            {
                int playerSign = activePlayer == 1 ? 1 : -1;
                int existingCount = game.TableValues[toColumn] * playerSign > 0
                    ? Math.Abs(game.TableValues[toColumn])
                    : 0;
                int targetSlot;

                if (existingCount >= GameDefines.PiecesPerColumnLayer)
                {
                    targetSlot = GameDefines.PiecesPerColumnLayer - 1;
                }
                else
                {
                    targetSlot = existingCount;
                }

                if (toColumn < GameDefines.TotalColumns / 2)
                {
                    return new Point2D(columnRectangles[toColumn].Left, columnRectangles[toColumn].Top + targetSlot * GameDefines.PieceSize);
                }
                else
                {
                    return new Point2D(columnRectangles[toColumn].Left, columnRectangles[toColumn].Bottom - (targetSlot + 1) * GameDefines.PieceSize);
                }
            }

            if (toColumn == GameDefines.ColHouseP1)
            {
                int centerX = houseBottom.Left + (houseBottom.Width - GameDefines.PieceSize) / 2;
                int existingCount = game.Player1.CompletedPieces;
                int targetSlot = existingCount;

                if (existingCount >= GameDefines.PiecesPerColumnLayer)
                {
                    targetSlot = 0;
                }

                return new Point2D(centerX, houseBottom.Bottom - (targetSlot + 1) * GameDefines.PieceSize);
            }

            int houseCenterX = houseTop.Left + (houseTop.Width - GameDefines.PieceSize) / 2;
            int existingInHouse = game.Player2.CompletedPieces;
            int houseTargetSlot;

            if (existingInHouse >= GameDefines.PiecesPerColumnLayer)
            {
                houseTargetSlot = GameDefines.PiecesPerColumnLayer - 1;
            }
            else
            {
                houseTargetSlot = existingInHouse;
            }

            return new Point2D(houseCenterX, houseTop.Top + houseTargetSlot * GameDefines.PieceSize);
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
            Point2D sourcePixel = hitColumn < GameDefines.TotalColumns / 2
                ? new Point2D(columnRectangles[hitColumn].Left, columnRectangles[hitColumn].Top)
                : new Point2D(columnRectangles[hitColumn].Left, columnRectangles[hitColumn].Bottom - GameDefines.PieceSize);

            Point2D destinationPixel;

            if (hitPlayer == 1)
            {
                int centerX = outColumnTop.Left + (outColumnTop.Width - GameDefines.PieceSize) / 2;
                int pieceCount = Math.Min(game.Player1.OutedPieces, GameDefines.PiecesPerColumnLayer);
                destinationPixel = new Point2D(centerX, outColumnTop.Top + (pieceCount - 1) * GameDefines.PieceSize);
                animationFromColumn = GameDefines.ColBarP1;
            }
            else
            {
                int centerX = outColumnBottom.Left + (outColumnBottom.Width - GameDefines.PieceSize) / 2;
                int pieceCount = Math.Min(game.Player2.OutedPieces, GameDefines.PiecesPerColumnLayer);
                destinationPixel = new Point2D(centerX, outColumnBottom.Bottom - pieceCount * GameDefines.PieceSize);
                animationFromColumn = GameDefines.ColBarP2;
            }

            animPiece.SourceRectangle = new Rectangle2D(hitPlayer == 1 ? 0 : pieceFrameSize, 0, pieceFrameSize, pieceFrameSize);
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
            int boardBottomY = GameDefines.RightFrameTopY + GameDefines.BoardHalfHeight;

            outColumnTop = new Rectangle2D(outColumnX, GameDefines.RightFrameTopY, outColumnWidth, boardMidY - GameDefines.RightFrameTopY);
            outColumnBottom = new Rectangle2D(outColumnX, boardMidY, outColumnWidth, boardBottomY - boardMidY);
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
