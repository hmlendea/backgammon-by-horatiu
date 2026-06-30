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
        static readonly Color ColorBackground = Color.Gray;
        static readonly Color ColorHouseColumn = new(63, 63, 63);
        static readonly Color ColorOutColumn = Color.Black;
        static readonly Color ColorPlayer1 = Color.White;
        static readonly Color ColorPlayer2 = new(139, 69, 19);
        TextureSprite animSpriteWhite;
        TextureSprite animSpriteBrown;
        bool isAnimating;
        int animFromCol;
        int animDestCol = -1;
        int pendingOutingCol = -1;
        int pendingOutingPlayer = -1;
        Action pendingMoveAction;

        public bool IsAnimating => isAnimating;

        Texture2D pixelTexture;
        GuiImage[] columnImages;
        GuiImage targetColumnImage;
        GuiImage[] pieces;
        GuiImage die1;
        GuiImage die2;
        GameTime lastGameTime = new();
        SpriteFont boardFont;

        Rectangle[] columnRects;
        Rectangle outColumnTop, outColumnBottom;
        Rectangle houseTop, houseBottom;
        Rectangle dice1Rect, dice2Rect;

        readonly float AnimationSpeed = 12f;

        public int SelectedColumn { get; set; } = -1;

        public IReadOnlyList<int> ValidDestinations { get; set; } = Array.Empty<int>();

        public bool IsOnDice(int x, int y) => dice1Rect.Contains(x, y) || dice2Rect.Contains(x, y);

        public bool IsOnHouse(int x, int y) => houseTop.Contains(x, y) || houseBottom.Contains(x, y);

        protected override void DoLoadContent()
        {
            var gd = GraphicsManager.Instance.Graphics.GraphicsDevice;

            pixelTexture = new Texture2D(gd, 1, 1);
            pixelTexture.SetData([Color.White]);

            boardFont = NuciContentManager.Instance.LoadSpriteFont("Fonts/InfoBarFont");

            BuildLayoutRectangles();

            const int colFrameWidth = 105;
            const int colFrameHeight = 512;

            columnImages = new GuiImage[24];
            for (int i = 0; i < 24; i++)
            {
                bool isTopHalf = i < 12;
                bool isYellow = isTopHalf ? i % 2 != 0 : i % 2 == 0;
                int srcX = isYellow ? 0 : colFrameWidth;

                columnImages[i] = new GuiImage
                {
                    ContentFile = "Table/columns",
                    Location = new Point2D(columnRects[i].X, columnRects[i].Y),
                    Size = new Size2D(columnRects[i].Width, columnRects[i].Height),
                    SourceRectangle = new Rectangle2D(srcX, 0, colFrameWidth, colFrameHeight)
                };

                if (isTopHalf)
                {
                    columnImages[i].Rotation = MathHelper.Pi;
                }

                columnImages[i].Hide();
            }

            targetColumnImage = new GuiImage
            {
                ContentFile = "Table/columns",
                SourceRectangle = new Rectangle2D(colFrameWidth * 2, 0, colFrameWidth, colFrameHeight)
            };
            targetColumnImage.Hide();

            die1 = new()
            {
                ContentFile = "Table/dice",
                Location = new Point2D(dice1Rect.X, dice1Rect.Y),
                Size = new Size2D(dice1Rect.Width, dice1Rect.Height)
            };

            die2 = new()
            {
                ContentFile = "Table/dice",
                Location = new Point2D(dice2Rect.X, dice2Rect.Y),
                Size = new Size2D(dice2Rect.Width, dice2Rect.Height)
            };

            animSpriteWhite = new()
            {
                ContentFile = "Table/pieces",
                MovementEffect = new MovementEffect { Speed = AnimationSpeed },
                IsActive = true
            };
            animSpriteBrown = new()
            {
                ContentFile = "Table/pieces",
                MovementEffect = new MovementEffect { Speed = AnimationSpeed },
                IsActive = true
            };
            animSpriteWhite.LoadContent();
            animSpriteBrown.LoadContent();
            animSpriteWhite.MovementEffect.Deactivated += OnAnimSpriteDeactivated;
            animSpriteBrown.MovementEffect.Deactivated += OnAnimSpriteDeactivated;

            int pieceFrameSize = animSpriteWhite.TextureSize.Height;
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
            pieces[0].Hide();
            pieces[1].Hide();
            pieces[2].Hide();

            RegisterChildren(die1, die2, pieces[0], pieces[1], pieces[2], targetColumnImage);
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
            int rowY = game.ActivePlayer == 1 ? 0 : dieFrameSize;

            die1.SourceRectangle = new Rectangle2D((game.Dice1 - 1) * dieFrameSize, rowY, dieFrameSize, dieFrameSize);
            die2.SourceRectangle = new Rectangle2D((game.Dice2 - 1) * dieFrameSize, rowY, dieFrameSize, dieFrameSize);
        }

        protected override void DoDraw(SpriteBatch spriteBatch)
        {
            int w = GameDefines.WindowWidth;
            int h = GameDefines.WindowHeight;

            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, w, h), ColorBackground);

            DrawColumns(spriteBatch);

            foreach (int dest in ValidDestinations)
            {
                if (dest >= 0 && dest < 24)
                {
                    DrawTargetColumn(spriteBatch, dest);
                }
                else if (dest == GameDefines.ColHouseP1)
                {
                    DrawBorder(spriteBatch, houseBottom, Color.Cyan, 3);
                }
                else if (dest == GameDefines.ColHouseP2)
                {
                    DrawBorder(spriteBatch, houseTop, Color.Cyan, 3);
                }
            }

            spriteBatch.Draw(pixelTexture, outColumnTop, ColorOutColumn);
            spriteBatch.Draw(pixelTexture, outColumnBottom, ColorOutColumn);
            spriteBatch.Draw(pixelTexture, houseTop, ColorHouseColumn);
            spriteBatch.Draw(pixelTexture, houseBottom, ColorHouseColumn);

            DrawPieces(spriteBatch);

            DrawCompletedPieces(spriteBatch);

            if (isAnimating)
            {
                TextureSprite animSprite = null;
                Color animColor = Color.Black;

                if (animSpriteWhite.MovementEffect.IsActive)
                {
                    animSprite = animSpriteWhite;
                    animColor = ColorPlayer1;
                }
                else if (animSpriteBrown.MovementEffect.IsActive)
                {
                    animSprite = animSpriteBrown;
                    animColor = ColorPlayer2;
                }

                if (animSprite is not null)
                {
                    Point2D pos = animSprite.Location + animSprite.MovementEffect.LocationOffset;
                    int ps = GameDefines.PieceSize;
                    DrawCircle(spriteBatch, new Rectangle(pos.X, pos.Y, ps, ps), animColor);
                }
            }
        }

        void DrawColumns(SpriteBatch spriteBatch)
        {
            foreach (GuiImage img in columnImages)
            {
                img.Draw(spriteBatch);
            }
        }

        void DrawTargetColumn(SpriteBatch spriteBatch, int colIndex)
        {
            Rectangle rect = columnRects[colIndex];
            bool isTopHalf = colIndex < 12;

            targetColumnImage.Location = new Point2D(rect.X, rect.Y);
            targetColumnImage.Size = new Size2D(rect.Width, rect.Height);
            targetColumnImage.Rotation = isTopHalf ? MathHelper.Pi : 0f;
            targetColumnImage.Update(lastGameTime);
            targetColumnImage.Draw(spriteBatch);
        }

        void DrawPieces(SpriteBatch spriteBatch)
        {
            int pieceSize = GameDefines.PieceSize;
            int piecesPerCol = GameDefines.ColumnHeight / pieceSize;
            int[] values = game.TableValues;
            int suppressFromCol = isAnimating ? animFromCol : int.MinValue;

            for (int i = 0; i < 24; i++)
            {
                int count = Math.Abs(values[i]);
                if (suppressFromCol == i && count > 0)
                {
                    count -= 1;
                }

                if (count == 0)
                {
                    continue;
                }

                Color pieceColor = values[i] > 0 ? ColorPlayer1 : ColorPlayer2;
                int visible = Math.Min(count, piecesPerCol);

                for (int z = 0; z < visible; z++)
                {
                    Rectangle dest;
                    if (i < 12)
                    {
                        dest = new Rectangle(columnRects[i].Left, columnRects[i].Top + z * pieceSize, pieceSize, pieceSize);
                    }
                    else
                    {
                        dest = new Rectangle(columnRects[i].Left, columnRects[i].Bottom - (z + 1) * pieceSize, pieceSize, pieceSize);
                    }

                    bool isTopPiece = z == visible - 1 && i == SelectedColumn;
                    DrawCircle(spriteBatch, dest, pieceColor, isTopPiece);
                }

                if (count > piecesPerCol)
                {
                    Rectangle labelRect;
                    if (i < 12)
                    {
                        labelRect = new Rectangle(columnRects[i].Left, columnRects[i].Top, pieceSize, pieceSize);
                    }
                    else
                    {
                        labelRect = new Rectangle(columnRects[i].Left, columnRects[i].Bottom - pieceSize, pieceSize, pieceSize);
                    }

                    Color overflowColor = pieceColor == ColorPlayer2 ? Color.White : Color.Black;
                    DrawCenteredText(spriteBatch, $"+{count - piecesPerCol}", labelRect, overflowColor);
                }
            }

            int piecesP1 = game.Player1.OutedPieces;
            int piecesP2 = game.Player2.OutedPieces;
            if (suppressFromCol == GameDefines.ColBarP1)
            {
                piecesP1 = Math.Max(0, piecesP1 - 1);
            }

            if (suppressFromCol == GameDefines.ColBarP2)
            {
                piecesP2 = Math.Max(0, piecesP2 - 1);
            }

            for (int z = 0; z < Math.Min(piecesP1, piecesPerCol); z++)
            {
                int cx = outColumnTop.Left + (outColumnTop.Width - pieceSize) / 2;
                Rectangle dest = new(cx, outColumnTop.Top + z * pieceSize, pieceSize, pieceSize);
                bool isTopPiece = z == Math.Min(piecesP1, piecesPerCol) - 1 && SelectedColumn == GameDefines.ColBarP1;
                DrawCircle(spriteBatch, dest, ColorPlayer1, isTopPiece);
            }
            if (piecesP1 > piecesPerCol)
            {
                int cx = outColumnTop.Left + (outColumnTop.Width - pieceSize) / 2;
                DrawCenteredText(spriteBatch, $"+{piecesP1 - piecesPerCol}",
                    new Rectangle(cx, outColumnTop.Top, pieceSize, pieceSize), Color.Black);
            }

            for (int z = 0; z < Math.Min(piecesP2, piecesPerCol); z++)
            {
                int cx = outColumnBottom.Left + (outColumnBottom.Width - pieceSize) / 2;
                Rectangle dest = new(cx, outColumnBottom.Bottom - pieceSize - z * pieceSize, pieceSize, pieceSize);
                bool isTopPiece = z == Math.Min(piecesP2, piecesPerCol) - 1 && SelectedColumn == GameDefines.ColBarP2;
                DrawCircle(spriteBatch, dest, ColorPlayer2, isTopPiece);
            }
            if (piecesP2 > piecesPerCol)
            {
                int cx = outColumnBottom.Left + (outColumnBottom.Width - pieceSize) / 2;
                DrawCenteredText(spriteBatch, $"+{piecesP2 - piecesPerCol}",
                    new Rectangle(cx, outColumnBottom.Bottom - pieceSize, pieceSize, pieceSize), Color.White);
            }
        }

        void DrawCompletedPieces(SpriteBatch spriteBatch)
        {
            int pieceSize = GameDefines.PieceSize;
            int piecesPerCol = GameDefines.ColumnHeight / pieceSize;

            int completedP2 = game.Player2.CompletedPieces;
            int completedP1 = game.Player1.CompletedPieces;

            for (int z = 0; z < Math.Min(completedP2, piecesPerCol); z++)
            {
                int cx = houseTop.Left + (houseTop.Width - pieceSize) / 2;
                Rectangle dest = new(cx, houseTop.Top + z * pieceSize, pieceSize, pieceSize);
                DrawCircle(spriteBatch, dest, ColorPlayer2);
            }

            if (completedP2 > piecesPerCol)
            {
                int cx = houseTop.Left + (houseTop.Width - pieceSize) / 2;
                DrawCenteredText(spriteBatch, $"+{completedP2 - piecesPerCol}",
                    new Rectangle(cx, houseTop.Top, pieceSize, pieceSize), Color.White);
            }

            for (int z = 0; z < Math.Min(completedP1, piecesPerCol); z++)
            {
                int cx = houseBottom.Left + (houseBottom.Width - pieceSize) / 2;
                Rectangle dest = new(cx, houseBottom.Bottom - pieceSize - z * pieceSize, pieceSize, pieceSize);
                DrawCircle(spriteBatch, dest, ColorPlayer1);
            }

            if (completedP1 > piecesPerCol)
            {
                int cx = houseBottom.Left + (houseBottom.Width - pieceSize) / 2;
                DrawCenteredText(spriteBatch, $"+{completedP1 - piecesPerCol}",
                    new Rectangle(cx, houseBottom.Bottom - pieceSize, pieceSize, pieceSize), Color.Black);
            }
        }

        void DrawCircle(SpriteBatch spriteBatch, Rectangle dest, Color fill, bool isSelected = false)
        {
            int idx = isSelected ? 2 : (fill == ColorPlayer2 ? 1 : 0);
            pieces[idx].Location = new Point2D(dest.X, dest.Y);
            pieces[idx].Size = new Size2D(dest.Width, dest.Height);
            pieces[idx].Update(lastGameTime);
            pieces[idx].Draw(spriteBatch);
        }

        void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
        }

        void DrawCenteredText(SpriteBatch spriteBatch, string text, Rectangle rect, Color color)
        {
            Vector2 size = boardFont.MeasureString(text);
            Vector2 pos = new(
                rect.X + (rect.Width - size.X) / 2f,
                rect.Y + (rect.Height - size.Y) / 2f);

            spriteBatch.DrawString(boardFont, text, pos, color);
        }

        public int ColumnAt(int x, int y)
        {
            for (int i = 0; i < 24; i++)
            {
                if (columnRects[i].Contains(x, y))
                {
                    return i;
                }
            }

            return -1;
        }

        public bool IsInOutColumnTop(int x, int y) => outColumnTop.Contains(x, y);
        public bool IsInOutColumnBottom(int x, int y) => outColumnBottom.Contains(x, y);

        public bool IsHoveringOverWhitePiece(int x, int y)
        {
            int pieceSize = GameDefines.PieceSize;
            int piecesPerCol = GameDefines.ColumnHeight / pieceSize;
            int[] values = game.TableValues;

            for (int i = 0; i < 24; i++)
            {
                if (values[i] <= 0)
                {
                    continue;
                }

                int count = Math.Min(values[i], piecesPerCol);

                for (int z = 0; z < count; z++)
                {
                    Rectangle dest;
                    if (i < 12)
                    {
                        dest = new Rectangle(columnRects[i].Left, columnRects[i].Top + z * pieceSize, pieceSize, pieceSize);
                    }
                    else
                    {
                        dest = new Rectangle(columnRects[i].Left, columnRects[i].Bottom - (z + 1) * pieceSize, pieceSize, pieceSize);
                    }

                    if (dest.Contains(x, y))
                    {
                        return true;
                    }
                }
            }

            int piecesP1 = game.Player1.OutedPieces;
            int cx = outColumnTop.Left + (outColumnTop.Width - pieceSize) / 2;

            for (int z = 0; z < Math.Min(piecesP1, piecesPerCol); z++)
            {
                Rectangle dest = new(cx, outColumnTop.Top + z * pieceSize, pieceSize, pieceSize);

                if (dest.Contains(x, y))
                {
                    return true;
                }
            }

            return false;
        }

        public void BeginPieceMoveAnimation(int fromCol, int toCol, int activePlayer, Action onComplete)
        {
            if (isAnimating)
            {
                onComplete?.Invoke();
                return;
            }

            int ps = GameDefines.PieceSize;
            int piecesPerCol = GameDefines.ColumnHeight / ps;

            animFromCol = fromCol;
            animDestCol = toCol;
            pendingMoveAction = onComplete;
            SetPendingOuting(toCol, activePlayer);

            Point2D srcPixel = GetAnimSourcePixel(fromCol, ps, piecesPerCol);
            Point2D dstPixel = GetAnimDestPixel(toCol, activePlayer, ps, piecesPerCol);

            TextureSprite sprite = activePlayer == 2 ? animSpriteBrown : animSpriteWhite;
            sprite.Location = srcPixel;
            sprite.MovementEffect.TargetLocation = dstPixel;
            sprite.MovementEffect.Activate();

            isAnimating = true;
        }

        public void CancelAnimation()
        {
            isAnimating = false;
            pendingMoveAction = null;
            animDestCol = -1;
            pendingOutingCol = -1;
            pendingOutingPlayer = -1;
        }

        public void ContinuePieceMoveAnimation(int toCol, int activePlayer, Action onComplete)
        {
            int ps = GameDefines.PieceSize;
            int piecesPerCol = GameDefines.ColumnHeight / ps;

            pendingMoveAction = onComplete;
            animDestCol = toCol;
            SetPendingOuting(toCol, activePlayer);

            TextureSprite sprite = activePlayer == 2 ? animSpriteBrown : animSpriteWhite;

            sprite.Location = sprite.MovementEffect.TargetLocation;

            Point2D dstPixel = GetAnimDestPixel(toCol, activePlayer, ps, piecesPerCol);
            sprite.MovementEffect.TargetLocation = dstPixel;
            sprite.MovementEffect.Activate();

            isAnimating = true;
        }

        Point2D GetAnimSourcePixel(int fromCol, int ps, int piecesPerCol)
        {
            if (fromCol >= 0 && fromCol < 24)
            {
                int count = Math.Min(Math.Abs(game.TableValues[fromCol]), piecesPerCol);
                if (fromCol < 12)
                {
                    return new Point2D(columnRects[fromCol].Left, columnRects[fromCol].Top + (count - 1) * ps);
                }
                else
                {
                    return new Point2D(columnRects[fromCol].Left, columnRects[fromCol].Bottom - count * ps);
                }
            }

            if (fromCol == GameDefines.ColBarP1)
            {
                int cx = outColumnTop.Left + (outColumnTop.Width - ps) / 2;
                int count = Math.Min(game.Player1.OutedPieces, piecesPerCol);
                return new Point2D(cx, outColumnTop.Top + (count - 1) * ps);
            }

            int cxBrown = outColumnBottom.Left + (outColumnBottom.Width - ps) / 2;
            int countBrown = Math.Min(game.Player2.OutedPieces, piecesPerCol);
            return new Point2D(cxBrown, outColumnBottom.Bottom - countBrown * ps);
        }

        Point2D GetAnimDestPixel(int toCol, int activePlayer, int ps, int piecesPerCol)
        {
            if (toCol >= 0 && toCol < 24)
            {
                int sign = activePlayer == 1 ? 1 : -1;
                int existing = game.TableValues[toCol] * sign > 0
                    ? Math.Abs(game.TableValues[toCol])
                    : 0;
                int slot = existing >= piecesPerCol ? piecesPerCol - 1 : existing;

                if (toCol < 12)
                {
                    return new Point2D(columnRects[toCol].Left, columnRects[toCol].Top + slot * ps);
                }
                else
                {
                    return new Point2D(columnRects[toCol].Left, columnRects[toCol].Bottom - (slot + 1) * ps);
                }
            }

            if (toCol == GameDefines.ColHouseP1)
            {
                int cx = houseBottom.Left + (houseBottom.Width - ps) / 2;
                int existing = game.Player1.CompletedPieces;
                int slot = existing >= piecesPerCol ? 0 : existing;
                return new Point2D(cx, houseBottom.Bottom - (slot + 1) * ps);
            }

            int cxH = houseTop.Left + (houseTop.Width - ps) / 2;
            int existingH = game.Player2.CompletedPieces;
            int slotH = existingH >= piecesPerCol ? piecesPerCol - 1 : existingH;
            return new Point2D(cxH, houseTop.Top + slotH * ps);
        }

        void SetPendingOuting(int toCol, int activePlayer)
        {
            int sign = activePlayer == 1 ? 1 : -1;
            if (toCol >= 0 && toCol < 24 &&
                game.TableValues[toCol] * sign < 0 &&
                Math.Abs(game.TableValues[toCol]) == 1)
            {
                pendingOutingCol = toCol;
                pendingOutingPlayer = activePlayer == 1 ? 2 : 1;
            }
            else
            {
                pendingOutingCol = -1;
                pendingOutingPlayer = -1;
            }
        }

        void BeginOutingAnimation(int hitCol, int hitPlayer)
        {
            int ps = GameDefines.PieceSize;
            int piecesPerCol = GameDefines.ColumnHeight / ps;

            Point2D srcPixel = hitCol < 12
                ? new Point2D(columnRects[hitCol].Left, columnRects[hitCol].Top)
                : new Point2D(columnRects[hitCol].Left, columnRects[hitCol].Bottom - ps);

            Point2D dstPixel;
            if (hitPlayer == 1)
            {
                int cx = outColumnTop.Left + (outColumnTop.Width - ps) / 2;
                int count = Math.Min(game.Player1.OutedPieces, piecesPerCol);
                dstPixel = new Point2D(cx, outColumnTop.Top + (count - 1) * ps);
                animFromCol = GameDefines.ColBarP1;
            }
            else
            {
                int cx = outColumnBottom.Left + (outColumnBottom.Width - ps) / 2;
                int count = Math.Min(game.Player2.OutedPieces, piecesPerCol);
                dstPixel = new Point2D(cx, outColumnBottom.Bottom - count * ps);
                animFromCol = GameDefines.ColBarP2;
            }

            TextureSprite sprite = hitPlayer == 1 ? animSpriteWhite : animSpriteBrown;
            sprite.Location = srcPixel;
            sprite.MovementEffect.TargetLocation = dstPixel;
            sprite.MovementEffect.Activate();

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

            int outingCol = pendingOutingCol;
            int outingPlayer = pendingOutingPlayer;
            pendingOutingCol = -1;
            pendingOutingPlayer = -1;

            Action action = pendingMoveAction;
            pendingMoveAction = null;
            action?.Invoke();

            if (outingCol >= 0 && !isAnimating)
            {
                BeginOutingAnimation(outingCol, outingPlayer);
            }
        }

        void BuildLayoutRectangles()
        {
            int ps = GameDefines.PieceSize;
            int pad = GameDefines.Padding;
            int colH = GameDefines.ColumnHeight;
            int bh = GameDefines.BoardHeight;

            columnRects = new Rectangle[24];

            columnRects[11] = new Rectangle(0, 0, ps, colH);
            for (int i = 10; i >= 0; i--)
            {
                if (i == 5)
                {
                    columnRects[i] = new Rectangle(
                        columnRects[i + 1].Right + ps + pad * 2, 0, ps, colH);
                }
                else
                {
                    columnRects[i] = new Rectangle(
                        columnRects[i + 1].Right, 0, ps, colH);
                }
            }

            int bottomY = bh - colH;
            columnRects[12] = new Rectangle(0, bottomY, ps, colH);
            for (int i = 13; i < 24; i++)
            {
                if (i == 18)
                {
                    columnRects[i] = new Rectangle(
                        columnRects[i - 1].Right + ps + pad * 2, bottomY, ps, colH);
                }
                else
                {
                    columnRects[i] = new Rectangle(
                        columnRects[i - 1].Right, bottomY, ps, colH);
                }
            }

            int barX = columnRects[6].Right;
            int barWidth = ps + pad * 2;
            int halfH = bh / 2;
            int houseX = columnRects[0].Right;
            int houseWidth = ps + pad * 3;

            outColumnTop = new Rectangle(barX, 0, barWidth, halfH);
            outColumnBottom = new Rectangle(barX, halfH, barWidth, halfH);
            houseTop = new Rectangle(houseX, 0, houseWidth, halfH);
            houseBottom = new Rectangle(houseX, halfH, houseWidth, halfH);

            int diceY = (bh - ps) / 2;
            dice1Rect = new Rectangle(barX - barWidth - pad * 4, diceY, ps, ps);
            dice2Rect = new Rectangle(outColumnTop.Right + pad * 4, diceY, ps, ps);
        }

    }
}
