using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace Lab3Variant9
{
    public enum BorderMode { Clamp, Reflect }

    public sealed class PixelBuffer
    {
        public int Width { get; private set; }
        public int Height { get; private set; }
        public byte[,,] Pixels { get; private set; }
        public PixelBuffer(int width, int height, byte[] rgb)
        {
            if (width < 1 || height < 1 || rgb == null || rgb.Length != checked(width * height * 3))
                throw new ArgumentException("Некорректный размер пиксельного буфера.");
            Width = width;
            Height = height;
            Pixels = new byte[height, width, 3];
            Buffer.BlockCopy(rgb, 0, Pixels, 0, rgb.Length);
        }

        public static int Index(int coordinate, int size, BorderMode mode)
        {
            if (size <= 1) return 0;
            if (mode == BorderMode.Clamp) return Math.Max(0, Math.Min(size - 1, coordinate));
      
            int period = 2 * size;
            int c = coordinate % period;
            if (c < 0) c += period;
            return c < size ? c : period - 1 - c;
        }

        public static float Sample(float[] data, int width, int height, int x, int y, BorderMode mode)
        {
            return data[Index(y, height, mode) * width + Index(x, width, mode)];
        }

        public static float Bilinear(float[] data, int width, int height, double x, double y, BorderMode mode)
        {
            int ix = (int)Math.Floor(x), iy = (int)Math.Floor(y);
            double dx = x - ix, dy = y - iy;
            return (float)((1 - dy) * ((1 - dx) * Sample(data, width, height, ix, iy, mode) +
                dx * Sample(data, width, height, ix + 1, iy, mode)) +
                dy * ((1 - dx) * Sample(data, width, height, ix, iy + 1, mode) +
                dx * Sample(data, width, height, ix + 1, iy + 1, mode)));
        }

        public byte[] Gray()
        {
            byte[] result = new byte[Width * Height];
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++)
                result[y * Width + x] = (byte)((299 * Pixels[y,x,0] + 587 * Pixels[y,x,1] + 114 * Pixels[y,x,2] + 500) / 1000);
            return result;
        }

        public static PixelBuffer FromGray(int width, int height, byte[] gray)
        {
            byte[] rgb = new byte[checked(width * height * 3)];
            for (int i = 0; i < gray.Length; i++) rgb[3 * i] = rgb[3 * i + 1] = rgb[3 * i + 2] = gray[i];
            return new PixelBuffer(width, height, rgb);
        }

        public static unsafe PixelBuffer Load(string path)
        {
            using (Bitmap source = new Bitmap(path))
            {
                if ((long)source.Width * source.Height > 24000000)
                    throw new ArgumentException("Максимальный размер изображения — 24 миллиона пикселей.");
                using (Bitmap normalized = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb))
                {
                 
                    using (Graphics g = Graphics.FromImage(normalized))
                    {
                        g.Clear(Color.White);
                        g.DrawImageUnscaled(source, 0, 0);
                    }
                    byte[] rgb = new byte[checked(source.Width * source.Height * 3)];
                    BitmapData bits = normalized.LockBits(new Rectangle(0, 0, source.Width, source.Height),
                        ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                    try
                    {
                        for (int y = 0; y < source.Height; y++)
                        {
                            byte* row = (byte*)bits.Scan0 + y * bits.Stride;
                            for (int x = 0; x < source.Width; x++)
                            {
                                int i = (y * source.Width + x) * 3;
                                rgb[i] = row[4 * x + 2]; rgb[i + 1] = row[4 * x + 1]; rgb[i + 2] = row[4 * x];
                            }
                        }
                    }
                    finally { normalized.UnlockBits(bits); }
                    return new PixelBuffer(source.Width, source.Height, rgb);
                }
            }
        }

        public unsafe Bitmap ToBitmap()
        {
            Bitmap bitmap = new Bitmap(Width, Height, PixelFormat.Format24bppRgb);
            BitmapData bits = bitmap.LockBits(new Rectangle(0, 0, Width, Height), ImageLockMode.WriteOnly, PixelFormat.Format24bppRgb);
            try
            {
                for (int y = 0; y < Height; y++)
                {
                    byte* row = (byte*)bits.Scan0 + y * bits.Stride;
                    for (int x = 0; x < Width; x++)
                    {
                        row[3 * x] = Pixels[y,x,2]; row[3 * x + 1] = Pixels[y,x,1]; row[3 * x + 2] = Pixels[y,x,0];
                    }
                }
            }
            finally { bitmap.UnlockBits(bits); }
            return bitmap;
        }
    }
}
