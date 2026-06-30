using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NuciXNA.DataAccess.Content;
using NuciXNA.Graphics;
using NuciXNA.Graphics.Drawing;
using NuciXNA.Graphics.SpriteEffects;
using NuciXNA.Gui.Controls;
using NuciXNA.Primitives;

using BackgammonByHoratiu.GameLogic.GameManagers;
using BackgammonByHoratiu.Settings;

namespace BackgammonByHoratiu.Gui.Controls
{
    public class GuiGameBoard(IGameManager game) : GuiControl
    {
        static readonly Color ColorPlayer1 = Color.White;
        static readonly Color ColorHouseColumn = new(63, 63, 63);
        static readonly Color ColorPlayer2 = new(139, 69, 19);
        TextureSprite animSpriteWhite;
        TextureSprite animSpriteBrown;
        bool isAnimating;
        int animationFromColumn;
        int animationDestinationColumn = -1;
        int pendingOutingColumn = -1;
        int pendingOutingPlayer = -1;
        Action pendingMoveAction;

        public bool IsAnimating => isAnimating;

        Texture2D pixelTexture;
        GuiImage boardImage;
        GuiImage[] columnImages;
        GuiImage targetColumnImage;
        GuiImage ghostPiece;
        GuiImage[] pieces;
        int pieceFrameSize;
        GuiImage die1;
        GuiImage die2;
        GameTime lastGameTime = new();

        Rectangle[] columnRectangles;
        Rectangle outColumnTop, outColumnBottom;
        Rectangle houseTop, houseBottom;
        Rectangle dice1Rectangle, dice2Rectangle;


        public int SelectedColumn { get; set; } = -1;
        public int HoveredColumn { get; set; } = -1;

        public IReadOnlyList<int> ValidDestinations { get; set; } = Array.Empty<int>();

        public bool IsOnDice(int x, int y) => dice1Rectangle.Contains(x, y) || dice2Rectangle.Contains(x, y);

        public bool IsOnHouse(int x, int y) => houseTop.Contains(x, y) || houseBottom.Contains(x, y);

        protected override void DoLoadContent()
        {
            pixelTexture = new Texture2D(GraphicsManager.Instance.Graphics.GraphicsDevice, 1, 1);
            pixelTexture.SetData([Color.White]);

            BuildLayoutRectangles();

            boardImage = new GuiImage
            {
                ContentFile = "Table/board",
                Location = Point2D.Empty,
                Size = new Size2D(GameDefines.WindowWidth, GameDefines.WindowHeight)
            };
            boardImage.Hide();

            const int columnFrameWidth = 105;
            const int columnFrameHeight = 512;

            columnImages = new GuiImage[24];

            for (int columnIndex = 0; columnIndex < 24; columnIndex++)
            {
                bool isTopHalf = columnIndex < 12;
                bool isYellow = isTopHalf ? columnIndex % 2 != 0 : columnIndex % 2 == 0;
                int sourceX = isYellow ? 0 : columnFrameWidth;

                columnImages[columnIndex] = new GuiImage
                {
                    ContentFile = "Table/columns",
                    Location = new Point2D(columnRectangles[columnIndex].X, columnRectangles[columnIndex].Y),
                    Size = new Size2D(columnRectangles[columnIndex].Width, columnRectangles[columnIndex].Height),
                    SourceRectangle = new Rectangle2D(sourceX, 0, columnFrameWidth, columnFrameHeight)
                };

                if (isTopHalf)
                {
                    columnImages[columnIndex].Rotation = MathHelper.Pi;
                }

                columnImages[columnIndex].Hide();
            }

            targetColumnImage = new GuiImage
            {
                ContentFile = "Table/columns",
                SourceRectangle = new Rectangle2D(columnFrameWidth * 2, 0, columnFrameWidth, columnFrameHeight)
            };
            targetColumnImage.Hide();

            die1 = new()
            {
                ContentFile = "Table/dice",
                Location = new Point2D(dice1Rectangle.X, dice1Rectangle.Y),
                Size = new Size2D(dice1Rectangle.Width, dice1Rectangle.Height)
            };

            die2 = new()
            {
                ContentFile = "Table/dice",
                Location = new Point2D(dice2Rectangle.X, dice2Rectangle.Y),
                Size = new Size2D(dice2Rectangle.Width, dice2Rectangle.Height)
            };

            animSpriteWhite = new()
            {
                ContentFile = "Table/pieces",
                MovementEffect = new MovementEffect { Speed = GameDefines.AnimationSpeed },
                IsActive = true
            };
            animSpriteBrown = new()
            {
                ContentFile = "Table/pieces",
                MovementEffect = new MovementEffect { Speed = GameDefines.AnimationSpeed },
                IsActive = true
            };
            animSpriteWhite.LoadContent();
            animSpriteBrown.LoadContent();
            animSpriteWhite.MovementEffect.Deactivated += OnAnimSpriteDeactivated;
            animSpriteBrown.MovementEffect.Deactivated += OnAnimSpriteDeactivated;

            this.pieceFrameSize = animSpriteWhite.TextureSize.Height;
            animSpriteWhite.SourceRectangle = new Rectangle2D(0, 0, pieceFrameSize, pieceFrameSize);
            animSpriteBrown.SourceRectangle = new Rectangle2D(pieceFrameSize, 0, pieceFrameSize, pieceFrameSize);

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

            RegisterChildren(die1, die2, pieces[0], pieces[1], pieces[2], ghostPiece, boardImage, targetColumnImage);
            RegisterChildren(columnImages);
        }

