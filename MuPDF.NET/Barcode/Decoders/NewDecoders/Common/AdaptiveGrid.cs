namespace BarcodeReader.Core.Common
{
	// Stores and manages a region to sample, defined by its 4 vertex + module width and height at each vertex
	internal class AdaptiveGrid
    {
        int rows, cols;
        MyPointF lu, ld, ru, rd;
        float[] topSampling, bottomSampling, leftSampling, rightSampling;
        float moduleLength;

        public AdaptiveGrid(int cols, int rows,
            MyPointF lu, MyPointF ld, MyPointF ru, MyPointF rd,
            float wLu, float wLd, float wRu, float wRd,
            float hLu, float hLd, float hRu, float hRd)
        {
            this.rows = rows; this.cols = cols;
            this.lu = lu; this.ld = ld; this.ru = ru; this.rd = rd;

            this.topSampling = adaptiveSampling(wLu, wRu, (lu - ru).Length, cols);
            this.bottomSampling = adaptiveSampling(wLd, wRd, (ld - rd).Length, cols);

            this.leftSampling = adaptiveSampling(hLu, hLd, (lu - ld).Length, rows);
            this.rightSampling = adaptiveSampling(hRu, hRd, (ru - rd).Length, rows);

            this.moduleLength = (lu - ru).Length / (float)cols;
        }

        //x=0..cols-1 y=0..rows-1
        public MyPointF GetSamplePoint(int x, int y)
        {
            //interpolate between CornerUp and RightUp vectors
            float tX = topSampling[x];
            MyPointF top = lu * (1.0F - tX) + ru * tX;

            float bX = bottomSampling[x];
            MyPointF bottom = ld * (1.0F - bX) + rd * bX;

            float lY = leftSampling[y];
            MyPointF left = lu * (1.0F - lY) + ld * lY;

            float rY = rightSampling[y];
            MyPointF right = ru * (1.0F - rY) + rd * rY;

            //intersection of two lines (top->bottom) and (left->right)
            float det = (top.X - bottom.X) * (left.Y - right.Y) - (top.Y - bottom.Y) * (left.X - right.X);
            float px = ((top.X * bottom.Y - top.Y * bottom.X) * (left.X - right.X) - (top.X - bottom.X) * (left.X * right.Y - left.Y * right.X)) / det;
            float py = ((top.X * bottom.Y - top.Y * bottom.X) * (left.Y - right.Y) - (top.Y - bottom.Y) * (left.X * right.Y - left.Y * right.X)) / det;
            return new MyPointF(px, py);
        }


        //extract points of the grid
        public void ExtractPoints(ImageScaner scan, bool[][] result, MyPoint resultIndex)
        {
            for (int y = 0; y < this.rows; ++y)
            {
                for (int x = 0; x < this.cols; ++x)
                {
                    MyPointF point = GetSamplePoint(x, y);
                    bool isBlack = scan.isBlackSample(point,moduleLength);
                    result[resultIndex.Y + y][resultIndex.X + x] = isBlack;
                }
            }
        }


        //w1 is the starting width
        //w2 is the end width
        //W is the total width
        //n is the number of samples
        //returns the module widths of samples
        float[] adaptiveSampling(float w1, float w2, float W, int n)
        {
            float N = (float)n;
            float M11 = 0f, M12 = 0f, M13 = 1f;
            float M21 = (N + 1f) * (N + 1f), M22 = N + 1f, M23 = 1f;
            float M31 = N * (N + 1f) * (2f * N + 1f) / 6f, M32 = N * (N + 1f) / 2f, M33 = N;

            //determinants
            float det = M11 * M22 * M33 + M12 * M23 * M31 + M13 * M21 * M32 - M31 * M22 * M13 - M32 * M23 * M11 - M33 * M21 * M12;
            if (Calc.Around(det, 0f, 0.0001f)) return null;

            float D11 = M22 * M33 - M32 * M23, D12 = M21 * M33 - M31 * M23, D13 = M21 * M32 - M31 * M22;
            float D21 = M12 * M33 - M32 * M13, D22 = M11 * M33 - M31 * M13, D23 = M11 * M32 - M31 * M12;
            float D31 = M12 * M23 - M22 * M13, D32 = M11 * M23 - M21 * M13, D33 = M11 * M22 - M21 * M12;

            //inverse matrix
            float I11 = D11 / det, I12 = -D21 / det, I13 = D31 / det;
            float I21 = -D12 / det, I22 = D22 / det, I23 = -D32 / det;
            float I31 = D13 / det, I32 = -D23 / det, I33 = D33 / det;

            //solution
            float a = I11 * w1 + I12 * w2 + I13 * W;
            float b = I21 * w1 + I22 * w2 + I23 * W;
            float c = I31 * w1 + I32 * w2 + I33 * W;

            //calculate sampling positions relative to the starting point
            float[] r = new float[n];
            float acum=0f;
            for (int i = 1; i <= n; i++)
            {
                float f = (float)i;
                float w = a * f * f + b * f + c; //width of the current module
                r[i-1]=(acum+w/2f)/W; //sample in the middle of the module (normalized to 0..1)
                acum+=w;
            }
            return r;
        }
    }
}
