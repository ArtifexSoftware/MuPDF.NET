using System.Drawing;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.FormOMR
{
#if CORE_DEV
    public
#else
    internal
#endif
    class FormOMRCircle : FormOMR
    {
        public FormOMRCircle()
        {
            this.minRatio = 0.9f;
        }

		public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.Circle;
		}

        protected override bool CheckOutline(Segment p, float[] outline)
        {
            MyPointF center=p.CenterF;
            float w = (float)p.Width / 2f;

            //check min max
            int nFails = 0;
            for (int i = 0; i < N; i++)
                if (!Calc.Around(outline[i] / w,1f,0.1f))
                    if (++nFails > 2) return false;
          
            return true;
        }
    }
}
