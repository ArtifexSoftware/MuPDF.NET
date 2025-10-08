namespace BarcodeReader.Core.Common
{
	internal class NotUniformGrid
    {
        SquareFinder LU, LD, RU;
        int cols, rows, modulesFinder;
        float h, w, moduleLength;
        MyPointF lu, ld, ru, rd;

        public NotUniformGrid(int cols, int rows, int modulesFinder, SquareFinder LU, SquareFinder LD, SquareFinder RU)
        {
            this.cols = cols;
            this.rows = rows;
            this.modulesFinder = modulesFinder;
            this.LU = LU;
            this.LD = LD;
            this.RU = RU;
            w = (LU.C - RU.D).Length;
            h = (LU.C - LD.A).Length;
            this.moduleLength = w / (float)cols;
            lu = LU.C;
            ld = LD.A;
            ru = RU.D;
            //MyPointF l = interpolation(lu, ld, RU.Height / LU.Height);
            //rd = l + (ru - lu)*(RU.Width/LD.Width);
            rd = ld + (RU.B - LU.A);
        }

        public MyPoint GetSamplePoint(int xx, int yy)
        {
            float x = (float)xx + 0.5f;
            float y = (float)yy + 0.5f;

            float betaL = interpolation(h, rows, LU.Height, LD.Height, modulesFinder, y);
            float betaR = interpolation(h, rows, RU.Height, LD.Height, modulesFinder, y);

            float wl=interpolation(LU.Width, LD.Width, betaL);
            float wr=interpolation(RU.Width, LD.Width, betaR);

            float alfa = interpolation(w, cols, wl, wr, modulesFinder, x);

            MyPointF l = interpolation(lu, ld, betaL);
            MyPointF r = interpolation(ru, rd, betaR);

            MyPointF p = interpolation(l, r, alfa);
            return p;
        }

        public void ExtractPoints(ImageScaner scan, bool[][] result)
        {
            for (int y = 0; y < rows; ++y)
            {
                for (int x = 0; x < cols; ++x)
                {
                    MyPointF point = GetSamplePoint(x, y);
                    bool isBlack = moduleLength < 4f ? scan.isBlackSample(point,0f) : scan.isBlack(point);
                    result[y][x] = isBlack;
                }
            }
        }

        float interpolation(float a, float b, float alfa)
        {
            return a * (1f-alfa) + b * alfa;
        }

        MyPointF interpolation(MyPointF a, MyPointF b, float alfa)
        {
            return a * (1f-alfa) + b*alfa;
        }

        //l-> length of the segment
        //N-> number of modules
        //la, lb-> length of the left and right finders
        //M-> number of modules of the finders
        //x-> coordinate from 0..N to convert to 0..1
        float interpolation(float l, int N, float lA, float lB, int M, float x)
        {
            if (x <= (float)M)
            {
                float alfa = lA / l;
                return alfa * x / (float)M;
            }
            else if (x >= (float)(N - M))
            {
                float beta = (l -lB) / l;
                return beta *((float)N-x)/(float)M + 1f*(x-(float)(N-M))/(float)M;
            }
            else
            {
                float alfa = lA / l;
                float beta = (l - lB) / l;
                float L = (float)(N - 2 * M);
                return alfa * ((float)(N - M) - x) / L + beta* (x - (float)M) / L;
            }
        }


    }
}
