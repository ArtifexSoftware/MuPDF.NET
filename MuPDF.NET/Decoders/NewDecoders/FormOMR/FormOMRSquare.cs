using System;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.FormOMR
{
#if CORE_DEV
    public
#else
    internal
#endif
    class FormOMRRectangle : FormOMRSquare
    {
        public FormOMRRectangle()
        {
            // allow square and rectangle checkboxes
            this.minRatio = 1.1f;
            this.maxRatio = 5;
            this.minDist = 1;
        }
    }

#if CORE_DEV
    public
#else
    internal
#endif
    class FormOMRSquare : FormOMR
    {
        public FormOMRSquare()
        {
            // allowing square checkboxes only
            this.minRatio = 1.0f;
            this.maxRatio = 1.2f;
            this.minDist = 1;
        }

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.Checkbox;
		}

        //outline[] holds the distance from the segment outline at a given angle 
        //to the center of the semgnet.
        //N is the size of the outline, and angles are sampled at 360º/N.
        protected override bool CheckOutline(Segment p, float[] outline)
        {            
            MyPointF center=p.CenterF;

            //check if outline is an symetric
            float e = 0f; //accumulated error
            for (int i = 0; i < N / 4; i++)  //from 0º to 90º
            {
                float e1 = outline[i] - outline[(N - 1 - i) % N];  //check 0..90º with 360....270º
                float e2 = outline[i] - outline[N / 2 + i];        //check 0..90º with 180..270º
                float e3 = outline[i] - outline[N / 2 - 1 - i];    //check 0..90º with 180..90º
                e += e1 * e1 + e2 * e2 + e3 * e3;
            }

            if (e > 15f) return false;
            
            //if error is low enough
                //check if corners are larger than mid
                float minW = (float)p.Width / 2f;
                float minH = (float)p.Height / 2f;
                float max =(float)Math.Sqrt(minW*minW+minH*minH);
            if (!(Calc.Around(outline[0] / minW, 1f, 0.1f) &&         //check that at 0º outline is around minW
                Calc.Around(outline[N / 4] / minH, 1f, 0.1f) &&     //check that at 90º outline is around minH
                Calc.Around(outline[2 * N / 4] / minW, 1f, 0.1f) && //check that at 180º outline is around minW
                Calc.Around(outline[3 * N / 4] / minH, 1f, 0.1f) && //check that at 270º outline is around minH
                //and now check that corners are larger (max is the diagonal)
                Calc.Around(outline[N / 8] / max, 1f, 0.15f) &&     //check that at 45º outline is around max
                Calc.Around(outline[3 * N / 8] / max, 1f, 0.15f) && //check that at 135º outline is around max
                Calc.Around(outline[5 * N / 8] / max, 1f, 0.15f) && //check that at 225º outline is around max
                Calc.Around(outline[7 * N / 8] / max, 1f, 0.15f)    //check that at 315º outline is around max
                )) return false;

            //check inner horizontal lines
            if (!checkHInnerLines(p)) return false;

            //check inner vertical lines
            if (!checkVInnerLines(p)) return false;

                    return true;
            }

        bool checkHInnerLines(Segment p)
        {
            int offset=p.Height/10;
            int n = 0;
            for (int y = (int)p.LU.Y + offset; y < p.LD.Y - offset; y++)
            {
                int cc = 0;
                for (int x = (int)p.LU.X + offset; x < p.RU.X - offset; x++)
                    if (scan.isBlack(x, y)) cc++;
                if (Calc.Around(cc, p.Width-2*offset, p.Width/4)) n++;
            }
            return n == 0;
        }

        bool checkVInnerLines(Segment p)
        {
            int offset = p.Width / 10;
            int n = 0;
            for (int x = (int)p.LU.X + offset; x < p.RU.X - offset; x++)
            {
                int cc = 0;
                for (int y = (int)p.LU.Y + offset; y < p.LD.Y - offset; y++)
                    if (scan.isBlack(x, y)) cc++;
                if (Calc.Around(cc, p.Height-2*offset, p.Height/4)) n++;
            }
            return n == 0;
        }
    }
}
