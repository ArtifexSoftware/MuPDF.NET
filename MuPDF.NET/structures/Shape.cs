using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GoogleGson;
using Microsoft.Maui.Layouts;
using mupdf;
using MuPDF.NET;

namespace MuPDF.NET
{
    public class Shape
    {
        public MuPDFPage Page;

        public MuPDFDocument Doc;

        public float Height;

        public float Width;

        public float X;

        public float Y;

        public Matrix Pctm;

        public Matrix IPctm;

        public string DrawCont;

        public string TextCont;

        public string TotalCont;

        public Point LastPoint;

        public Rect Rect;

        public Shape(MuPDFPage page)
        {
            this.Page = page;
            this.Doc = page.Parent;

            if (!Doc.IsPDF)
                throw new Exception("is no PDF");
            Height = page.MediaBoxSize.Y;
            Width = page.MediaBoxSize.X;
            X = page.CropBoxPosition.X;
            Y = page.CropBoxPosition.Y;

            Pctm = page.TransformationMatrix;
            IPctm = ~page.TransformationMatrix;

            DrawCont = "";
            TextCont = "";
            TotalCont = "";
            LastPoint = null;
            Rect = null;
        }


        /// <summary>
        /// Insert text lines
        /// </summary>
        /// <param name="point">the bottom-left position of the first character of text in pixels</param>
        /// <param name="buffer"></param>
        /// <param name="fontSize"></param>
        /// <param name="lineHeight"></param>
        /// <param name="fontName"></param>
        /// <param name="fontFile"></param>
        /// <param name="setSimple"></param>
        /// <param name="encoding"></param>
        /// <param name="color"></param>
        /// <param name="fill"></param>
        /// <param name="renderMode"></param>
        /// <param name="borderWidth"></param>
        /// <param name="rotate"></param>
        /// <param name="morph"></param>
        /// <param name="strokeOpacity">set transparency for stroke colors (the border line of a character). Only 0 <= value <= 1 will be considered. Default is 1</param>
        /// <param name="fillOpacity">set transparency for fill colors. Default is 1</param>
        /// <param name="oc">the xref number of an OCG or OCMD to make this text conditionally displayable</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public int InsertText(
            Point point,
            dynamic buffer,
            float fontSize = 11,
            float lineHeight = 0,
            string fontName = "helv",
            string fontFile = null,
            bool setSimple = false,
            int encoding = 0,
            float[] color = null,
            float[] fill = null,
            int renderMode = 0,
            float borderWidth = 0.05f,
            int rotate = 0,
            MorphStruct morph = new MorphStruct(),
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0
            )
        {
            List<string> text = new List<string>();
            if (buffer is null)
                return 0;
            if (!(buffer is List<string>) || !(buffer is Tuple<string>))
                text = new List<string>(Convert.ToString(buffer).Split("\n"));
            else
                text = buffer;

            if (text.Count <= 0)
                return 0;

            int maxCode = 0;
            try
            {
                foreach (char c in String.Join(" ", text))
                {
                    if (maxCode < Convert.ToInt32(c))
                        maxCode = Convert.ToInt32(c);
                }
            }
            catch (Exception)
            {
                return 0;
            }

            string fName = fontName;
            if (fName.StartsWith("/"))
                fName = fName.Substring(1);

            int xref = Page.InsertFont(
                fontName: fName,
                fontFile: fontFile,
                encoding: encoding,
                setSimple: setSimple
                );

            FontStruct fontInfo = Utils.CheckFontInfo(Doc, xref);

            int ordering = fontInfo.Ordering;
            bool simple = fontInfo.Simple;
            string bfName = fontInfo.Name;
            float ascender = fontInfo.Ascender;
            float descender = fontInfo.Descender;
            float lheight = 0;

            if (lineHeight != 0)
                lineHeight = fontSize * lineHeight;
            else if (ascender - descender <= 1)
                lheight = fontSize * 1.2f;
            else
                lheight = fontSize * (ascender - descender);

            List<(int, double)> glyphs = new List<(int, double)>();
            if (maxCode > 255)
                glyphs = Utils.GetCharWidths(Doc, xref: xref, limit: maxCode + 1);
            else
                glyphs = fontInfo.Glyphs;

            List<string> tab = new List<string>();
            List<(int, double)> g = null;
            foreach (string t in text)
            {
                if (simple && (bfName != "Symbol" || bfName != "ZapfDingbats"))
                    g = null;
                else
                    g = new List<(int, double)>(glyphs);
                tab.Add(Utils.GetTJstr(t, g, simple, ordering));
            }

            text = tab;

            string colorStr = Utils.GetColorCode(color, "c");
            string fillStr = Utils.GetColorCode(fill, "f");
            if (fill == null && renderMode == 0)
            {
                fill = color;
                fillStr = Utils.GetColorCode(color, "f");
            }

            bool morphing = Utils.CheckMorph(morph);
            int rot = rotate;
            if (rot % 90 != 0)
                throw new Exception("bad rotate value");

            while (rot < 0)
                rot += 360;
            rot = rot % 360;

            string cmp90 = "0 1 -1 0 0 0 cm\n";// rotates 90 deg counter-clockwise
            string cmm90 = "0 -1 1 0 0 0 cm\n";// rotates 90 deg clockwise
            string cm180 = "-1 0 0 -1 0 0 cm\n";// rotates by 180 deg.
            float height = Height;
            float width = Width;
            string cm;

            if (morphing)
            {
                Matrix m1 = new Matrix(1, 0, 0, 1, morph.FixPoint.X + X, height - morph.FixPoint.Y - Y);
                Matrix mat = ~m1 * morph.Matrix * m1;
                cm = $"{mat.A} {mat.B} {mat.C} {mat.D} {mat.E} {mat.F} cm\n";
            }
            else cm = "";

            float top = height - point.Y - Y;
            float left = point.X + X;
            float space = top;
            if (rot == 90)
            {
                left = height - point.Y - Y;
                top = -point.X - X;
                cm += cmp90;
                space = width - Math.Abs(top);
            }
            else if (rot == 270)
            {
                left = -height + point.Y + Y;
                top = point.X + X;
                cm += cmm90;
                space = Math.Abs(top);
            }
            else if (rot == 180)
            {
                left = -point.X - X;
                top = -height + point.Y + Y;
                cm += cm180;
                space = Math.Abs(point.Y + Y);
            }

            string optCont = Page.GetOptionalContent(oc);
            string bdc;
            string emc;
            if (optCont != null)
            {
                bdc = $"/OC /{optCont} BDC\n";
                emc = "EMC\n";
            }
            else bdc = emc = "";

            string alpha = Page.SetOpacity(CA: strokeOpacity, ca: fillOpacity);
            if (alpha == null)
                alpha = "";
            else
                alpha = $"/{alpha} gs\n";

            string nres = $"\nq\n{bdc}{alpha}BT\n{cm}1 0 0 1 {left} {top} Tm\n/{fName} {fontSize} Tf ";
            if (renderMode > 0)
                nres += $"{renderMode} Tr ";
            if (borderWidth != 1)
                nres += $"{borderWidth} w ";
            if (color != null)
                nres += colorStr;
            if (fill != null)
                nres += fillStr;

            /// start text insertion
            nres += text[0];
            int nLines = 1;
            string template = "TJ\n0 -{0} TD\n";
            if (text.Count > 1)
                nres += string.Format(template, lheight);
            else nres += template.Substring(0, 2);

            for (int i = 1; i < text.Count; i++)
            {
                if (space < lheight)
                    break; // no space left on page
                if (i > 1)
                    nres += "\nT* ";
                nres += text[i] + template.Substring(0, 2);
                space -= lheight;
                nLines += 1;
            }

            nres += $"\nET\n{emc}Q\n";

            // end of text insertion
            TextCont += nres;
            return nLines;
        }

