using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MuPDF.NET
{
    /// <summary>
    /// PDF-only helper for batching vector drawings and text onto a <see cref="Page"/>.
    /// </summary>
    /// <remarks>
    /// <para>Create via <see cref="Page.NewShape"/>. Each draw method appends PDF operators to
    /// <see cref="DrawCont"/>; call <see cref="Finish"/> to apply stroke/fill/morph attributes, then
    /// <see cref="Commit"/> to write <see cref="TotalCont"/> into the page contents stream.</para>
    /// <para><see cref="InsertText"/> and <see cref="InsertTextbox"/> append to <see cref="TextCont"/> and
    /// implicitly finish the current path; only <see cref="Commit"/> is required afterward.</para>
    /// <para>Ports PyMuPDF <c>Shape</c>.</para>
    /// </remarks>
    public class Shape : IDisposable
    {
        private bool _disposed;
        private Page _page;
        private Document _doc;
        private float _width;
        private float _height;
        private StringBuilder _contents;
        private StringBuilder _totalContents;
        private Rect _rect;
        private Point _lastPoint;
        private Point _firstPoint;
        private int _drawCount;
        private int _pathCount;
        private readonly StringBuilder _textCont;
        private readonly float _shapeX;
        private readonly float _shapeY;
        private readonly Matrix _pctm;
        private readonly Matrix _ipctm;

        /// <summary>Bounding box of all drawings and textboxes (null after <see cref="Commit"/> until next draw).</summary>
        public Rect Rect => _rect;

        /// <summary>Owning PDF page.</summary>
        public Page Page => _page;

        /// <summary>Document containing the page.</summary>
        public Document Doc => _doc;

        /// <summary>Page height in points (copy at construction).</summary>
        public float Height => _height;

        /// <summary>Page width in points (copy at construction).</summary>
        public float Width => _width;

        /// <summary>Crop-box X offset used when transforming to PDF coordinates.</summary>
        public float X => _shapeX;

        /// <summary>Crop-box Y offset used when transforming to PDF coordinates.</summary>
        public float Y => _shapeY;

        /// <summary>Page transformation matrix (<c>page.transformation_matrix</c>).</summary>
        public Matrix Pctm => _pctm;

        /// <summary>Inverse of <see cref="Pctm"/>.</summary>
        public Matrix IPctm => _ipctm;

        /// <summary>Raw PDF path operators since the last <see cref="Finish"/>.</summary>
        public string DrawCont
        {
            get => _contents.ToString();
            set
            {
                _contents.Clear();
                if (!string.IsNullOrEmpty(value))
                    _contents.Append(value);
            }
        }

        /// <summary>Accumulated PDF text operators (merged on <see cref="Commit"/>).</summary>
        public string TextCont
        {
            get => _textCont.ToString();
            set
            {
                _textCont.Clear();
                if (!string.IsNullOrEmpty(value))
                    _textCont.Append(value);
            }
        }

        /// <summary>Finished draw segments plus text, written by <see cref="Commit"/>.</summary>
        public string TotalCont
        {
            get => _totalContents.ToString();
            set
            {
                _totalContents.Clear();
                if (!string.IsNullOrEmpty(value))
                    _totalContents.Append(value);
            }
        }

        /// <summary>Current path end point; null after <see cref="Finish"/> or <see cref="Commit"/>.</summary>
        public Point LastPoint => _lastPoint;

        /// <summary>Count of draw primitives since the last <see cref="Finish"/>.</summary>
        public int DrawCount => _drawCount;

        /// <summary>
        /// Creates a shape for a PDF page (throws if the page is not PDF).
        /// </summary>
        /// <param name="page">Target page; use <see cref="Page.NewShape"/> as the usual entry point.</param>
        /// <exception cref="ArgumentNullException"><paramref name="page"/> is null.</exception>
        /// <exception cref="ValueErrorException">Page has no parent or is not a PDF.</exception>
        public Shape(Page page)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
            _doc = page.Parent ?? throw new ValueErrorException("orphaned object: parent is None");
            if (!_doc.IsPdf)
                throw new ValueErrorException("is no PDF");
            _width = (float)page.MediaBoxSize.X;
            _height = (float)page.MediaBoxSize.Y;
            var cp = page.CropBoxPosition;
            _shapeX = (float)cp.X;
            _shapeY = (float)cp.Y;
            _pctm = new Matrix(page.TransformationMatrix);
            _ipctm = _pctm.Inverted() ?? Matrix.Identity;
            _contents = new StringBuilder();
            _totalContents = new StringBuilder();
            _textCont = new StringBuilder();
            _rect = new Rect();
            _lastPoint = null;
            _firstPoint = null;
        }

        // ─── Drawing Primitives ─────────────────────────────────────────

        /// <summary>
        /// Angle in radians from <paramref name="c"/> to <paramref name="p"/> (PyMuPDF <c>horizontal_angle</c>).
        /// </summary>
        /// <param name="c">Center or origin point.</param>
        /// <param name="p">Target point.</param>
        public static float HorizontalAngle(Point c, Point p)
        {
            var s = (p - c).Unit;
            float alfa = (float)Math.Asin(Math.Abs(s.Y));
            if (s.X < 0)
            {
                if (s.Y <= 0)
                    alfa = -(float)(Math.PI - alfa);
                else
                    alfa = (float)Math.PI - alfa;
            }
            else if (s.Y < 0)
                alfa = -alfa;
            return alfa;
        }

        private Point Tf(Point p) => p * _ipctm;

        private static bool SamePoint(Point a, Point b) =>
            a != null && b != null && Math.Abs(a.X - b.X) < Constants.Epsilon && Math.Abs(a.Y - b.Y) < Constants.Epsilon;

        private void AppendM(Point p)
        {
            var t = Tf(p);
            _contents.Append(Helpers.FormatPdfReals(t.X, t.Y)).Append(" m\n");
        }

        private void AppendL(Point p)
        {
            var t = Tf(p);
            _contents.Append(Helpers.FormatPdfReals(t.X, t.Y)).Append(" l\n");
        }

        private void AppendC(Point p2, Point p3, Point p4)
        {
            var t2 = Tf(p2);
            var t3 = Tf(p3);
            var t4 = Tf(p4);
            _contents.Append(Helpers.FormatPdfReals(t2.X, t2.Y, t3.X, t3.Y, t4.X, t4.Y)).Append(" c\n");
        }

        /// <summary>Draw a straight line from <paramref name="p1"/> to <paramref name="p2"/>.</summary>
        /// <param name="p1">Start point.</param>
        /// <param name="p2">End point.</param>
        /// <param name="count">When false, does not increment <see cref="DrawCount"/>.</param>
        /// <returns><paramref name="p2"/>.</returns>
        public Point DrawLine(Point p1, Point p2, bool count = true)
        {
            p1 = new Point(p1);
            p2 = new Point(p2);
            if (!SamePoint(_lastPoint, p1))
            {
                AppendM(p1);
                _lastPoint = p1;
                UpdateRect(p1);
            }
            AppendL(p2);
            UpdateRect(p2);
            _lastPoint = p2;
            if (_firstPoint == null) _firstPoint = p1;
            if (count) _drawCount++;
            return _lastPoint;
        }

        /// <summary>
        /// Draw a rectangle; optional rounded corners via <paramref name="radius"/>.
        /// </summary>
        /// <param name="rect">Rectangle in page coordinates.</param>
        /// <param name="radius">
        /// <c>null</c> for a plain rect; a float in (0, 0.5] as a fraction of min(width, height);
        /// or a two-element sequence <c>(rx, ry)</c> for asymmetric corner radii.
        /// </param>
        /// <returns>Top-left corner of <paramref name="rect"/>.</returns>
        public Point DrawRect(Rect rect, object radius = null)
        {
            var r = new Rect(rect);
            if (radius == null)
            {
                var bl = Tf(r.BottomLeft);
                _contents.Append(Helpers.FormatPdfReals(bl.X, bl.Y, r.Width, r.Height)).Append(" re\n");
                UpdateRect(r);
                _lastPoint = r.TopLeft;
                if (_firstPoint == null) _firstPoint = r.TopLeft;
                _drawCount++;
                return _lastPoint;
            }

            Point px, py;
            float? rad = radius is float f ? f : radius is float dub ? dub : radius is int i ? i : (float?)null;
            if (rad.HasValue)
            {
                float rv = rad.Value;
                if (rv <= 0 || rv > 0.5)
                    throw new ValueErrorException($"bad radius value {radius}.");
                float d = Math.Min(r.Width, r.Height) * rv;
                px = new Point(d, 0);
                py = new Point(0, d);
            }
            else if (radius is IList rl && rl.Count == 2)
            {
                float rx = (float)Convert.ToDouble(rl[0], CultureInfo.InvariantCulture);
                float ry = (float)Convert.ToDouble(rl[1], CultureInfo.InvariantCulture);
                if (Math.Min(rx, ry) <= 0 || Math.Max(rx, ry) > 0.5)
                    throw new ValueErrorException($"bad radius value {radius}.");
                px = new Point(rx * r.Width, 0);
                py = new Point(0, ry * r.Height);
            }
            else
                throw new ValueErrorException($"bad radius value {radius}.");

            Point lp = DrawLine(r.TopLeft + py, r.BottomLeft - py, count: false);
            lp = DrawCurve(lp, r.BottomLeft, r.BottomLeft + px, count: false);
            lp = DrawLine(lp, r.BottomRight - px, count: false);
            lp = DrawCurve(lp, r.BottomRight, r.BottomRight - py, count: false);
            lp = DrawLine(lp, r.TopRight + py, count: false);
            lp = DrawCurve(lp, r.TopRight, r.TopRight - px, count: false);
            lp = DrawLine(lp, r.TopLeft + px, count: false);
            _lastPoint = DrawCurve(lp, r.TopLeft, r.TopLeft + py, count: false);
            UpdateRect(r);
            _drawCount++;
            return _lastPoint;
        }

        internal static int ParseLineCapForPdf(string lineCap)
        {
            if (string.IsNullOrEmpty(lineCap)) return 0;
            return lineCap.ToLowerInvariant() switch
            {
                "round" => 1,
                "square" => 2,
                _ => int.TryParse(lineCap, NumberStyles.Integer, CultureInfo.InvariantCulture, out var j) ? j : 0,
            };
        }

        internal static int ParseLineJoinForPdf(string lineJoin)
        {
            if (string.IsNullOrEmpty(lineJoin)) return 0;
            return lineJoin.ToLowerInvariant() switch
            {
                "round" => 1,
                "bevel" => 2,
                "miter" => 0,
                _ => int.TryParse(lineJoin, NumberStyles.Integer, CultureInfo.InvariantCulture, out var j) ? j : 0,
            };
        }

        internal static string DashesArrayToPdfString(float[] dashes)
        {
            if (dashes == null || dashes.Length == 0) return null;
            return "[" + string.Join(" ", dashes.Select(d => d.ToString("G9", CultureInfo.InvariantCulture))) + "] 0";
        }

        /// <summary>
        /// Draw a circle (shortcut for <c>DrawSector(..., 360, fullSector: false)</c>).
        /// </summary>
        /// <param name="center">Circle center.</param>
        /// <param name="radius">Radius in points (must be positive).</param>
        /// <returns><c>(center.X - radius, center.Y)</c>.</returns>
        public Point DrawCircle(Point center, float radius)
        {
            if (!(radius > Constants.Epsilon))
                throw new ValueErrorException("radius must be positive");
            center = new Point(center);
            var p1 = center - new Point(radius, 0);
            var q = DrawSector(center, p1, 360, fullSector: false, count: false);
            _drawCount++;
            return q;
        }

        /// <summary>Draw an ellipse inside a rectangle (converted to <see cref="Quad"/>).</summary>
        /// <param name="rect">Bounding rectangle.</param>
        /// <returns>Midpoint of the left edge (path start).</returns>
        public Point DrawOval(Rect rect) => DrawOval(new Quad(rect));

        /// <summary>
        /// Draw an ellipse inside a quadrilateral (anti-clockwise from left edge midpoint).
        /// </summary>
        /// <param name="quad">Target quad.</param>
        /// <returns>Midpoint of <c>UL–LL</c> after the path is built.</returns>
        public Point DrawOval(Quad quad)
        {
            var q = new Quad(quad);
            var mt = q.UL + (q.UR - q.UL) * 0.5f;
            var mr = q.UR + (q.LR - q.UR) * 0.5f;
            var mb = q.LL + (q.LR - q.LL) * 0.5f;
            var ml = q.UL + (q.LL - q.UL) * 0.5f;
            if (!SamePoint(_lastPoint, ml))
            {
                AppendM(ml);
                _lastPoint = ml;
            }
            DrawCurve(ml, q.LL, mb, count: false);
            DrawCurve(mb, q.LR, mr, count: false);
            DrawCurve(mr, q.UR, mt, count: false);
            DrawCurve(mt, q.UL, ml, count: false);
            UpdateRect(q.Rect);
            _lastPoint = ml;
            if (_firstPoint == null) _firstPoint = ml;
            _drawCount++;
            return _lastPoint;
        }

        /// <summary>Draw a cubic Bézier from <paramref name="p1"/> to <paramref name="p3"/> via helper <paramref name="p2"/>.</summary>
        /// <param name="p1">Start point.</param>
        /// <param name="p2">Helper point (controls curvature on both segments).</param>
        /// <param name="p3">End point.</param>
        /// <param name="count">When false, does not increment <see cref="DrawCount"/>.</param>
        /// <returns><paramref name="p3"/>.</returns>
        public Point DrawCurve(Point p1, Point p2, Point p3, bool count = true)
        {
            const float kappa = 0.55228474983f;
            p1 = new Point(p1);
            p2 = new Point(p2);
            p3 = new Point(p3);
            var k1 = p1 + (p2 - p1) * kappa;
            var k2 = p3 + (p2 - p3) * kappa;
            var q = DrawBezier(p1, k1, k2, p3, count: false);
            if (count) _drawCount++;
            return q;
        }

        /// <summary>Draw a standard cubic Bézier from <paramref name="p1"/> to <paramref name="p4"/>.</summary>
        /// <param name="p1">Start point.</param>
        /// <param name="p2">First control point.</param>
        /// <param name="p3">Second control point.</param>
        /// <param name="p4">End point.</param>
        /// <param name="count">When false, does not increment <see cref="DrawCount"/>.</param>
        /// <returns><paramref name="p4"/>.</returns>
        public Point DrawBezier(Point p1, Point p2, Point p3, Point p4, bool count = true)
        {
            p1 = new Point(p1);
            p2 = new Point(p2);
            p3 = new Point(p3);
            p4 = new Point(p4);
            if (!SamePoint(_lastPoint, p1))
                AppendM(p1);
            AppendC(p2, p3, p4);
            UpdateRect(p1);
            UpdateRect(p2);
            UpdateRect(p3);
            UpdateRect(p4);
            _lastPoint = p4;
            if (_firstPoint == null) _firstPoint = p1;
            if (count) _drawCount++;
            return p4;
        }

        /// <summary>Draw connected segments through <paramref name="points"/> (length ≥ 1).</summary>
        /// <param name="points">Vertices; close a polygon by repeating the first point at the end.</param>
        /// <param name="count">When false, does not increment <see cref="DrawCount"/>.</param>
        /// <returns>Last point in <paramref name="points"/>.</returns>
        public Point DrawPolyline(Point[] points, bool count = true)
        {
            if (points == null || points.Length == 0)
                throw new ArgumentException("need at least 1 point");
            for (int i = 0; i < points.Length; i++)
            {
                var p = new Point(points[i]);
                if (i == 0)
                {
                    if (!SamePoint(_lastPoint, p))
                    {
                        AppendM(p);
                        _lastPoint = p;
                    }
                }
                else
                    AppendL(p);
                UpdateRect(p);
            }
            _lastPoint = new Point(points[points.Length - 1]);
            if (_firstPoint == null) _firstPoint = new Point(points[0]);
            if (count) _drawCount++;
            return _lastPoint;
        }

        /// <summary>Draw a quad as <c>UL → LL → LR → UR → UL</c>.</summary>
        /// <param name="quad">Quadrilateral in page coordinates.</param>
        /// <returns><see cref="Quad.UL"/>.</returns>
        public Point DrawQuad(Quad quad)
        {
            return DrawPolyline(new[] { quad.UL, quad.LL, quad.LR, quad.UR, quad.UL });
        }

        /// <summary>
        /// Draw a circular sector (pie slice) from <paramref name="point"/> by angle <paramref name="beta"/> degrees.
        /// </summary>
        /// <param name="center">Circle center.</param>
        /// <param name="point">One arc endpoint (radius defined by distance to center).</param>
        /// <param name="beta">Sweep angle in degrees (sign sets direction).</param>
        /// <param name="fullSector">When true, connect arc ends to center (filled pie).</param>
        /// <param name="count">When false, does not increment <see cref="DrawCount"/>.</param>
        /// <returns>The other arc endpoint (use as next <paramref name="point"/> for connected pies).</returns>
        public Point DrawSector(Point center, Point point, float beta, bool fullSector = true, bool count = true)
        {
            center = new Point(center);
            point = new Point(point);
            float betar = -beta * (float)Math.PI / 180.0f;
            float w360 = (float)Math.Sign(betar) * (float)Math.PI * 2 * -1;
            float w90 = (float)Math.Sign(betar) * (float)Math.PI / 2;
            float w45 = w90 / 2;
            while (Math.Abs(betar) > 2 * Math.PI)
                betar += w360;

            if (!SamePoint(_lastPoint, point))
            {
                AppendM(point);
                _lastPoint = point;
            }

            Point Q = new Point(0, 0);
            var C = center;
            var P = point;
            var S = P - C;
            float rad = S.Norm;
            if (!(rad > Constants.Epsilon))
                throw new ValueErrorException("radius must be positive");

            float alfa = HorizontalAngle(center, point);
            while (Math.Abs(betar) > Math.Abs(w90))
            {
                float q1 = C.X + (float)Math.Cos(alfa + w90) * rad;
                float q2 = C.Y + (float)Math.Sin(alfa + w90) * rad;
                Q = new Point(q1, q2);
                float r1 = C.X + (float)Math.Cos(alfa + w45) * rad / (float)Math.Cos(w45);
                float r2 = C.Y + (float)Math.Sin(alfa + w45) * rad / (float)Math.Cos(w45);
                var R = new Point(r1, r2);
                float kappah = (1 - (float)Math.Cos(w45)) * 4 / 3 / (R - Q).Norm;
                float kappa = kappah * (P - Q).Norm;
                var cp1 = P + (R - P) * kappa;
                var cp2 = Q + (R - Q) * kappa;
                AppendC(cp1, cp2, Q);
                betar -= w90;
                alfa += w90;
                P = Q;
            }

            if (Math.Abs(betar) > 1e-3)
            {
                float beta2 = betar / 2;
                float q1 = C.X + (float)Math.Cos(alfa + betar) * rad;
                float q2 = C.Y + (float)Math.Sin(alfa + betar) * rad;
                Q = new Point(q1, q2);
                float r1 = C.X + (float)Math.Cos(alfa + beta2) * rad / (float)Math.Cos(beta2);
                float r2 = C.Y + (float)Math.Sin(alfa + beta2) * rad / (float)Math.Cos(beta2);
                var R = new Point(r1, r2);
                float kappah = (1 - (float)Math.Cos(beta2)) * 4 / 3 / (R - Q).Norm;
                float kappa = kappah * (P - Q).Norm / (1 - (float)Math.Cos(betar));
                var cp1 = P + (R - P) * kappa;
                var cp2 = Q + (R - Q) * kappa;
                AppendC(cp1, cp2, Q);
            }

            if (fullSector)
            {
                AppendM(point);
                AppendL(center);
                AppendL(Q);
            }

            _lastPoint = Q;
            if (_firstPoint == null) _firstPoint = point;
            if (count) _drawCount++;
            return _lastPoint;
        }

        /// <summary>
        /// Draw a wavy line; wave period length is <c>4 * breadth</c> (requires <c>2 * breadth &lt; |p2 - p1|</c>).
        /// </summary>
        /// <param name="p1">Start point.</param>
        /// <param name="p2">End point.</param>
        /// <param name="breadth">Wave amplitude.</param>
        /// <returns><paramref name="p2"/>.</returns>
        public Point DrawSquiggle(Point p1, Point p2, float breadth = 2)
        {
            p1 = new Point(p1);
            p2 = new Point(p2);
            var S = p2 - p1;
            float rad = S.Norm;
            int cnt = 4 * (int)Math.Round(rad / (4 * breadth), MidpointRounding.AwayFromZero);
            if (cnt < 4)
                throw new ValueErrorException("points too close");
            float mb = rad / cnt;
            var matrix = Helpers.UtilHorMatrix(p1, p2);
            var iMat = matrix.Inverted() ?? throw new ValueErrorException("singular matrix");
            const float k = 2.4142135623765633f;
            var points = new List<Point> { p1 };
            for (int i = 1; i < cnt; i++)
            {
                Point p;
                if (i % 4 == 1)
                    p = new Point(i, -k) * mb;
                else if (i % 4 == 3)
                    p = new Point(i, k) * mb;
                else
                    p = new Point(i, 0) * mb;
                points.Add(p * iMat);
            }
            points.Add(p2);
            int n = points.Count;
            int idx = 0;
            while (idx + 2 < n)
            {
                DrawCurve(points[idx], points[idx + 1], points[idx + 2], count: false);
                idx += 2;
            }
            _drawCount++;
            return p2;
        }

        /// <summary>
        /// Draw a zigzag line with the same period rules as <see cref="DrawSquiggle"/>.
        /// </summary>
        /// <param name="p1">Start point.</param>
        /// <param name="p2">End point.</param>
        /// <param name="breadth">Zigzag amplitude.</param>
        /// <returns><paramref name="p2"/>.</returns>
        public Point DrawZigzag(Point p1, Point p2, float breadth = 2)
        {
            p1 = new Point(p1);
            p2 = new Point(p2);
            var S = p2 - p1;
            float rad = S.Norm;
            int cnt = 4 * (int)Math.Round(rad / (4 * breadth), MidpointRounding.AwayFromZero);
            if (cnt < 4)
                throw new ValueErrorException("points too close");
            float mb = rad / cnt;
            var matrix = Helpers.UtilHorMatrix(p1, p2);
            var iMat = matrix.Inverted() ?? throw new ValueErrorException("singular matrix");
            var points = new List<Point>();
            for (int i = 1; i < cnt; i++)
            {
                if (i % 4 == 1)
                    points.Add(new Point(i, -1) * mb * iMat);
                else if (i % 4 == 3)
                    points.Add(new Point(i, 1) * mb * iMat);
            }
            var polyline = new List<Point>(points.Count + 2) { p1 };
            polyline.AddRange(points);
            polyline.Add(p2);
            DrawPolyline(polyline.ToArray(), count: false);
            _drawCount++;
            return p2;
        }

        // ─── Finish / Commit ────────────────────────────────────────────

        /// <summary>
        /// Finalize the current path: stroke/fill, dashes, opacity, optional morph, and append to <see cref="TotalCont"/>.
        /// </summary>
        /// <param name="color">RGB stroke color, or null for no stroke.</param>
        /// <param name="fill">RGB fill color, or null for no fill.</param>
        /// <param name="width">Line width in points (0 forces no stroke).</param>
        /// <param name="lineCap">PDF line cap: 0 butt, 1 round, 2 square.</param>
        /// <param name="lineJoin">PDF line join: 0 miter, 1 round, 2 bevel.</param>
        /// <param name="dashes">PDF dash array string, e.g. <c>[3 2] 0</c>.</param>
        /// <param name="closePath">When true, close subpath with <c>h</c> before painting.</param>
        /// <param name="evenOdd">Use even-odd fill rule when both stroke and fill are set.</param>
        /// <param name="strokeOpacity">Stroke alpha 0–1.</param>
        /// <param name="fillOpacity">Fill alpha 0–1.</param>
        /// <param name="blendMode">Optional PDF blend mode name.</param>
        /// <param name="oc">Optional content group xref (0 = none).</param>
        /// <param name="morphFix">Morph fixed point (with <paramref name="morphMat"/>).</param>
        /// <param name="morphMat">Morph matrix (translation components should be zero).</param>
        public void Finish(
            float[] color = null,
            float[] fill = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            string dashes = null,
            bool closePath = true,
            bool evenOdd = false,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            string blendMode = null,
            int oc = 0,
            Point morphFix = null,
            Matrix morphMat = null)
        {
            if (_contents.Length == 0)
                return;

            if (width == 0)
                color = null;
            else if (color == null)
                width = 0;

            string drawCont = _contents.ToString();

            string optcont = _page._get_optional_content(oc == 0 ? null : oc);
            string emc;
            if (optcont != null)
            {
                drawCont = "/OC /" + optcont + " BDC\n" + drawCont;
                emc = "EMC\n";
            }
            else
                emc = "";

            string alpha = _page._set_opacity(null, strokeOpacity, fillOpacity, blendMode);
            if (alpha != null)
                drawCont = "/" + alpha + " gs\n" + drawCont;

            if (Math.Abs(width - 1f) > 1e-6 && Math.Abs(width) > 1e-6)
                drawCont += width.ToString("G9", CultureInfo.InvariantCulture) + " w\n";

            if (lineCap != 0)
                drawCont = lineCap.ToString(CultureInfo.InvariantCulture) + " J\n" + drawCont;
            if (lineJoin != 0)
                drawCont = lineJoin.ToString(CultureInfo.InvariantCulture) + " j\n" + drawCont;

            if (!string.IsNullOrEmpty(dashes) && dashes != "[] 0")
                drawCont = dashes + " d\n" + drawCont;

            if (closePath)
            {
                drawCont += "h\n";
                _lastPoint = null;
            }

            string colorStr = Helpers.ColorCode(color, "c");
            string fillStr = Helpers.ColorCode(fill, "f");
            if (color != null)
                drawCont += colorStr;
            if (fill != null)
            {
                drawCont += fillStr;
                if (color != null)
                    drawCont += evenOdd ? "B*\n" : "B\n";
                else
                    drawCont += evenOdd ? "f*\n" : "f\n";
            }
            else
                drawCont += "S\n";

            drawCont += emc;

            if (morphFix != null && morphMat != null && Helpers.CheckMorph(morphFix, morphMat))
            {
                var m1 = new Matrix(1, 0, 0, 1, morphFix.X + _shapeX, _height - morphFix.Y - _shapeY);
                var inv = m1.Inverted() ?? throw new ValueErrorException("singular morph matrix");
                var mat = inv * morphMat * m1;
                drawCont = Helpers.FormatPdfReals(mat.A, mat.B, mat.C, mat.D, mat.E, mat.F) + " cm\n" + drawCont;
            }

            // PyMuPDF: self.totalcont += "\nq\n" + self.draw_cont + "Q\n"
            _totalContents.Append("\nq\n");
            _totalContents.Append(drawCont);
            _totalContents.Append("Q\n");
            _contents.Clear();
            _drawCount = 0;
            _lastPoint = null;
            _firstPoint = null;
            _pathCount++;
        }

        /// <summary>
        /// Writes <see cref="TotalCont"/> and <see cref="TextCont"/> to the page and resets the shape buffers.
        /// </summary>
        /// <param name="overlay">
        /// When true (default), new content is foreground; otherwise background. Calls <see cref="Page.WrapContents"/> when true.
        /// </param>
        /// <remarks>Must be called or nothing is persisted. Clears <see cref="Rect"/>, <see cref="LastPoint"/>, and content buffers.</remarks>
        public void Commit(bool overlay = true)
        {
            // PyMuPDF Shape.commit:
            //     The argument controls whether data appear in foreground (default)
            //     or background."""
            //     CheckParent(self.page)  # doc may have died meanwhile
            //     self.totalcont += self.text_cont
            //     self.totalcont = self.totalcont.encode()
            //     if self.totalcont:
            //         if overlay:
            //             self.page.wrap_contents()  # ensure a balanced graphics state
            //         xref = TOOLS._insert_contents(self.page, b" ", overlay)
            //         self.doc.UpdateStream(xref, self.totalcont)
            //     self.last_point = None  # clean up ...
            //     self.rect = None  #
            //     self.draw_cont = ""  # for potential ...
            //     self.text_cont = ""  # ...
            //     self.totalcont = ""  # re-use
            if (_textCont.Length > 0)
            {
                _totalContents.Append(_textCont);
                _textCont.Clear();
            }
            if (_totalContents.Length == 0) return;
            // CheckParent(self.page)  # doc may have died meanwhile
            if (overlay)
                _page.WrapContents(); // ensure a balanced graphics state
            byte[] content = Encoding.UTF8.GetBytes(_totalContents.ToString());
            // xref = TOOLS._insert_contents(self.page, b" ", overlay)
            int xref = Tools.InsertContents(_page, new byte[] { (byte)' ' }, overlay);
            // self.doc.UpdateStream(xref, self.totalcont)
            _doc.UpdateStream(xref, content);

            _totalContents.Clear();
            _pathCount = 0;
            // self.last_point = None  # clean up ...
            // self.rect = None  #
            // self.draw_cont = ""  # for potential ...
            // self.text_cont = ""  # ...
            // self.totalcont = ""  # re-use
        }

        // ─── Text methods ───────────────────────────────────────────────

        /// <summary>
        /// Insert one or more text lines starting at <paramref name="point"/> (appends to <see cref="TextCont"/>).
        /// </summary>
        /// <param name="point">Bottom-left of the first glyph (depends on <paramref name="rotate"/>).</param>
        /// <param name="buffer">Text; newline-separated lines are supported.</param>
        /// <param name="fontSize">Font size in points.</param>
        /// <param name="lineHeight">Optional line-height factor (× fontsize).</param>
        /// <param name="fontName">Base-14 name, registered font, or <c>/RefName</c>.</param>
        /// <param name="fontFile">External font file when required.</param>
        /// <param name="setSimple">Use simple font embedding when non-zero.</param>
        /// <param name="encoding">Font encoding flag.</param>
        /// <param name="color">Stroke RGB components.</param>
        /// <param name="fill">Fill RGB (defaults to <paramref name="color"/> for text mode 0).</param>
        /// <param name="renderMode">PDF text rendering mode.</param>
        /// <param name="borderWidth">Glyph border width as fraction of fontsize.</param>
        /// <param name="miterLimit">Miter limit for stroked text.</param>
        /// <param name="rotate">Rotation: 0, 90, 180, or 270 degrees.</param>
        /// <param name="morphFix">Morph fixed point.</param>
        /// <param name="morphMat">Morph matrix.</param>
        /// <param name="strokeOpacity">Stroke opacity 0–1.</param>
        /// <param name="fillOpacity">Fill opacity 0–1.</param>
        /// <param name="oc">Optional content xref (0 = none).</param>
        /// <returns>Number of lines written.</returns>
        public int InsertText(
            Point point,
            string buffer,
            float fontSize = 11,
            float? lineHeight = null,
            string fontName = "helv",
            string fontFile = null,
            int setSimple = 0,
            int encoding = 0,
            float[] color = null,
            float[] fill = null,
            int renderMode = 0,
            float borderWidth = 0.05f,
            float? miterLimit = 1f,
            int rotate = 0,
            Point morphFix = null,
            Matrix morphMat = null,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            int oc = 0)
        {
            // ensure 'text' is a list of strings, worth dealing with
            if (string.IsNullOrEmpty(buffer))
                return 0;

            var textLines = buffer.Replace("\r\n", "\n").Split('\n');
            if (textLines.Length == 0)
                return 0;

            int maxcode;
            try
            {
                var joined = string.Join(" ", textLines);
                maxcode = joined.Length == 0 ? 0 : joined.Max(ch => ch);
            }
            catch
            {
                return 0;
            }

            // ensure valid 'fontname'
            string fname = fontName;
            if (fname.StartsWith("/", StringComparison.Ordinal))
                fname = fname.Substring(1);

            int xref = _page.InsertFont(fname, fontFile, null, setSimple != 0, wmode: 0, encoding: encoding);
            var fontinfo = _doc.CheckFontInfo(xref);
            if (fontinfo == null)
                throw new InvalidOperationException($"font '{fname}' was not registered in the document");
            var fontdict = fontinfo[1] as Dictionary<string, object>;
            if (fontdict == null)
                throw new InvalidOperationException($"font info for '{fname}' is invalid");

            bool simple = fontdict.TryGetValue("simple", out var sobj) && sobj is bool sb && sb;
            int ordering = fontdict.TryGetValue("ordering", out var oobj) && oobj is int oi ? oi : -1;
            string bfname = fontdict.TryGetValue("name", out var nobj) ? nobj?.ToString() ?? "" : "";

            float ascender = 0.8f, descender = -0.2f;
            if (fontdict.TryGetValue("ascender", out var ascObj) && fontdict.TryGetValue("descender", out var dscObj))
            {
                ascender = Convert.ToSingle(ascObj);
                descender = Convert.ToSingle(dscObj);
            }
            else
                ReadAscDesc(xref, ref ascender, ref descender);

            float lheight;
            if (lineHeight.HasValue)
                lheight = fontSize * lineHeight.Value;
            else if (ascender - descender <= 1)
                lheight = fontSize * 1.2f;
            else
                lheight = fontSize * (ascender - descender);

            List<(int glyph, float width)> glyphs;
            if (maxcode > 255)
                glyphs = _doc.GetCharWidths(xref, maxcode + 1);
            else
                glyphs = fontdict.TryGetValue("glyphs", out var gobj) && gobj is List<(int glyph, float width)> gl
                    ? gl
                    : null;

            var tab = new List<string>();
            foreach (var t in textLines)
            {
                List<(int glyph, float width)> gline;
                if (simple && bfname != "Symbol" && bfname != "ZapfDingbats")
                    gline = null;
                else
                    gline = glyphs;
                tab.Add(Helpers.GetTJstr(t, gline, simple, ordering));
            }

            string colorStr = Helpers.ColorCode(color, "c");
            string fillStr = Helpers.ColorCode(fill, "f");
            if (fill == null && renderMode == 0) // ensure fill color when 0 Tr
            {
                fill = color;
                fillStr = Helpers.ColorCode(color, "f");
            }

            bool morphing = false;
            if (morphFix != null && morphMat != null)
            {
                morphing = Helpers.CheckMorph(morphFix, morphMat);
            }

            int rot = rotate;
            if (rot % 90 != 0)
                throw new ValueErrorException("bad rotate value");
            while (rot < 0)
                rot += 360;
            rot %= 360; // text rotate = 0, 90, 270, 180

            string cmp90 = "0 1 -1 0 0 0 cm\n"; // rotates 90 deg counter-clockwise
            string cmm90 = "0 -1 1 0 0 0 cm\n"; // rotates 90 deg clockwise
            string cm180 = "-1 0 0 -1 0 0 cm\n"; // rotates by 180 deg.
            float height = _height;
            float width = _width;

            // setting up for standard rotation directions
            // case rotate = 0
            string cm;
            if (morphing)
            {
                var m1 = new Matrix(1, 0, 0, 1, morphFix.X + _shapeX, height - morphFix.Y - _shapeY);
                var inv = m1.Inverted() ?? throw new ValueErrorException("singular morph matrix");
                var mat = inv * morphMat * m1;
                cm = Helpers.FormatPdfReals(mat.A, mat.B, mat.C, mat.D, mat.E, mat.F) + " cm\n";
            }
            else
                cm = "";

            float top = height - (float)point.Y - _shapeY; // start of 1st char
            float left = (float)point.X + _shapeX; // start of 1. char
            float space = top;
            if (rot == 90)
            {
                left = height - (float)point.Y - _shapeY;
                top = -(float)point.X - _shapeX;
                cm += cmp90;
                space = width - Math.Abs(top);
            }
            else if (rot == 270)
            {
                left = -height + (float)point.Y + _shapeY;
                top = (float)point.X + _shapeX;
                cm += cmm90;
                space = Math.Abs(top);
            }
            else if (rot == 180)
            {
                left = -(float)point.X - _shapeX;
                top = -height + (float)point.Y + _shapeY;
                cm += cm180;
                space = Math.Abs((float)point.Y + _shapeY);
            }

            string optcont = _page._get_optional_content(oc);
            string bdc, emc;
            if (optcont != null)
            {
                bdc = $"/OC /{optcont} BDC\n";
                emc = "EMC\n";
            }
            else
                bdc = emc = "";

            string alpha = _page._set_opacity(CA: strokeOpacity, ca: fillOpacity);
            if (alpha == null)
                alpha = "";
            else
                alpha = $"/{alpha} gs\n";

            // PyMuPDF Shape.insert_text uses fontname (e.g. "helv"), not "F{xref}".
            string resKey = fname;
            var nres = new StringBuilder();
            nres.Append("\nq\n");
            nres.Append(bdc);
            nres.Append(alpha);
            nres.Append(cm);
            nres.Append("BT\n");
            nres.Append("1 0 0 1 ");
            nres.Append(Helpers.FormatPdfReals(left, top));
            nres.Append(" Tm\n/");
            nres.Append(resKey);
            nres.Append(' ');
            nres.Append(fontSize.ToString("G9", CultureInfo.InvariantCulture));
            nres.Append(" Tf ");

            if (renderMode > 0)
            {
                nres.Append(renderMode.ToString(CultureInfo.InvariantCulture)).Append(" Tr ");
                nres.Append(Helpers.FormatPdfReals(borderWidth * fontSize)).Append(" w ");
                if (miterLimit != null)
                    nres.Append(Helpers.FormatPdfReals(miterLimit.Value)).Append(" M ");
            }
            if (color != null)
                nres.Append(colorStr);
            if (fill != null)
                nres.Append(fillStr);

            // =========================================================================
            //   start text insertion
            // =========================================================================
            nres.Append(tab[0]);
            int nlines = 1; // set output line counter
            if (tab.Count > 1)
            {
                nres.Append("TJ\n0 -");
                nres.Append(lheight.ToString("G9", CultureInfo.InvariantCulture));
                nres.Append(" TD\n");
            }
            else
                nres.Append("TJ");

            for (int i = 1; i < tab.Count; i++)
            {
                if (space < lheight)
                    break; // no space left on page
                if (i > 1)
                    nres.Append("\nT* ");
                nres.Append(tab[i]).Append("TJ");
                space -= lheight;
                nlines++;
            }

            nres.Append("\nET\n").Append(emc).Append("Q\n");

            // =========================================================================
            //   end of text insertion
            // =========================================================================
            // update the /Contents object
            _textCont.Append(nres.ToString());
            return nlines;
        }

        private void ReadAscDesc(int xref, ref float ascender, ref float descender)
        {
            var ef = _doc.ExtractFont(xref);
            var font = Document.LoadFzFontForCharWidths(_doc, xref, ef.name, ef.content);
            bool disposeFont = ef.content != null && ef.content.Length > 0;
            try
            {
                ascender = (float)font.fz_font_ascender();
                descender = (float)font.fz_font_descender();
            }
            finally
            {
                if (disposeFont)
                    font?.Dispose();
            }
        }

        // ==============================================================================
        // Shape.insert_textbox
        // ==============================================================================
        /// <summary>
        /// Fit text into a rectangle with wrapping and alignment (appends to <see cref="TextCont"/>).
        /// </summary>
        /// <param name="rect">Finite, non-empty text box.</param>
        /// <param name="buffer">String or sequence of strings.</param>
        /// <param name="align">0 left, 1 center, 2 right, 3 justified.</param>
        /// <param name="borderWidth">Glyph border width as fraction of fontsize.</param>
        /// <param name="color">Stroke RGB.</param>
        /// <param name="encoding">Font encoding.</param>
        /// <param name="expandTabs">Tab expansion width (spaces per tab).</param>
        /// <param name="fillOpacity">Text fill opacity.</param>
        /// <param name="fill">Fill RGB.</param>
        /// <param name="fontFile">External font file.</param>
        /// <param name="fontname">Font name or <c>/RefName</c>.</param>
        /// <param name="fontSize">Font size in points.</param>
        /// <param name="lineHeight">Optional line-height factor.</param>
        /// <param name="miterLimit">Miter limit for stroked glyphs.</param>
        /// <param name="morphFix">Morph fixed point.</param>
        /// <param name="morphMat">Morph matrix.</param>
        /// <param name="oc">Optional content xref.</param>
        /// <param name="renderMode">PDF text rendering mode.</param>
        /// <param name="rotate">0, 90, 180, or 270 degrees.</param>
        /// <param name="setSimple">Simple font mode when non-zero.</param>
        /// <param name="strokeOpacity">Stroke opacity.</param>
        /// <returns>Unused rectangle height in points if ≥ 0; negative deficit if text did not fit.</returns>
        public float InsertTextbox(
            Rect rect,
            object buffer,
            int align = 0,
            float borderWidth = 0.05f,
            float[] color = null,
            int encoding = 0,
            float expandTabs = 1,
            float fillOpacity = 1,
            float[] fill = null,
            string fontFile = null,
            string fontName = "helv",
            float fontSize = 11,
            float? lineHeight = null,
            float? miterLimit = 1,
            Point morphFix = null,
            Matrix morphMat = null,
            int oc = 0,
            int renderMode = 0,
            int rotate = 0,
            int setSimple = 0,
            float strokeOpacity = 1)
        {
            // Args:
            //     rect -- the textbox to fill
            //     buffer -- text to be inserted
            //     fontname -- a Base-14 font, font name or '/name'
            //     fontfile -- name of a font file
            //     lineheight -- overwrite the font property
            //     color -- RGB stroke color triple
            //     fill -- RGB fill color triple
            //     render_mode -- text rendering control
            //     border_width -- thickness of glyph borders as percentage of fontsize
            //     expandtabs -- handles tabulators with string function
            //     align -- left, center, right, justified
            //     rotate -- 0, 90, 180, or 270 degrees
            //     morph -- morph box with a matrix and a fixpoint
            // Returns:
            //     unused or deficit rectangle area (float)
            rect = new Rect(rect);
            if (rect.IsEmpty || rect.IsInfinite)
                throw new ValueErrorException("text box must be finite and not empty");

            string colorStr = Helpers.ColorCode(color, "c");
            string fillStr = Helpers.ColorCode(fill, "f");
            if (fill == null && renderMode == 0) // ensure fill color for 0 Tr
            {
                fill = color;
                fillStr = Helpers.ColorCode(color, "f");
            }

            string optcont = _page._get_optional_content(oc);
            string bdc, emc;
            if (optcont != null)
            {
                bdc = $"/OC /{optcont} BDC\n";
                emc = "EMC\n";
            }
            else
                bdc = emc = "";

            // determine opacity / transparency
            string alpha = _page._set_opacity(CA: strokeOpacity, ca: fillOpacity);
            if (alpha == null)
                alpha = "";
            else
                alpha = $"/{alpha} gs\n";

            if (rotate % 90 != 0)
                throw new ValueErrorException("rotate must be multiple of 90");

            int rot = rotate;
            while (rot < 0)
                rot += 360;
            rot %= 360;

            // is buffer worth of dealing with?
            bool hasBuffer = buffer switch
            {
                null => false,
                string s => !string.IsNullOrEmpty(s),
                IEnumerable<string> seq => seq.Any(),
                IEnumerable<object> seq => seq.Any(),
                _ => true
            };
            if (!hasBuffer)
                return (float)(rot == 0 || rot == 180 ? rect.Height : rect.Width);

            string cmp90 = "0 1 -1 0 0 0 cm\n"; // rotates counter-clockwise
            string cmm90 = "0 -1 1 0 0 0 cm\n"; // rotates clockwise
            string cm180 = "-1 0 0 -1 0 0 cm\n"; // rotates by 180 deg.
            float height = _height;

            string fname = fontName;
            if (fname.StartsWith("/", StringComparison.Ordinal))
                fname = fname.Substring(1);

            int xref = _page.InsertFont(fname, fontFile, null, setSimple != 0, wmode: 0, encoding: encoding);
            var fontdict = _doc.GetFontDictForXref(xref);
            if (fontdict == null)
                throw new ValueErrorException("font not found");

            int ordering = fontdict.TryGetValue("ordering", out var oobj) && oobj is int oi ? oi : -1;
            bool simple = fontdict.TryGetValue("simple", out var sobj) && sobj is bool sb && sb;
            string bfname = fontdict.TryGetValue("name", out var nobj) ? nobj?.ToString() ?? "" : "";
            float ascender = 0.8f, descender = -0.2f;
            if (fontdict.TryGetValue("ascender", out var ascObj) && fontdict.TryGetValue("descender", out var dscObj))
            {
                ascender = Convert.ToSingle(ascObj);
                descender = Convert.ToSingle(dscObj);
            }
            else
                ReadAscDesc(xref, ref ascender, ref descender);

            float lheightFactor;
            if (lineHeight.HasValue)
                lheightFactor = lineHeight.Value;
            else if (ascender - descender <= 1)
                lheightFactor = 1.2f;
            else
                lheightFactor = ascender - descender;
            float lheight = fontSize * lheightFactor;

            // create a list from buffer, split into its lines
            string t0;
            if (buffer is IEnumerable<string> listBuf && buffer is not string)
                t0 = string.Join("\n", listBuf);
            else if (buffer is IEnumerable<object> objBuf && buffer is not string)
                t0 = string.Join("\n", objBuf.Select(o => o?.ToString() ?? ""));
            else
                t0 = buffer?.ToString() ?? "";

            int maxcode = t0.Length == 0 ? 0 : t0.Max(c => c);
            // replace invalid char codes for simple fonts
            if (simple && maxcode > 255)
            {
                var sbChars = new StringBuilder(t0.Length);
                foreach (char c in t0)
                    sbChars.Append(c < 256 ? c : '?');
                t0 = sbChars.ToString();
            }

            string[] t0Lines = t0.Split('\n');

            List<(int glyph, float width)> glyphs = _doc.GetCharWidths(xref, maxcode + 1);
            List<(int glyph, float width)> tjGlyphs;
            if (simple && bfname != "Symbol" && bfname != "ZapfDingbats")
                tjGlyphs = null;
            else
                tjGlyphs = glyphs;

            // ----------------------------------------------------------------------
            // calculate pixel length of a string
            // ----------------------------------------------------------------------
            float Pixlen(string x)
            {
                if (ordering < 0)
                {
                    float sum = 0;
                    foreach (char ch in x)
                    {
                        int code = ch;
                        if (code >= 0 && code < glyphs.Count)
                            sum += (float)glyphs[code].Item2;
                    }
                    return sum * fontSize;
                }
                return x.Length * fontSize;
            }

            // ---------------------------------------------------------------------

            float blen;
            if (ordering < 0)
                blen = (float)glyphs[32].Item2 * fontSize; // pixel size of space character
            else
                blen = fontSize;

            string text = ""; // output buffer

            string cm;
            if (morphFix != null && morphMat != null && Helpers.CheckMorph(morphFix, morphMat))
            {
                var m1 = new Matrix(1, 0, 0, 1, morphFix.X + _shapeX, height - morphFix.Y - _shapeY);
                var inv = m1.Inverted() ?? throw new ValueErrorException("singular morph matrix");
                var mat = inv * morphMat * m1;
                cm = Helpers.FormatPdfReals(mat.A, mat.B, mat.C, mat.D, mat.E, mat.F) + " cm\n";
            }
            else
                cm = "";

            // ---------------------------------------------------------------------
            // adjust for text orientation / rotation
            // ---------------------------------------------------------------------
            float progr = 1; // direction of line progress
            Point c_pnt = new Point(0, fontSize * ascender); // used for line progress
            Point point;
            float maxwidth;
            float maxheight;
            if (rot == 0) // normal orientation
            {
                point = rect.TopLeft + c_pnt; // line 1 is 'lheight' below top
                maxwidth = (float)rect.Width; // pixels available in one line
                maxheight = (float)rect.Height; // available text height
            }
            else if (rot == 90) // rotate counter clockwise
            {
                c_pnt = new Point(fontSize * ascender, 0); // progress in x-direction
                point = rect.BottomLeft + c_pnt; // line 1 'lheight' away from left
                maxwidth = (float)rect.Height; // pixels available in one line
                maxheight = (float)rect.Width; // available text height
                cm += cmp90;
            }
            else if (rot == 180) // text upside down
            {
                // progress upwards in y direction
                c_pnt = new Point(0, -fontSize * ascender);
                point = rect.BottomRight + c_pnt; // line 1 'lheight' above bottom
                maxwidth = (float)rect.Width; // pixels available in one line
                progr = -1; // subtract lheight for next line
                maxheight = (float)rect.Height; // available text height
                cm += cm180;
            }
            else // rotate clockwise (270 or -90)
            {
                // progress from right to left
                c_pnt = new Point(-fontSize * ascender, 0);
                point = rect.TopRight + c_pnt; // line 1 'lheight' left of right
                maxwidth = (float)rect.Height; // pixels available in one line
                progr = -1; // subtract lheight for next line
                maxheight = (float)rect.Width; // available text height
                cm += cmm90;
            }

            // =====================================================================
            // line loop
            // =====================================================================
            var justTab = new List<bool>(); // 'justify' indicators per line

            int tabSize = (int)expandTabs;
            if (tabSize < 1)
                tabSize = 8;

            for (int i = 0; i < t0Lines.Length; i++)
            {
                string line = t0Lines[i];
                string[] lineT = ExpandTabsLine(line, tabSize).Split(' '); // split into words
                int numWords = lineT.Length;
                string lbuff = ""; // init line buffer
                float rest = maxwidth; // available line pixels
                // =================================================================
                // word loop
                // =================================================================
                for (int j = 0; j < numWords; j++)
                {
                    string word = lineT[j];
                    float plW = Pixlen(word); // pixel len of word
                    if (rest >= plW) // does it fit on the line?
                    {
                        lbuff += word + " "; // yes, append word
                        rest -= plW + blen; // update available line space
                        continue; // next word
                    }

                    // word doesn't fit - output line (if not empty)
                    if (!string.IsNullOrEmpty(lbuff))
                    {
                        lbuff = lbuff.TrimEnd() + "\n"; // line full, append line break
                        text += lbuff; // append to total text
                        justTab.Add(true); // can align-justify
                    }

                    lbuff = ""; // re-init line buffer
                    rest = maxwidth; // re-init avail. space

                    if (plW <= maxwidth) // word shorter than 1 line?
                    {
                        lbuff = word + " "; // start the line with it
                        rest = maxwidth - plW - blen; // update free space
                        continue;
                    }

                    // long word: split across multiple lines - char by char ...
                    if (justTab.Count > 0)
                        justTab[justTab.Count - 1] = false; // cannot align-justify
                    foreach (char c in word)
                    {
                        if (Pixlen(lbuff) <= maxwidth - Pixlen(c.ToString()))
                            lbuff += c;
                        else // line full
                        {
                            lbuff += "\n"; // close line
                            text += lbuff; // append to text
                            justTab.Add(false); // cannot align-justify
                            lbuff = c.ToString(); // start new line with this char
                        }
                    }

                    lbuff += " "; // finish long word
                    rest = maxwidth - Pixlen(lbuff); // long word stored
                }

                if (!string.IsNullOrEmpty(lbuff)) // unprocessed line content?
                {
                    text += lbuff.TrimEnd(); // append to text
                    justTab.Add(false); // cannot align-justify
                }

                if (i < t0Lines.Length - 1) // not the last line?
                    text += "\n"; // insert line break
            }

            // compute used part of the textbox
            if (text.EndsWith("\n", StringComparison.Ordinal))
                text = text.Substring(0, text.Length - 1);
            int lbCount = text.Count(c => c == '\n') + 1; // number of lines written

            // text height = line count * line height plus one descender value
            float textHeight = lheight * lbCount - descender * fontSize;

            float more = textHeight - maxheight; // difference to height limit
            if (more > Constants.Epsilon) // landed too much outside rect
                return -more; // return deficit, don't output

            more = Math.Abs(more);
            if (more < Constants.Epsilon)
                more = 0; // don't bother with epsilons
            var nres = new StringBuilder();
            nres.Append("\nq\n").Append(bdc).Append(alpha).Append("BT\n").Append(cm); // initialize output buffer
            // templ = lambda a, b, c, d: f"1 0 0 1 {_format_g((a, b))} Tm /{c} {_format_g(d)} Tf "
            // center, right, justify: output each line with its own specifics
            string[] textT = text.Split('\n'); // split text in lines again
            if (justTab.Count > 0)
                justTab[justTab.Count - 1] = false; // never justify last line
            for (int i = 0; i < textT.Length; i++)
            {
                string t = textT[i];
                float spacing = 0;
                float pl = maxwidth - Pixlen(t); // length of empty line part
                Point pnt = point + c_pnt * (i * lheightFactor); // text start of line
                if (align == 1) // center: right shift by half width
                {
                    if (rot == 0 || rot == 180)
                        pnt += new Point(pl / 2, 0) * progr;
                    else
                        pnt -= new Point(0, pl / 2) * progr;
                }
                else if (align == 2) // right: right shift by full width
                {
                    if (rot == 0 || rot == 180)
                        pnt += new Point(pl, 0) * progr;
                    else
                        pnt -= new Point(0, pl) * progr;
                }
                else if (align == 3) // justify
                {
                    int spaces = t.Count(ch => ch == ' '); // number of spaces in line
                    if (spaces > 0 && i < justTab.Count && justTab[i]) // if any, and we may justify
                        spacing = pl / spaces; // make every space this much larger
                    else
                        spacing = 0; // keep normal space length
                }
                float top = height - (float)pnt.Y - _shapeY;
                float left = (float)pnt.X + _shapeX;
                if (rot == 90)
                {
                    left = height - (float)pnt.Y - _shapeY;
                    top = -(float)pnt.X - _shapeX;
                }
                else if (rot == 270)
                {
                    left = -height + (float)pnt.Y + _shapeY;
                    top = (float)pnt.X + _shapeX;
                }
                else if (rot == 180)
                {
                    left = -(float)pnt.X - _shapeX;
                    top = -height + (float)pnt.Y + _shapeY;
                }

                nres.Append("1 0 0 1 ");
                nres.Append(Helpers.FormatPdfReals(left, top));
                nres.Append(" Tm /");
                nres.Append(fname);
                nres.Append(' ');
                nres.Append(Helpers.FormatPdfReals(fontSize));
                nres.Append(" Tf ");

                if (renderMode > 0)
                {
                    nres.Append(renderMode.ToString(CultureInfo.InvariantCulture)).Append(" Tr ");
                    nres.Append(Helpers.FormatPdfReals(borderWidth * fontSize)).Append(" w ");
                    if (miterLimit != null)
                        nres.Append(Helpers.FormatPdfReals(miterLimit.Value)).Append(" M ");
                }

                if (align == 3)
                    nres.Append(Helpers.FormatPdfReals(spacing)).Append(" Tw ");

                if (color != null)
                    nres.Append(colorStr);
                if (fill != null)
                    nres.Append(fillStr);
                nres.Append(Helpers.GetTJstr(t, tjGlyphs, simple, ordering)).Append("TJ\n");
            }

            nres.Append("ET\n").Append(emc).Append("Q\n");

            _textCont.Append(nres.ToString());
            UpdateRect(rect);
            return more;
        }

        private static string ExpandTabsLine(string line, int tabSize)
        {
            if (!line.Contains('\t'))
                return line;
            var sb = new StringBuilder(line.Length);
            int col = 0;
            foreach (char ch in line)
            {
                if (ch == '\t')
                {
                    int next = ((col / tabSize) + 1) * tabSize;
                    sb.Append(' ', next - col);
                    col = next;
                }
                else
                {
                    sb.Append(ch);
                    col++;
                }
            }
            return sb.ToString();
        }

        // ─── Internal helpers ───────────────────────────────────────────

        /// <summary>Expands <see cref="Rect"/> to include <paramref name="p"/>.</summary>
        public void UpdateRect(Point p) => _rect = _rect.IncludePoint(p);

        /// <summary>Expands <see cref="Rect"/> to include <paramref name="r"/>.</summary>
        public void UpdateRect(Rect r) => _rect = _rect.IncludeRect(r);

        // ─── IDisposable ────────────────────────────────────────────────

        /// <summary>Releases managed resources (does not call <see cref="Commit"/>).</summary>
        public void Dispose()
        {
            if (!_disposed) { _disposed = true; }
            GC.SuppressFinalize(this);
        }

        ~Shape() { Dispose(); }

        // ─── PyMuPDF API names (internal, same assembly) ─────────────────

        internal static float horizontal_angle(Point c, Point p) => HorizontalAngle(c, p);
        internal Point draw_bezier(Point p1, Point p2, Point p3, Point p4) => DrawBezier(p1, p2, p3, p4);
        internal Point draw_circle(Point center, float radius) => DrawCircle(center, radius);
        internal Point draw_curve(Point p1, Point p2, Point p3) => DrawCurve(p1, p2, p3);
        internal Point draw_line(Point p1, Point p2) => DrawLine(p1, p2);
        internal Point draw_oval(Rect rect) => DrawOval(rect);
        internal Point draw_polyline(Point[] points) => DrawPolyline(points);
        internal Point draw_quad(Quad quad) => DrawQuad(quad);
        internal Point draw_rect(Rect rect, object radius = null) => DrawRect(rect, radius);
        internal Point draw_sector(Point center, Point point, float angle, bool fullSector = true) => DrawSector(center, point, angle, fullSector);
        internal Point draw_squiggle(Point p1, Point p2, float breadth = 2) => DrawSquiggle(p1, p2, breadth);
        internal Point draw_zigzag(Point p1, Point p2, float breadth = 2) => DrawZigzag(p1, p2, breadth);
        internal void finish(
            float[] color = null,
            float[] fill = null,
            float width = 1,
            int lineCap = 0,
            int lineJoin = 0,
            string dashes = null,
            bool closePath = true,
            bool even_odd = false,
            float strokeOpacity = 1,
            float fillOpacity = 1,
            string blendMode = null,
            int oc = 0,
            Point morphFix = null,
            Matrix morphMat = null) =>
            Finish(color, fill, width, lineCap, lineJoin, dashes, closePath, even_odd, strokeOpacity, fillOpacity, blendMode, oc, morphFix, morphMat);
        internal void commit(bool overlay = true) => Commit(overlay);
        internal int insert_text(Point point, string text, float fontsize = 11, string fontname = "helv",
            float[] color = null, int renderMode = 0, float borderWidth = 0.05f, int rotate = 0)
            => InsertText(point, text, fontsize, null, fontname, null, 0, 0, color, null, renderMode, borderWidth, 1f, rotate, null, null, 1, 1, 0);
        internal (int rc, List<string> rest) insert_textbox(Rect rect, string text, float fontsize = 11,
            string fontname = "helv", float[] color = null, int align = 0, int renderMode = 0,
            float borderWidth = 0.05f, int rotate = 0, float expandTabs = 1)
        {
            float more = InsertTextbox(rect, text, align, borderWidth, color, 0, expandTabs, 1, null, null, fontname,
                fontsize, null, 1, null, null, 0, renderMode, rotate, 0, 1);
            return (more >= 0 ? 0 : -1, new List<string>());
        }

        /// <summary>Diagnostic summary of path and draw counts.</summary>
        public override string ToString() => $"Shape(paths={_pathCount}, draws={_drawCount})";
    }
}
