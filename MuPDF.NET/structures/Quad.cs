﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using mupdf;

namespace MuPDF.NET
{
    public class Quad
    {
        public Point UpperLeft { get; set; }
        public Point UpperRight { get; set; }
        public Point LowerLeft { get; set; }
        public Point LowerRight { get; set; }

        public float Width
        {
            get
            {
                return Math.Max((UpperLeft - UpperRight).Abs(), (LowerLeft - LowerRight).Abs());
            }
        }

        public float Height
        {
            get
            {
                return Math.Max((UpperLeft - LowerLeft).Abs(), (UpperRight - LowerRight).Abs());
            }
        }

        public Rect Rect
        {
            get
            {
                Rect r = new Rect();
                r.X0 = Math.Min(Math.Min(UpperLeft.X, UpperRight.X), Math.Min(LowerLeft.X, LowerRight.X));
                r.Y0 = Math.Min(Math.Min(UpperLeft.Y, UpperRight.Y), Math.Min(LowerLeft.Y, LowerRight.Y));
                r.X1 = Math.Max(Math.Max(UpperLeft.X, UpperRight.X), Math.Max(LowerLeft.X, LowerRight.X));
                r.Y1 = Math.Max(Math.Max(UpperLeft.Y, UpperRight.Y), Math.Max(LowerLeft.Y, LowerRight.Y));
                return r;
            }
        }
        public int Length { get; set; } = 4;

        public bool IsConvex
        {
            get
            {
                
                Matrix m = Utils.PlanishLine(this.UpperLeft, this.LowerRight);
                Point p1 = this.LowerLeft * m;
                Point p2 = this.UpperRight * m;

                if (p1.Y * p2.Y > 0)
                    return false;
                m = Utils.PlanishLine(this.LowerLeft, this.UpperRight);
                p1 = this.LowerRight * m;
                p2 = this.UpperLeft * m;
                if (p1.Y * p2.Y > 0)
                    return false;
                return true;
            }
        }

        public bool IsRectangular
        {
            get
            {
                float sine = Utils.SineBetween(UpperLeft, UpperRight, LowerRight);
                if (Math.Abs(sine - 1) > float.Epsilon)
                    return false;

                sine = Utils.SineBetween(UpperRight, LowerRight, LowerLeft);
                if (Math.Abs(sine - 1) > float.Epsilon)
                    return false;

                sine = Utils.SineBetween(LowerRight, LowerLeft, UpperLeft);
                if (Math.Abs(sine - 1) > float.Epsilon)
                    return false;

                return true;
            }
        }

        public Quad()
        {
            UpperLeft = UpperRight = LowerLeft = LowerRight = new Point(0.0f, 0.0f);
        }

        public bool IsEmpty
        {
            get
            {
                return (Width < float.Epsilon) || (Height < float.Epsilon);
            }
        }

        public bool IsInfinite
        {
            get
            {
                return this.Rect.IsInfinite;
            }
        }
        public Quad(Point ul, Point ur, Point ll, Point lr)
        {
            UpperLeft = ul;
            UpperRight = ur;
            LowerLeft = ll;
            LowerRight = lr;
        }

        public Quad(Rect rect)
        {

        }

        public Quad(Quad quad)
        {
            UpperLeft = quad.UpperLeft;
            UpperRight = quad.UpperRight;
            LowerLeft = quad.LowerLeft;
            LowerRight = quad.LowerRight;
        }

        public Quad(FzQuad quad)
        {
            UpperLeft = new Point(new FzPoint(quad.ul));
            UpperRight = new Point(new FzPoint(quad.ur));
            LowerLeft = new Point(new FzPoint(quad.ll));
            LowerRight = new Point(new FzPoint(quad.lr));
        }

        public Point this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0: return this.UpperLeft;
                    case 1: return this.UpperRight;
                    case 2: return this.LowerLeft;
                    case 3: return this.LowerRight;
                    default: throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (i)
                {
                    case 0: this.UpperLeft = value; break;
                    case 1: this.UpperRight = value; break;
                    case 2: this.LowerLeft = value; break;
                    case 3: this.LowerRight = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }

        }

        public FzQuad ToFzQuad()
        {
            return mupdf.mupdf.fz_make_quad(
                UpperLeft.X, UpperLeft.Y,
                UpperRight.X, UpperRight.Y,
                LowerLeft.X, LowerLeft.Y,
                LowerRight.X, LowerRight.Y);
        }
        public float Abs()
        {
            if (IsEmpty)
            {
                return 0.0f;
            }
            return (UpperLeft - UpperRight).Abs() * (UpperLeft - LowerLeft).Abs();
        }

        public static Quad operator +(Quad op1, float op2)
        {
            return new Quad(op1.UpperLeft + op2, op1.UpperRight + op2, op1.LowerLeft + op2, op1.LowerRight + op2);
        }

        public static Quad operator +(Quad op1, Quad op2)
        {
            return new Quad(op1.UpperLeft + op2.UpperLeft, op1.UpperRight + op2.UpperRight, op1.LowerLeft + op2.LowerLeft, op1.LowerRight + op2.LowerRight);
        }

        public static Quad operator -(Quad op1, float op2)
        {
            return new Quad(op1.UpperLeft - op2, op1.UpperRight - op2, op1.LowerLeft - op2, op1.LowerRight - op2);
        }

        public static Quad operator -(Quad op1, Quad op2)
        {
            return new Quad(op1.UpperLeft - op2.UpperLeft, op1.UpperRight - op2.UpperRight, op1.LowerLeft - op2.LowerLeft, op1.LowerRight - op2.LowerRight);
        }

        public static Quad operator -(Quad op)
        {
            return new Quad(-op.UpperLeft, -op.UpperRight, -op.LowerLeft, -op.LowerRight);
        }

        override
        public string ToString() => $"Quad ({UpperLeft}, {UpperRight}, {LowerLeft}, {LowerRight})";

        public bool Contains(Point p)
        {
            return mupdf.mupdf.fz_is_point_inside_quad(p.ToFzPoint(), ToFzQuad()) == 0 ? false : true;
        }

        public bool Contains(Rect r)
        {
            if (r.IsEmpty)
            {
                return true;
            }
            return Contains(r.TopLeft) && Contains(r.BottomRight);
        }

        public bool Contains(Quad q)
        {
            for (int i = 0; i < 4; i++)
            {
                if (Contains(q[i]) == false)
                {
                    return false;
                }
            }
            return true;
        }

        public Quad Transform(Matrix m)
        {
            UpperLeft *= m;
            UpperRight *= m;
            UpperLeft *= m;
            UpperLeft *= m;

            return this;
        }

        public static Quad operator *(Quad op1, Matrix op2)
        {
            return op1.Transform(op2);
        }

        public Quad Morph(Point p, Matrix m)
        {
            if (IsInfinite)
            {
                return Utils.INFINITE_RECT().Quad;
            }

            Matrix delta = (new Matrix(1f, 1f)).Pretranslate(p.X, p.Y);
            return (this * ~delta * m) * delta;
        }

    }
}