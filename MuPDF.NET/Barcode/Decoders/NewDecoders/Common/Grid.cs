namespace BarcodeReader.Core.Common
{
 
    // Stores and manages rectangular regions to sample.
    // A rectangular region can be defined based on 3 vertexs (assuming no deformation). We call regular sampling
    // 4 corners: this allows deformation

	internal class Grid
    {
        int Cols, Rows;
        MyPointF Corner, Right, CornerUp, RightUp;
        MyVectorF CornerUpVD, RightUpVD, BottomRightVD;
        float BottomWidth, CornerHeight, RightHeight;
        float moduleLength;
        bool areCorners;

        //grid with perspective deformation. Points are corners if areCorners is true, 
        //or module centers otherwise
        public Grid(int cols, int rows, MyPointF corner, MyPointF cornerUp, MyPointF right, 
            MyPointF rightUp, bool areCorners)
        {
            Cols = cols;
            Rows = rows;
            Corner = corner;
            CornerUp = cornerUp;
            Right = right;
            RightUp = rightUp;

            MyVectorF vCUp = (cornerUp - corner);
            CornerHeight = vCUp.Length;
            CornerUpVD = vCUp.Normalized * (CornerHeight / rows);

            MyVectorF vRUp = (rightUp - right);
            RightHeight = vRUp.Length;
            RightUpVD = vRUp.Normalized * (RightHeight / rows);

            MyVectorF vBRight = (right - corner);
            BottomWidth = vBRight.Length;
            BottomRightVD = vBRight.Normalized * (BottomWidth / cols);

            this.areCorners = areCorners;
            if (areCorners) moduleLength = BottomWidth / (float)cols;
            else moduleLength = BottomWidth / (float)(cols - 1);
        }

        //grid without perspective deformation --> called regular
        public Grid(MyPointF center, MyVectorF vdX, MyVectorF vdY)
        {
            CornerUp = center;
            CornerUpVD = vdY;
            BottomRightVD = vdX;
        }

        public MyPointF GetSamplePointRegular(int x, int y)
        {
            return CornerUp + CornerUpVD * y + BottomRightVD * x;
        }

        public MyPointF GetSamplePointRegular(float x, float y)
        {
            return CornerUp + CornerUpVD * y + BottomRightVD * x;
        }

        //x=0..cols-1 y=0..rows-1
        public MyPointF GetSamplePoint(int x, int y)
        {
            //interpolate between CornerUp and RightUp vectors
            float fX=((float)x+0.5F)/(float)(Cols); //]0..1[
            MyVectorF up = CornerUpVD * (1.0F-fX) + RightUpVD * fX;

            MyPointF p = Corner + BottomRightVD * ((float)x + (areCorners?0.5F:0F));
            MyVectorF q = up * ((float)y + (areCorners ? 0.5F : 0F));
            p += q;
            return p;
        }

        public MyPointF GetSamplePoint(float x, float y)
        {
            //interpolate between CornerUp and RightUp vectors
            float fX = x / (float)(Cols); //]0..1[
            MyVectorF up = CornerUpVD * (1.0F - fX) + RightUpVD * fX;

            MyPointF p = Corner + BottomRightVD *x;
            MyVectorF q = up * y;
            p += q;
            return p;
        }

        public void ExtractPointsRegular(ImageScaner scan, bool[][] result, MyPoint sourceIndex, MyPoint resultIndex, int cx, int cy)
        {
            for (int y = 0; y < cy; ++y)
            {
                for (int x = 0; x < cx; ++x)
                {
                    MyPointF point = GetSamplePointRegular(sourceIndex.X + x, sourceIndex.Y + y);
                    bool isBlack = scan.isBlackSample(point,moduleLength);
                    result[resultIndex.Y + y][resultIndex.X + x] = isBlack;
                }
            }
        }
        //extract points of a rectangular area of the grid
        public void ExtractPoints(ImageScaner scan, bool[][] result, MyPoint sourceIndex, MyPoint resultIndex, int cx, int cy)
        {
            //Console.WriteLine("sampled grid:");
            for (int y = 0; y < cy; ++y)
            {
                for (int x = 0; x < cx; ++x)
                {
                    MyPointF point = GetSamplePoint(sourceIndex.X + x, sourceIndex.Y + y);
                    bool isBlack = scan.isBlackSample(point,moduleLength);
                    result[resultIndex.Y + y][resultIndex.X + x] = isBlack;
                }
            }
        }
     }
}
