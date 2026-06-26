using System;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using NuciXNA.DataAccess.Content;
using NuciXNA.Graphics;
using NuciXNA.Gui.Controls;
using NuciXNA.Primitives;

using BackgammonByHoratiu.GameLogic.GameManagers;
using BackgammonByHoratiu.Settings;

namespace BackgammonByHoratiu.Gui.Controls
{
    public class GuiGameBoard : GuiControl
    {
        static readonly Color ColorBackground   = Color.Gray;
        static readonly Color ColorOddColumn    = new Color(255, 255, 127);
        static readonly Color ColorEvenColumn   = new Color(0, 127, 0);
        static readonly Color ColorHouseColumn  = new Color(63, 63, 63);
        static readonly Color ColorOutColumn    = Color.Black;
        static readonly Color ColorPlayer1      = Color.White;
        static readonly Color ColorPlayer2      = new Color(139, 69, 19);   // Brown
        static readonly Color ColorPieceBorder  = Color.Black;
        static readonly Color ColorColumnBorder = Color.Black;
        static readonly Color ColorDiceBorder   = Color.Black;

        readonly IGameManager game;

        Texture2D pixelTexture;
        Texture2D triangleDownTexture;
        Texture2D triangleUpTexture;
        Texture2D circleTexture;
        SpriteFont boardFont;

        // Precomputed hit-test rectangles mirroring the original MainWindow layout
        Rectangle[] columnRects;
        Rectangle outColumnTop, outColumnBottom;
        Rectangle houseTop, houseBottom;
        Rectangle dice1Rect, dice2Rect;

        public int SelectedColumn { get; set; } = -1;

        public bool IsOnDice(int x, int y) => dice1Rect.Contains(x, y) || dice2Rect.Contains(x, y);

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
            triangleUpTexture   = CreateTriangleTexture(gd, GameDefines.PieceSize, GameDefines.ColumnHeight, pointsDown: false);
            circleTexture       = CreateCircleTexture(gd, GameDefines.PieceSize);

            boardFont = NuciContentManager.Instance.LoadSpriteFont("Fonts/InfoBarFont");

            BuildLayoutRectangles();
        }

        protected override void DoUnloadContent()
        {
            pixelTexture?.Dispose();
            triangleDownTexture?.Dispose();
            triangleUpTexture?.Dispose();
            circleTexture?.Dispose();
        }

        protected override void DoUpdate(GameTime gameTime) { }

