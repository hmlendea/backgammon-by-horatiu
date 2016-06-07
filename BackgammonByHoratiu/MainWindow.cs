using System;
using System.Drawing;
using System.Drawing.Drawing2D;

using WinFormsGTK;

using BackgammonByHoratiu.Entities;

public partial class MainWindow: Gtk.Window
{
    Table table;
    Point mousePosition;
    bool dragging;
    int dragBeginCol;

    Rectangle[] rTable = new Rectangle[24];
    Rectangle rHouseTop, rHouseBottom, rOutColumnTop, rOutColumnBottom, rDice1, rDice2;

    Pen penColumnBorder = new Pen(Brushes.Black, 2);
    Pen penPieceBorder = new Pen(Brushes.Black, 2);

    Brush brPlayer1, brPlayer2;
    Brush brBackground, brHouseColumn, brOutColumn, brOddColumn, brEvenColumn;


    const int pieceSize = 48;
    const int padding = 8;

    public MainWindow()
        : base(Gtk.WindowType.Toplevel)
    {
        Build();

        da.AddEvents((int)
            (Gdk.EventMask.ButtonPressMask
            | Gdk.EventMask.ButtonReleaseMask
            | Gdk.EventMask.KeyPressMask
            | Gdk.EventMask.PointerMotionMask));
        //da.AddEvents((int)Gdk.EventMask.ButtonPressMask);
        //da.AddEvents((int)Gdk.EventMask.ButtonReleaseMask);

        da.ExposeEvent += delegate
        {
            DrawTable(da.GdkWindow);
            da.Realize();
        };
        
        table = new Table();

        int width, height;

        height = pieceSize * 10 + pieceSize * 3;

        brBackground = new SolidBrush(table.BackgroundColor);
        brHouseColumn = new SolidBrush(table.HouseColumnColor);
        brOutColumn = new SolidBrush(table.OutColumnColor);
        brOddColumn = new SolidBrush(table.OddColumnColor);
        brEvenColumn = new SolidBrush(table.EvenColumnColor);
        brPlayer1 = new SolidBrush(table.Player1.Color);
        brPlayer2 = new SolidBrush(table.Player2.Color);

        rTable[11] = new Rectangle(0, 0, pieceSize, pieceSize * 5);
        rTable[12] = new Rectangle(0, height - rTable[11].Height, rTable[11].Width, rTable[11].Height);

        for (int i = 10; i >= 0; i--)
            if (i == 5)
                rTable[i] = new Rectangle(
                    rTable[i + 1].Right + rTable[i + 1].Width + padding * 2, rTable[i + 1].Y,
                    rTable[i + 1].Width, rTable[i + 1].Height);
            else
                rTable[i] = new Rectangle(
                    rTable[i + 1].Right, rTable[i + 1].Y,
                    rTable[i + 1].Width, rTable[i + 1].Height);

        for (int i = 13; i < 24; i++)
            if (i == 18)
                rTable[i] = new Rectangle(
                    rTable[i - 1].Right + rTable[i - 1].Width + padding * 2, rTable[i - 1].Top,
                    rTable[i - 1].Width, rTable[i - 1].Height);
            else
                rTable[i] = new Rectangle(
                    rTable[i - 1].Right, rTable[i - 1].Top,
                    rTable[i - 1].Width, rTable[i - 1].Height);

        rOutColumnTop = new Rectangle(rTable[6].Right, 0, pieceSize + padding * 2, height / 2);
        rOutColumnBottom = new Rectangle(rOutColumnTop.Left, rOutColumnTop.Bottom, rOutColumnTop.Width, rOutColumnTop.Height);
        rHouseTop = new Rectangle(rTable[0].Right, 0, pieceSize + padding * 3, height / 2);
        rHouseBottom = new Rectangle(rTable[23].Right, height - rHouseTop.Height, rHouseTop.Width, rHouseTop.Height);

        rDice1 = new Rectangle(
            rOutColumnTop.Left - rOutColumnTop.Width - padding * 4, rOutColumnTop.Top + ((rOutColumnTop.Height + rOutColumnBottom.Height) - pieceSize) / 2,
            pieceSize, pieceSize);
        rDice2 = new Rectangle(
            rOutColumnTop.Right + padding * 4, rDice1.Y,
            pieceSize, pieceSize);

        width = rOutColumnTop.Width + rHouseTop.Width;

        for (int i = 0; i < 12; i++)
            width += rTable[i].Width;

        da.WidthRequest = width;
        da.HeightRequest = height;
    }

    protected void OnDeleteEvent(object sender, Gtk.DeleteEventArgs a)
    {
        Gtk.Application.Quit();
        a.RetVal = true;
    }

