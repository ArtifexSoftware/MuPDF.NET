using MuPDF.NET;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Demo
{
    public static class Constants
    {
        // some colors
        public static float[] red = new float[] { 1, 0, 0 };
        public static float[] blue = new float[] { 0, 0, 1 };
        public static float[] gold = new float[] { 1, 1, 0 };
        public static float[] green = new float[] { 0, 1, 0 };
        public static float[] white = new float[] { 1, 1, 1 };
        public static float[] black = new float[] { 0, 0, 0 };

        // rectangles and points
        public static Rect displ = new Rect(0, 50, 0, 50);
        public static Rect r = new Rect(72, 72, 220, 100);
        public static Rect rect = new Rect(100, 100, 200, 200);

        // string
        public static string t1 = "têxt üsès Lätiñ charß,\nEUR: €, mu: µ, super scripts: ²³!";
        public static string highlight = "this text is highlighted";
        public static string underline = "this text is underlined";
        public static string strikeout = "this text is striked out";
        public static string squiggled = "this text is zigzag-underlined";

        public static Func<string> FILENAME = () =>
        {
            return System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
        };
    }
}
