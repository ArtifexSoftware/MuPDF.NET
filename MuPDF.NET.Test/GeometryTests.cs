using Xunit;

namespace MuPDF.NET.Test
{
    /// <summary>
    /// Tests for Point, Rect, IRect, Matrix, Quad geometry types.
    /// Ported from tests/test_geometry.py.
    /// </summary>
    public class GeometryTests
    {
        // ─── Rect tests ─────────────────────────────────────────────────

        [Fact]
        public void Rect_DefaultIsZero()
        {
            var r = new Rect();
            Assert.Equal(0, r.X0);
            Assert.Equal(0, r.Y0);
            Assert.Equal(0, r.X1);
            Assert.Equal(0, r.Y1);
        }

        [Fact]
        public void Rect_FromCoordinates()
        {
            var r = new Rect(10, 20, 100, 200);
            Assert.Equal(10, r.X0);
            Assert.Equal(20, r.Y0);
            Assert.Equal(100, r.X1);
            Assert.Equal(200, r.Y1);
        }

        [Fact]
        public void Rect_FromPoints()
        {
            var p1 = new Point(10, 20);
            var p2 = new Point(100, 200);
            var r = new Rect(p1, p2);
            Assert.Equal(10, r.X0);
            Assert.Equal(20, r.Y0);
            Assert.Equal(100, r.X1);
            Assert.Equal(200, r.Y1);
        }

        [Fact]
        public void Rect_IncludePoint()
        {
            var r = new Rect(10, 20, 100, 200);
            var p = new Point(150, 250);
            var r2 = r.IncludePoint(p);
            Assert.Equal(10, r2.X0);
            Assert.Equal(20, r2.Y0);
            Assert.Equal(150, r2.X1);
            Assert.Equal(250, r2.Y1);
        }

        /// <summary>
        /// Default rect is empty; chaining include_point uses MuPDF fz_include_point_in_rect (same as PyMuPDF).
        /// From (0,0,0,0), corners union to the axis-aligned box through the origin and the points.
        /// </summary>
        [Fact]
        public void Rect_IncludePoint_ChainFromEmpty()
        {
            var r = new Rect();
            r.IncludePoint(new Point(50, 50));
            r.IncludePoint(new Point(200, 200));
            Assert.Equal(0, r.X0);
            Assert.Equal(0, r.Y0);
            Assert.Equal(200, r.X1);
            Assert.Equal(200, r.Y1);
            Assert.True(r.Width > 0);
            Assert.True(r.Height > 0);
        }

        [Fact]
        public void Rect_IncludeRect()
        {
            var r = new Rect(10, 20, 100, 200);
            var r2 = r.IncludeRect(new Rect(100, 200, 110, 220));
            Assert.Equal(10, r2.X0);
            Assert.Equal(20, r2.Y0);
            Assert.Equal(110, r2.X1);
            Assert.Equal(220, r2.Y1);
        }

        [Fact]
        public void Rect_IncludeEmptyRectNoChange()
        {
            var r = new Rect(10, 20, 100, 200);
            var r2 = r.IncludeRect(new Rect(0, 0, 0, 0));
            Assert.Equal(r, r2);
        }

        /// <summary>Including an invalid (non-normalized) rect must not change the receiver (test_geometry.py).</summary>
        [Fact]
        public void Rect_IncludeInvalidRectNoChange()
        {
            var r = new Rect(10, 20, 100, 200);
            var before = new Rect(r);
            r.IncludeRect(new Rect(1, 1, -1, -1));
            Assert.Equal(before, r);
        }

        /// <summary>PyMuPDF: default rect divided by a scalar stays the default empty rect.</summary>
        [Fact]
        public void Rect_DivideByScalar_PreservesDefaultEmpty()
        {
            var r = new Rect() / 5;
            Assert.Equal(new Rect(), r);
        }

        [Fact]
        public void Rect_Indexer()
        {
            var r = new Rect();
            r[0] = 1; r[1] = 2; r[2] = 3; r[3] = 4;
            Assert.Equal(new Rect(1, 2, 3, 4), r);
        }