    void DrawTable(Gdk.Drawable drw)
    {
        Bitmap bmp;
        Graphics g;
        int width, height;

        drw.GetSize(out width, out height);
        bmp = new Bitmap(width, height);
        g = Graphics.FromImage(bmp);

        g.FillRectangle(brBackground, new RectangleF(0, 0, width, height));

        DrawColumns(bmp);

        g.FillRectangle(brHouseColumn, rHouseTop);
        g.FillRectangle(brHouseColumn, rHouseBottom);
        g.FillRectangle(brOutColumn, rOutColumnTop);
        g.FillRectangle(brOutColumn, rOutColumnBottom);

        DrawPieces(bmp);
        DrawDice(bmp);

        g.Dispose();

        g = Gtk.DotNet.Graphics.FromDrawable(drw);
        g.DrawImage(bmp, 0, 0);

        g.Dispose();
    }

    void DrawColumns(Bitmap bmp)
    {
        Graphics g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.HighQuality;

        for (int i = 0; i < 24; i++)
        {
            PointF[] polygon;

            if (i < 12)
            {
                polygon = new []
                {
                    new PointF(rTable[i].Left, rTable[i].Top),
                    new PointF(rTable[i].Left + rTable[i].Width / 2, rTable[i].Bottom),

                    new PointF(rTable[i].X + rTable[i].Width / 2, rTable[i].Bottom),
                    new PointF(rTable[i].Right, rTable[i].Top)
                };

                if (i % 2 != 0)
                    g.FillPolygon(brOddColumn, polygon);
                else
                    g.FillPolygon(brEvenColumn, polygon);
            }
            else
            {
                polygon = new []
                {
                    new PointF(rTable[i].Left, rTable[i].Bottom),
                    new PointF(rTable[i].Left + rTable[i].Width / 2, rTable[i].Top),

                    new PointF(rTable[i].X + rTable[i].Width / 2, rTable[i].Top),
                    new PointF(rTable[i].Right, rTable[i].Bottom)
                };

                if (i % 2 != 0)
                    g.FillPolygon(brEvenColumn, polygon);
                else
                    g.FillPolygon(brOddColumn, polygon);
            }

            g.DrawPolygon(penColumnBorder, polygon);
        }

        g.Dispose();
    }

    void DrawPieces(Bitmap bmp)
    {
        Graphics g = Graphics.FromImage(bmp);
        Font f = new Font(FontFamily.GenericSansSerif, (int)(pieceSize * 0.35), FontStyle.Bold);
        StringFormat sf = new StringFormat();
        int piecesPerCol = rTable[0].Height / pieceSize;

        g.SmoothingMode = SmoothingMode.HighQuality;
        sf.Alignment = StringAlignment.Center;
        sf.LineAlignment = StringAlignment.Center;

        for (int i = 0; i < 24; i++)
        {
            int pieces = Math.Abs(table.TableValues[i]);

            for (int z = 0; z < Math.Min(pieces, piecesPerCol); z++)
            {
                RectangleF rPiece;

                if (i < 12)
                    rPiece = new RectangleF(rTable[i].Left, rTable[i].Top + z * pieceSize, pieceSize, pieceSize);
                else
                    rPiece = new RectangleF(rTable[i].Left, rTable[i].Bottom - (z + 1) * pieceSize, pieceSize, pieceSize);

                if (table.TableValues[i] > 0)
                    g.FillEllipse(brPlayer1, rPiece);
                else
                    g.FillEllipse(brPlayer2, rPiece);

                g.DrawEllipse(penPieceBorder, rPiece);

                if (pieces > piecesPerCol)
                {
                    if (i < 12)
                        rPiece = new RectangleF(rTable[i].Left, rTable[i].Top, pieceSize, pieceSize);
                    else
                        rPiece = new RectangleF(rTable[i].Left, rTable[i].Bottom - pieceSize, pieceSize, pieceSize);

                    g.DrawString("+" + (pieces - piecesPerCol), f, Brushes.Black, rPiece, sf);
                }
            }
        }

        for (int z = 0; z < Math.Min(table.Player1.OutedPieces, piecesPerCol); z++)
        {
            RectangleF rPiece = new RectangleF(
                                    rOutColumnTop.Left + (rOutColumnTop.Width - pieceSize) / 2, rOutColumnTop.Top + z * pieceSize,
                                    pieceSize, pieceSize);

            g.FillEllipse(brPlayer1, rPiece);
            g.DrawEllipse(penPieceBorder, rPiece);

            if (table.Player1.OutedPieces > piecesPerCol)
            {
                rPiece = new RectangleF(
                    rOutColumnTop.Left + (rOutColumnTop.Width - pieceSize) / 2, rOutColumnBottom.Top,
                    pieceSize, pieceSize);

                g.DrawString("+" + (table.Player1.OutedPieces - piecesPerCol), f, Brushes.Black, rPiece, sf);
            }
        }

        for (int z = 0; z < Math.Min(table.Player2.OutedPieces, piecesPerCol); z++)
        {
            RectangleF rPiece = new RectangleF(
                                    rOutColumnBottom.Left + (rOutColumnBottom.Width - pieceSize) / 2, rOutColumnBottom.Bottom - pieceSize - z * pieceSize,
                                    pieceSize, pieceSize);

            g.FillEllipse(brPlayer2, rPiece);
            g.DrawEllipse(penPieceBorder, rPiece);

            if (table.Player2.OutedPieces > piecesPerCol)
            {
                rPiece = new RectangleF(
                    rOutColumnBottom.Left + (rOutColumnBottom.Width - pieceSize) / 2, rOutColumnBottom.Bottom - pieceSize,
                    pieceSize, pieceSize);

                g.DrawString("+" + (table.Player2.OutedPieces - piecesPerCol), f, Brushes.Black, rPiece, sf);
            }
        }

        g.Dispose();
    }

