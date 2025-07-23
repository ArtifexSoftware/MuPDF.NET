using System;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.FormOMR
{
#if CORE_DEV
    public
#else
    internal
#endif
    class FormOMROval : FormOMR
    {
        public FormOMROval()
        {
            this.minRatio = 0.3f;
        }

        const float maxMargin = 0.1f;

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.Oval;
		}

        protected override bool CheckOutline(Segment p, float[] outline)
        {            
            MyPointF center=p.CenterF;
            float w = (float)p.Width / 2f;
            float h = (float)p.Height / 2f;

            //reject circles
            if (Calc.Around(w / h, 1f, 0.15f)) return false;

            //check min max
            int nFails = 0;
            for (int i = 0; i < N; i++)
                if (outline[i]!=0f && outline[i] / w < 0.8f && outline[i] / h < 0.8f)
                    if (++nFails > 2) return false;

            //check if outline is an symetric
            float e = 0f;
            for (int i = 0; i < N / 4; i++)
            {
                float o0 = outline[i];
                float o1 = outline[N / 2 - 1 - i];
                float o2 = outline[N / 2 + i];
                float o3 = outline[(N - 1 - i) % N];

                float e1 = o0 != 0f && o3 != 0f ? o0 - o3 : 0f;
                float e2 = o0 != 0f && o2 != 0f ? o0 - o2 : 0f;
                float e3 = o0 != 0f && o1 != 0f ? o0 - o1 : 0f;
                e += e1 * e1 + e2 * e2 + e3 * e3;
            }

            if (e < 15f)
            {
                //check that at 0º and 180º outline is around w
                //check that at 90º and 270º outline is around h
                //check that at 45, 135,...are 90% shorter that max(w,h)
                float max = (float)Math.Sqrt(w * w + h * h);
                if ((Calc.Around(outline[0] / w, 1f, maxMargin) || Calc.Around(outline[N - 1] / w, 1f, maxMargin)) &&
                    (Calc.Around(outline[N / 4] / h, 1f, maxMargin) || Calc.Around(outline[N / 4 - 1] / h, 1f, maxMargin)) &&
                    (Calc.Around(outline[2 * N / 4] / w, 1f, maxMargin) || Calc.Around(outline[2 * N / 4 - 1] / w, 1f, maxMargin)) &&
                    (Calc.Around(outline[3 * N / 4] / h, 1f, maxMargin) || Calc.Around(outline[3 * N / 4 - 1] / h, 1f, maxMargin)) &&
                    outline[N / 8] / max <0.9f &&
                    outline[3 * N / 8] / max <0.9f &&
                    outline[5 * N / 8] / max <0.9f &&
                    outline[7 * N / 8] / max <0.9f
                    )
                    return true;
                else
                    return false;
            }
            return false;
        }
    }
}