        protected override void DoUnloadContent()
        {
            animSpriteWhite.MovementEffect.Deactivated -= OnAnimSpriteDeactivated;
            animSpriteBrown.MovementEffect.Deactivated -= OnAnimSpriteDeactivated;
            animSpriteWhite.UnloadContent();
            animSpriteBrown.UnloadContent();

            pixelTexture?.Dispose();
        }

        protected override void DoUpdate(GameTime gameTime)
        {
            lastGameTime = gameTime;

            if (isAnimating)
            {
                if (animSpriteWhite.MovementEffect.IsActive)
                {
                    animSpriteWhite.Update(gameTime);
                }
                else if (animSpriteBrown.MovementEffect.IsActive)
                {
                    animSpriteBrown.Update(gameTime);
                }
            }

            const int dieFrameSize = 200;
            int diceRowY = game.ActivePlayer == 1 ? 0 : dieFrameSize;

            die1.SourceRectangle = new Rectangle2D((game.Dice1 - 1) * dieFrameSize, diceRowY, dieFrameSize, dieFrameSize);
            die2.SourceRectangle = new Rectangle2D((game.Dice2 - 1) * dieFrameSize, diceRowY, dieFrameSize, dieFrameSize);
        }

        protected override void DoDraw(SpriteBatch spriteBatch)
        {
            boardImage.Draw(spriteBatch);

            foreach (GuiImage columnImage in columnImages)
            {
                columnImage.Draw(spriteBatch);
            }

            foreach (int destination in ValidDestinations)
            {
                if (destination >= 0 && destination < 24)
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

            spriteBatch.Draw(pixelTexture, houseTop, ColorHouseColumn);
            spriteBatch.Draw(pixelTexture, houseBottom, ColorHouseColumn);

            DrawPieces(spriteBatch);

            DrawCompletedPieces(spriteBatch);

            DrawGhostPiece(spriteBatch);

            if (isAnimating)
            {
                TextureSprite activeAnimSprite = null;
                Color activeAnimColor = Color.Black;

                if (animSpriteWhite.MovementEffect.IsActive)
                {
                    activeAnimSprite = animSpriteWhite;
                    activeAnimColor = ColorPlayer1;
                }
                else if (animSpriteBrown.MovementEffect.IsActive)
                {
                    activeAnimSprite = animSpriteBrown;
                    activeAnimColor = ColorPlayer2;
                }

                if (activeAnimSprite is not null)
                {
                    Point2D animationPosition = activeAnimSprite.Location + activeAnimSprite.MovementEffect.LocationOffset;
                    int pieceSize = GameDefines.PieceSize;

                    DrawCircle(spriteBatch, new Rectangle(animationPosition.X, animationPosition.Y, pieceSize, pieceSize), activeAnimColor);
                }
            }
        }

        void DrawGhostPiece(SpriteBatch spriteBatch)
        {
            if (SelectedColumn < 0 || !ValidDestinations.Contains(HoveredColumn))
            {
                return;
            }

            int pieceSize = GameDefines.PieceSize;
            int piecesPerColumn = GameDefines.ColumnHeight / pieceSize;
            Color pieceColor = game.ActivePlayer == 1 ? ColorPlayer1 : ColorPlayer2;
            ghostPiece.SourceRectangle = new Rectangle2D(pieceColor == ColorPlayer2 ? pieceFrameSize : 0, 0, pieceFrameSize, pieceFrameSize);

            Rectangle destination;

            if (HoveredColumn >= 0 && HoveredColumn < 24)
            {
                int[] tableValues = game.TableValues;
                int playerSign = game.ActivePlayer == 1 ? 1 : -1;
                int existingCount = tableValues[HoveredColumn] * playerSign > 0 ? Math.Abs(tableValues[HoveredColumn]) : 0;
                int targetSlot = Math.Min(existingCount, piecesPerColumn - 1);

                if (HoveredColumn < 12)
                {
                    destination = new Rectangle(columnRectangles[HoveredColumn].Left, columnRectangles[HoveredColumn].Top + targetSlot * pieceSize, pieceSize, pieceSize);
                }
                else
                {
                    destination = new Rectangle(columnRectangles[HoveredColumn].Left, columnRectangles[HoveredColumn].Bottom - (targetSlot + 1) * pieceSize, pieceSize, pieceSize);
                }
            }
            else if (HoveredColumn == GameDefines.ColHouseP1)
            {
                int existingCount = Math.Min(game.Player1.CompletedPieces, piecesPerColumn - 1);
                int centerX = houseBottom.Left + (houseBottom.Width - pieceSize) / 2;
                destination = new Rectangle(centerX, houseBottom.Bottom - (existingCount + 1) * pieceSize, pieceSize, pieceSize);
            }
            else if (HoveredColumn == GameDefines.ColHouseP2)
            {
                int existingCount = Math.Min(game.Player2.CompletedPieces, piecesPerColumn - 1);
                int centerX = houseTop.Left + (houseTop.Width - pieceSize) / 2;
                destination = new Rectangle(centerX, houseTop.Top + existingCount * pieceSize, pieceSize, pieceSize);
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
            Rectangle rectangle = columnRectangles[columnIndex];
            bool isTopHalf = columnIndex < 12;

            targetColumnImage.Location = new Point2D(rectangle.X, rectangle.Y);
            targetColumnImage.Size = new Size2D(rectangle.Width, rectangle.Height);
            targetColumnImage.Rotation = isTopHalf ? MathHelper.Pi : 0f;
            targetColumnImage.Update(lastGameTime);
            targetColumnImage.Draw(spriteBatch);
        }

        void DrawPieces(SpriteBatch spriteBatch)
        {
            int pieceSize = GameDefines.PieceSize;
            int piecesPerColumn = GameDefines.ColumnHeight / pieceSize;
            int[] tableValues = game.TableValues;
            int suppressedColumn = isAnimating ? animationFromColumn : int.MinValue;

            for (int columnIndex = 0; columnIndex < 24; columnIndex++)
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
                    int layer = stackIndex / piecesPerColumn;
                    int indexInLayer = stackIndex % piecesPerColumn;
                    int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * pieceSize / pieceFrameSize;
                    Rectangle destination;

                    if (columnIndex < 12)
                    {
                        destination = new Rectangle(columnRectangles[columnIndex].Left, columnRectangles[columnIndex].Top - layerOffset + indexInLayer * pieceSize, pieceSize, pieceSize);
                    }
                    else
                    {
                        destination = new Rectangle(columnRectangles[columnIndex].Left, columnRectangles[columnIndex].Bottom - pieceSize - layerOffset - indexInLayer * pieceSize, pieceSize, pieceSize);
                    }

                    bool isTopPiece = stackIndex == pieceCount - 1 && columnIndex == SelectedColumn;
                    DrawCircle(spriteBatch, destination, pieceColor, isTopPiece);
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

            for (int stackIndex = 0; stackIndex < player1OutedPieces; stackIndex++)
            {
                int layer = stackIndex / piecesPerColumn;
                int indexInLayer = stackIndex % piecesPerColumn;
                int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * pieceSize / pieceFrameSize;
                int centerX = outColumnTop.Left + (outColumnTop.Width - pieceSize) / 2;
                Rectangle destination = new(centerX, outColumnTop.Top - layerOffset + indexInLayer * pieceSize, pieceSize, pieceSize);
                bool isTopPiece = stackIndex == player1OutedPieces - 1 && SelectedColumn == GameDefines.ColBarP1;
                DrawCircle(spriteBatch, destination, ColorPlayer1, isTopPiece);
            }

            for (int stackIndex = 0; stackIndex < player2OutedPieces; stackIndex++)
            {
                int layer = stackIndex / piecesPerColumn;
                int indexInLayer = stackIndex % piecesPerColumn;
                int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * pieceSize / pieceFrameSize;
                int centerX = outColumnBottom.Left + (outColumnBottom.Width - pieceSize) / 2;
                Rectangle destination = new(centerX, outColumnBottom.Bottom - pieceSize - layerOffset - indexInLayer * pieceSize, pieceSize, pieceSize);
                bool isTopPiece = stackIndex == player2OutedPieces - 1 && SelectedColumn == GameDefines.ColBarP2;
                DrawCircle(spriteBatch, destination, ColorPlayer2, isTopPiece);
            }
        }

        void DrawCompletedPieces(SpriteBatch spriteBatch)
        {
            int pieceSize = GameDefines.PieceSize;
            int piecesPerColumn = GameDefines.ColumnHeight / pieceSize;

            int completedPiecesPlayer2 = game.Player2.CompletedPieces;
            int completedPiecesPlayer1 = game.Player1.CompletedPieces;

            for (int stackIndex = 0; stackIndex < completedPiecesPlayer2; stackIndex++)
            {
                int layer = stackIndex / piecesPerColumn;
                int indexInLayer = stackIndex % piecesPerColumn;
                int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * pieceSize / pieceFrameSize;
                int centerX = houseTop.Left + (houseTop.Width - pieceSize) / 2;
                Rectangle destination = new(centerX, houseTop.Top - layerOffset + indexInLayer * pieceSize, pieceSize, pieceSize);
                DrawCircle(spriteBatch, destination, ColorPlayer2);
            }

            for (int stackIndex = 0; stackIndex < completedPiecesPlayer1; stackIndex++)
            {
                int layer = stackIndex / piecesPerColumn;
                int indexInLayer = stackIndex % piecesPerColumn;
                int layerOffset = layer * GameDefines.OverflowLayerSourceOffset * pieceSize / pieceFrameSize;
                int centerX = houseBottom.Left + (houseBottom.Width - pieceSize) / 2;
                Rectangle destination = new(centerX, houseBottom.Bottom - pieceSize - layerOffset - indexInLayer * pieceSize, pieceSize, pieceSize);
                DrawCircle(spriteBatch, destination, ColorPlayer1);
            }
        }

        void DrawCircle(SpriteBatch spriteBatch, Rectangle destination, Color fill, bool isSelected = false)
        {
            int spriteIndex = isSelected ? 2 : (fill == ColorPlayer2 ? 1 : 0);
            pieces[spriteIndex].Location = new Point2D(destination.X, destination.Y);
            pieces[spriteIndex].Size = new Size2D(destination.Width, destination.Height);
            pieces[spriteIndex].Update(lastGameTime);
            pieces[spriteIndex].Draw(spriteBatch);
        }

        void DrawBorder(SpriteBatch spriteBatch, Rectangle rectangle, Color color, int thickness)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rectangle.Left, rectangle.Top, rectangle.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rectangle.Left, rectangle.Bottom - thickness, rectangle.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rectangle.Left, rectangle.Top, thickness, rectangle.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rectangle.Right - thickness, rectangle.Top, thickness, rectangle.Height), color);
        }

