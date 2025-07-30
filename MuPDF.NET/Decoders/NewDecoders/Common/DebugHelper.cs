using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Text;

namespace BarcodeReader.Core.Common
{
#if CORE_DEV
    public
#else
    internal
#endif
    static class DebugHelper
    {
        private static string saveFolder = "c:\\temp";
        private static int saveCounter = 0;
        private static SKBitmap OriginalImage;
        private static SKBitmap NoScaledImage;
        public static SKBitmap ScaledImage;
        private static int scale = 1;
        private static SKCanvas gr;
        private static SKCanvas gr_noScaled;
        private static SKPaint fillPaint = new SKPaint { IsAntialias = false, Style = SKPaintStyle.Fill };
        private static SKPaint strokePaint = new SKPaint { IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 1 };

        private static List<ListItem> Items = new List<ListItem>();

        public static List<ListItem> GetItems()
        {
            var items = Items;
            Items = new List<ListItem>();
            return items;
        }

        public static void AddDebugItem(string name, IEnumerable<float> list, params MyPointF[] polygon)
        {
            var item = new ListItem() { Name = name, Polygon = polygon };
            foreach (var v in list)
                item.List.Add(v);

            Items.Add(item);
        }


#if DEBUG

        public static void InitImage(SKBitmap bmp)
        {
            OriginalImage = bmp.Copy();
            ReInitImage();
            saveCounter = 0;

            //remove all images
            if (Directory.Exists(saveFolder))
                foreach (var file in Directory.GetFiles(saveFolder, "*.png"))
                    File.Delete(file);
        }

        public static void InitImageIfNotInit(SKImage img)
        {
            if (OriginalImage != null)
                return;

            OriginalImage = new SKBitmap(img.Info);
            ReInitImage();
            saveCounter = 0;

            //remove all images
            if (Directory.Exists(saveFolder))
                foreach (var file in Directory.GetFiles(saveFolder, "*.png"))
                    File.Delete(file);
        }

        public static void ReInitImage()
        {
            if (OriginalImage == null)
                return;

            // Dispose old images
            ScaledImage?.Dispose();
            NoScaledImage?.Dispose();

            int sw = OriginalImage.Width * scale;
            int sh = OriginalImage.Height * scale;

            // Scaled image
            ScaledImage = new SKBitmap(sw, sh);
            using (var canvas = new SKCanvas(ScaledImage))
            {
                var destRect = new SKRect(0, 0, sw, sh);
                var paint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.None, // NearestNeighbor equivalent
                    IsAntialias = false
                };

                canvas.DrawBitmap(OriginalImage, destRect, paint);

                // Overlay semi-transparent white
                var overlayPaint = new SKPaint
                {
                    Color = new SKColor(255, 255, 255, 180),
                    Style = SKPaintStyle.Fill
                };
                canvas.DrawRect(destRect, overlayPaint);
            }

            // No scaled image
            NoScaledImage = new SKBitmap(OriginalImage.Width, OriginalImage.Height);
            using (var canvas = new SKCanvas(NoScaledImage))
            {
                var destRect = new SKRect(0, 0, OriginalImage.Width, OriginalImage.Height);
                var paint = new SKPaint
                {
                    FilterQuality = SKFilterQuality.None,
                    IsAntialias = false
                };
                canvas.DrawBitmap(OriginalImage, destRect, paint);
            }

            gr = new SKCanvas(ScaledImage);
            gr_noScaled = new SKCanvas(NoScaledImage);
        }

        public static void DrawSquare(float x, float y, Color color)
        {
            if (OriginalImage == null) return;

            fillPaint.Color = new SKColor(color.R, color.G, color.B, color.A);
            gr.DrawRect(x * scale, y * scale, scale, scale, fillPaint);
        }

        public static void FillInNoScaled(Color color, params MyPoint[] points)
        {
            if (OriginalImage == null) return;

            fillPaint.Color = new SKColor(color.R, color.G, color.B, color.A);

            foreach (var p in points)
                gr_noScaled.DrawRect(p.X, p.Y, 1, 1, fillPaint);
        }

        public static void FillInNoScaled(Color color, int radius, params MyPoint[] points)
        {
            if (OriginalImage == null) return;

            fillPaint.Color = new SKColor(color.R, color.G, color.B, color.A);

            foreach (var p in points)
                gr_noScaled.DrawRect(p.X - radius, p.Y - radius, 2 * radius + 1, 2 * radius + 1, fillPaint);
        }

        public static void DrawSquare(Color color, params MyPoint[] points)
        {
            if (OriginalImage == null) return;

            fillPaint.Color = new SKColor(color.R, color.G, color.B, color.A);
            foreach (var p in points)
                gr.DrawRect(p.X * scale, p.Y * scale, scale, scale, fillPaint);
        }

