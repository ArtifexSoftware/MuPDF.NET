using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MuPDF.NET
{
    /// <summary>
    /// Create a new shape for drawing on a page.
    /// </summary>
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

        /// <summary>
        /// Rectangle surrounding all drawings.
        /// </summary>
        public Rect Rect => _rect;
        /// <summary>
        /// Last point of the current drawing.
        /// </summary>
        public Point LastPoint => _lastPoint;
        /// <summary>
        /// Number of unfinished drawings.
        /// </summary>
        public int DrawCount => _drawCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="Shape"/> class for the specified page.
        /// </summary>
        public Shape(Page page)
        {
            _page = page;
            _doc = page.Parent;
            _width = page.Width;
            _height = page.Height;
            _contents = new StringBuilder();
            _totalContents = new StringBuilder();
            _rect = new Rect();
            _lastPoint = null;
            _firstPoint = null;
        }

        // ─── Drawing Primitives ─────────────────────────────────────────

        /// <summary>
        /// Draw a line from p1 to p2.
        /// </summary>
        public Point DrawLine(Point p1, Point p2)
        {
            if (_lastPoint == null || _lastPoint.X != p1.X || _lastPoint.Y != p1.Y)
                _contents.AppendLine($"{p1.X:F4} {_height - p1.Y:F4} m");
            _contents.AppendLine($"{p2.X:F4} {_height - p2.Y:F4} l");
            UpdateRect(p1);
            UpdateRect(p2);
            _lastPoint = p2;
            if (_firstPoint == null) _firstPoint = p1;
            _drawCount++;
            return p2;
        }

        /// <summary>
        /// Draw a rectangle.
        /// </summary>
        public Point DrawRect(Rect rect)
        {
            _contents.AppendLine($"{rect.X0:F4} {_height - rect.Y1:F4} {rect.Width:F4} {rect.Height:F4} re");
            UpdateRect(rect.TopLeft);
            UpdateRect(rect.BottomRight);
            _lastPoint = rect.TopLeft;
            if (_firstPoint == null) _firstPoint = rect.TopLeft;
            _drawCount++;
            return rect.TopLeft;
        }

        /// <summary>
        /// Draw a circle given center and radius.
        /// </summary>
        public Point DrawCircle(Point center, float radius)
        {
            return DrawOval(new Rect(
                center.X - radius, center.Y - radius,
                center.X + radius, center.Y + radius));
        }

        /// <summary>
        /// Draw an oval (ellipse) within a given rectangle.
        /// </summary>
        public Point DrawOval(Rect rect)
        {
            float mx = (float)((rect.X0 + rect.X1) / 2);
            float my = (float)((rect.Y0 + rect.Y1) / 2);
            float w2 = (float)(rect.Width / 2);
            float h2 = (float)(rect.Height / 2);
            float kappa = 0.5522848f;
            float ox = w2 * kappa;
            float oy = h2 * kappa;

            float top = (float)rect.Y0, bottom = (float)rect.Y1;
            float left = (float)rect.X0, right = (float)rect.X1;

            _contents.AppendLine($"{right:F4} {_height - my:F4} m");
            _contents.AppendLine($"{right:F4} {_height - (my - oy):F4} {mx + ox:F4} {_height - top:F4} {mx:F4} {_height - top:F4} c");
            _contents.AppendLine($"{mx - ox:F4} {_height - top:F4} {left:F4} {_height - (my - oy):F4} {left:F4} {_height - my:F4} c");
            _contents.AppendLine($"{left:F4} {_height - (my + oy):F4} {mx - ox:F4} {_height - bottom:F4} {mx:F4} {_height - bottom:F4} c");
            _contents.AppendLine($"{mx + ox:F4} {_height - bottom:F4} {right:F4} {_height - (my + oy):F4} {right:F4} {_height - my:F4} c");

            UpdateRect(rect.TopLeft);
            UpdateRect(rect.BottomRight);
            _lastPoint = new Point(right, my);
            if (_firstPoint == null) _firstPoint = new Point(right, my);
            _drawCount++;
            return new Point(right, my);
        }

        /// <summary>
        /// Draw a curve from current point through p2 to p3.
        /// </summary>
        public Point DrawCurve(Point p1, Point p2, Point p3)
        {
            if (_lastPoint == null || _lastPoint.X != p1.X || _lastPoint.Y != p1.Y)
                _contents.AppendLine($"{p1.X:F4} {_height - p1.Y:F4} m");
            float kx1 = (float)(p1.X + 2.0 / 3.0 * (p2.X - p1.X));
            float ky1 = (float)(p1.Y + 2.0 / 3.0 * (p2.Y - p1.Y));
            float kx2 = (float)(p3.X + 2.0 / 3.0 * (p2.X - p3.X));
            float ky2 = (float)(p3.Y + 2.0 / 3.0 * (p2.Y - p3.Y));
            _contents.AppendLine($"{kx1:F4} {_height - ky1:F4} {kx2:F4} {_height - ky2:F4} {p3.X:F4} {_height - p3.Y:F4} c");
            UpdateRect(p1); UpdateRect(p2); UpdateRect(p3);
            _lastPoint = p3;
            if (_firstPoint == null) _firstPoint = p1;
            _drawCount++;
            return p3;
        }

        /// <summary>
        /// Draw a cubic Bezier curve.
        /// </summary>
        public Point DrawBezier(Point p1, Point p2, Point p3, Point p4)
        {
            if (_lastPoint == null || _lastPoint.X != p1.X || _lastPoint.Y != p1.Y)
                _contents.AppendLine($"{p1.X:F4} {_height - p1.Y:F4} m");
            _contents.AppendLine($"{p2.X:F4} {_height - p2.Y:F4} {p3.X:F4} {_height - p3.Y:F4} {p4.X:F4} {_height - p4.Y:F4} c");
            UpdateRect(p1); UpdateRect(p2); UpdateRect(p3); UpdateRect(p4);
            _lastPoint = p4;
            if (_firstPoint == null) _firstPoint = p1;
            _drawCount++;
            return p4;
        }

        /// <summary>
        /// Draw a polyline connecting the given points.
        /// </summary>
        public Point DrawPolyline(Point[] points)
        {
            if (points == null || points.Length < 2) throw new ArgumentException("need at least 2 points");
            _contents.AppendLine($"{points[0].X:F4} {_height - points[0].Y:F4} m");
            for (int i = 1; i < points.Length; i++)
                _contents.AppendLine($"{points[i].X:F4} {_height - points[i].Y:F4} l");
            foreach (var p in points) UpdateRect(p);
            _lastPoint = points[points.Length - 1];
            if (_firstPoint == null) _firstPoint = points[0];
            _drawCount++;
            return _lastPoint;
        }

        /// <summary>
        /// Draw a quadrilateral.
        /// </summary>
        public Point DrawQuad(Quad quad)
        {
            return DrawPolyline(new[] { quad.UL, quad.LL, quad.LR, quad.UR, quad.UL });
        }

        /// <summary>
        /// Draw a sector.
        /// </summary>
        public Point DrawSector(Point center, Point point, float angle, bool fullSector = true)
        {
            double rad = angle * Math.PI / 180.0;
            double dx = point.X - center.X, dy = point.Y - center.Y;
            double cos = Math.Cos(rad), sin = Math.Sin(rad);
            float ex = (float)(center.X + dx * cos - dy * sin);
            float ey = (float)(center.Y + dx * sin + dy * cos);

            if (fullSector)
            {
                _contents.AppendLine($"{center.X:F4} {_height - center.Y:F4} m");
                _contents.AppendLine($"{point.X:F4} {_height - point.Y:F4} l");
            }
            else
            {
                _contents.AppendLine($"{point.X:F4} {_height - point.Y:F4} m");
            }

            DrawArc(center, point, new Point(ex, ey), angle);

            if (fullSector)
                _contents.AppendLine($"{center.X:F4} {_height - center.Y:F4} l");

            UpdateRect(center); UpdateRect(point); UpdateRect(new Point(ex, ey));
            _lastPoint = new Point(ex, ey);
            if (_firstPoint == null) _firstPoint = center;
            _drawCount++;
            return new Point(ex, ey);
        }

        /// <summary>
        /// Draw a squiggly (wavy) line from p1 to p2.
        /// </summary>
        public Point DrawSquiggle(Point p1, Point p2, float breadth = 2)
        {
            float dx = (float)(p2.X - p1.X), dy = (float)(p2.Y - p1.Y);
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 4 * breadth) { DrawLine(p1, p2); return p2; }

            int nsegs = (int)(length / (4 * breadth));
            float segLen = length / nsegs;
            float nx = -dy / length * breadth, ny = dx / length * breadth;
            float sx = dx / length * segLen, sy = dy / length * segLen;

            _contents.AppendLine($"{p1.X:F4} {_height - p1.Y:F4} m");
            float cx = (float)p1.X, cy = (float)p1.Y;
            for (int i = 0; i < nsegs; i++)
            {
                float m1x = cx + sx * 0.25f + nx;
                float m1y = cy + sy * 0.25f + ny;
                float m2x = cx + sx * 0.75f - nx;
                float m2y = cy + sy * 0.75f - ny;
                float ex2 = cx + sx;
                float ey2 = cy + sy;
                _contents.AppendLine($"{m1x:F4} {_height - m1y:F4} {m2x:F4} {_height - m2y:F4} {ex2:F4} {_height - ey2:F4} c");
                cx = ex2; cy = ey2;
            }
            UpdateRect(p1); UpdateRect(p2);
            _lastPoint = p2;
            if (_firstPoint == null) _firstPoint = p1;
            _drawCount++;
            return p2;
        }

        /// <summary>
        /// Draw a zigzag line from p1 to p2.
        /// </summary>
        public Point DrawZigzag(Point p1, Point p2, float breadth = 2)
        {
            float dx = (float)(p2.X - p1.X), dy = (float)(p2.Y - p1.Y);
            float length = (float)Math.Sqrt(dx * dx + dy * dy);
            if (length < 4 * breadth) { DrawLine(p1, p2); return p2; }

            int nsegs = (int)(length / (4 * breadth));
            float segLen = length / nsegs;
            float nx = -dy / length * breadth, ny = dx / length * breadth;
            float sx = dx / length * segLen, sy = dy / length * segLen;

            var points = new List<Point> { p1 };
            float cx = (float)p1.X, cy = (float)p1.Y;
            for (int i = 0; i < nsegs; i++)
            {
                points.Add(new Point(cx + sx * 0.25f + nx, cy + sy * 0.25f + ny));
                points.Add(new Point(cx + sx * 0.5f, cy + sy * 0.5f));
                points.Add(new Point(cx + sx * 0.75f - nx, cy + sy * 0.75f - ny));
                cx += sx; cy += sy;
                points.Add(new Point(cx, cy));
            }
            return DrawPolyline(points.ToArray());
        }

        // ─── Finish / Commit ────────────────────────────────────────────

        /// <summary>
        /// Finish the current set of drawings with stroke/fill/opacity properties.
        /// </summary>
        public void Finish(float[] color = null, float[] fill = null, float width = 1,
            string lineCap = null, string lineJoin = null, float[] dashes = null,
            bool closePath = true, bool even_odd = false, float opacity = 1,
            string blendMode = null, int oc = 0, string morph = null)
        {
            if (_drawCount == 0) return;
            var sb = new StringBuilder();
            sb.AppendLine("q");

            if (opacity < 1)
            {
                sb.AppendLine($"/GS0 gs");
            }

            sb.AppendLine($"{width:F4} w");

            if (color != null && color.Length > 0)
            {
                if (color.Length == 1) sb.AppendLine($"{color[0]:F4} G");
                else if (color.Length == 3) sb.AppendLine($"{color[0]:F4} {color[1]:F4} {color[2]:F4} RG");
                else if (color.Length == 4) sb.AppendLine($"{color[0]:F4} {color[1]:F4} {color[2]:F4} {color[3]:F4} K");
            }

            if (fill != null && fill.Length > 0)
            {
                if (fill.Length == 1) sb.AppendLine($"{fill[0]:F4} g");
                else if (fill.Length == 3) sb.AppendLine($"{fill[0]:F4} {fill[1]:F4} {fill[2]:F4} rg");
                else if (fill.Length == 4) sb.AppendLine($"{fill[0]:F4} {fill[1]:F4} {fill[2]:F4} {fill[3]:F4} k");
            }

            sb.Append(_contents);
            if (closePath) sb.AppendLine("h");

            if (fill != null && color != null) sb.AppendLine(even_odd ? "B*" : "B");
            else if (fill != null) sb.AppendLine(even_odd ? "f*" : "f");
            else sb.AppendLine("S");

            sb.AppendLine("Q");
            _totalContents.Append(sb);
            _contents.Clear();
            _drawCount = 0;
            _lastPoint = null;
            _firstPoint = null;
            _pathCount++;
        }

        /// <summary>
        /// Write the accumulated content to the page.
        /// </summary>
        public void Commit(bool overlay = true)
        {
            if (_totalContents.Length == 0) return;
            var pdf = _doc.NativePdfDocument;
            var pdfPage = _page.NativePdfPage;
            var content = Encoding.UTF8.GetBytes(_totalContents.ToString());
            var buf = Helpers.BufferFromBytes(content);
            var stream = mupdf.mupdf.pdf_add_stream(pdf, buf, new mupdf.PdfObj(), 0);

            var contents = mupdf.mupdf.pdf_dict_get(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Contents"));
            if (contents.m_internal == null || mupdf.mupdf.pdf_is_array(contents) == 0)
            {
                var arr = mupdf.mupdf.pdf_new_array(pdf, 2);
                if (contents.m_internal != null) mupdf.mupdf.pdf_array_push(arr, contents);
                mupdf.mupdf.pdf_dict_put(pdfPage.obj(), mupdf.mupdf.pdf_new_name("Contents"), arr);
                contents = arr;
            }
            if (overlay) mupdf.mupdf.pdf_array_push(contents, stream);
            else mupdf.mupdf.pdf_array_insert(contents, stream, 0);

            _totalContents.Clear();
            _pathCount = 0;
        }

        // ─── Text methods ───────────────────────────────────────────────

        /// <summary>
        /// Insert text starting at a given point.
        /// </summary>
        public int InsertText(Point point, string text, float fontsize = 11, string fontname = "helv",
            float[] color = null, int renderMode = 0, float borderWidth = 0.05f, int rotate = 0)
        {
            var tw = new TextWriter(_page.Rect, color: color);
            tw.Append(point, text, fontsize: fontsize, fontname: fontname);
            tw.WriteText(_page);
            return text.Split('\n').Length;
        }

        /// <summary>
        /// Insert text into a rectangle.
        /// </summary>
        public (int rc, List<string> rest) InsertTextbox(Rect rect, string text, float fontsize = 11,
            string fontname = "helv", float[] color = null, int align = 0, int renderMode = 0,
            float borderWidth = 0.05f, int rotate = 0, float expandTabs = 1)
        {
            var tw = new TextWriter(_page.Rect, color: color);
            var rest = tw.FillTextbox(rect, text, fontsize: fontsize, fontname: fontname, align: align);
            return (rest.Count == 0 ? 0 : -1, rest);
        }

        // ─── Internal helpers ───────────────────────────────────────────

        private void DrawArc(Point center, Point from, Point to, float angle)
        {
            // Approximate arc with bezier segments
            int nSegs = (int)Math.Ceiling(Math.Abs(angle) / 90.0);
            if (nSegs == 0) nSegs = 1;
            float segAngle = angle / nSegs;
            float rad = (float)(segAngle * Math.PI / 360.0); // half angle
            float kappa = (float)(4.0 / 3.0 * Math.Tan(rad));

            double dx = from.X - center.X, dy = from.Y - center.Y;
            double radius = Math.Sqrt(dx * dx + dy * dy);
            double currentAngle = Math.Atan2(dy, dx);

            for (int i = 0; i < nSegs; i++)
            {
                double a1 = currentAngle + i * segAngle * Math.PI / 180.0;
                double a2 = a1 + segAngle * Math.PI / 180.0;
                float px1 = (float)(center.X + radius * Math.Cos(a1));
                float py1 = (float)(center.Y + radius * Math.Sin(a1));
                float px2 = (float)(center.X + radius * Math.Cos(a2));
                float py2 = (float)(center.Y + radius * Math.Sin(a2));
                float cpx1 = (float)(px1 - kappa * radius * Math.Sin(a1));
                float cpy1 = (float)(py1 + kappa * radius * Math.Cos(a1));
                float cpx2 = (float)(px2 + kappa * radius * Math.Sin(a2));
                float cpy2 = (float)(py2 - kappa * radius * Math.Cos(a2));
                _contents.AppendLine($"{cpx1:F4} {_height - cpy1:F4} {cpx2:F4} {_height - cpy2:F4} {px2:F4} {_height - py2:F4} c");
            }
        }

        private void UpdateRect(Point p) => _rect.IncludePoint(p);

        // ─── IDisposable ────────────────────────────────────────────────

        /// <summary>
        /// Releases all resources used by the <see cref="Shape"/>.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed) { _disposed = true; }
            GC.SuppressFinalize(this);
        }

        ~Shape() { Dispose(); }

        // Python/legacy compatibility aliases (mirrors _alias(Shape, ...)).
        public Point draw_bezier(Point p1, Point p2, Point p3, Point p4) => DrawBezier(p1, p2, p3, p4);
        public Point draw_circle(Point center, float radius) => DrawCircle(center, radius);
        public Point draw_curve(Point p1, Point p2, Point p3) => DrawCurve(p1, p2, p3);
        public Point draw_line(Point p1, Point p2) => DrawLine(p1, p2);
        public Point draw_oval(Rect rect) => DrawOval(rect);
        public Point draw_polyline(Point[] points) => DrawPolyline(points);
        public Point draw_quad(Quad quad) => DrawQuad(quad);
        public Point draw_rect(Rect rect) => DrawRect(rect);
        public Point draw_sector(Point center, Point point, float angle, bool fullSector = true) => DrawSector(center, point, angle, fullSector);
        public Point draw_squiggle(Point p1, Point p2, float breadth = 2) => DrawSquiggle(p1, p2, breadth);
        public Point draw_zigzag(Point p1, Point p2, float breadth = 2) => DrawZigzag(p1, p2, breadth);
        public int insert_text(Point point, string text, float fontsize = 11, string fontname = "helv",
            float[] color = null, int renderMode = 0, float borderWidth = 0.05f, int rotate = 0)
            => InsertText(point, text, fontsize, fontname, color, renderMode, borderWidth, rotate);
        public (int rc, List<string> rest) insert_textbox(Rect rect, string text, float fontsize = 11,
            string fontname = "helv", float[] color = null, int align = 0, int renderMode = 0,
            float borderWidth = 0.05f, int rotate = 0, float expandTabs = 1)
            => InsertTextbox(rect, text, fontsize, fontname, color, align, renderMode, borderWidth, rotate, expandTabs);

        /// <summary>
        /// Returns a string that represents the current shape.
        /// </summary>
        public override string ToString() => $"Shape(paths={_pathCount}, draws={_drawCount})";
    }
}
