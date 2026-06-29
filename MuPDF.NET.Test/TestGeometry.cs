using System;
using System.Collections.Generic;
using System.Reflection;
using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Port of <c>PyMuPDF-1.27.2.2/tests/test_geometry.py</c>.
    /// </summary>
    /// <remarks>
    /// * Check various construction methods of rects, points, matrices
    /// * Check matrix inversions in variations
    /// * Check algebra constructs
    /// Inputs: <c>TestDocuments/TestGeometry/</c>; outputs: <c>TestDocuments/_Output/TestGeometry/</c>.
    /// </remarks>
    [Collection("MuPDF.NET native")]
    public class TestGeometry
    {
        private const float EPSILON = Constants.Epsilon;
        private const string TestClassName = nameof(TestGeometry);

        private static string Doc(string fileName) => _Path.ForTestClass(fileName, TestClassName);

        private static string Out(string fileName) => _Path.ForOutput(fileName, TestClassName);

        private static (float x0, float y0, float x1, float y1) T(Rect r) => (r.X0, r.Y0, r.X1, r.Y1);
        private static (int x0, int y0, int x1, int y1) T(IRect r) => (r.X0, r.Y0, r.X1, r.Y1);
        private static (float a, float b, float c, float d, float e, float f) T(Matrix m) => (m.A, m.B, m.C, m.D, m.E, m.F);
        private static (float x, float y) T(Point p) => (p.X, p.Y);

        /// <summary>Python <c>~matrix</c> (<see cref="Matrix.Invert"/>; zero matrix if singular).</summary>
        private static Matrix Invert(Matrix self)
        {
            var m1 = new Matrix();
            m1.Invert(self);
            return m1;
        }

        private static float Abs(Matrix a, Matrix b) => (a - b).Norm;

        private static bool Near(Matrix a, Matrix b) => Abs(a, b) < EPSILON;

        private static void ExpectFailed(Action act)
        {
            bool failed = false;
            try
            {
                act();
            }
            catch
            {
                failed = true;
            }
            Assert.True(failed);
        }

        /// <summary>Python <c>point in rect</c> (<c>fz_is_point_inside_rect</c>).</summary>
        private static bool PyIn(Rect r, Point p) =>
            mupdf.mupdf.fz_is_point_inside_rect(p.ToFzPoint(), r.ToFzRect()) != 0;

        /// <summary>Python <c>item in rect</c> for points (and <c>not in</c> for non-points).</summary>
        private static bool PyIn(Rect r, object x)
        {
            if (x is Point p)
                return PyIn(r, p);
            return false;
        }

        /// <summary>Python <c>item in quad</c>.</summary>
        private static bool PyIn(Quad q, object x)
        {
            if (x is Point p)
                return q.Contains(p);
            if (x is Rect rect)
                return q.Contains(rect);
            if (x is Quad quad)
                return q.Contains(quad);
            return false;
        }

        /// <summary>Regression test: rect (PyMuPDF <c>tests/test_geometry.py::test_rect</c>).</summary>
        [Fact]
        public void test_rect()
        {
            Assert.Equal((0, 0, 0, 0), T(new Rect()));
            Assert.Equal((0, 12, 0, 0), T(new Rect(0, 12, 0, 0))); // Rect(y0=12)
            Assert.Equal((10, 20, 12, 200), T(new Rect(10, 20, 12, 200))); // Rect(10, 20, 100, 200, x1=12)
            var p1 = new Point(10, 20);
            var p2 = new Point(100, 200);
            var p3 = new Point(150, 250);
            var r = new Rect(10, 20, 100, 200);
            var r_tuple = T(r);
            Assert.Equal(r_tuple, T(new Rect(p1, p2)));
            Assert.Equal(r_tuple, T(new Rect(p1, 100, 200)));
            Assert.Equal(r_tuple, T(new Rect(10, 20, p2)));
            Assert.Equal((10, 20, 150, 250), T(r.IncludePoint(p3)));
            r = new Rect(10, 20, 100, 200);
            Assert.Equal((10, 20, 110, 220), T(r.IncludeRect(new Rect(100, 200, 110, 220))));
            r = new Rect(10, 20, 100, 200);
            // include empty rect makes no change
            Assert.Equal(r_tuple, T(r.IncludeRect(new Rect(0, 0, 0, 0))));
            // include invalid rect makes no change
            Assert.Equal(r_tuple, T(r.IncludeRect(new Rect(1, 1, -1, -1))));
            r = new Rect();
            for (int i = 0; i < 4; i++)
                r[i] = i + 1;
            Assert.Equal(new Rect(1, 2, 3, 4), r);
            Assert.Equal(new Rect(), new Rect() / 5);
            Assert.Equal(new Rect(1, 1, 2, 2), new Rect(1, 1, 2, 2) / Matrix.Identity);
            ExpectFailed(() =>
            {
                var ctor = typeof(Rect).GetConstructor(new[] { typeof(float) });
                if (ctor == null)
                    throw new MissingMethodException();
                ctor.Invoke(new object[] { 1.0 });
            });
            ExpectFailed(() =>
            {
                var ctor = typeof(Rect).GetConstructor(new[]
                {
                    typeof(float), typeof(float), typeof(float), typeof(float), typeof(float),
                });
                if (ctor == null)
                    throw new MissingMethodException();
                ctor.Invoke(new object[] { 1.0, 2.0, 3.0, 4.0, 5.0 });
            });
            ExpectFailed(() =>
            {
                var ctor = typeof(Rect).GetConstructor(new[] { typeof(float[]) });
                if (ctor == null)
                    throw new MissingMethodException();
                ctor.Invoke(new object[] { new float[] { 1, 2, 3, 4, 5 } });
            });
            ExpectFailed(() => _ = new Rect(1, 2, 3, (float)Convert.ToDouble("x")));
            ExpectFailed(() =>
            {
                var r0 = new Rect();
                r0[5] = 1;
            });
        }

        /// <summary>Regression test: irect (PyMuPDF <c>tests/test_geometry.py::test_irect</c>).</summary>
        [Fact]
        public void test_irect()
        {
            var p1 = new Point(10, 20);
            var p2 = new Point(100, 200);
            var p3 = new Point(150, 250);
            var r = new IRect(10, 20, 100, 200);
            var r_tuple = T(r);
            Assert.Equal(r_tuple, T(new IRect(new Rect(p1, p2))));
            Assert.Equal(r_tuple, T(new IRect(new Rect(p1, 100, 200))));
            Assert.Equal(r_tuple, T(new IRect(new Rect(10, 20, p2))));
            Assert.Equal((10, 20, 150, 250), T(r.IncludePoint(p3)));
            r = new IRect(10, 20, 100, 200);
            Assert.Equal((10, 20, 110, 220), T(r.IncludeRect(new IRect(100, 200, 110, 220))));
            r = new IRect(10, 20, 100, 200);
            // include empty rect makes no change
            Assert.Equal(r_tuple, T(r.IncludeRect(new IRect(0, 0, 0, 0))));
            r = new IRect();
            for (int i = 0; i < 4; i++)
                r[i] = i + 1;
            Assert.Equal(new IRect(1, 2, 3, 4), r);

            ExpectFailed(() =>
            {
                var ctor = typeof(IRect).GetConstructor(new[] { typeof(int) });
                if (ctor == null)
                    throw new MissingMethodException();
                ctor.Invoke(new object[] { 1 });
            });
            ExpectFailed(() =>
            {
                var ctor = typeof(IRect).GetConstructor(new[]
                {
                    typeof(int), typeof(int), typeof(int), typeof(int), typeof(int),
                });
                if (ctor == null)
                    throw new MissingMethodException();
                ctor.Invoke(new object[] { 1, 2, 3, 4, 5 });
            });
            ExpectFailed(() =>
            {
                var ctor = typeof(IRect).GetConstructor(new[] { typeof(int[]) });
                if (ctor == null)
                    throw new MissingMethodException();
                ctor.Invoke(new object[] { new int[] { 1, 2, 3, 4, 5 } });
            });
            ExpectFailed(() => _ = new IRect(1, 2, 3, Convert.ToInt32("x")));
            ExpectFailed(() =>
            {
                r = new IRect();
                r[5] = 1;
            });
        }

        /// <summary>Regression test: inversion (PyMuPDF <c>tests/test_geometry.py::test_inversion</c>).</summary>
        [Fact]
        public void test_inversion()
        {
            int alpha = 255;
            var m1 = new Matrix(alpha);
            var m2 = new Matrix(-alpha);
            var m3 = m1 * m2; // should equal identity matrix
            Assert.True(Abs(m3, Matrix.Identity) < EPSILON);
            var m = new Matrix(1, 0, 1, 0, 1, 0); // not invertible!
            // inverted matrix must be zero
            Assert.Equal(new Matrix(), Invert(m));
        }

        /// <summary>Regression test: matrix (PyMuPDF <c>tests/test_geometry.py::test_matrix</c>).</summary>
        [Fact]
        public void test_matrix()
        {
            Assert.Equal((0, 0, 0, 0, 0, 0), T(new Matrix()));
            Assert.Equal((0, 1, -1, 0, 0, 0), T(new Matrix(90)));
            Assert.Equal((0, 0, 1, 0, 0, 0), T(new Matrix(0, 0, 1, 0, 0, 0))); // Matrix(c=1)
            Assert.Equal((0, 1, -1, 0, 5, 0), T(new Matrix(90) { E = 5 })); // Matrix(90, e=5)
            var m45p = new Matrix(45);
            var m45m = new Matrix(-45);
            var m90 = new Matrix(90);
            Assert.True(Abs(m90, m45p * m45p) < EPSILON);
            Assert.True(Abs(Matrix.Identity, m45p * m45m) < EPSILON);
            Assert.True(Abs(m45p, Invert(m45m)) < EPSILON);
            Assert.Equal(new Matrix(1, 3, 2, 1, 0, 0), new Matrix(2, 3, 1));
            var m = new Matrix(2, 3, 1);
            m.Invert();
            Assert.True(Near(m * new Matrix(2, 3, 1), Matrix.Identity));
            Assert.Equal(new Matrix(1, 0, 0, 1, 2, 3), new Matrix(1, 1).Pretranslate(2, 3));
            Assert.Equal(new Matrix(2, 0, 0, 3, 0, 0), new Matrix(1, 1).Prescale(2, 3));
            Assert.Equal(new Matrix(1, 3, 2, 1, 0, 0), new Matrix(1, 1).Preshear(2, 3));
            Assert.True(Abs(new Matrix(1, 1).Prerotate(30), new Matrix(30)) < EPSILON);
            float small = 1e-6f;
            Assert.Equal(new Matrix(90), new Matrix(1, 1).Prerotate(90 + small));
            Assert.Equal(new Matrix(180), new Matrix(1, 1).Prerotate(180 + small));
            Assert.Equal(new Matrix(270), new Matrix(1, 1).Prerotate(270 + small));
            Assert.Equal(new Matrix(0), new Matrix(1, 1).Prerotate(small));
            Assert.Equal(
                new Matrix(3, 0, 0, 8, 0, 0),
                new Matrix(1, 1).ConcatInto(new Matrix(1, 2), new Matrix(3, 4))); // .concat(
            Assert.Equal(new Matrix(1, 2, 3, 4, 5, 6), new Matrix(1, 2, 3, 4, 5, 6) / 1);
            Assert.Equal(m[0], m.A);
            Assert.Equal(m[1], m.B);
            Assert.Equal(m[2], m.C);
            Assert.Equal(m[3], m.D);
            Assert.Equal(m[4], m.E);
            Assert.Equal(m[5], m.F);
            m = new Matrix();
            for (int i = 0; i < 6; i++)
                m[i] = i + 1;
            Assert.Equal(new Matrix(1, 2, 3, 4, 5, 6), m);
            ExpectFailed(() =>
            {
                var ctor = typeof(Matrix).GetConstructor(new[]
                {
                    typeof(float), typeof(float), typeof(float),
                });
                if (ctor == null)
                    throw new MissingMethodException();
                ctor.Invoke(new object[] { 1.0, 2.0, 3.0 });
            });
            ExpectFailed(() =>
            {
                var ctor = typeof(Matrix).GetConstructor(new[]
                {
                    typeof(float), typeof(float), typeof(float),
                    typeof(float), typeof(float), typeof(float), typeof(float),
                });
                if (ctor == null)
                    throw new MissingMethodException();
                ctor.Invoke(new object[] { 1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0 });
            });
            ExpectFailed(() =>
            {
                var ctor = typeof(Matrix).GetConstructor(new[] { typeof(float[]) });
                if (ctor == null)
                    throw new MissingMethodException();
                ctor.Invoke(new object[] { new float[] { 1, 2, 3, 4, 5, 6, 7 } });
            });
            ExpectFailed(() => _ = new Matrix(1, 2, 3, 4, 5, (float)Convert.ToDouble("x")));
            ExpectFailed(() =>
            {
                var singular = new Matrix(1, 0, 1, 0, 1, 0);
                var n = new Matrix(1, 1) / singular;
            });
        }

        /// <summary>Regression test: point (PyMuPDF <c>tests/test_geometry.py::test_point</c>).</summary>
        [Fact]
        public void test_point()
        {
            Assert.Equal((0, 0), T(new Point()));
            Assert.Equal(new Point(1, -1).Unit, new Point(5, -5).Unit);
            Assert.Equal(new Point(-1, -1).AbsUnit, new Point(1, 1).Unit);
            Assert.Equal(0, new Point(1, 1).DistanceTo(new Point(1, 1)));
            Assert.Equal(0, new Point(1, 1).DistanceTo(new Rect(1, 1, 2, 2)));
            Assert.True(new Point().DistanceTo(new Rect(1, 1, 2, 2)) > 0);
            ExpectFailed(() =>
            {
                var ctor = typeof(Point).GetConstructor(new[]
                {
                    typeof(float), typeof(float), typeof(float),
                });
                if (ctor == null)
                    throw new MissingMethodException();
                ctor.Invoke(new object[] { 1.0, 2.0, 3.0 });
            });
            ExpectFailed(() => _ = new Point(new float[] { 1, 2, 3 }));
            ExpectFailed(() => _ = new Point(1, (float)Convert.ToDouble("x")));
            ExpectFailed(() =>
            {
                var p = new Point();
                p[3] = 1;
            });
        }

        /// <summary>Regression test: algebra (PyMuPDF <c>tests/test_geometry.py::test_algebra</c>).</summary>
        [Fact]
        public void test_algebra()
        {
            var p = new Point(1, 2);
            var m = new Matrix(1, 2, 3, 4, 5, 6);
            var r = new Rect(1, 1, 2, 2);
            Assert.Equal(p + p, p * 2);
            Assert.Equal(new Point(), p - p);
            Assert.Equal(m + m, m * 2);
            Assert.Equal(new Matrix(), m - m);
            Assert.Equal(r + r, r * 2);
            Assert.Equal(new Rect(), r - r);
            Assert.Equal(new Point(6, 7), p + 5);
            Assert.Equal(new Matrix(6, 7, 8, 9, 10, 11), m + 5);
            Assert.True(PyIn(r, r.TL));
            Assert.False(PyIn(r, r.TR));
            Assert.False(PyIn(r, r.BR));
            Assert.False(PyIn(r, r.BL));
            Assert.Equal(new Point(12, 16), p * m);
            Assert.Equal(new Rect(9, 12, 13, 18), r * m);
            Assert.True((new Rect(1, 1, 2, 2) & new Rect(2, 2, 3, 3)).IsEmpty);
            Assert.False(new Rect(1, 1, 2, 2).Intersects(new Rect(2, 2, 4, 4)));
            ExpectFailed(() => { dynamic dm = m; dynamic dp = p; object x = dm + dp; });
            ExpectFailed(() => { dynamic dm = m; dynamic dr = r; object x = dm + dr; });
            ExpectFailed(() => { dynamic dp = p; dynamic dr = r; object x = dp + dr; });
            ExpectFailed(() => { dynamic dr = r; dynamic dm = m; object x = dr + dm; });
            Assert.False(PyIn(r, m));
        }

        /// <summary>Regression test: quad (PyMuPDF <c>tests/test_geometry.py::test_quad</c>).</summary>
        [Fact]
        public void test_quad()
        {
            var r = new Rect(10, 10, 20, 20);
            var q = r.Quad;
            Assert.True(q.IsRectangular);
            Assert.False(q.IsEmpty);
            Assert.True(q.IsConvex);
            q = q * new Matrix(1, 1).Preshear(2, 3);
            Assert.False(q.IsRectangular);
            Assert.False(q.IsEmpty);
            Assert.True(q.IsConvex);
            Assert.False(PyIn(q, r.TL));
            Assert.False(PyIn(q, r));
            Assert.False(PyIn(q, r.Quad));
            ExpectFailed(() =>
            {
                q[5] = new Point();
            });
            ExpectFailed(() =>
            {
                q /= new Matrix(1, 0, 1, 0, 1, 0); // q /= (1, 0, 1, 0, 1, 0)
            });
        }

        /// <summary>Regression test: pageboxes (PyMuPDF <c>tests/test_geometry.py::test_pageboxes</c>).</summary>
        [Fact]
        public void test_pageboxes()
        {
            /// Tests concerning ArtBox, TrimBox, BleedBox.
            using var doc = new Document();
            var page = doc.NewPage();
            Assert.Equal(page.CropBox, page.ArtBox);
            Assert.Equal(page.ArtBox, page.BleedBox);
            Assert.Equal(page.BleedBox, page.TrimBox);
            Action<Rect>[] rect_methods =
            {
                page.set_cropbox,
                page.set_artbox,
                page.set_bleedbox,
                page.set_trimbox,
            };
            string[] keys = { "CropBox", "ArtBox", "BleedBox", "TrimBox" };
            var rect = new Rect(100, 200, 400, 700);
            foreach (var f in rect_methods)
                f(rect);
            foreach (var key in keys)
                Assert.Equal(("array", "[100 142 400 642]"), doc.XrefGetKey(page.Xref, key));
            Assert.Equal(page.CropBox, page.ArtBox);
            Assert.Equal(page.ArtBox, page.BleedBox);
            Assert.Equal(page.BleedBox, page.TrimBox);
        }

        /// <summary>Regression test: 3163 (PyMuPDF <c>tests/test_geometry.py::test_3163</c>).</summary>
        [Fact]
        public void test_3163()
        {
            var b = new Dictionary<string, object>
            {
                ["number"] = 0,
                ["type"] = 0,
                ["bbox"] = new float[]
                {
                    403.3577880859375f, 330.8871765136719f, 541.2731323242188f, 349.5766296386719f,
                },
                ["lines"] = new object[]
                {
                    new Dictionary<string, object>
                    {
                        ["spans"] = new object[]
                        {
                            new Dictionary<string, object>
                            {
                                ["size"] = 14.0,
                                ["flags"] = 4,
                                ["font"] = "SFHello-Medium",
                                ["color"] = 1907995,
                                ["ascender"] = 1.07373046875f,
                                ["descender"] = -0.26123046875f,
                                ["text"] = "Inclusion and diversity",
                                ["origin"] = new float[] { 403.3577880859375f, 345.9194030761719f },
                                ["bbox"] = new float[]
                                {
                                    403.3577880859375f, 330.8871765136719f, 541.2731323242188f, 349.5766296386719f,
                                },
                            },
                        },
                        ["wmode"] = 0,
                        ["dir"] = new float[] { 1.0f, 0.0f },
                        ["bbox"] = new float[]
                        {
                            403.3577880859375f, 330.8871765136719f, 541.2731323242188f, 349.5766296386719f,
                        },
                    },
                },
            };
            var bb = (float[])b["bbox"];
            var bbox = new IRect(new Rect(bb[0], bb[1], bb[2], bb[3]));
        }

        /// <summary>Regression test: 3182 (PyMuPDF <c>tests/test_geometry.py::test_3182</c>).</summary>
        [Fact]
        public void test_3182()
        {
            var pix = new Pixmap(Doc("img-transparent.png"));
            var rect = new Rect(0, 0, 100, 100);
            pix.invert_irect(new IRect(rect));
            pix.Save(Out("test_3182.png"));
        }
    }
}