        public static void DrawRegion(Color color, BarCodeRegion r)
        {
            if (OriginalImage == null || gr == null) return;

            strokePaint.Color = new SKColor(color.R, color.G, color.B, color.A);

            var path = new SKPath();
            path.MoveTo(r.A.X * scale, r.A.Y * scale);
            path.LineTo(r.B.X * scale, r.B.Y * scale);
            path.LineTo(r.C.X * scale, r.C.Y * scale);
            path.LineTo(r.D.X * scale, r.D.Y * scale);
            path.Close(); // Important to close the polygon

            gr.DrawPath(path, strokePaint);
        }

        public static void DrawArrow(float x1, float y1, float x2, float y2, Color color)
        {
            if (OriginalImage == null || gr == null) return;

            strokePaint.Color = new SKColor(color.R, color.G, color.B, color.A);
            strokePaint.Style = SKPaintStyle.Stroke;
            strokePaint.StrokeWidth = 1;

            var offset = scale / 2f;

            var start = new SKPoint(x1 * scale + offset, y1 * scale + offset);
            var end = new SKPoint(x2 * scale + offset, y2 * scale + offset);

            // Draw main line
            gr.DrawLine(start, end, strokePaint);

            // Draw arrowhead
            float arrowLength = 6f;
            float arrowAngle = (float)(Math.PI / 6); // 30 degrees

            var angle = (float)Math.Atan2(end.Y - start.Y, end.X - start.X);

            var left = new SKPoint(
                end.X - arrowLength * (float)Math.Cos(angle - arrowAngle),
                end.Y - arrowLength * (float)Math.Sin(angle - arrowAngle));

            var right = new SKPoint(
                end.X - arrowLength * (float)Math.Cos(angle + arrowAngle),
                end.Y - arrowLength * (float)Math.Sin(angle + arrowAngle));

            gr.DrawLine(end, left, strokePaint);
            gr.DrawLine(end, right, strokePaint);
        }

        public static void SaveImage(string prefix = "")
        {
            if (OriginalImage == null) return;

            if (Directory.Exists(saveFolder))
            {
                using (var image = SKImage.FromBitmap(ScaledImage))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var stream = File.OpenWrite(saveFolder + "\\" + prefix + saveCounter + ".png"))
                {
                    data.SaveTo(stream);
                }
            }
            saveCounter++;
        }

        public static void SaveNoScaledImage(string prefix = "")
        {
            if (OriginalImage == null) return;

            if (Directory.Exists(saveFolder))
            {
                using (var image = SKImage.FromBitmap(NoScaledImage))
                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                using (var stream = File.OpenWrite(saveFolder + "\\NoScaled_" + prefix + saveCounter + ".png"))
                {
                    data.SaveTo(stream);
                }
            }
            saveCounter++;
        }

        static Stopwatch SW1 = new Stopwatch();
        static Stopwatch SW2 = new Stopwatch();
        static Stopwatch SW3 = new Stopwatch();
        static Stopwatch SW4 = new Stopwatch();

        static int countSW1 = 0;
        static int countSW2 = 0;
        static int countSW3 = 0;
        static int countSW4 = 0;


        public static void StartSW1(){ SW1.Start(); countSW1++; }
        public static void StopSW1() { SW1.Stop(); }

        public static void StartSW2() { SW2.Start(); countSW2++; }
        public static void StopSW2() { SW2.Stop(); }

        public static void StartSW3() { SW3.Start(); countSW3++; }
        public static void StopSW3() { SW3.Stop(); }

        public static void StartSW4() { SW4.Start(); countSW4++; }
        public static void StopSW4() { SW4.Stop(); }

        public static int Counter0 = 0;

        public static void WriteStopwath()
        {
            Console.WriteLine("SW1  Calls: {0} Time: {1:000.0} ms", countSW1.ToString().PadRight(10), SW1.ElapsedMilliseconds);
            Console.WriteLine("SW2  Calls: {0} Time: {1:000.0} ms", countSW2.ToString().PadRight(10), SW2.ElapsedMilliseconds);
            Console.WriteLine("SW3  Calls: {0} Time: {1:000.0} ms", countSW3.ToString().PadRight(10), SW3.ElapsedMilliseconds);
            Console.WriteLine("SW4  Calls: {0} Time: {1:000.0} ms", countSW4.ToString().PadRight(10), SW4.ElapsedMilliseconds);
            //Console.WriteLine("Counter0: " + Counter0);

            SW1.Reset();
            SW2.Reset();
            SW3.Reset();
            SW4.Reset();

            countSW1 = 0;
            countSW2 = 0;
            countSW3 = 0;
            countSW4 = 0;

            Counter0 = 0;
        }

        public static void CopyArrayToClipboard<T>(IEnumerable<T> list)
        {
            var sb = new StringBuilder();
            foreach(var v in list)
            {
                sb.Append(v + "\t");
            }
            //System.Windows.Forms.Clipboard.SetText(sb.ToString());
        }
#endif
    }

#if CORE_DEV
    public
#else
    internal
#endif
    class ListItem
    {
        public List<float> List = new List<float>();
        public string Name;
        public MyPointF[] Polygon;

        public override string ToString()
        {
            return Name;
        }
    }

}