        public int ColumnAt(int x, int y)
        {
            for (int columnIndex = 0; columnIndex < 24; columnIndex++)
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
            int pieceSize = GameDefines.PieceSize;
            int piecesPerColumn = GameDefines.ColumnHeight / pieceSize;
            int[] tableValues = game.TableValues;

            for (int columnIndex = 0; columnIndex < 24; columnIndex++)
            {
                if (tableValues[columnIndex] <= 0)
                {
                    continue;
                }

                int pieceCount = Math.Min(tableValues[columnIndex], piecesPerColumn);

                for (int stackIndex = 0; stackIndex < pieceCount; stackIndex++)
                {
                    Rectangle destination;
                    if (columnIndex < 12)
                    {
                        destination = new Rectangle(columnRectangles[columnIndex].Left, columnRectangles[columnIndex].Top + stackIndex * pieceSize, pieceSize, pieceSize);
                    }
                    else
                    {
                        destination = new Rectangle(columnRectangles[columnIndex].Left, columnRectangles[columnIndex].Bottom - (stackIndex + 1) * pieceSize, pieceSize, pieceSize);
                    }

                    if (destination.Contains(x, y))
                    {
                        return true;
                    }
                }
            }

            int player1OutedPieces = game.Player1.OutedPieces;
            int centerX = outColumnTop.Left + (outColumnTop.Width - pieceSize) / 2;

            for (int stackIndex = 0; stackIndex < Math.Min(player1OutedPieces, piecesPerColumn); stackIndex++)
            {
                Rectangle destination = new(centerX, outColumnTop.Top + stackIndex * pieceSize, pieceSize, pieceSize);

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

            int pieceSize = GameDefines.PieceSize;
            int piecesPerColumn = GameDefines.ColumnHeight / pieceSize;

            animationFromColumn = fromColumn;
            animationDestinationColumn = toColumn;
            pendingMoveAction = onComplete;
            SetPendingOuting(toColumn, activePlayer);

            Point2D sourcePixel = GetAnimSourcePixel(fromColumn, pieceSize, piecesPerColumn);
            Point2D destinationPixel = GetAnimDestPixel(toColumn, activePlayer, pieceSize, piecesPerColumn);

            TextureSprite animationSprite = activePlayer == 2 ? animSpriteBrown : animSpriteWhite;
            animationSprite.Location = sourcePixel;
            animationSprite.MovementEffect.TargetLocation = destinationPixel;
            animationSprite.MovementEffect.Activate();

            isAnimating = true;
        }

        public void CancelAnimation()
        {
            isAnimating = false;
            pendingMoveAction = null;
            animationDestinationColumn = -1;
            pendingOutingColumn = -1;
            pendingOutingPlayer = -1;
        }

        public void ContinuePieceMoveAnimation(int toColumn, int activePlayer, Action onComplete)
        {
            int pieceSize = GameDefines.PieceSize;
            int piecesPerColumn = GameDefines.ColumnHeight / pieceSize;

            pendingMoveAction = onComplete;
            animationDestinationColumn = toColumn;
            SetPendingOuting(toColumn, activePlayer);

            TextureSprite animationSprite = activePlayer == 2 ? animSpriteBrown : animSpriteWhite;

            animationSprite.Location = animationSprite.MovementEffect.TargetLocation;

            Point2D destinationPixel = GetAnimDestPixel(toColumn, activePlayer, pieceSize, piecesPerColumn);
            animationSprite.MovementEffect.TargetLocation = destinationPixel;
            animationSprite.MovementEffect.Activate();

            isAnimating = true;
        }

        Point2D GetAnimSourcePixel(int fromColumn, int pieceSize, int piecesPerColumn)
        {
            if (fromColumn >= 0 && fromColumn < 24)
            {
                int pieceCount = Math.Min(Math.Abs(game.TableValues[fromColumn]), piecesPerColumn);

                if (fromColumn < 12)
                {
                    return new Point2D(
                        columnRectangles[fromColumn].Left,
                        columnRectangles[fromColumn].Top + (pieceCount - 1) * pieceSize);
                }
                else
                {
                    return new Point2D(
                        columnRectangles[fromColumn].Left,
                        columnRectangles[fromColumn].Bottom - pieceCount * pieceSize);
                }
            }

            if (fromColumn == GameDefines.ColBarP1)
            {
                int centerX = outColumnTop.Left + (outColumnTop.Width - pieceSize) / 2;
                int pieceCount = Math.Min(game.Player1.OutedPieces, piecesPerColumn);

                return new Point2D(centerX, outColumnTop.Top + (pieceCount - 1) * pieceSize);
            }

            int player2BarCenterX = outColumnBottom.Left + (outColumnBottom.Width - pieceSize) / 2;
            int player2OutedCount = Math.Min(game.Player2.OutedPieces, piecesPerColumn);

            return new Point2D(player2BarCenterX, outColumnBottom.Bottom - player2OutedCount * pieceSize);
        }

        Point2D GetAnimDestPixel(int toColumn, int activePlayer, int pieceSize, int piecesPerColumn)
        {
            if (toColumn >= 0 && toColumn < 24)
            {
                int playerSign = activePlayer == 1 ? 1 : -1;
                int existingCount = game.TableValues[toColumn] * playerSign > 0
                    ? Math.Abs(game.TableValues[toColumn])
                    : 0;
                int targetSlot = existingCount >= piecesPerColumn ? piecesPerColumn - 1 : existingCount;

                if (toColumn < 12)
                {
                    return new Point2D(columnRectangles[toColumn].Left, columnRectangles[toColumn].Top + targetSlot * pieceSize);
                }
                else
                {
                    return new Point2D(columnRectangles[toColumn].Left, columnRectangles[toColumn].Bottom - (targetSlot + 1) * pieceSize);
                }
            }

            if (toColumn == GameDefines.ColHouseP1)
            {
                int centerX = houseBottom.Left + (houseBottom.Width - pieceSize) / 2;
                int existingCount = game.Player1.CompletedPieces;
                int targetSlot = existingCount >= piecesPerColumn ? 0 : existingCount;

                return new Point2D(centerX, houseBottom.Bottom - (targetSlot + 1) * pieceSize);
            }

            int houseCenterX = houseTop.Left + (houseTop.Width - pieceSize) / 2;
            int existingInHouse = game.Player2.CompletedPieces;
            int houseTargetSlot = existingInHouse >= piecesPerColumn ? piecesPerColumn - 1 : existingInHouse;

            return new Point2D(houseCenterX, houseTop.Top + houseTargetSlot * pieceSize);
        }

        void SetPendingOuting(int toColumn, int activePlayer)
        {
            int playerSign = activePlayer == 1 ? 1 : -1;

            if (toColumn >= 0 && toColumn < 24 &&
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
            int pieceSize = GameDefines.PieceSize;
            int piecesPerColumn = GameDefines.ColumnHeight / pieceSize;

            Point2D sourcePixel = hitColumn < 12
                ? new Point2D(columnRectangles[hitColumn].Left, columnRectangles[hitColumn].Top)
                : new Point2D(columnRectangles[hitColumn].Left, columnRectangles[hitColumn].Bottom - pieceSize);

            Point2D destinationPixel;

            if (hitPlayer == 1)
            {
                int centerX = outColumnTop.Left + (outColumnTop.Width - pieceSize) / 2;
                int pieceCount = Math.Min(game.Player1.OutedPieces, piecesPerColumn);
                destinationPixel = new Point2D(centerX, outColumnTop.Top + (pieceCount - 1) * pieceSize);
                animationFromColumn = GameDefines.ColBarP1;
            }
            else
            {
                int centerX = outColumnBottom.Left + (outColumnBottom.Width - pieceSize) / 2;
                int pieceCount = Math.Min(game.Player2.OutedPieces, piecesPerColumn);
                destinationPixel = new Point2D(centerX, outColumnBottom.Bottom - pieceCount * pieceSize);
                animationFromColumn = GameDefines.ColBarP2;
            }

            TextureSprite animationSprite = hitPlayer == 1 ? animSpriteWhite : animSpriteBrown;
            animationSprite.Location = sourcePixel;
            animationSprite.MovementEffect.TargetLocation = destinationPixel;
            animationSprite.MovementEffect.Activate();

            pendingMoveAction = null;
            isAnimating = true;
        }

        void OnAnimSpriteDeactivated(object sender, EventArgs e)
        {
            if (!isAnimating)
            {
                return;
            }

            isAnimating = false;

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
            int pieceSize = GameDefines.PieceSize;
            int padding = GameDefines.Padding;
            int columnHeight = GameDefines.ColumnHeight;
            int boardHeight = GameDefines.BoardHeight;

            columnRectangles = new Rectangle[24];
            columnRectangles[11] = new Rectangle(0, 0, pieceSize, columnHeight);

            for (int columnIndex = 10; columnIndex >= 0; columnIndex--)
            {
                if (columnIndex == 5)
                {
                    columnRectangles[columnIndex] = new Rectangle(
                        columnRectangles[columnIndex + 1].Right + pieceSize + padding * 2, 0, pieceSize, columnHeight);
                }
                else
                {
                    columnRectangles[columnIndex] = new Rectangle(
                        columnRectangles[columnIndex + 1].Right, 0, pieceSize, columnHeight);
                }
            }

            int bottomY = boardHeight - columnHeight;
            columnRectangles[12] = new Rectangle(0, bottomY, pieceSize, columnHeight);

            for (int columnIndex = 13; columnIndex < 24; columnIndex++)
            {
                if (columnIndex == 18)
                {
                    columnRectangles[columnIndex] = new Rectangle(
                        columnRectangles[columnIndex - 1].Right + pieceSize + padding * 2, bottomY, pieceSize, columnHeight);
                }
                else
                {
                    columnRectangles[columnIndex] = new Rectangle(
                        columnRectangles[columnIndex - 1].Right, bottomY, pieceSize, columnHeight);
                }
            }

            int barPositionX = columnRectangles[6].Right;
            int barWidth = pieceSize + padding * 2;
            int halfHeight = boardHeight / 2;
            int housePositionX = columnRectangles[0].Right;
            int houseWidth = pieceSize + padding * 3;

            outColumnTop = new Rectangle(barPositionX, 0, barWidth, halfHeight);
            outColumnBottom = new Rectangle(barPositionX, halfHeight, barWidth, halfHeight);
            houseTop = new Rectangle(housePositionX, 0, houseWidth, halfHeight);
            houseBottom = new Rectangle(housePositionX, halfHeight, houseWidth, halfHeight);

            int dicePositionY = (boardHeight - pieceSize) / 2;
            dice1Rectangle = new Rectangle(barPositionX - barWidth - padding * 4, dicePositionY, pieceSize, pieceSize);
            dice2Rectangle = new Rectangle(outColumnTop.Right + padding * 4, dicePositionY, pieceSize, pieceSize);
        }

    }
}