        public void Commit(int overlay)
        {
            TotalCont += this.TextCont;
            byte[] bTotal = Encoding.UTF8.GetBytes(TotalCont);
            if (TotalCont != "")
            {
                int xref = Utils.InsertContents(Page, bTotal, overlay);
                /*mupdf.mupdf.pdf_update_stream(Doc, xref, TotalCont);//issue*/
            }

            LastPoint = null;
            Rect = null;
            DrawCont = "";
            TextCont = "";
            TotalCont = "";

            return;
        }

        /// <summary>
        /// Draw a standard cubic Bezier curve.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="p3"></param>
        /// <param name="p4"></param>
        /// <returns></returns>
        public Point DrawBezier(Point p1, Point p2, Point p3, Point p4)
        {
            if (LastPoint != p1)
            {
                Point t = p1 * IPctm;
                DrawCont += $"{t.X} {t.Y} m\n";
            }

            Point t2 = p2 * IPctm;
            Point t3 = p3 * IPctm;
            Point t4 = p4 * IPctm;
            DrawCont += $"{t2.X} {t2.Y} {t3.X} {t3.Y} {t4.X} {t4.Y} c\n";

            UpdateRect(p1);
            UpdateRect(p2);
            UpdateRect(p3);
            UpdateRect(p4);
            return LastPoint;
        }

