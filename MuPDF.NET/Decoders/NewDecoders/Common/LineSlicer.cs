using System;
using System.Collections;
using System.Collections.Generic;

namespace BarcodeReader.Core.Common
{
	internal class LineSlicer
    {
        BlackAndWhiteImage bwImage;
        int distToJoin;
        ArrayList[] angles;
        float maxVariance = 0.1f;

        int edgeId = 0;

        public float MaxVariance { get { return maxVariance; } set { maxVariance = value; } }

        public LineSlicer(BlackAndWhiteImage bwImage) {
            this.bwImage = bwImage;
        }

        public LinkedList<EdgeVar> GetEdges(int minRad, int maxRad, bool findHorizontalLines, bool findVerticalLines)
        {
            EdgeVarSlicer slicer = new EdgeVarSlicer(minRad, maxRad, findVerticalLines, findHorizontalLines, maxVariance);            
            return slicer.GetEdges(bwImage);
        }
        
        //minRad,maxRad: min and max segment length during segmentation
        //distToJoin: min distance to join aligned segments to create larger ones.
        //minLength: min distance to remove short lines
        public ArrayList[] GetLineEdges(int minRad, int maxRad, int distToJoin, int minLength, bool findHorizontalLines, bool findVerticalLines)
        {
            this.distToJoin = distToJoin;
            LinkedList<EdgeVar> edges = GetEdges(minRad, maxRad, findHorizontalLines, findVerticalLines);

            //classify segments by angle
            float PI = (float)Math.PI;
            float DISCRET = 4f;
            int N_ANGLES = (int)Math.Round(PI * 2f * DISCRET) + 1;
            angles = new ArrayList[N_ANGLES];
            for (int i = 0; i < angles.Length; i++) angles[i] = new ArrayList();

            foreach (EdgeVar f in edges)
            {
                MyPoint a = f.In;
                MyPoint b = f.End;
                MyVector vd = b - a;
                float angle = vd.Angle;
                if (angle < 0) angle += PI * 2f;
                int iAngle = (int)Math.Round(angle * DISCRET);
                angles[iAngle].Add(new LineEdge(a, b, f.Variance));
            }

            //join segments
            int ii, jj;
            for (int i = 0; i < angles.Length; i++)
            {
                for (int j = 0; j < angles[i].Count; j++)
                {
                    while (JoinsWith(i, j, out ii, out jj))
                    {
                        //JoinsWith(i, j, out ii, out jj);
                        ((LineEdge)angles[i][j]).Join((LineEdge)angles[ii][jj]);
                        angles[ii].RemoveAt(jj);
                        if (i == ii && jj < j) j--;
                    }
                }
            }

            //remove short segments
            for (int i = 0; i < angles.Length; i++)
                for (int j = 0; j < angles[i].Count; j++)
                {
                    LineEdge s = (LineEdge)angles[i][j];
                    if (s.length < minLength || s.TpcWhite>0.1f)
                    {
                        angles[i].RemoveAt(j);
                        j--;
                    }
                }

            return angles;
        }

        int OFFSET = 1;
        bool JoinsWith(int i, int j, out int ii, out int jj)
        {
            ii = jj = 0;
            LineEdge a = (LineEdge)angles[i][j];
            MyVector aVD = a.VD;
            for (int o = -OFFSET; o <= OFFSET; o++) //try to join with prev and next discrete angles
            {
                ii = (i + o + angles.Length) % angles.Length;
                for (jj = 0; jj < angles[ii].Count; jj++)
                    if (i != ii || j != jj)
                    {
                        LineEdge b = (LineEdge)angles[ii][jj];
                        //if (a.variance + b.variance < 0.1f)
                        {
                            if (Calc.Around(a.Angle, b.Angle, 0.1f))
                            {
                                float dist1 = (a.b - b.a).Length;
                                float dist2 = (b.b - a.a).Length;
                                if (dist1 < distToJoin || dist2 < distToJoin)
                                {
                                    //check alignment
                                    if (a.length != 0f && b.length != 0)
                                    {
                                        float dist;
                                        MyVector vdIn, vdEnd;
                                        if (dist1 < dist2) { dist = dist1; vdIn = b.a - a.b; vdEnd = b.b - a.b; }
                                        else { dist = dist2; vdIn = b.b - a.a; vdEnd = b.a - a.a; }

                                        float crossIn = MyVector.crossProduct(aVD, vdIn); //cross product is the area of the parallelapipede formed by the two vectors
                                        float angleIn = crossIn / a.length / vdIn.Length;

                                        float crossEnd = MyVector.crossProduct(aVD, vdEnd); //cross product is the area of the parallelapipede formed by the two vectors
                                        float angleEnd = crossEnd / a.length / vdEnd.Length;

                                        float meanError = crossEnd / 2f / (a.length + vdEnd.Length);

                                        float threshold = 1f / dist + 0.2f;


                                        if (//Calc.Around(angleIn, 0f, threshold) &&
                                            Calc.Around(angleEnd, 0f, 0.2f) &&
                                            Calc.Around(meanError, 0f, 1f)) return true;
                                    }
                                }
                            }
                        }
                    }
            }
            return false;
        }

        public EdgeVar mirror(EdgeVar e) {
            return e.mirror(edgeId++);
        }
    }

	internal class LineEdge : IComparable
    {
        public MyPoint a, b;
        public float length, angle, variance, whitePixels;

        public LineEdge(MyPoint a, MyPoint b, float variance)
        {
            this.a = a;
            this.b = b;
            this.length = (a - b).Length;
            this.angle = (float)Math.Atan2(this.b.Y - this.a.Y, this.b.X - this.a.X);
            this.variance = variance;
            this.whitePixels = 0f;
        }

        public void Join(LineEdge b)
        {
            //check if need to join to the left or to the right
            float dist1 = (this.b - b.a).Length;
            float dist2 = (b.b - this.a).Length;
            float dist=dist2;
            if (dist1 < dist2) { this.b = b.b; dist=dist1; }
            else this.a = b.a;
            this.length = (this.a - this.b).Length;
            this.angle = (float)Math.Atan2(this.b.Y - this.a.Y, this.b.X - this.a.X);
            this.variance += b.variance;
            this.whitePixels += b.whitePixels + dist;
        }

        public override string ToString()
        {
            return a.ToString() + " -- " + b.ToString()+ "L:"+length+" ("+angle+"º) var:"+variance;
        }

        public int CompareTo(object b)
        {
            LineEdge s = (LineEdge)b;
            return this.a.Y - s.a.Y;
        }

        public float Angle { get { return this.angle; } }

        public MyVector VD{ get { return b-a; } }

        public MyVectorF NormalizedVdY()
        {
            MyVectorF vdy = this.VD.Perpendicular;
            return vdy.Normalized;
        }

        public float Variance { get { return this.variance; } }

        public float TpcWhite { get { return whitePixels / (a - b).Length; } }

        public float Dist(MyPoint p)
        {
            float m = (this.a - this.b).Length;
            float d=((b.X-a.X)*(a.Y-p.Y)-(a.X-p.X)*(b.Y-a.Y))/m;
            return d > 0 ? d : -d;
        }
    }
}