        [Fact]
        public void Rect_IndexOutOfRange()
        {
            var r = new Rect();
            Assert.Throws<IndexOutOfRangeException>(() => r[5] = 1);
        }

        [Fact]
        public void Rect_DivideByIdentity()
        {
            var r = new Rect(1, 1, 2, 2);
            var r2 = new Rect(r);
            r2.Transform(Matrix.Identity);
            Assert.Equal(r, r2);
        }

        [Fact]
        public void Rect_WidthHeight()
        {
            var r = new Rect(10, 20, 110, 220);
            Assert.Equal(100, r.Width);
            Assert.Equal(200, r.Height);
        }

        [Fact]
        public void Rect_GetArea()
        {
            var r = new Rect(0, 0, 10, 20);
            Assert.Equal(200, r.GetArea());
        }

        [Fact]
        public void Rect_IsEmpty()
        {
            Assert.True(new Rect().IsEmpty);
            Assert.False(new Rect(0, 0, 1, 1).IsEmpty);
        }

        [Fact]
        public void Rect_Intersects()
        {
            var r1 = new Rect(0, 0, 10, 10);
            var r2 = new Rect(5, 5, 15, 15);
            Assert.True(r1.Intersects(r2));

            var r3 = new Rect(20, 20, 30, 30);
            Assert.False(r1.Intersects(r3));
        }

        [Fact]
        public void Rect_ContainsPoint()
        {
            var r = new Rect(0, 0, 10, 10);
            Assert.True(r.Contains(new Point(5, 5)));
            Assert.False(r.Contains(new Point(15, 15)));
        }

        [Fact]
        public void Rect_Round()
        {
            var r = new Rect(1.2, 3.7, 10.5, 20.1);
            var ir = r.Round();
            Assert.Equal(1, ir.X0);
            Assert.Equal(3, ir.Y0);
            Assert.Equal(11, ir.X1);
            Assert.Equal(21, ir.Y1);
        }

        [Fact]
        public void Rect_Normalize()
        {
            var r = new Rect(10, 20, 5, 10);
            var rn = r.Normalize();
            Assert.True(rn.IsValid);
            Assert.Equal(5, rn.X0);
            Assert.Equal(10, rn.Y0);
            Assert.Equal(10, rn.X1);
            Assert.Equal(20, rn.Y1);
        }

        // ─── IRect tests ────────────────────────────────────────────────

        [Fact]
        public void IRect_DefaultIsZero()
        {
            var r = new IRect();
            Assert.Equal(0, r.X0);
            Assert.Equal(0, r.Y0);
            Assert.Equal(0, r.X1);
            Assert.Equal(0, r.Y1);
        }

        [Fact]
        public void IRect_FromCoordinates()
        {
            var r = new IRect(10, 20, 100, 200);
            Assert.Equal(10, r.X0);
            Assert.Equal(100, r.X1);
        }

        [Fact]
        public void IRect_WidthHeight()
        {
            var r = new IRect(10, 20, 110, 220);
            Assert.Equal(100, r.Width);
            Assert.Equal(200, r.Height);
        }

        [Fact]
        public void IRect_Indexer()
        {
            var r = new IRect();
            r[0] = 1; r[1] = 2; r[2] = 3; r[3] = 4;
            Assert.Equal(new IRect(1, 2, 3, 4), r);
        }

        [Fact]
        public void IRect_IndexOutOfRange()
        {
            var r = new IRect();
            Assert.Throws<IndexOutOfRangeException>(() => r[5] = 1);
        }

        [Fact]
        public void IRect_IncludePoint()
        {
            var r = new IRect(10, 20, 100, 200);
            r.IncludePoint(new Point(150, 250));
            Assert.Equal(10, r.X0);
            Assert.Equal(20, r.Y0);
            Assert.Equal(150, r.X1);
            Assert.Equal(250, r.Y1);
        }

        [Fact]
        public void IRect_IncludeRect()
        {
            var r = new IRect(10, 20, 100, 200);
            r.IncludeRect(new IRect(100, 200, 110, 220));
            Assert.Equal(10, r.X0);
            Assert.Equal(20, r.Y0);
            Assert.Equal(110, r.X1);
            Assert.Equal(220, r.Y1);
        }