        /// <summary>
        /// Draw a circle given its center and radius.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="radius"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Point DrawCircle(Point center, float radius)
        {
            if (!(radius > Utils.FLT_EPSILON))
                throw new Exception("radius must be postive");
            Point p1 = center - new Point(radius, 0);
            return DrawSector(center, p1, 360, fullSector: false);
        }

        /// <summary>
        /// Draw a circle sector.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="point"></param>
        /// <param name="beta"></param>
        /// <param name="fullSector"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public Point DrawSector(Point center, Point point, float beta, bool fullSector)
        {
            string l3 = "{0} {1} m\n";
            string l4 = "{0} {1} {2} {3} {4} {5} c\n";
            string l5 = "{0} {1} l\n";
            float betar = (float)Math.PI / 180 * (-beta);
            float w360 = (float)Math.PI / 180 * (360 * betar / Math.Abs(betar)) * -1;
            float w90 = (float)Math.PI / 180 * (90 * betar / Math.Abs(betar));
            float w45 = w90 / 2;
            while (Math.Abs(betar) > 2 * Math.PI)
                betar += 360;
            
            if (!(LastPoint == point))
            {
                Point t = point * IPctm;
                DrawCont += string.Format(l3, t.X, t.Y);
                LastPoint = point;
            }

            Point q = new Point(0, 0);
            Point c = center;
            Point p = point;
            Point s = p - c;
            float rad = s.Abs();

            if (!(rad > Utils.FLT_EPSILON))
                throw new Exception("radius must be positive");

            float alfa = HorizontalAngle(center, point);
            while (Math.Abs(betar) > Math.Abs(w90))
            {
                float q1 = c.X + (float)(Math.Cos(alfa + w90) * rad);
                float q2 = c.Y + (float)(Math.Sin(alfa + w90) * rad);
                q = new Point(q1, q2);
                float r1 = c.X + (float)(Math.Cos(alfa + w45) * rad / Math.Cos(w45));
                float r2 = c.Y + (float)(Math.Cos(alfa + w45) * rad / Math.Cos(w45));
                Point r = new Point(r1, r2);

                float kappah = (float)(((double)1 - Math.Cos(w45)) * 4 / 3 / (r - q).Abs());
                float kappa = kappah * (p - q).Abs();
                Point cp1 = p + (r - p) * kappa;
                Point cp2 = q + (r - q) * kappa;

                Point t1 = cp1 * IPctm;
                Point t2 = cp2 * IPctm;
                Point t3 = q * IPctm;
                DrawCont += string.Format(l4, t1.X, t1.Y, t2.X, t2.Y, t3.X, t3.Y);

                betar -= w90;
                alfa += w90;
                p = q;
            }

            if (Math.Abs(betar) > 1e-3)
            {
                float beta2 = betar / 2;
                float q1 = c.X + (float)(Math.Cos(alfa + betar) * rad);
                float q2 = c.Y + (float)(Math.Sin(alfa + betar) * rad);
                q = new Point(q1, q2);
                float r1 = c.X + (float)(Math.Cos(alfa + beta2) * rad / Math.Cos(beta2));
                float r2 = c.Y + (float)(Math.Sin(alfa + beta2) * rad / Math.Cos(beta2));
                Point r = new Point(r1, r2);
                float kappah = (float)(1 - Math.Cos(beta2) * 4 / 3 / (r - q).Abs());
                float kappa = (float)(kappah * (p - q).Abs() / (1 - Math.Cos(betar)));
                Point cp1 = p + (r - p) * kappa;
                Point cp2 = q + (r - q) * kappa;

                Point t1 = cp1 * IPctm;
                Point t2 = cp2 * IPctm;
                Point t3 = q * IPctm;
                DrawCont += string.Format(l4, t1.X, t1.Y, t2.X, t2.Y, t3.X, t3.Y);
            }

            if (fullSector)
            {
                Point t = point * IPctm;
                DrawCont += string.Format(l3, t.X, t.Y);
                t = center * IPctm;
                DrawCont += string.Format(l5, t.X, t.Y);
                t = q * IPctm;
                DrawCont += string.Format(l5, t.X, t.Y);
            }
            LastPoint = q;
            return LastPoint;
        }