        protected override void DoDraw(SpriteBatch spriteBatch)
        {
            int w = GameDefines.WindowWidth;
            int h = GameDefines.WindowHeight;

            // Background
            spriteBatch.Draw(pixelTexture, new Rectangle(0, 0, w, h), ColorBackground);

            // Column triangles
            DrawColumns(spriteBatch);

            // Bar and house backgrounds
            spriteBatch.Draw(pixelTexture, outColumnTop,    ColorOutColumn);
            spriteBatch.Draw(pixelTexture, outColumnBottom, ColorOutColumn);
            spriteBatch.Draw(pixelTexture, houseTop,        ColorHouseColumn);
            spriteBatch.Draw(pixelTexture, houseBottom,     ColorHouseColumn);

            // Pieces on board columns
            DrawPieces(spriteBatch);

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
                bool isOdd     = i % 2 != 0;

                Color fillColor;
                if (isTopHalf)
                    fillColor = isOdd ? ColorOddColumn : ColorEvenColumn;
                else
                    fillColor = isOdd ? ColorEvenColumn : ColorOddColumn;

                Texture2D tri = isTopHalf ? triangleDownTexture : triangleUpTexture;
                spriteBatch.Draw(tri, col, fillColor);

                DrawBorder(spriteBatch, col, ColorColumnBorder, 1);
            }
        }

        void DrawPieces(SpriteBatch spriteBatch)
        {
            int pieceSize    = GameDefines.PieceSize;
            int piecesPerCol = GameDefines.ColumnHeight / pieceSize;
            int[] values     = game.TableValues;

            for (int i = 0; i < 24; i++)
            {
                int count = Math.Abs(values[i]);
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

            // Outed pieces in the bar
            int piecesP1 = game.Player1.OutedPieces;
            int piecesP2 = game.Player2.OutedPieces;

            for (int z = 0; z < Math.Min(piecesP1, piecesPerCol); z++)
            {
                int cx = outColumnTop.Left + (outColumnTop.Width - pieceSize) / 2;
                Rectangle dest = new Rectangle(cx, outColumnTop.Top + z * pieceSize, pieceSize, pieceSize);
                DrawCircle(spriteBatch, dest, ColorPlayer1);
            }
            if (piecesP1 > piecesPerCol)
            {
                int cx = outColumnTop.Left + (outColumnTop.Width - pieceSize) / 2;
                DrawCenteredText(spriteBatch, $"+{piecesP1 - piecesPerCol}",
                    new Rectangle(cx, outColumnBottom.Top, pieceSize, pieceSize), Color.White);
            }

            for (int z = 0; z < Math.Min(piecesP2, piecesPerCol); z++)
            {
                int cx = outColumnBottom.Left + (outColumnBottom.Width - pieceSize) / 2;
                Rectangle dest = new Rectangle(cx, outColumnBottom.Bottom - pieceSize - z * pieceSize, pieceSize, pieceSize);
                DrawCircle(spriteBatch, dest, ColorPlayer2);
            }
            if (piecesP2 > piecesPerCol)
            {
                int cx = outColumnBottom.Left + (outColumnBottom.Width - pieceSize) / 2;
                DrawCenteredText(spriteBatch, $"+{piecesP2 - piecesPerCol}",
                    new Rectangle(cx, outColumnBottom.Bottom - pieceSize, pieceSize, pieceSize), Color.White);
            }
        }

        void DrawDice(SpriteBatch spriteBatch)
        {
            Color diceColor = game.ActivePlayer == 1 ? ColorPlayer1 : ColorPlayer2;

            spriteBatch.Draw(pixelTexture, dice1Rect, diceColor);
            spriteBatch.Draw(pixelTexture, dice2Rect, diceColor);

            DrawBorder(spriteBatch, dice1Rect, ColorDiceBorder, 2);
            DrawBorder(spriteBatch, dice2Rect, ColorDiceBorder, 2);

            DrawCenteredText(spriteBatch, game.Dice1.ToString(), dice1Rect, Color.Black);
            DrawCenteredText(spriteBatch, game.Dice2.ToString(), dice2Rect, Color.Black);
        }

        void DrawCircle(SpriteBatch spriteBatch, Rectangle dest, Color fill)
        {
            // Draw shadow border slightly larger
            Rectangle border = new Rectangle(dest.X - 1, dest.Y - 1, dest.Width + 2, dest.Height + 2);
            spriteBatch.Draw(circleTexture, border, ColorPieceBorder);
            spriteBatch.Draw(circleTexture, dest, fill);
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
            Vector2 pos  = new Vector2(
                rect.X + (rect.Width  - size.X) / 2f,
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

        // ------------------------------------------------------------------ //
        //  Layout builder — mirrors original MainWindow constructor logic     //
        // ------------------------------------------------------------------ //

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

            int barX      = columnRects[6].Right;           // 288
            int barWidth  = ps + pad * 2;                   // 64
            int halfH     = bh / 2;                         // 312
            int houseX    = columnRects[0].Right;           // 640
            int houseWidth = ps + pad * 3;                  // 72

            outColumnTop    = new Rectangle(barX, 0, barWidth, halfH);
            outColumnBottom = new Rectangle(barX, halfH, barWidth, halfH);
            houseTop        = new Rectangle(houseX, 0, houseWidth, halfH);
            houseBottom     = new Rectangle(houseX, halfH, houseWidth, halfH);

            int diceY   = (bh - ps) / 2;
            dice1Rect   = new Rectangle(barX - barWidth - pad * 4, diceY, ps, ps);
            dice2Rect   = new Rectangle(outColumnTop.Right + pad * 4, diceY, ps, ps);
        }

        // ------------------------------------------------------------------ //
        //  Texture factories                                                  //
        // ------------------------------------------------------------------ //

        static Texture2D CreateTriangleTexture(GraphicsDevice gd, int width, int height, bool pointsDown)
        {
            Texture2D tex  = new Texture2D(gd, width, height);
            Color[]   data = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                // For pointsDown: wide at top (y=0), apex at bottom (y=height)
                // For pointsUp:   apex at top (y=0), wide at bottom (y=height)
                float t = pointsDown
                    ? (float)y / height          // 0..1 as we go down
                    : (float)(height - y) / height; // 0..1 as we go up

                float leftX  = (width / 2f) * t;
                float rightX = width - leftX;

                for (int x = 0; x < width; x++)
                    data[y * width + x] = (x >= leftX && x < rightX) ? Color.White : Color.Transparent;
            }

            tex.SetData(data);
            return tex;
        }

        static Texture2D CreateCircleTexture(GraphicsDevice gd, int diameter)
        {
            Texture2D tex    = new Texture2D(gd, diameter, diameter);
            Color[]   data   = new Color[diameter * diameter];
            float     radius = diameter / 2f;
            Vector2   center = new Vector2(radius, radius);

            for (int y = 0; y < diameter; y++)
                for (int x = 0; x < diameter; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    data[y * diameter + x] = dist <= radius ? Color.White : Color.Transparent;
                }

            tex.SetData(data);
            return tex;
        }
    }
}