    void DrawDice(Bitmap bmp)
    {
        Graphics g = Graphics.FromImage(bmp);
        Font f = new Font(FontFamily.GenericSansSerif, (int)(pieceSize * 0.35), FontStyle.Bold);
        StringFormat sf = new StringFormat();

        g.SmoothingMode = SmoothingMode.HighQuality;
        sf.Alignment = StringAlignment.Center;
        sf.LineAlignment = StringAlignment.Center;

        if (table.ActivePlayer == 1)
        {
            g.FillRectangle(brPlayer1, rDice1);
            g.FillRectangle(brPlayer1, rDice2);
        }
        else
        {
            g.FillRectangle(brPlayer2, rDice1);
            g.FillRectangle(brPlayer2, rDice2);
        }

        g.DrawRectangle(penPieceBorder, rDice1);
        g.DrawRectangle(penPieceBorder, rDice2);

        g.DrawString(table.Dice1.ToString(), f, Brushes.Black, rDice1, sf);
        g.DrawString(table.Dice2.ToString(), f, Brushes.Black, rDice2, sf);

        g.Dispose();
    }

    protected void OnAboutActionActivated(object sender, EventArgs e)
    {
        MessageBox.Show(
            "Backgammon by Horatiu" + Environment.NewLine + Environment.NewLine +
            "Email:\t<a href=\"mailto://Mlendea.Horatiu@GMail.com\">Mlendea.Horatiu@GMail.com</a>" + Environment.NewLine +
            "Website:\t<a href=\"http://hori.go.ro\">http://hori.go.ro</a>",
            "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    protected void OnExitActionActivated(object sender, EventArgs e)
    {
        Gtk.Application.Quit();
    }

    protected void OnDaButtonPressEvent(object o, Gtk.ButtonPressEventArgs args)
    {
        if (args.Event.Button != 1)
            return;

        dragBeginCol = -1;
        mousePosition = new Point((int)args.Event.X, (int)args.Event.Y);

        for (int i = 0; i < 24; i++)
            if (rTable[i].Contains(mousePosition))
            {
                dragBeginCol = i;
                break;
            }

        Console.WriteLine(dragBeginCol);

        if (dragBeginCol == -1)
            return;
        else if (table.TableValues[dragBeginCol] == 0)
            return;

        dragging = true;
        GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Hand1);
    }

    protected void OnDaButtonReleaseEvent(object o, Gtk.ButtonReleaseEventArgs args)
    {
        if (args.Event.Button != 1)
            return;

        dragging = false;
        GdkWindow.Cursor = new Gdk.Cursor(Gdk.CursorType.Arrow);
        mousePosition = new Point((int)args.Event.X, (int)args.Event.Y);

        if (rOutColumnTop.Contains(mousePosition) && table.Player1.OutedPieces != 0)
            table.MoveOutedPiece(table.Player1.MovesLeft[0]);
        else if (rOutColumnBottom.Contains(mousePosition) && table.Player2.OutedPieces != 0)
            table.MoveOutedPiece(table.Player2.MovesLeft[0]);
        else
            for (int i = 0; i < 24; i++)
                if (rTable[i].Contains(mousePosition))
                {
                    Console.WriteLine(dragBeginCol + " " + i);
                    try
                    {
                        if (table.TableValues[dragBeginCol] > 0)
                        {
                            if (dragBeginCol == i)
                                table.MovePiece(dragBeginCol, table.Player1.MovesLeft[0]);
                            else
                                table.MovePiece(dragBeginCol, i - dragBeginCol);

                            Console.WriteLine(i - dragBeginCol);
                        }
                        else
                        {
                            if (dragBeginCol == i)
                                table.MovePiece(dragBeginCol, table.Player2.MovesLeft[0]);
                            else
                                table.MovePiece(dragBeginCol, dragBeginCol - i);
                            
                            Console.WriteLine(dragBeginCol - i);
                        }
                    }
                    catch (PieceMoveException pme)
                    {
                        MessageBox.Show(pme.Message, "Invalid move", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
        
        DrawTable(da.GdkWindow);
    }

    protected void OnDaMotionNotifyEvent(object o, Gtk.MotionNotifyEventArgs args)
    {
        if (!dragging)
            return;
        
        mousePosition = new Point((int)args.Event.X, (int)args.Event.Y);
        int width, height;

        da.GdkWindow.GetSize(out width, out height);

        if (mousePosition.X < 0 || mousePosition.Y < 0 ||
            mousePosition.X >= width || mousePosition.Y >= height)
            return;
    }
}
