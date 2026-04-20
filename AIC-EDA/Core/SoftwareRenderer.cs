using Microsoft.UI;
using System;
using System.Linq;
using Windows.UI;

namespace AIC_EDA.Core
{
    /// <summary>
    /// Simple software rasterizer for isometric 3D view.
    /// Owns an RGBA framebuffer and provides line/triangle drawing.
    /// </summary>
    public class SoftwareRenderer
    {
        public int Width { get; }
        public int Height { get; }
        public byte[] Pixels { get; } // RGBA, 4 bytes per pixel, row-major

        public SoftwareRenderer(int width, int height)
        {
            Width = width;
            Height = height;
            Pixels = new byte[width * height * 4];
        }

        public void Resize(int width, int height)
        {
            if (Width == width && Height == height) return;
            // Re-initialize with new size (simplified; caller should recreate if size changes often)
            // For now we rely on caller checking size
        }

        public void Clear(Color color)
        {
            byte r = color.R;
            byte g = color.G;
            byte b = color.B;
            byte a = color.A;
            int len = Pixels.Length;
            for (int i = 0; i < len; i += 4)
            {
                Pixels[i + 0] = b;
                Pixels[i + 1] = g;
                Pixels[i + 2] = r;
                Pixels[i + 3] = a;
            }
        }

        public void SetPixel(int x, int y, Color color)
        {
            if (x < 0 || x >= Width || y < 0 || y >= Height) return;
            int idx = (y * Width + x) * 4;
            byte sr = color.R;
            byte sg = color.G;
            byte sb = color.B;
            byte sa = color.A;
            if (sa == 255)
            {
                Pixels[idx + 0] = sb;
                Pixels[idx + 1] = sg;
                Pixels[idx + 2] = sr;
                Pixels[idx + 3] = 255;
            }
            else if (sa > 0)
            {
                byte dr = Pixels[idx + 2];
                byte dg = Pixels[idx + 1];
                byte db = Pixels[idx + 0];
                int invA = 255 - sa;
                Pixels[idx + 2] = (byte)((sr * sa + dr * invA) / 255);
                Pixels[idx + 1] = (byte)((sg * sa + dg * invA) / 255);
                Pixels[idx + 0] = (byte)((sb * sa + db * invA) / 255);
                Pixels[idx + 3] = 255;
            }
        }

        public void DrawLine(int x0, int y0, int x1, int y1, Color color)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                SetPixel(x0, y0, color);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
            }
        }

        public void DrawDashedLine(int x0, int y0, int x1, int y1, Color color, int dashOn, int dashOff)
        {
            int dx = Math.Abs(x1 - x0);
            int dy = Math.Abs(y1 - y0);
            int sx = x0 < x1 ? 1 : -1;
            int sy = y0 < y1 ? 1 : -1;
            int err = dx - dy;
            int counter = 0;
            int period = dashOn + dashOff;

            while (true)
            {
                if (counter % period < dashOn)
                    SetPixel(x0, y0, color);
                if (x0 == x1 && y0 == y1) break;
                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 < dx) { err += dx; y0 += sy; }
                counter++;
            }
        }

        /// <summary>Draw a filled triangle using scanline rasterization.</summary>
        public void DrawTriangle(int x0, int y0, int x1, int y1, int x2, int y2, Color color)
        {
            // Sort vertices by Y
            if (y0 > y1) { (x0, x1) = (x1, x0); (y0, y1) = (y1, y0); }
            if (y0 > y2) { (x0, x2) = (x2, x0); (y0, y2) = (y2, y0); }
            if (y1 > y2) { (x1, x2) = (x2, x1); (y1, y2) = (y2, y1); }

            if (y0 == y2) return; // degenerate

            // Edge interpolation helpers
            for (int y = Math.Max(0, y0); y <= Math.Min(Height - 1, y2); y++)
            {
                double xl, xr;
                if (y < y1)
                {
                    // Top half
                    double t0 = y0 == y2 ? 0 : (double)(y - y0) / (y2 - y0);
                    double t1 = y0 == y1 ? 0 : (double)(y - y0) / (y1 - y0);
                    xl = x0 + t0 * (x2 - x0);
                    xr = x0 + t1 * (x1 - x0);
                }
                else
                {
                    // Bottom half
                    double t0 = y0 == y2 ? 0 : (double)(y - y0) / (y2 - y0);
                    double t1 = y1 == y2 ? 0 : (double)(y - y1) / (y2 - y1);
                    xl = x0 + t0 * (x2 - x0);
                    xr = x1 + t1 * (x2 - x1);
                }
                if (xl > xr) (xl, xr) = (xr, xl);
                int xStart = Math.Max(0, (int)Math.Ceiling(xl));
                int xEnd = Math.Min(Width - 1, (int)Math.Floor(xr));
                for (int x = xStart; x <= xEnd; x++)
                    SetPixel(x, y, color);
            }
        }

        public void DrawQuad(int x0, int y0, int x1, int y1, int x2, int y2, int x3, int y3, Color color)
        {
            DrawTriangle(x0, y0, x1, y1, x2, y2, color);
            DrawTriangle(x0, y0, x2, y2, x3, y3, color);
        }

        /// <summary>
        /// Isometric projection: (gx, gy, gz) -> screen (sx, sy).
        /// Uses standard 2:1 dimetric projection.
        /// </summary>
        public static (double sx, double sy) ProjectIsometric(double gx, double gy, double gz, double cell, double panX, double panY, double centerOffset)
        {
            double isoX = (gx - gy) * cell * 0.5;
            double isoY = (gx + gy) * cell * 0.5 - gz * cell * 0.4;
            double sx = panX + isoX + centerOffset;
            double sy = panY + isoY;
            return (sx, sy);
        }

        /// <summary>Copy framebuffer to a WriteableBitmap pixel buffer (BGRA pre-multiplied).</summary>
        public unsafe void CopyToWriteableBitmapBuffer(byte* dest, int destWidth, int destHeight, int destStride)
        {
            int copyW = Math.Min(Width, destWidth);
            int copyH = Math.Min(Height, destHeight);
            for (int y = 0; y < copyH; y++)
            {
                byte* destRow = dest + y * destStride;
                int srcRowStart = y * Width * 4;
                for (int x = 0; x < copyW; x++)
                {
                    int srcIdx = srcRowStart + x * 4;
                    byte b = Pixels[srcIdx + 0];
                    byte g = Pixels[srcIdx + 1];
                    byte r = Pixels[srcIdx + 2];
                    byte a = Pixels[srcIdx + 3];
                    destRow[x * 4 + 0] = b;
                    destRow[x * 4 + 1] = g;
                    destRow[x * 4 + 2] = r;
                    destRow[x * 4 + 3] = a;
                }
            }
        }
    }
}
