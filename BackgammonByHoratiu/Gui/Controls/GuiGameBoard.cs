using System;

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
    public class GuiGameBoard : GuiControl
    {
        public const int ColBarP1 = 100;
        public const int ColBarP2 = 101;
        public const int ColHouseP1 = 200;
        public const int ColHouseP2 = 201;

        static readonly Color ColorBackground = Color.Gray;
        static readonly Color ColorOddColumn = new(255, 255, 127);
        static readonly Color ColorEvenColumn = new(0, 127, 0);
        static readonly Color ColorHouseColumn = new(63, 63, 63);
        static readonly Color ColorOutColumn = Color.Black;
        static readonly Color ColorPlayer1 = Color.White;
        static readonly Color ColorPlayer2 = new(139, 69, 19);

        readonly IGameManager game;

        TextureSprite animSpriteWhite;
        TextureSprite animSpriteBrown;
        bool isAnimating;
        int animFromCol;
        Action pendingMoveAction;

        public bool IsAnimating => isAnimating;

        Texture2D pixelTexture;
        Texture2D triangleDownTexture;
        Texture2D triangleUpTexture;
        Texture2D brownPieceTexture;
        Texture2D whitePieceTexture;
        Texture2D brownDieTexture;
        Texture2D whiteDieTexture;
        SpriteFont boardFont;

        // Precomputed hit-test rectangles mirroring the original MainWindow layout
        Rectangle[] columnRects;
        Rectangle outColumnTop, outColumnBottom;
        Rectangle houseTop, houseBottom;
        Rectangle dice1Rect, dice2Rect;

        public int SelectedColumn { get; set; } = -1;

        public bool IsOnDice(int x, int y) => dice1Rect.Contains(x, y) || dice2Rect.Contains(x, y);

        public bool IsOnHouse(int x, int y) => houseTop.Contains(x, y) || houseBottom.Contains(x, y);

        public GuiGameBoard(IGameManager game)
        {
            this.game = game;
        }

        protected override void DoLoadContent()
        {
            var gd = GraphicsManager.Instance.Graphics.GraphicsDevice;

            pixelTexture = new Texture2D(gd, 1, 1);
            pixelTexture.SetData(new[] { Color.White });

            triangleDownTexture = CreateTriangleTexture(gd, GameDefines.PieceSize, GameDefines.ColumnHeight, pointsDown: true);
            triangleUpTexture = CreateTriangleTexture(gd, GameDefines.PieceSize, GameDefines.ColumnHeight, pointsDown: false);
            brownPieceTexture = NuciContentManager.Instance.LoadTexture2D("Table/BrownPiece");
            whitePieceTexture = NuciContentManager.Instance.LoadTexture2D("Table/WhitePiece");
            brownDieTexture = NuciContentManager.Instance.LoadTexture2D("Table/BrownDie");
            whiteDieTexture = NuciContentManager.Instance.LoadTexture2D("Table/WhiteDie");

            boardFont = NuciContentManager.Instance.LoadSpriteFont("Fonts/InfoBarFont");

            BuildLayoutRectangles();

            animSpriteWhite = new TextureSprite
            {
                ContentFile = "Table/WhitePiece",
                MovementEffect = new MovementEffect { Speed = 8f },
                IsActive = true
            };
            animSpriteBrown = new TextureSprite
            {
                ContentFile = "Table/BrownPiece",
                MovementEffect = new MovementEffect { Speed = 8f },
                IsActive = true
            };
            animSpriteWhite.LoadContent();
            animSpriteBrown.LoadContent();
            animSpriteWhite.MovementEffect.Deactivated += OnAnimSpriteDeactivated;
            animSpriteBrown.MovementEffect.Deactivated += OnAnimSpriteDeactivated;
        }

        protected override void DoUnloadContent()
        {
            animSpriteWhite.MovementEffect.Deactivated -= OnAnimSpriteDeactivated;
            animSpriteBrown.MovementEffect.Deactivated -= OnAnimSpriteDeactivated;
            animSpriteWhite.UnloadContent();
            animSpriteBrown.UnloadContent();

            pixelTexture?.Dispose();
            triangleDownTexture?.Dispose();
            triangleUpTexture?.Dispose();
        }

        protected override void DoUpdate(GameTime gameTime)
        {
            if (isAnimating)
            {
                if (animSpriteWhite.MovementEffect.IsActive)
                    animSpriteWhite.Update(gameTime);
                else if (animSpriteBrown.MovementEffect.IsActive)
                    animSpriteBrown.Update(gameTime);
            }
        }

        protected override void DoDraw(SpriteBatch spriteBatch)
        {
            int w = GameDefines.WindowWidth;
            int h = GameDefines.WindowHeight;

            // Background
            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, w, h), ColorBackground);

            // Column triangles
            DrawColumns(spriteBatch);

            // Bar and house backgrounds
            spriteBatch.Draw(pixelTexture, outColumnTop, ColorOutColumn);
            spriteBatch.Draw(pixelTexture, outColumnBottom, ColorOutColumn);
            spriteBatch.Draw(pixelTexture, houseTop, ColorHouseColumn);
            spriteBatch.Draw(pixelTexture, houseBottom, ColorHouseColumn);

            // Pieces on board columns
            DrawPieces(spriteBatch);

            DrawCompletedPieces(spriteBatch);

            // Animated piece in flight
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

            // Dice
            DrawDice(spriteBatch);

            // Highlight selected column
            if (SelectedColumn >= 0)
            {
                Rectangle sel = columnRects[SelectedColumn];
                DrawBorder(spriteBatch, sel, Color.Yellow, 3);
            }
        }

        // ------------------------------------------------------------------ //
        //  Drawing helpers                                                     //
        // ------------------------------------------------------------------ //

        void DrawColumns(SpriteBatch spriteBatch)
        {
            for (int i = 0; i < 24; i++)
            {
                Rectangle col = columnRects[i];

                bool isTopHalf = i < 12;
                bool isOdd = i % 2 != 0;

                Color fillColor;
                if (isTopHalf)
                    fillColor = isOdd ? ColorOddColumn : ColorEvenColumn;
                else
                    fillColor = isOdd ? ColorEvenColumn : ColorOddColumn;

                Texture2D tri = isTopHalf ? triangleDownTexture : triangleUpTexture;
                spriteBatch.Draw(tri, col, fillColor);
            }
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
                    count -= 1;
                if (count == 0)
                    continue;

                Color pieceColor = values[i] > 0 ? ColorPlayer1 : ColorPlayer2;
                int visible = Math.Min(count, piecesPerCol);

                for (int z = 0; z < visible; z++)
                {
                    Rectangle dest;
                    if (i < 12)
                        dest = new Rectangle(columnRects[i].Left, columnRects[i].Top + z * pieceSize, pieceSize, pieceSize);
                    else
                        dest = new Rectangle(columnRects[i].Left, columnRects[i].Bottom - (z + 1) * pieceSize, pieceSize, pieceSize);

                    DrawCircle(spriteBatch, dest, pieceColor);
                }

                if (count > piecesPerCol)
                {
                    Rectangle labelRect;
                    if (i < 12)
                        labelRect = new Rectangle(columnRects[i].Left, columnRects[i].Top, pieceSize, pieceSize);
                    else
                        labelRect = new Rectangle(columnRects[i].Left, columnRects[i].Bottom - pieceSize, pieceSize, pieceSize);

                    DrawCenteredText(spriteBatch, $"+{count - piecesPerCol}", labelRect, Color.Black);
                }
            }

            int piecesP1 = game.Player1.OutedPieces;
            int piecesP2 = game.Player2.OutedPieces;
            if (suppressFromCol == ColBarP1) piecesP1 = Math.Max(0, piecesP1 - 1);
            if (suppressFromCol == ColBarP2) piecesP2 = Math.Max(0, piecesP2 - 1);

            for (int z = 0; z < Math.Min(piecesP2, piecesPerCol); z++)
            {
                int cx = outColumnTop.Left + (outColumnTop.Width - pieceSize) / 2;
                Rectangle dest = new(cx, outColumnTop.Top + z * pieceSize, pieceSize, pieceSize);
                DrawCircle(spriteBatch, dest, ColorPlayer2);
            }
            if (piecesP2 > piecesPerCol)
            {
                int cx = outColumnTop.Left + (outColumnTop.Width - pieceSize) / 2;
                DrawCenteredText(spriteBatch, $"+{piecesP2 - piecesPerCol}",
                    new Rectangle(cx, outColumnTop.Top, pieceSize, pieceSize), Color.White);
            }

            for (int z = 0; z < Math.Min(piecesP1, piecesPerCol); z++)
            {
                int cx = outColumnBottom.Left + (outColumnBottom.Width - pieceSize) / 2;
                Rectangle dest = new(cx, outColumnBottom.Bottom - pieceSize - z * pieceSize, pieceSize, pieceSize);
                DrawCircle(spriteBatch, dest, ColorPlayer1);
            }
            if (piecesP1 > piecesPerCol)
            {
                int cx = outColumnBottom.Left + (outColumnBottom.Width - pieceSize) / 2;
                DrawCenteredText(spriteBatch, $"+{piecesP1 - piecesPerCol}",
                    new Rectangle(cx, outColumnBottom.Bottom - pieceSize, pieceSize, pieceSize), Color.White);
            }
        }

        void DrawDice(SpriteBatch spriteBatch)
        {
            Texture2D dieTex = game.ActivePlayer == 1 ? whiteDieTexture : brownDieTexture;

            spriteBatch.Draw(dieTex, dice1Rect, Color.White);
            spriteBatch.Draw(dieTex, dice2Rect, Color.White);

            Color dieTextColor = game.ActivePlayer == 1 ? Color.Black : Color.White;

            DrawCenteredText(spriteBatch, game.Dice1.ToString(), dice1Rect, dieTextColor);
            DrawCenteredText(spriteBatch, game.Dice2.ToString(), dice2Rect, dieTextColor);
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
                Rectangle dest = new Rectangle(cx, houseTop.Top + z * pieceSize, pieceSize, pieceSize);
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
                Rectangle dest = new Rectangle(cx, houseBottom.Bottom - pieceSize - z * pieceSize, pieceSize, pieceSize);
                DrawCircle(spriteBatch, dest, ColorPlayer1);
            }

            if (completedP1 > piecesPerCol)
            {
                int cx = houseBottom.Left + (houseBottom.Width - pieceSize) / 2;
                DrawCenteredText(spriteBatch, $"+{completedP1 - piecesPerCol}",
                    new Rectangle(cx, houseBottom.Bottom - pieceSize, pieceSize, pieceSize), Color.White);
            }
        }

        void DrawCircle(SpriteBatch spriteBatch, Rectangle dest, Color fill)
        {
            Texture2D tex = fill == ColorPlayer2 ? brownPieceTexture : whitePieceTexture;
            spriteBatch.Draw(tex, dest, Color.White);
        }

        void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness)
        {
            // Top
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
            // Bottom
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
            // Left
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
            // Right
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

        // ------------------------------------------------------------------ //
        //  Hit-test helpers (used by GameplayScreen)                          //
        // ------------------------------------------------------------------ //

        public int ColumnAt(int x, int y)
        {
            for (int i = 0; i < 24; i++)
                if (columnRects[i].Contains(x, y))
                    return i;
            return -1;
        }

        public bool IsInOutColumnTop(int x, int y) => outColumnTop.Contains(x, y);
        public bool IsInOutColumnBottom(int x, int y) => outColumnBottom.Contains(x, y);

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
            pendingMoveAction = onComplete;

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
        }

        Point2D GetAnimSourcePixel(int fromCol, int ps, int piecesPerCol)
        {
            if (fromCol >= 0 && fromCol < 24)
            {
                int count = Math.Min(Math.Abs(game.TableValues[fromCol]), piecesPerCol);
                if (fromCol < 12)
                    return new Point2D(columnRects[fromCol].Left, columnRects[fromCol].Top + (count - 1) * ps);
                else
                    return new Point2D(columnRects[fromCol].Left, columnRects[fromCol].Bottom - count * ps);
            }

            if (fromCol == ColBarP1)
            {
                int cx = outColumnBottom.Left + (outColumnBottom.Width - ps) / 2;
                int count = Math.Min(game.Player1.OutedPieces, piecesPerCol);
                return new Point2D(cx, outColumnBottom.Bottom - count * ps);
            }

            // ColBarP2
            int cxBrown = outColumnTop.Left + (outColumnTop.Width - ps) / 2;
            int countBrown = Math.Min(game.Player2.OutedPieces, piecesPerCol);
            return new Point2D(cxBrown, outColumnTop.Top + (countBrown - 1) * ps);
        }

        Point2D GetAnimDestPixel(int toCol, int activePlayer, int ps, int piecesPerCol)
        {
            if (toCol >= 0 && toCol < 24)
            {
                int sign = activePlayer == 1 ? 1 : -1;
                int existing = game.TableValues[toCol] * sign > 0
                    ? Math.Min(Math.Abs(game.TableValues[toCol]), piecesPerCol - 1)
                    : 0;
                if (toCol < 12)
                    return new Point2D(columnRects[toCol].Left, columnRects[toCol].Top + existing * ps);
                else
                    return new Point2D(columnRects[toCol].Left, columnRects[toCol].Bottom - (existing + 1) * ps);
            }

            if (toCol == ColHouseP1)
            {
                int cx = houseBottom.Left + (houseBottom.Width - ps) / 2;
                int count = Math.Min(game.Player1.CompletedPieces, piecesPerCol);
                return new Point2D(cx, houseBottom.Bottom - (count + 1) * ps);
            }

            // ColHouseP2
            int cxH = houseTop.Left + (houseTop.Width - ps) / 2;
            int countH = Math.Min(game.Player2.CompletedPieces, piecesPerCol);
            return new Point2D(cxH, houseTop.Top + countH * ps);
        }

        void OnAnimSpriteDeactivated(object sender, EventArgs e)
        {
            if (!isAnimating)
                return;

            isAnimating = false;
            pendingMoveAction?.Invoke();
            pendingMoveAction = null;
        }

        void BuildLayoutRectangles()
        {
            int ps = GameDefines.PieceSize;
            int pad = GameDefines.Padding;
            int colH = GameDefines.ColumnHeight;
            int bh = GameDefines.BoardHeight;

            columnRects = new Rectangle[24];

            // Top-half columns 0-11 (triangles point DOWN from top)
            // Column 11 starts at x=0; 10..6 extend right; gap before 5; 5..0 continue right
            columnRects[11] = new Rectangle(0, 0, ps, colH);
            for (int i = 10; i >= 0; i--)
            {
                if (i == 5)
                    columnRects[i] = new Rectangle(
                        columnRects[i + 1].Right + ps + pad * 2, 0, ps, colH);
                else
                    columnRects[i] = new Rectangle(
                        columnRects[i + 1].Right, 0, ps, colH);
            }

            // Bottom-half columns 12-23 (triangles point UP from bottom)
            int bottomY = bh - colH;
            columnRects[12] = new Rectangle(0, bottomY, ps, colH);
            for (int i = 13; i < 24; i++)
            {
                if (i == 18)
                    columnRects[i] = new Rectangle(
                        columnRects[i - 1].Right + ps + pad * 2, bottomY, ps, colH);
                else
                    columnRects[i] = new Rectangle(
                        columnRects[i - 1].Right, bottomY, ps, colH);
            }

            int barX = columnRects[6].Right;           // 288
            int barWidth = ps + pad * 2;                   // 64
            int halfH = bh / 2;                         // 312
            int houseX = columnRects[0].Right;           // 640
            int houseWidth = ps + pad * 3;                  // 72

            outColumnTop = new Rectangle(barX, 0, barWidth, halfH);
            outColumnBottom = new Rectangle(barX, halfH, barWidth, halfH);
            houseTop = new Rectangle(houseX, 0, houseWidth, halfH);
            houseBottom = new Rectangle(houseX, halfH, houseWidth, halfH);

            int diceY = (bh - ps) / 2;
            dice1Rect = new Rectangle(barX - barWidth - pad * 4, diceY, ps, ps);
            dice2Rect = new Rectangle(outColumnTop.Right + pad * 4, diceY, ps, ps);
        }

        // ------------------------------------------------------------------ //
        //  Texture factories                                                  //
        // ------------------------------------------------------------------ //

        static Texture2D CreateTriangleTexture(GraphicsDevice gd, int width, int height, bool pointsDown)
        {
            Texture2D tex = new(gd, width, height);
            Color[] data = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                // For pointsDown: wide at top (y=0), apex at bottom (y=height)
                // For pointsUp:   apex at top (y=0), wide at bottom (y=height)
                float t = pointsDown
                    ? (float)y / height          // 0..1 as we go down
                    : (float)(height - y) / height; // 0..1 as we go up

                float leftX = (width / 2f) * t;
                float rightX = width - leftX;

                for (int x = 0; x < width; x++)
                    data[y * width + x] = (x >= leftX && x < rightX) ? Color.White : Color.Transparent;
            }

            tex.SetData(data);
            return tex;
        }

    }
}