        [Fact]
        public void IRect_IncludeEmptyRectNoChange()
        {
            var r = new IRect(10, 20, 100, 200);
            var before = new IRect(r);
            r.IncludeRect(new IRect(0, 0, 0, 0));
            Assert.Equal(before, r);
        }

        // ─── Point tests ────────────────────────────────────────────────

        [Fact]
        public void Point_DefaultIsZero()
        {
            var p = new Point();
            Assert.Equal(0, p.X);
            Assert.Equal(0, p.Y);
        }

        [Fact]
        public void Point_UnitVector()
        {
            var u1 = new Point(1, -1).Unit;
            var u2 = new Point(5, -5).Unit;
            Assert.True(TestHelper.IsClose(u1.X, u2.X));
            Assert.True(TestHelper.IsClose(u1.Y, u2.Y));
        }

        [Fact]
        public void Point_AbsUnit()
        {
            var au = new Point(-1, -1).AbsUnit;
            var u = new Point(1, 1).Unit;
            Assert.True(TestHelper.IsClose(au.X, u.X));
            Assert.True(TestHelper.IsClose(au.Y, u.Y));
        }

        [Fact]
        public void Point_DistanceToPoint()
        {
            var p = new Point(1, 1);
            Assert.Equal(0, p.DistanceTo(new Point(1, 1)));
        }

        [Fact]
        public void Point_DistanceToRect()
        {
            var p = new Point(1, 1);
            Assert.Equal(0, p.DistanceTo(new Rect(1, 1, 2, 2)));
        }

        [Fact]
        public void Point_DistanceToRectPositive()
        {
            var p = new Point(0, 0);
            Assert.True(p.DistanceTo(new Rect(1, 1, 2, 2)) > 0);
        }

        [Fact]
        public void Point_Arithmetic()
        {
            var p = new Point(1, 2);
            var p2 = p + p;
            Assert.Equal(new Point(2, 4), p2);

            var p3 = p - new Point(1, 2);
            Assert.Equal(new Point(0, 0), p3);

            var p4 = p * 3;
            Assert.Equal(new Point(3, 6), p4);
        }

        [Fact]
        public void Point_Norm()
        {
            var p = new Point(3, 4);
            Assert.True(TestHelper.IsClose(5.0, p.Norm));
        }

        [Fact]
        public void Point_IndexOutOfRange()
        {
            var p = new Point();
            Assert.Throws<IndexOutOfRangeException>(() => p[3] = 1);
        }

        // ─── Matrix tests ───────────────────────────────────────────────

        [Fact]
        public void Matrix_DefaultIsZero()
        {
            var m = new Matrix();
            Assert.Equal(0, m.A);
            Assert.Equal(0, m.B);
            Assert.Equal(0, m.C);
            Assert.Equal(0, m.D);
            Assert.Equal(0, m.E);
            Assert.Equal(0, m.F);
        }

        [Fact]
        public void Matrix_Rotation90()
        {
            var m = Matrix.Rotation(90);
            Assert.True(TestHelper.IsClose(0, m.A));
            Assert.True(TestHelper.IsClose(1, m.B));
            Assert.True(TestHelper.IsClose(-1, m.C));
            Assert.True(TestHelper.IsClose(0, m.D));
        }

        [Fact]
        public void Matrix_RotationInversion()
        {
            var m45p = Matrix.Rotation(45);
            var m45m = Matrix.Rotation(-45);
            var product = m45p * m45m;
            Assert.True((product - Matrix.Identity).Norm < Constants.Epsilon);
        }

        [Fact]
        public void Matrix_NonInvertibleIsZero()
        {
            var m = new Matrix(1, 0, 1, 0, 1, 0);
            Assert.Null(m.Inverted());
        }

        [Fact]
        public void Matrix_Pretranslate()
        {
            var m = new Matrix(1, 0, 0, 1, 0, 0);
            var m2 = m.Pretranslate(2, 3);
            Assert.Equal(1, m2.A);
            Assert.Equal(0, m2.B);
            Assert.Equal(0, m2.C);
            Assert.Equal(1, m2.D);
            Assert.Equal(2, m2.E);
            Assert.Equal(3, m2.F);
        }

