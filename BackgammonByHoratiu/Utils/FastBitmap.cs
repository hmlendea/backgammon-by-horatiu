using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace BackgammonByHoratiu.Utils
{
    public class FastBitmap
    {
        Bitmap source;
        IntPtr Iptr = IntPtr.Zero;
        BitmapData bitmapData;

        byte[] pixels;
        int depth, width, height;

        /// <summary>
        /// Gets or sets the pixels.
        /// </summary>
        /// <value>The pixels.</value>
        public byte[] Pixels
        {
            get { return pixels; }
            set { pixels = value; }
        }

        /// <summary>
        /// Gets the depth.
        /// </summary>
        /// <value>The depth.</value>
        public int Depth
        {
            get { return depth; }
        }

        /// <summary>
        /// Gets the width.
        /// </summary>
        /// <value>The width.</value>
        public int Width
        {
            get { return width; }
        }

        /// <summary>
        /// Gets the height.
        /// </summary>
        /// <value>The height.</value>
        public int Height
        {
            get { return height; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgammonByHori.Utils.FastBitmap"/> class.
        /// </summary>
        /// <param name="sourceBitmap">Source bitmap.</param>
        public FastBitmap(Bitmap sourceBitmap)
        {
            source = sourceBitmap;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgammonByHori.Utils.FastBitmap"/> class.
        /// </summary>
        /// <param name="sourceImage">Source image.</param>
        public FastBitmap(Image sourceImage)
        {
            source = (Bitmap)sourceImage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgammonByHori.Utils.FastBitmap"/> class.
        /// </summary>
        /// <param name="fileName">File name.</param>
        public FastBitmap(string fileName)
        {
            source = new Bitmap(fileName);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgammonByHori.Utils.FastBitmap"/> class.
        /// </summary>
        /// <param name="type">Type.</param>
        /// <param name="resource">Resource.</param>
        public FastBitmap(Type type, string resource)
        {
            source = new Bitmap(type, resource);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgammonByHori.Utils.FastBitmap"/> class.
        /// </summary>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        public FastBitmap(int width, int height)
        {
            source = new Bitmap(width, height);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgammonByHori.Utils.FastBitmap"/> class.
        /// </summary>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="format">Format.</param>
        public FastBitmap(int width, int height, PixelFormat format)
        {
            source = new Bitmap(width, height, format);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BackgammonByHori.Utils.FastBitmap"/> class.
        /// </summary>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="stride">Stride.</param>
        /// <param name="format">Format.</param>
        /// <param name="scan0">Scan0.</param>
        public FastBitmap(int width, int height, int stride, PixelFormat format, IntPtr scan0)
        {
            source = new Bitmap(width, height, stride, format, scan0);
        }

        /// <summary>
        /// Locks the bitmap data
        /// </summary>
        public virtual void LockBits()
        {
            width = source.Width;
            height = source.Height;

            int pixelCount = width * height;

            Rectangle rect = new Rectangle(0, 0, width, height);

            depth = Image.GetPixelFormatSize(source.PixelFormat);

            if (depth != 8 && depth != 24 && depth != 32)
                throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");

            bitmapData = source.LockBits(rect, ImageLockMode.ReadWrite, source.PixelFormat);

            int step = depth / 8;
            pixels = new byte[pixelCount * step];
            Iptr = bitmapData.Scan0;

            Marshal.Copy(Iptr, pixels, 0, pixels.Length);
        }

        /// <summary>
        /// Unlocks the bitmap data
        /// </summary>
        public virtual void UnlockBits()
        {
            Marshal.Copy(pixels, 0, Iptr, pixels.Length);
            source.UnlockBits(bitmapData);
        }

        /// <summary>
        /// Get the color of the specified pixel
        /// </summary>
        /// <param name="x">X Coordinate</param>
        /// <param name="y">Y Coordinate</param>
        /// <returns>Pixel color</returns>
        public virtual Color GetPixel(int x, int y)
        {
            Color clr = Color.Empty;
            byte a, r, g, b;
            int colorComponentsCount = depth / 8;
            int index = ((y * width) + x) * colorComponentsCount;

            if (index > pixels.Length - colorComponentsCount)
                throw new IndexOutOfRangeException();

            switch (depth)
            {
                case 32:
                    b = pixels[index];
                    g = pixels[index + 1];
                    r = pixels[index + 2];
                    a = pixels[index + 3];
                    clr = Color.FromArgb(a, r, g, b);
                    break;

                case 24:
                    b = pixels[index];
                    g = pixels[index + 1];
                    r = pixels[index + 2];
                    clr = Color.FromArgb(r, g, b);
                    break;

                case 8:
                    b = pixels[index];
                    clr = Color.FromArgb(b, b, b);
                    break;
            }

            return clr;
        }

        /// <summary>
        /// Set the color of the specified pixel
        /// </summary>
        /// <param name="x">X Coordinate</param>
        /// <param name="y">Y Coordinate</param>
        /// <param name="color">Pixel color</param>
        public virtual void SetPixel(int x, int y, Color color)
        {
            int colorComponentsCount = depth / 8;
            int index = ((y * width) + x) * colorComponentsCount;

            switch (depth)
            {
                case 32:
                    pixels[index] = color.B;
                    pixels[index + 1] = color.G;
                    pixels[index + 2] = color.R;
                    pixels[index + 3] = color.A;
                    break;

                case 24:
                    pixels[index] = color.B;
                    pixels[index + 1] = color.G;
                    pixels[index + 2] = color.R;
                    break;

                case 8:
                    pixels[index] = color.B;
                    break;
            }        
        }

        /// <summary>
        /// Releases all resource used by the <see cref="BackgammonByHori.Utils.FastBitmap"/> object.
        /// </summary>
        /// <remarks>Call <see cref="Dispose"/> when you are finished using the <see cref="BackgammonByHori.Utils.FastBitmap"/>. The
        /// <see cref="Dispose"/> method leaves the <see cref="BackgammonByHori.Utils.FastBitmap"/> in an unusable state. After
        /// calling <see cref="Dispose"/>, you must release all references to the <see cref="BackgammonByHori.Utils.FastBitmap"/>
        /// so the garbage collector can reclaim the memory that the <see cref="BackgammonByHori.Utils.FastBitmap"/> was occupying.</remarks>
        public virtual void Dispose()
        {
            source.Dispose();
        }

        /// <param name="fbmp">FastBitmap.</param>
        public static implicit operator Bitmap(FastBitmap fbmp)
        {
            return fbmp.source;
        }

        /// <param name="bmp">Bitmap.</param>
        public static implicit operator FastBitmap(Bitmap bmp)
        {
            return new FastBitmap(bmp);
        }

        /// <param name="fbmp">FastBitmap.</param>
        public static implicit operator Image(FastBitmap fbmp)
        {
            return fbmp.source;
        }

        /// <param name="img">Image.</param>
        public static implicit operator FastBitmap(Image img)
        {
            return new FastBitmap(img);
        }
    }
}