        public void UpdateRect(Point x)
        {
            if (Rect == null)
                Rect = new Rect(x, x);
            else
            {
                Rect.X0 = Math.Min(Rect.X0, x.X);
                Rect.Y0 = Math.Min(Rect.Y0, x.Y);
                Rect.X1 = Math.Min(Rect.X1, x.X);
                Rect.Y1 = Math.Min(Rect.Y1, x.Y);
            }
        }

        public void UpdateRect(Rect x)
        {
            if (Rect == null)
                Rect = x;
            else
            {
                Rect.X0 = Math.Min(Rect.X0, x.X0);
                Rect.Y0 = Math.Min(Rect.Y0, x.Y0);
                Rect.X1 = Math.Min(Rect.X1, x.X1);
                Rect.Y1 = Math.Min(Rect.Y1, x.Y1);
            }
        }

        public static float HorizontalAngle(Point c, Point p)
        {
            Point s = (p - c).Unit;
            float alfa = (float)Math.Asin(Math.Abs(s.Y));
            if (s.X < 0)
            {
                if (s.Y <= 0)
                    alfa = -((float)Math.PI - alfa);
                else
                    alfa = (float)Math.PI - alfa;
            }
            else
            {
                if (s.Y > 0) { }
                else alfa -= alfa;
            }
            return alfa;
        }

        public Point DrawCurve(Point p1, Point p2, Point p3)
        {
            float kappa = 0.55228474983f;
            Point k1 = p1 + (p2 - p1) * kappa;
            Point k2 = p3 + (p2 - p3) * kappa;
            return DrawBezier(p1, k1, k2, p3);
        }

        /// <summary>
        /// Draw a line between two points.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public Point DrawLine(Point p1, Point p2)
        {
            Point t;
            if (!(LastPoint == p1))
            {
                t = p1 * IPctm;
                DrawCont += $"{t.X} {t.Y} m\n";
                LastPoint = p1;
                UpdateRect(p1);
            }

            t = p2 * IPctm;
            DrawCont += $"{t.X} {t.Y} l\n";
            UpdateRect(p2);
            LastPoint = p2;
            return LastPoint;
        }

        /// <summary>
        /// Draw an ellipse inside a tetrapod.
        /// </summary>
        /// <param name="tetra"></param>
        /// <returns></returns>
        public Point DrawOval(Rect tetra)
        {
            _DrawOval(tetra.Quad);
            return LastPoint;
        }

        public Point DrawOval(Quad tetra)
        {
            _DrawOval(tetra);
            return LastPoint;
        }

        private void _DrawOval(Quad tetra)
        {
            Point mt = tetra.UpperLeft + (tetra.UpperRight - tetra.UpperLeft) * 0.5f;
            Point mr = tetra.UpperRight + (tetra.LowerRight - tetra.UpperRight) * 0.5f;
            Point mb = tetra.LowerLeft + (tetra.LowerRight - tetra.LowerLeft) * 0.5f;
            Point ml = tetra.LowerLeft + (tetra.LowerLeft - tetra.UpperLeft) * 0.5f;

            if (!(LastPoint == ml))
            {
                Point t = ml * IPctm;
                DrawCont += $"{t.X} {t.Y} m\n";
                LastPoint = ml;
            }

            DrawCurve(ml, tetra.LowerLeft, mb);
            DrawCurve(mb, tetra.LowerRight, mr);
            DrawCurve(mr, tetra.UpperRight, mt);
            DrawCurve(mt, tetra.UpperLeft, ml);
            UpdateRect(tetra.Rect);
            LastPoint = ml;
        }