        [Fact]
        public void Matrix_Prescale()
        {
            var m = new Matrix(1, 0, 0, 1, 0, 0);
            var m2 = m.Prescale(2, 3);
            Assert.Equal(2, m2.A);
            Assert.Equal(0, m2.B);
            Assert.Equal(0, m2.C);
            Assert.Equal(3, m2.D);
        }

        [Fact]
        public void Matrix_Preshear()
        {
            var m = new Matrix(1, 0, 0, 1, 0, 0);
            var m2 = m.Preshear(2, 3);
            Assert.Equal(1, m2.A);
            Assert.Equal(3, m2.B);
            Assert.Equal(2, m2.C);
            Assert.Equal(1, m2.D);
        }

        [Fact]
        public void Matrix_Indexer()
        {
            var m = new Matrix(1, 2, 3, 4, 5, 6);
            Assert.Equal(m.A, m[0]);
            Assert.Equal(m.B, m[1]);
            Assert.Equal(m.C, m[2]);
            Assert.Equal(m.D, m[3]);
            Assert.Equal(m.E, m[4]);
            Assert.Equal(m.F, m[5]);
        }

        [Fact]
        public void Matrix_IndexerSet()
        {
            var m = new Matrix();
            for (int i = 0; i < 6; i++) m[i] = i + 1;
            Assert.Equal(new Matrix(1, 2, 3, 4, 5, 6), m);
        }

        [Fact]
        public void Matrix_Identity()
        {
            var id = Matrix.Identity;
            Assert.Equal(1, id.A);
            Assert.Equal(0, id.B);
            Assert.Equal(0, id.C);
            Assert.Equal(1, id.D);
            Assert.Equal(0, id.E);
            Assert.Equal(0, id.F);
        }

        [Fact]
        public void Matrix_IsRectilinear()
        {
            Assert.True(Matrix.Identity.IsRectilinear);
            Assert.True(Matrix.Rotation(90).IsRectilinear);
            Assert.False(Matrix.Rotation(45).IsRectilinear);
        }

        /// <summary>PyMuPDF <c>Matrix(1, 1) / singular</c> raises (test_geometry.py).</summary>
        [Fact]
        public void Matrix_DivisionBySingularThrows()
        {
            var singular = new Matrix(1, 0, 1, 0, 1, 0);
            Assert.Throws<DivideByZeroException>(() => _ = Matrix.Identity / singular);
        }

        /// <summary>PyMuPDF <c>Matrix(1, 1).concat(Matrix(1, 2), Matrix(3, 4))</c> (zoom × zoom).</summary>
        [Fact]
        public void Matrix_Concat_ZoomMatrices_PythonSample()
        {
            var one = new Matrix(1, 0, 0, 2, 0, 0);
            var two = new Matrix(3, 0, 0, 4, 0, 0);
            var m = new Matrix(1, 1).Concat(one, two);
            Assert.Equal(new Matrix(3, 0, 0, 8, 0, 0), m);
        }

        /// <summary>PyMuPDF shear constructor <c>Matrix(2, 3, 1)</c> matches <c>preshear</c> on identity.</summary>
        [Fact]
        public void Matrix_Shear23MatchesPreshearOnIdentity()
        {
            var fromPreshear = new Matrix(1, 0, 0, 1, 0, 0).Preshear(2, 3);
            Assert.Equal(new Matrix(1, 3, 2, 1, 0, 0), fromPreshear);
            Assert.Equal(fromPreshear, new Matrix(2, 3, 1));
            Assert.Equal(fromPreshear, Matrix.Shear(2, 3));
        }

        [Fact]
        public void Matrix_ConstructorSingleAngleMatchesRotation()
        {
            Assert.Equal(Matrix.Rotation(90), new Matrix(90));
            Assert.Equal(Matrix.Rotation(-45), new Matrix(-45));
        }

