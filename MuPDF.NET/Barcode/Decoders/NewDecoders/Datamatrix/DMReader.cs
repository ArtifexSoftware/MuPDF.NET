using System;
using System.Collections.Generic;
using BarcodeReader.Core.Common;

namespace BarcodeReader.Core.Datamatrix
{
#if CORE_DEV
        public
#else
    internal
#endif
    class DMReader : SymbologyReader2D
    {
        int decodingMaxRetries = int.MaxValue;// 120; //number of edges to process in connectReader. Default=0 -->skip connectReader

        bool checkMirrored = false; //by default doesn't check mirrored barcodes

        //allow connect 10 pixels holes
        internal double MaxHoleSizeInsideLines = 11d;

        // max hole size between _ and | lines in relative to the largest side (15% = 0.15d etc)
        internal double MaxAllowedHoleBetweenLinesInLPattern = 0.15d;

        //Max dispersion of ends of L pattern (in units)
        public int LPatternDispersion { get; set; } = 0;

        public override SymbologyType GetBarCodeType()
		{
			return SymbologyType.DataMatrix;
		}

        protected override FoundBarcode[] DecodeBarcode()
        {
#if DEBUG_IMAGE
            var temp = BWImage.GetAsBitmap();
            DebugHelper.InitImage(temp);
#endif
            ImageScaner scan =new ImageScaner(BWImage);
            DMLPatternReader lPatternReader = new DMLPatternReader(scan, Encoding, checkMirrored);

            FoundBarcode[] r = new FoundBarcode[0];
            
            if (IsTimeout())
                throw new SymbologyReader2DTimeOutException();
            
            //===== Skip this phase because it is covered by second phase ======
            //var weNeededMoreBarcodes = false;
            //DMSimpleReader simpleReader = new DMSimpleReader() { LPatternDispersion = LPatternDispersion };
            //simpleReader.MaxNumberOfBarcodes = this.ExpectedNumberOfBarcodes;
            //simpleReader.MaxAllowedHoleBetweenLinesInLPattern = this.MaxAllowedHoleBetweenLinesInLPattern;
            //r = simpleReader.DecodeBarcode(BWImage, lPatternReader, this.MinAllowedBarcodeSideSize, this.MaxAllowedBarcodeSideSize);
            //weNeededMoreBarcodes = (r.Length == 0 && simpleReader.NumLPattnerns < 100);
            //weNeededMoreBarcodes = weNeededMoreBarcodes || this.ExpectedNumberOfBarcodes > 0 && r.Length < this.ExpectedNumberOfBarcodes;
            //if (weNeededMoreBarcodes)
            {
                DMConnectReader connectReader = new DMConnectReader() { LPatternDispersion = LPatternDispersion };
                connectReader.MaxHoleSizeInsideLines = this.MaxHoleSizeInsideLines;
                connectReader.MaxAllowedHoleBetweenLinesInLPattern = this.MaxAllowedHoleBetweenLinesInLPattern;
                connectReader.MaxNumberOfBarcodes = this.ExpectedNumberOfBarcodes;
                connectReader.decodingMaxRetries = decodingMaxRetries;
                connectReader.TimeoutTimeInTicks = TimeoutTimeInTicks;

                FoundBarcode[] r2 = connectReader.DecodeBarcode(BWImage, lPatternReader,
                    this.MinAllowedBarcodeSideSize, this.MaxAllowedBarcodeSideSize);

                r = Combine(r, r2);
            }

            return r;
        }

        private FoundBarcode[] Combine(FoundBarcode[] r, FoundBarcode[] r2)
        {
            //add to result first array
            var res = new List<FoundBarcode>(r);
            //enumerate second array
            foreach(var b2 in  r2)
            {
                //is presented in first array?
                var presentedAlready = false;
                foreach (var b1 in r)
                    if(b1.Rect.IntersectsWith(b2.Rect))
                    {
                        presentedAlready = true;
                        break;
                    }
                //if not presented => add to result
                if (!presentedAlready)
                    res.Add(b2);
            }

            return res.ToArray();
        }

        public int DecodingDeep
        {
            get { return this.decodingMaxRetries; }
            set { this.decodingMaxRetries = value; }
        }

        public bool CheckMirrored
        {
            get { return this.checkMirrored; }
            set { this.checkMirrored = value; }
        }

    }
}