        public Point DrawPolyline(Point[] points)
        {
            for (int i = 0; i < points.Length; i++)
            {
                if (i == 0)
                {
                    if (!(LastPoint == points[i]))
                    {
                        Point t = points[i] * IPctm;
                        DrawCont += $"{t.X} {t.Y} m\n";
                        LastPoint = points[i];
                    }
                }
                else
                {
                    Point t = points[i] * IPctm;
                    DrawCont += $"{t.X} {t.Y} m\n";
                }
                UpdateRect(points[i]);
            }
            LastPoint = points[points.Length - 1];
            return LastPoint;
        }

        /// <summary>
        /// Draw a Quad.
        /// </summary>
        /// <param name="quad"></param>
        /// <returns></returns>
        public Point DrawQuad(Quad quad)
        {
            Point[] points = new Point[5] { quad.UpperLeft, quad.LowerLeft, quad.LowerRight, quad.UpperRight, quad.UpperLeft };
            return DrawPolyline(points);
        }

        /// <summary>
        /// Draw a rectangle.
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public Point DrawRect(Rect rect)
        {
            Point t = rect.BottomLeft * IPctm;
            DrawCont += $"{t.X} {t.Y} {rect.Width} {rect.Height} re\n";
            UpdateRect(rect);
            LastPoint = rect.TopLeft;
            return LastPoint;
        }

        /// <summary>
        /// Draw a squiggly line from p1 to p2.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="breadth"></param>
        /// <returns></returns>
        public Point DrawSquiggle(Point p1, Point p2, int breadth = 2)
        {
            Point s = p2 - p1;
            float rad = s.Abs();
            int cnt = 4 * (int)Math.Round(rad / (4 * breadth), 0);
            if (cnt < 4)
                throw new Exception("points too close");
            float mb = rad / cnt;
            Matrix matrix = Utils.HorMatrix(p1, p2);
            Matrix iMat = ~matrix;
            float k = 2.4142135623765633f;

            List<Point> points = new List<Point>();
            int i;
            for (i = 0; i < cnt; i++)
            {
                Point p;
                if (i % 4 == 1)
                    p = (new Point(i, -k) * mb);
                else if (i % 4 == 3)
                    p = (new Point(i, k) * mb);
                else
                    p = (new Point(i, 0) * mb);
                points.Append(p * iMat);
            }

            points.Insert(0, p1);
            points.Append(p2);
            cnt = points.Count;
            i = 0;

            while ((i + 2) < cnt)
            {
                DrawCurve(points[i], points[i + 2], points[i + 2]);
                i += 2;
            }

            return p2;
        }

        public Point DrawZigzag(Point p1, Point p2, float breadth = 2.0f)
        {
            Point s = p2 - p1;
            float rad = s.Abs();
            int cnt = 4 * (int)(Math.Round(rad / (4 * breadth)));
            if (cnt < 4)
                throw new Exception("points too close");
            float mb = rad / cnt;
            Matrix matrix = Utils.HorMatrix(p1, p2);
            Matrix iMat = ~matrix;
            
            List<Point> points = new List<Point>();
            for (int i = 1; i < cnt; i++)
            {
                Point p;
                if (i % 4 == 1)
                    p = (new Point(i, -1) * mb);
                else if (i % 4 == 3)
                    p = (new Point(i, 1) * mb);
                else continue;
                points.Append(p * iMat);
            }
            points.Insert(0, p1);
            points.Append(p2);
            DrawPolyline(points.ToArray());
            
            return p2;
        }

        public void Finish(float width = 1.0f, float[] color = null, float[] fill = null, int lineCap = 0,
            int lineJoin = 0, string dashes = null, bool evenOdd = false, float[] morph = null, bool closePath = true,
            float fillOpacity = 1.0f, float strokeOpacity = 1.0f, int oc = 0)
        {

        }
    }
}
