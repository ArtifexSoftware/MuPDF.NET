using System.Text;

namespace MuPDF.NET
{
    public class Shape
    {
        static Shape()
        {
            if (!File.Exists("mupdfcpp64.dll"))
                Utils.LoadEmbeddedDll();
        }

        public Page Page { get; set; }

        public Document Doc { get; set; }

        public float Height { get; set; }

        public float Width { get; set; }

        public float X { get; set; }

        public float Y { get; set; }

        public Matrix Pctm { get; set; }

        public Matrix IPctm { get; set; }

        public string DrawCont { get; set; }

        public string TextCont { get; set; }

        public string TotalCont { get; set; }

        public Point LastPoint { get; set; }

        public Rect Rect { get; set; }

        public Shape(Page page)
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
        /// <returns>number of lines inserted.</returns>
        public int InsertText(
            Point point,
            List<string> buffer,
            string fontName,
            string fontFile,
            float fontSize = 11,
            float lineHeight = 0,
            bool setSimple = false,
            int encoding = 0,
            float[] color = null,
            float[] fill = null,
            int renderMode = 0,
            float borderWidth = 0.05f,
            int rotate = 0,
            Morph morph = null,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0
            )
        {
            if (string.IsNullOrEmpty(fontName) || string.IsNullOrEmpty(fontFile))
                throw new Exception("should include fontName and fontFile.");

            return _InsertText(point, buffer, fontSize, lineHeight, fontName, fontFile, setSimple, encoding,
                color, fill, renderMode, borderWidth, rotate, morph, strokeOpacity, fillOpacity, oc);
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
        /// <returns>number of lines inserted.</returns>
        public int InsertText(
            Point point,
            string buffer,
            string fontName,
            string fontFile,
            float fontSize = 11,
            float lineHeight = 0,
            bool setSimple = false,
            int encoding = 0,
            float[] color = null,
            float[] fill = null,
            int renderMode = 0,
            float borderWidth = 0.05f,
            int rotate = 0,
            Morph morph = null,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0
            )
        {
            string[] list = buffer.Split('\n');
            if (string.IsNullOrEmpty(fontName) || string.IsNullOrEmpty(fontFile))
                throw new Exception("should include fontName and fontFile.");

            return _InsertText(point, new List<string>(list), fontSize, lineHeight, fontName, fontFile, setSimple, encoding,
                color, fill, renderMode, borderWidth, rotate, morph, strokeOpacity, fillOpacity, oc);
        }

        internal int _InsertText(
            Point point,
            List<string> buffer,
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
            Morph morph = null,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0
            )
        {
            List<string> text = buffer;
            if (text.Count == 0)
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
            FontInfo fontInfo = Utils.CheckFontInfo(Doc, xref);
            
            int ordering = fontInfo.Ordering;
            bool simple = fontInfo.Simple;
            string bfName = fontInfo.Name;
            float ascender = fontInfo.Ascender;
            float descender = fontInfo.Descender;
            float lheight = 0;

            if (lineHeight != 0)
                lheight = fontSize * lineHeight;
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
                if (simple && !(bfName == "Symbol" || bfName == "ZapfDingbats"))
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
            
            bool morphing = (morph != null);
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
                Matrix m1 = new Matrix(1, 0, 0, 1, morph.P.X + X, height - morph.P.Y - Y);
                Matrix mat = ~m1 * morph.M * m1;
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

        public void Commit(bool overlay = true)
        {
            TotalCont += this.TextCont;
            byte[] bTotal = Encoding.UTF8.GetBytes(TotalCont);
            if (TotalCont != "")
            {
                int xref = Utils.InsertContents(Page, Encoding.UTF8.GetBytes(" "), overlay ? 1 : 0);
                Doc.UpdateStream(xref, bTotal);
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
            if (LastPoint == null || !(LastPoint.X == p1.X && LastPoint.Y == p1.Y))
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
            LastPoint = p4;
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
        public Point DrawSector(Point center, Point point, float beta, bool fullSector = true)
        {
            string l3 = "{0} {1} m\n";
            string l4 = "{0} {1} {2} {3} {4} {5} c\n";
            string l5 = "{0} {1} l\n";
            double betar = ((-beta) * Math.PI / 180);
            double w360 = (Math.Sign(betar) * 360.0f * (Math.PI / 180) * -1);
            double w90 = (Math.Sign(betar) * 90.0f * (Math.PI / 180));
            double w45 = w90 / 2;
            while (Math.Abs(betar) > 2 * Math.PI)
                betar += w360;

            if (LastPoint == null || !(LastPoint.X == point.X && LastPoint.Y == point.Y))
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

            double alfa = HorizontalAngle(center, point);
            while (Math.Abs(betar) > Math.Abs(w90))
            {
                float q1 = c.X + (float)(Math.Cos(alfa + w90) * rad);
                float q2 = c.Y + (float)(Math.Sin(alfa + w90) * rad);
                q = new Point(q1, q2);
                float r1 = c.X + (float)(Math.Cos(alfa + w45) * rad / Math.Cos(w45));
                float r2 = c.Y + (float)(Math.Sin(alfa + w45) * rad / Math.Cos(w45));
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
                double beta2 = betar / 2.0f;
                double q1 = c.X + (Math.Cos(alfa + betar) * rad);
                double q2 = c.Y + (Math.Sin(alfa + betar) * rad);
                q = new Point((float)q1, (float)q2);
                double r1 = c.X + Math.Cos(alfa + beta2) * rad / Math.Cos(beta2);
                double r2 = c.Y + Math.Sin(alfa + beta2) * rad / Math.Cos(beta2);
                Point r = new Point((float)r1, (float)r2);

                double kappah = (1 - Math.Cos(beta2)) * 4 / 3 / (r - q).Abs();
                double kappa = kappah * (p - q).Abs() / (1 - Math.Cos(betar));
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
            Console.WriteLine(DrawCont);
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
                Rect.X1 = Math.Max(Rect.X1, x.X);
                Rect.Y1 = Math.Max(Rect.Y1, x.Y);
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
                Rect.X1 = Math.Max(Rect.X1, x.X1);
                Rect.Y1 = Math.Max(Rect.Y1, x.Y1);
            }
        }

        public static double HorizontalAngle(Point c, Point p)
        {
            Point s = (p - c).Unit;
            double alfa = Math.Asin(Math.Abs(s.Y));
            if (s.X < 0)
            {
                if (s.Y <= 0)
                    alfa = -((float)Math.PI - alfa);
                else
                    alfa = (float)Math.PI - alfa;
            }
            else
            {
                if (s.Y >= 0) { }
                else alfa = -alfa;
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
            if (LastPoint == null || !(LastPoint.X == p1.X && LastPoint.Y == p1.Y))
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
            Point ml = tetra.UpperLeft + (tetra.LowerLeft - tetra.UpperLeft) * 0.5f;

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
        public Point DrawRect(Rect rect, float radius)
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
        public Point DrawSquiggle(Point p1, Point p2, float breadth = 2)
        {
            Point s = p2 - p1;
            float rad = s.Abs();
            int cnt = 4 * Convert.ToInt32(Math.Round(rad / (4 * breadth), 0));
            if (cnt < 4)
                throw new Exception("points too close");
            float mb = rad / cnt;
            
            Matrix matrix = Utils.HorMatrix(p1, p2);
            Matrix iMat = ~matrix;
            float k = 2.4142135623765633f;
            
            List<Point> points = new List<Point>();

            int i;
            for (i = 1; i < cnt; i++)
            {
                Point p;
                if (i % 4 == 1)
                    p = (new Point(i, -k)) * mb;
                else if (i % 4 == 3)
                    p = (new Point(i, k)) * mb;
                else
                    p = (new Point(i, 0)) * mb;
                points.Add(p * iMat);
            }
            points = (new List<Point>() { p1}).Concat(points).Concat(new List<Point>() { p2 }).ToList(); 
            cnt = points.Count;
            i = 0;
            
            while ((i + 2) < cnt)
            {
                DrawCurve(points[i], points[i + 1], points[i + 2]);
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

        /// <summary>
        /// Finish the current drawing segment.
        /// Apply colors, opacity, dashes, line style and width, or morphing.Also whether to close the path by connecting last to first point.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="color"></param>
        /// <param name="fill"></param>
        /// <param name="lineCap"></param>
        /// <param name="lineJoin"></param>
        /// <param name="dashes"></param>
        /// <param name="evenOdd">request the “even-odd rule” for filling operations. Default is False, so that the “nonzero winding number rule” is used.</param>
        /// <param name="morph">morph the text or the compound drawing around some arbitrary Point fixpoint by applying Matrix matrix to it. This implies that fixpoint is a fixed point of this operation: it will not change its position. Default is no morphing (None). The matrix can contain any values in its first 4 components, matrix.e == matrix.f == 0 must be true, however. This means that any combination of scaling, shearing, rotating, flipping, etc. is possible, but translations are not.</param>
        /// <param name="closePath"></param>
        /// <param name="fillOpacity">(new in v1.18.1) set transparency for fill colors. Default is 1 (intransparent).</param>
        /// <param name="strokeOpacity">(new in v1.18.1) set transparency for stroke colors. Value < 0 or > 1 will be ignored. Default is 1 (intransparent).</param>
        /// <param name="oc">(new in v1.18.4) the xref number of an OCG or OCMD to make this drawing conditionally displayable.</param>
        public void Finish(
            float width = 1.0f,
            float[] color = null,
            float[] fill = null,
            int lineCap = 0,
            int lineJoin = 0,
            string dashes = null,
            bool evenOdd = false,
            Morph morph = null,
            bool closePath = true,
            float fillOpacity = 1.0f,
            float strokeOpacity = 1.0f,
            int oc = 0
            )
        {
            if (string.IsNullOrEmpty(DrawCont))
                return;
            if (color == null)
                color = new float[]{ 0f, };
            if (width == 0)
                color = null;
            else if (color == null)
                width = 0;

            string colorStr = Utils.GetColorCode(color, "c");
            string fillStr = Utils.GetColorCode(fill, "f");

            string optCont = Page.GetOptionalContent(oc);
            string emc = "";
            if (!string.IsNullOrEmpty(optCont))
            {
                DrawCont = $"/OC /{optCont} BDC\n" + DrawCont;
                emc = "EMC\n";
            }

            string alpha = Page.SetOpacity(CA: strokeOpacity, ca: fillOpacity);
            if (!string.IsNullOrEmpty(alpha))
                DrawCont = $"/{alpha} gs\n" + DrawCont;
            if (width != 1 && width != 0)
                DrawCont += $"{width} w\n";

            if (lineCap != 0)
                DrawCont = $"{lineCap} J\n" + DrawCont;
            if (lineJoin != 0)
                DrawCont = $"{lineJoin} j\n" + DrawCont;

            if (!string.IsNullOrEmpty(dashes) || dashes != "[] 0")
                DrawCont = $"{dashes} d\n" + DrawCont;

            if (closePath)
            {
                DrawCont += "h\n";
                LastPoint = null;
            }

            if (color != null)
                DrawCont += colorStr;

            if (fill != null)
            {
                DrawCont += fillStr;
                if (color != null)
                {
                    if (!evenOdd)
                    {
                        DrawCont += "B\n";
                    }
                    else
                    {
                        DrawCont += "B*\n";
                    }
                }
                else
                {
                    if (!evenOdd)
                        DrawCont += "f\n";
                    else
                        DrawCont += "f*\n";
                }
            }
            else
            {
                DrawCont += "S\n";
            }

            DrawCont += emc;
            if (morph != null)
            {
                Matrix m1 = new Matrix(1, 0, 0, 1, morph.P.X + X, Height - morph.P.Y - Y);
                Matrix mat = ~m1 * morph.M * m1;
                DrawCont = $"{mat.A} {mat.B} {mat.C} {mat.D} {mat.E} {mat.F} cm\n" + DrawCont;
            }

            TotalCont += "\nq\n" + DrawCont + "Q\n";
            DrawCont = "";
            LastPoint = null;
            return;
        }

        /// <summary>
        /// Insert text into a given rectangle.
        /// </summary>
        /// <param name="rect">the textbox to fill</param>
        /// <param name="buffer">text to be inserted</param>
        /// <param name="fontSize">font size</param>
        /// <param name="lineHeight">overwrite the font property</param>
        /// <param name="fontName">a Base-14 font, font name or '/name'</param>
        /// <param name="fontFile">name of a font file</param>
        /// <param name="setSimple"></param>
        /// <param name="encoding"></param>
        /// <param name="color">RGB stroke color triple</param>
        /// <param name="fill">RGB fill color triple</param>
        /// <param name="expandTabs">handles tabulators with string function</param>
        /// <param name="align">left, center, right, justified</param>
        /// <param name="renderMode">text rendering control</param>
        /// <param name="borderWidth">thickness of glyph borders</param>
        /// <param name="rotate">0, 90, 180, or 270 degrees</param>
        /// <param name="morph">morph box with a matrix and a fixpoint</param>
        /// <param name="strokeOpacity"></param>
        /// <param name="fillOpacity"></param>
        /// <param name="oc"></param>
        /// <returns>unused or deficit rectangle area (float)</returns>
        public float InsertTextbox(
            Rect rect,
            List<string> buffer,
            float fontSize = 11,
            float lineHeight = 0,
            string fontName = "helv",
            string fontFile = null,
            bool setSimple = false,
            int encoding = 0,
            float[] color = null,
            float[] fill = null,
            int expandTabs = 1,
            int align = 1,
            int renderMode = 0,
            float borderWidth = 1.0f,
            int rotate = 0,
            Morph morph = null,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0
            )
        {
            return _InsertTextbox(rect, buffer, fontSize, lineHeight, fontName, fontFile, setSimple, encoding,
                color, fill, expandTabs, align, renderMode, borderWidth, rotate, morph, strokeOpacity, fillOpacity, oc);
        }

        /// <summary>
        /// Insert text into a given rectangle.
        /// </summary>
        /// <param name="rect">the textbox to fill</param>
        /// <param name="buffer">text to be inserted</param>
        /// <param name="fontSize">font size</param>
        /// <param name="lineHeight">overwrite the font property</param>
        /// <param name="fontName">a Base-14 font, font name or '/name'</param>
        /// <param name="fontFile">name of a font file</param>
        /// <param name="setSimple"></param>
        /// <param name="encoding"></param>
        /// <param name="color">RGB stroke color triple</param>
        /// <param name="fill">RGB fill color triple</param>
        /// <param name="expandTabs">handles tabulators with string function</param>
        /// <param name="align">left, center, right, justified</param>
        /// <param name="renderMode">text rendering control</param>
        /// <param name="borderWidth">thickness of glyph borders</param>
        /// <param name="rotate">0, 90, 180, or 270 degrees</param>
        /// <param name="morph">morph box with a matrix and a fixpoint</param>
        /// <param name="strokeOpacity"></param>
        /// <param name="fillOpacity"></param>
        /// <param name="oc"></param>
        /// <returns>unused or deficit rectangle area (float)</returns>
        public float InsertTextbox(
            Rect rect,
            string buffer,
            float fontSize = 11,
            float lineHeight = 0,
            string fontName = "helv",
            string fontFile = null,
            bool setSimple = false,
            int encoding = 0,
            float[] color = null,
            float[] fill = null,
            int expandTabs = 1,
            int align = 1,
            int renderMode = 0,
            float borderWidth = 0.05f,
            int rotate = 0,
            Morph morph = null,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0
            )
        {
            string[] list = buffer.Split("\n");
            return _InsertTextbox(rect, new List<string>(list), fontSize, lineHeight, fontName, fontFile, setSimple, encoding,
                color, fill, expandTabs, align, renderMode, borderWidth, rotate, morph, strokeOpacity, fillOpacity, oc);
        }

        internal float _InsertTextbox(
            Rect rect,
            List<string> buffer,
            float fontSize = 11,
            float lineHeight = 0,
            string fontName = "helv",
            string fontFile = null,
            bool setSimple = false,
            int encoding = 0,
            float[] color = null,
            float[] fill = null,
            int expandTabs = 1,
            int align = 1,
            int renderMode = 0,
            float borderWidth = 0.05f,
            int rotate = 0,
            Morph morph = null,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0
            )
        {
            if (rect.IsEmpty || rect.IsInfinite)
                throw new Exception("text box must be finite and not empty");

            string colorStr = Utils.GetColorCode(color, "c");
            string fillStr = Utils.GetColorCode(fill, "f");
            
            
            if (fill == null && renderMode == 0)
            {
                fill = color;
                fillStr = Utils.GetColorCode(color, "f");
            }

            string optCont = Page.GetOptionalContent(oc);
            string bdc = "", emc = "";
            if (!string.IsNullOrEmpty(optCont))
            {
                bdc = $"/OC /{optCont} BDC\n";
                emc = "EMC\n";
            }

            string alpha = Page.SetOpacity(CA: strokeOpacity, ca: fillOpacity);
            if (string.IsNullOrEmpty(alpha))
                alpha = "";
            else
                alpha = $"/{alpha} gs\n";

            if (rotate % 90 != 0)
                throw new Exception("rotate must be multiple of 90");

            int rot = rotate;
            while (rot < 0)
                rot += 360;
            rot = rot % 360;

            if (buffer.Count == 0 )
                return (rot == 0 || rot == 180) ? rect.Height : rect.Width;

            string cmp90 = "0 1 -1 0 0 0 cm\n";
            string cmm90 = "0 -1 1 0 0 0 cm\n";
            string cm180 = "-1 0 0 -1 0 0 cm\n";
            float height = this.Height;

            string fname = fontName;
            if (fname.StartsWith("/"))
                fname = fname.Substring(1);

            int xref = Page.InsertFont(fontName: fname, fontFile: fontFile, encoding: encoding, setSimple: setSimple);
            FontInfo fontInfo = Utils.CheckFontInfo(Doc, xref);
            
            if (fontInfo == null)
                throw new Exception("no found font info");
            
            int ordering = fontInfo.Ordering;
            bool simple = fontInfo.Simple;
            List<(int, double)> glyphs = fontInfo.Glyphs;
            string bfName = fontInfo.Name;
            float asc = fontInfo.Ascender;
            float des = fontInfo.Descender;
            float lheightFactor;
            if (lineHeight != 0)
                lheightFactor = lineHeight;
            else if (asc - des <= 1)
                lheightFactor = 1.2f;
            else
                lheightFactor = asc - des;
            float lheight = fontSize * lheightFactor;
            string t0 = string.Join('\n', buffer);
            int maxCode = 0;
            foreach (char c in t0)
                maxCode = maxCode < Convert.ToInt32(c) ? Convert.ToInt32(c) : maxCode;

            string t1 = "";
            if (simple && maxCode > 255)
                foreach (char c in t0)
                    t1 += Convert.ToInt32(c) < 256 ? c : '?';

            string[] t2 = string.IsNullOrEmpty(t1) ? t0.Split("\n") : t1.Split("\n");
            glyphs = Utils.GetCharWidths(Doc, xref, maxCode + 1);

            List<(int, double)> tj_glyphs;
            if (simple && !(bfName == "Symbol" || bfName == "ZapfDingbats"))
                tj_glyphs = null;
            else
                tj_glyphs = glyphs;

            float PixLen(string x)
            {
                if (ordering < 0)
                {
                    double sum = 0;
                    foreach (char c in x)
                        sum += glyphs[Convert.ToInt32(c)].Item2;
                    return (float)(sum * fontSize);
                }
                else
                {
                    return x.Length * fontSize;
                }
            }

            float blen;
            if (ordering < 0)
                blen = (float)(glyphs[32].Item2 * fontSize);
            else
                blen = fontSize;

            string text = "";
            string cm = "";
            if (morph != null)
            {
                Matrix m1 = new Matrix(1, 0, 0, 1, morph.P.X + X, Height - morph.P.Y - Y);
                Matrix mat = ~m1 * morph.M * m1;
                cm = $"{mat.A} {mat.B} {mat.C} {mat.D} {mat.E} {mat.F} cm\n";
            }

            int progr = 1;
            Point cPnt = new Point(0, fontSize * asc);
            Point point = new Point();
            float pos = 0, maxWidth = 0, maxHeight = 0;
            if (rot == 0)
            {
                point = rect.TopLeft + cPnt;
                maxWidth = rect.Width;
                maxHeight = rect.Height;
            }
            else if (rot == 90)
            {
                cPnt = new Point(fontSize * asc, 0);
                point = rect.BottomLeft + cPnt;
                maxWidth = rect.Height;
                maxHeight = rect.Width;
                cm += cmp90;
            }
            else if (rot == 180)
            {
                cPnt = -(new Point(0, fontSize * asc));
                point = rect.BottomRight + cPnt;
                maxWidth = rect.Width;
                maxHeight = rect.Height;
                cm += cm180;
            }
            else
            {
                cPnt = -(new Point(fontSize * asc, 0));
                point = rect.TopRight + cPnt;
                pos = point.X + X;
                maxWidth = rect.Height;
                progr = -1;
                maxHeight = rect.Width;
                cm += cmm90;
            }

            List<bool> justTab = new List<bool>();
            for (int i = 0; i < t2.Length; i++)
            {
                string[] line_t = t2[i].Replace("\t", new string(' ', expandTabs)).Split(" ");
                string lbuff = "";
                float rest = maxWidth;

                foreach (string word in line_t)
                {
                    float pl_w = PixLen(word);
                    if (rest >= pl_w)
                    {
                        lbuff += word + " ";
                        rest -= (pl_w + blen);
                        continue;
                    }
                    if (!string.IsNullOrEmpty(lbuff))
                    {
                        lbuff = lbuff.TrimEnd() + "\n";
                        text += lbuff;
                        justTab.Add(true);
                    }
                    lbuff = "";
                    rest = maxWidth;
                    if (pl_w <= maxWidth)
                    {
                        lbuff = word + " ";
                        rest = maxWidth - pl_w - blen;
                        continue;
                    }

                    if (justTab.Count > 0)
                        justTab[justTab.Count - 1] = false;
                    foreach (char c in word)
                    {
                        if (PixLen(lbuff) <= (maxWidth - PixLen(Convert.ToString(c))))
                            lbuff += c;
                        else
                        {
                            lbuff += "\n";
                            text += lbuff;
                            justTab.Add(false);
                            lbuff = Convert.ToString(c);
                        }
                    }

                    lbuff += " ";
                    rest = maxWidth - PixLen(lbuff);
                }
                if (!string.IsNullOrEmpty(lbuff))
                {
                    text += lbuff.TrimEnd();
                    justTab.Add(false);
                }
                if (i < t2.Count() - 1)
                {
                    text += "\n";
                }
            }
            if (text.EndsWith("\n"))
                text = text.Substring(0, text.Length - 1);

            int lbCount = text.Split('\n').Length;

            float more = (lheight * lbCount) - des * fontSize - maxHeight;
            if (more > Utils.FLT_EPSILON)
                return (-1) * more;
            
            more = Math.Abs(more);
            if (more < Utils.FLT_EPSILON)
                more = 0;

            string nres = $"\nq\n{bdc}{alpha}BT\n" + cm;
            string template = "1 0 0 1 {0} {1} Tm /{2} {3} Tf ";
            string[] text_t = text.Split("\n");

            justTab[justTab.Count - 1] = false;
            
            for (int i = 0; i < text_t.Length; i++)
            {
                float pl = maxWidth - PixLen(text_t[i]);
                Point pnt = point + cPnt * (i * lheightFactor);
                float spacing = 0;
                if (align == 1)
                {
                    if (rot == 0 && rot == 180)
                        pnt = pnt + new Point(pl / 2, 0) * progr;
                    else
                        pnt = pnt - new Point(0, pl / 2) * progr;
                }
                else if (align == 2)
                {
                    if (rot == 0 || rot == 180)
                        pnt = pnt + new Point(pl, 0) * progr;
                    else
                        pnt = pnt - new Point(0, pl) * progr;
                }
                else if (align == 3)
                {
                    int spaces = text_t[i].Count(c => c == ' ');
                    if (spaces > 0 && justTab[i])
                        spacing = pl / spaces;
                }
                float top = height - pnt.Y - Y;
                float left = pnt.X + X;
                if (rot == 90)
                {
                    left = height - pnt.Y - Y;
                    top = -pnt.X - X;
                }
                else if (rot == 270)
                {
                    left = -height + pnt.Y + Y;
                    top = pnt.X + X;
                }
                else if (rot == 180)
                {
                    left = -pnt.X - X;
                    top = -height + pnt.Y + Y;
                }

                nres += string.Format(template, left, top, fname, fontSize);
                if (renderMode > 0)
                    nres += $"{renderMode} Tr ";
                if (align == 3)
                    nres += $"{spacing} Tw ";

                if (color != null)
                    nres += colorStr;
                if (fill != null)
                    nres += fillStr;
                if (borderWidth != 1)
                    nres += $"{borderWidth} w ";
                nres += $"{Utils.GetTJstr(text_t[i], tj_glyphs, simple, ordering)}TJ\n";
            }
            nres += $"ET\n{emc}Q\n";

            TextCont += nres;
            UpdateRect(rect);
            return more;
        }
    }
}