        [Fact]
        public void Matrix_ConstructorTripleZoomMatchesTwoArgZoom()
        {
            Assert.Equal(new Matrix(2, 3), new Matrix(2, 3, 0));
        }

        [Fact]
        public void Matrix_ConstructorTripleInvalidThirdThrows()
        {
            Assert.Throws<ArgumentException>(() => new Matrix(1, 2, 2));
        }

        /// <summary>PyMuPDF <c>prerotate(90 + 1e-6)</c> snaps to cardinal rotations.</summary>
        [Fact]
        public void Matrix_PrerotateSnapsNearCardinals()
        {
            double small = 1e-6;
            var id = new Matrix(1, 0, 0, 1, 0, 0);
            Assert.True((new Matrix(id).Prerotate(90 + small) - Matrix.Rotation(90)).Norm < Constants.Epsilon);
            Assert.True((new Matrix(id).Prerotate(180 + small) - Matrix.Rotation(180)).Norm < Constants.Epsilon);
            Assert.True((new Matrix(id).Prerotate(270 + small) - Matrix.Rotation(270)).Norm < Constants.Epsilon);
            Assert.True((new Matrix(id).Prerotate(small) - Matrix.Rotation(0)).Norm < Constants.Epsilon);
        }

        /// <summary>PyMuPDF <c>test_inversion</c> large opposite rotations compose to identity.</summary>
        [Fact]
        public void Matrix_OppositeRotationsComposeToIdentity()
        {
            var m1 = Matrix.Rotation(255);
            var m2 = Matrix.Rotation(-255);
            Assert.True(((m1 * m2) - Matrix.Identity).Norm < Constants.Epsilon);
        }

        /// <summary>PyMuPDF <c>abs(m45p - ~m45m) &lt; EPSILON</c>.</summary>
        [Fact]
        public void Matrix_Pos45MatchesInverseOfNeg45()
        {
            var m45p = Matrix.Rotation(45);
            var invNeg = Matrix.Rotation(-45).Inverted();
            Assert.NotNull(invNeg);
            Assert.True((m45p - invNeg).Norm < Constants.Epsilon);
        }

        // ─── Quad tests ─────────────────────────────────────────────────

        [Fact]
        public void Quad_FromRect()
        {
            var r = new Rect(0, 0, 100, 100);
            var q = r.Quad;
            Assert.True(q.IsRectangular);
            Assert.True(TestHelper.IsClose(10000, q.Area));
        }

        [Fact]
        public void Quad_EmptyRect()
        {
            var q = new Rect().Quad;
            Assert.True(q.IsEmpty);
            Assert.Equal(0, q.Area);
        }

        [Fact]
        public void Quad_RectRoundTrip()
        {
            var r = new Rect(10, 20, 100, 200);
            var q = new Quad(r);
            var r2 = q.Rect;
            Assert.Equal(r.X0, r2.X0);
            Assert.Equal(r.Y0, r2.Y0);
            Assert.Equal(r.X1, r2.X1);
            Assert.Equal(r.Y1, r2.Y1);
        }

        // ─── Cross-type algebra (test_geometry.py test_algebra) ─────────

        [Fact]
        public void Algebra_PointTimesMatrix()
        {
            var p = new Point(1, 2);
            var m = Matrix.Identity;
            var p2 = p.Transform(m);
            Assert.Equal(p, p2);
        }

        [Fact]
        public void Algebra_PointTimesMatrix_DoesNotMutateOperand()
        {
            var p = new Point(1, 2);
            var m = new Matrix(1, 2, 3, 4, 5, 6);
            var q = p * m;
            Assert.Equal(new Point(12, 16), q);
            Assert.Equal(new Point(1, 2), p);
        }

        [Fact]
        public void Algebra_PointPlusPointEqualsScale()
        {
            var p = new Point(1, 2);
            Assert.Equal(p + p, p * 2);
            Assert.Equal(new Point(), p - p);
        }

        [Fact]
        public void Algebra_MatrixPlusMatrixEqualsScale()
        {
            var m = new Matrix(1, 2, 3, 4, 5, 6);
            Assert.Equal(m + m, m * 2);
            Assert.Equal(new Matrix(), m - m);
        }

