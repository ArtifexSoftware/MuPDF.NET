using System;
using System.Globalization;

namespace MuPDF.NET
{
    /// <summary>Line-end appearance helpers.</summary>
    public partial class Annot
    {
        private delegate string LeFunction(Annot annot, Point p1, Point p2, bool lr, float[] fillColor);

        internal static (
            Matrix m,
            Matrix im,
            Point L,
            Point R,
            float w,
            string scol,
            string fcol,
            string opacity
        ) LeAnnotParms(Annot annot, Point p1, Point p2, float[] fillColor)
        {
            float w = annot.BorderWidth;
            if (w < 0)
                w = 1;

            float[] sc = annot.StrokeColor;
            if (sc == null || sc.Length == 0)
                sc = new float[] { 0, 0, 0 };
            string scol = Helpers.ColorCode(sc, "c").TrimEnd() + "\n";

            float[] fc = fillColor;
            if (fc == null || fc.Length == 0)
                fc = annot.InteriorColor;
            if (fc == null || fc.Length == 0)
                fc = new float[] { 1, 1, 1 };
            string fcol = Helpers.ColorCode(fc, "f").TrimEnd() + "\n";

            Matrix m = new Matrix(Helpers.UtilHorMatrix(p1, p2));
            Matrix im = m.Inverted() ?? Matrix.Identity;
            Point L = p1 * m;
            Point R = p2 * m;
            float op = annot.Opacity;
            string opacity = op >= 0 && op < 1 ? "/H gs\n" : "";
            return (m, im, L, R, w, scol, fcol, opacity);
        }

        private static string LeBezier(Point p, Point q, Point r) =>
            string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1} {2} {3} {4} {5} c\n",
                Helpers.FormatPdfReal(p.X), Helpers.FormatPdfReal(p.Y),
                Helpers.FormatPdfReal(q.X), Helpers.FormatPdfReal(q.Y),
                Helpers.FormatPdfReal(r.X), Helpers.FormatPdfReal(r.Y));

        private static string LeOvalString(Point p1, Point p2, Point p3, Point p4)
        {
            const float kappa = 0.55228474983f;
            Point ml = p1 + (p4 - p1) * 0.5f;
            Point mo = p1 + (p2 - p1) * 0.5f;
            Point mr = p2 + (p3 - p2) * 0.5f;
            Point mu = p4 + (p3 - p4) * 0.5f;
            Point ol1 = ml + (p1 - ml) * kappa;
            Point ol2 = mo + (p1 - mo) * kappa;
            Point or1 = mo + (p2 - mo) * kappa;
            Point or2 = mr + (p2 - mr) * kappa;
            Point ur1 = mr + (p3 - mr) * kappa;
            Point ur2 = mu + (p3 - mu) * kappa;
            Point ul1 = mu + (p4 - mu) * kappa;
            Point ul2 = ml + (p4 - ml) * kappa;

            string ap = string.Format(
                CultureInfo.InvariantCulture,
                "{0} {1} m\n",
                Helpers.FormatPdfReal(ml.X), Helpers.FormatPdfReal(ml.Y));
            ap += LeBezier(ol1, ol2, mo);
            ap += LeBezier(or1, or2, mr);
            ap += LeBezier(ur1, ur2, mu);
            ap += LeBezier(ul1, ul2, ml);
            return ap;
        }

        internal static string LeSquare(Annot annot, Point p1, Point p2, bool lr, float[] fillColor)
        {
            var (_, im, L, R, w, scol, fcol, opacity) = LeAnnotParms(annot, p1, p2, fillColor);
            float shift = 2.5f;
            float d = shift * Math.Max(1, w);
            Point M = lr ? R - new Point(d / 2f, 0) : L + new Point(d / 2f, 0);
            Rect r = new Rect(M, M) + new Rect(-d, -d, d, d);
            Point p = r.TopLeft * im;
            string ap = string.Format(CultureInfo.InvariantCulture, "q\n{0}{1} {2} m\n", opacity, Helpers.FormatPdfReal(p.X), Helpers.FormatPdfReal(p.Y));
            p = r.TopRight * im;
            ap += string.Format(CultureInfo.InvariantCulture, "{0} {1} l\n", Helpers.FormatPdfReal(p.X), Helpers.FormatPdfReal(p.Y));
            p = r.BottomRight * im;
            ap += string.Format(CultureInfo.InvariantCulture, "{0} {1} l\n", Helpers.FormatPdfReal(p.X), Helpers.FormatPdfReal(p.Y));
            p = r.BottomLeft * im;
            ap += string.Format(CultureInfo.InvariantCulture, "{0} {1} l\n", Helpers.FormatPdfReal(p.X), Helpers.FormatPdfReal(p.Y));
            ap += Helpers.FormatPdfReal(w) + " w\n";
            ap += scol + fcol + "b\nQ\n";
            return ap;
        }

        internal static string LeDiamond(Annot annot, Point p1, Point p2, bool lr, float[] fillColor)
        {
            var (_, im, L, R, w, scol, fcol, opacity) = LeAnnotParms(annot, p1, p2, fillColor);
            float shift = 2.5f;
            float d = shift * Math.Max(1, w);
            Point M = lr ? R - new Point(d / 2f, 0) : L + new Point(d / 2f, 0);
            Rect r = new Rect(M, M) + new Rect(-d, -d, d, d);
            Point p = (r.TopLeft + (r.BottomLeft - r.TopLeft) * 0.5f) * im;
            string ap = string.Format(CultureInfo.InvariantCulture, "q\n{0}{1} {2} m\n", opacity, Helpers.FormatPdfReal(p.X), Helpers.FormatPdfReal(p.Y));
            p = (r.TopLeft + (r.TopRight - r.TopLeft) * 0.5f) * im;
            ap += string.Format(CultureInfo.InvariantCulture, "{0} {1} l\n", Helpers.FormatPdfReal(p.X), Helpers.FormatPdfReal(p.Y));
            p = (r.TopRight + (r.BottomRight - r.TopRight) * 0.5f) * im;
            ap += string.Format(CultureInfo.InvariantCulture, "{0} {1} l\n", Helpers.FormatPdfReal(p.X), Helpers.FormatPdfReal(p.Y));
            p = (r.BottomRight + (r.BottomLeft - r.BottomRight) * 0.5f) * im;
            ap += string.Format(CultureInfo.InvariantCulture, "{0} {1} l\n", Helpers.FormatPdfReal(p.X), Helpers.FormatPdfReal(p.Y));
            ap += Helpers.FormatPdfReal(w) + " w\n";
            ap += scol + fcol + "b\nQ\n";
            return ap;
        }

        internal static string LeCircle(Annot annot, Point p1, Point p2, bool lr, float[] fillColor)
        {
            var (_, im, L, R, w, scol, fcol, opacity) = LeAnnotParms(annot, p1, p2, fillColor);
            float shift = 2.5f;
            float d = shift * Math.Max(1, w);
            Point M = lr ? R - new Point(d / 2f, 0) : L + new Point(d / 2f, 0);
            Rect r = new Rect(M, M) + new Rect(-d, -d, d, d);
            string ap = "q\n" + opacity + LeOvalString(r.TopLeft * im, r.TopRight * im, r.BottomRight * im, r.BottomLeft * im);
            ap += Helpers.FormatPdfReal(w) + " w\n";
            ap += scol + fcol + "b\nQ\n";
            return ap;
        }

        internal static string LeOpenArrow(Annot annot, Point p1, Point p2, bool lr, float[] fillColor)
        {
            var (_, im, L, R, w, scol, _, opacity) = LeAnnotParms(annot, p1, p2, fillColor);
            float d = 2.5f * Math.Max(1, w);
            p2 = lr ? R + new Point(d / 2f, 0) : L - new Point(d / 2f, 0);
            p1 = lr ? p2 + new Point(-2 * d, -d) : p2 + new Point(2 * d, -d);
            Point p3 = lr ? p2 + new Point(-2 * d, d) : p2 + new Point(2 * d, d);
            p1 *= im;
            p2 *= im;
            p3 *= im;
            string ap = "\nq\n" + opacity + Helpers.FormatPdfReal(p1.X) + " " + Helpers.FormatPdfReal(p1.Y) + " m\n";
            ap += Helpers.FormatPdfReal(p2.X) + " " + Helpers.FormatPdfReal(p2.Y) + " l\n";
            ap += Helpers.FormatPdfReal(p3.X) + " " + Helpers.FormatPdfReal(p3.Y) + " l\n";
            ap += Helpers.FormatPdfReal(w) + " w\n";
            ap += scol + "S\nQ\n";
            return ap;
        }

        internal static string LeClosedArrow(Annot annot, Point p1, Point p2, bool lr, float[] fillColor)
        {
            var (_, im, L, R, w, scol, fcol, opacity) = LeAnnotParms(annot, p1, p2, fillColor);
            float d = 2.5f * Math.Max(1, w);
            p2 = lr ? R + new Point(d / 2f, 0) : L - new Point(d / 2f, 0);
            p1 = lr ? p2 + new Point(-2 * d, -d) : p2 + new Point(2 * d, -d);
            Point p3 = lr ? p2 + new Point(-2 * d, d) : p2 + new Point(2 * d, d);
            p1 *= im;
            p2 *= im;
            p3 *= im;
            string ap = $"\nq\n{opacity}{Helpers.FormatPdfReal(p1.X)} {Helpers.FormatPdfReal(p1.Y)} m\n";
            ap += $"{Helpers.FormatPdfReal(p2.X)} {Helpers.FormatPdfReal(p2.Y)} l\n";
            ap += $"{Helpers.FormatPdfReal(p3.X)} {Helpers.FormatPdfReal(p3.Y)} l\n";
            ap += $"{Helpers.FormatPdfReal(w)} w\n";
            ap += $"{scol}{fcol}b\nQ\n";
            return ap;
        }

        internal static string LeButt(Annot annot, Point p1, Point p2, bool lr, float[] fillColor)
        {
            var (_, im, L, R, w, scol, _, opacity) = LeAnnotParms(annot, p1, p2, fillColor);
            float d = 3 * Math.Max(1, w);
            Point M = lr ? R : L;
            Point top = new Point(M.X, M.Y - d / 2f) * im;
            Point bot = new Point(M.X, M.Y + d / 2f) * im;
            string ap = $"\nq\n{opacity}{Helpers.FormatPdfReal(top.X)} {Helpers.FormatPdfReal(top.Y)} m\n";
            ap += $"{Helpers.FormatPdfReal(bot.X)} {Helpers.FormatPdfReal(bot.Y)} l\n";
            ap += $"{Helpers.FormatPdfReal(w)} w\n";
            ap += $"{scol}s\nQ\n";
            return ap;
        }

        internal static string LeROpenArrow(Annot annot, Point p1, Point p2, bool lr, float[] fillColor)
        {
            var (_, im, L, R, w, scol, fcol, opacity) = LeAnnotParms(annot, p1, p2, fillColor);
            float d = 2.5f * Math.Max(1, w);
            p2 = lr ? new Point(R.X - d / 3f, R.Y) : new Point(L.X + d / 3f, L.Y);
            p1 = lr ? new Point(p2.X + 2 * d, p2.Y - d) : new Point(p2.X - 2 * d, p2.Y - d);
            Point p3 = lr ? new Point(p2.X + 2 * d, p2.Y + d) : new Point(p2.X - 2 * d, p2.Y + d);
            p1 *= im;
            p2 *= im;
            p3 *= im;
            string ap = "\nq\n" + opacity + Helpers.FormatPdfReal(p1.X) + " " + Helpers.FormatPdfReal(p1.Y) + " m\n";
            ap += Helpers.FormatPdfReal(p2.X) + " " + Helpers.FormatPdfReal(p2.Y) + " l\n";
            ap += Helpers.FormatPdfReal(p3.X) + " " + Helpers.FormatPdfReal(p3.Y) + " l\n";
            ap += Helpers.FormatPdfReal(w) + " w\n";
            ap += scol + fcol + "S\nQ\n";
            return ap;
        }

        internal static string LeRClosedArrow(Annot annot, Point p1, Point p2, bool lr, float[] fillColor)
        {
            var (_, im, L, R, w, scol, fcol, opacity) = LeAnnotParms(annot, p1, p2, fillColor);
            float d = 2.5f * Math.Max(1, w);
            p2 = lr ? new Point(R.X - 2 * d, R.Y) : new Point(L.X + 2 * d, L.Y);
            p1 = lr ? p2 + new Point(2 * d, -d) : p2 + new Point(-2 * d, -d);
            Point p3 = lr ? p2 + new Point(2 * d, d) : p2 + new Point(-2 * d, d);
            p1 *= im;
            p2 *= im;
            p3 *= im;
            string ap = "\nq\n" + opacity + Helpers.FormatPdfReal(p1.X) + " " + Helpers.FormatPdfReal(p1.Y) + " m\n";
            ap += Helpers.FormatPdfReal(p2.X) + " " + Helpers.FormatPdfReal(p2.Y) + " l\n";
            ap += Helpers.FormatPdfReal(p3.X) + " " + Helpers.FormatPdfReal(p3.Y) + " l\n";
            ap += Helpers.FormatPdfReal(w) + " w\n";
            ap += scol + fcol + "b\nQ\n";
            return ap;
        }

        internal static string LeSlash(Annot annot, Point p1, Point p2, bool lr, float[] fillColor)
        {
            var (_, im, L, R, w, scol, _, opacity) = LeAnnotParms(annot, p1, p2, fillColor);
            float rw = 1.1547f * Math.Max(1, w);
            Point M = lr ? R : L;
            Rect r = new Rect(M.X - rw, M.Y - 2 * w, M.X + rw, M.Y + 2 * w);
            Point top = r.TopLeft * im;
            Point bot = r.BottomRight * im;
            string ap = "\nq\n" + opacity + Helpers.FormatPdfReal(top.X) + " " + Helpers.FormatPdfReal(top.Y) + " m\n";
            ap += Helpers.FormatPdfReal(bot.X) + " " + Helpers.FormatPdfReal(bot.Y) + " l\n";
            ap += Helpers.FormatPdfReal(w) + " w\n";
            ap += scol + "b\nQ\n";
            return ap;
        }

        private static string AppendLineEndSymbols(
            Annot annot,
            string apText,
            int lineEndLe,
            int lineEndRi,
            Matrix iMat,
            float[] fillColor,
            ref Rect? lineEndRect)
        {
            LeFunction[] leFuncs =
            {
                null,
                LeSquare,
                LeCircle,
                LeDiamond,
                LeOpenArrow,
                LeClosedArrow,
                LeButt,
                LeROpenArrow,
                LeRClosedArrow,
                LeSlash,
            };

            float d = 2 * Math.Max(1, annot.BorderWidth > 0 ? annot.BorderWidth : 1);
            lineEndRect = annot.Rect + new Rect(-d, -d, d, d);
            var points = annot.Vertices;

            if (lineEndLe > 0 && lineEndLe < leFuncs.Length && leFuncs[lineEndLe] != null)
            {
                Point p1 = points[0] * iMat;
                Point p2 = points[1] * iMat;
                apText += leFuncs[lineEndLe](annot, p1, p2, false, fillColor);
            }

            if (lineEndRi > 0 && lineEndRi < leFuncs.Length && leFuncs[lineEndRi] != null)
            {
                Point p1 = points[points.Count - 2] * iMat;
                Point p2 = points[points.Count - 1] * iMat;
                apText += leFuncs[lineEndRi](annot, p1, p2, true, fillColor);
            }

            return apText;
        }
    }
}