        [Fact]
        public void Algebra_RectPlusRectEqualsScale()
        {
            var r = new Rect(1, 1, 2, 2);
            Assert.Equal(r + r, r * 2);
            Assert.Equal(new Rect(), r - r);
        }

        [Fact]
        public void Algebra_RectTimesMatrix_PythonSample()
        {
            var r = new Rect(1, 1, 2, 2);
            var m = new Matrix(1, 2, 3, 4, 5, 6);
            var t = r * m;
            Assert.Equal(new Rect(9, 12, 13, 18), t);
        }

        [Fact]
        public void Algebra_RectIntersectionTouchingCornersIsEmpty()
        {
            var a = new Rect(1, 1, 2, 2);
            var b = new Rect(2, 2, 3, 3);
            Assert.True((a & b).IsEmpty);
            Assert.False(a.Intersects(b));
        }

        [Fact]
        public void Algebra_PointDivideBySingularMatrixThrows()
        {
            var singular = new Matrix(1, 0, 1, 0, 1, 0);
            Assert.Throws<DivideByZeroException>(() => _ = new Point(1, 1) / singular);
        }

        [Fact]
        public void Algebra_RectContainsPoint()
        {
            var r = new Rect(0, 0, 595, 842);
            Assert.True(r.Contains(new Point(100, 100)));
        }

        // ─── Quad transform (test_geometry.py test_quad) ───────────────

        [Fact]
        public void Quad_IndexSetOutOfRange()
        {
            var q = new Quad(new Rect(10, 10, 20, 20));
            Assert.Throws<IndexOutOfRangeException>(() => q[5] = new Point());
        }

        [Fact]
        public void Quad_PreshearStopsBeingRectangular()
        {
            var q = new Rect(10, 10, 20, 20).Quad;
            Assert.True(q.IsRectangular);
            var m = new Matrix(1, 0, 0, 1, 0, 0).Preshear(2, 3);
            var q2 = q * m;
            Assert.False(q2.IsRectangular);
            Assert.False(q2.IsEmpty);
            Assert.True(q2.IsConvex);
        }

        /// <summary>PyMuPDF <c>tests/test_geometry.py::test_quad</c> containment after preshear.</summary>
        [Fact]
        public void Quad_Contains_AfterPreshear_OriginalRectOutside()
        {
            var r = new Rect(10, 10, 20, 20);
            var q = r.Quad;
            var m = new Matrix(1, 0, 0, 1, 0, 0).Preshear(2, 3);
            var q2 = q * m;
            Assert.False(q2.Contains(r.TopLeft));
            Assert.False(q2.Contains(r));
            Assert.False(q2.Contains(r.Quad));
        }

        [Fact]
        public void Quad_Contains_RectCorners_AxisAligned()
        {
            var r = new Rect(10, 10, 20, 20);
            var q = r.Quad;
            Assert.True(q.Contains(r.TopLeft));
            Assert.True(q.Contains(r));
            Assert.True(q.Contains(r.Quad));
        }

        [Fact]
        public void Quad_Contains_EmptyRect_IsTrue()
        {
            var q = new Rect(0, 0, 10, 10).Quad;
            Assert.True(q.Contains(new Rect()));
        }

        [Fact]
        public void Quad_Contains_EmptyQuad_IsFalseForPoint()
        {
            var q = new Rect().Quad;
            Assert.True(q.IsEmpty);
            Assert.False(q.Contains(new Point(0, 0)));
        }

        [Fact]
        public void Quad_DivideBySingularMatrixThrows()
        {
            var q = new Rect(0, 0, 10, 10).Quad;
            var singular = new Matrix(1, 0, 1, 0, 1, 0);
            Assert.Throws<DivideByZeroException>(() => _ = q / singular);
        }

        [Fact]
        public void Quad_ScalarMultiplyDivideRoundTrip()
        {
            var q = new Rect(1, 1, 3, 3).Quad;
            var q2 = q * 2 / 2;
            Assert.Equal(q, q2);
        }
    }
}
