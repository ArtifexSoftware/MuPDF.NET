/**************************************************
 *
 *
 *
 *
**************************************************/

using SkiaSharp;
using System;
using System.Drawing;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using ITF-14 (GTIN-14, UCC-14) symbology rules.
    /// This symbology is generally used on a packaging step of products.
    /// The ITF-14 always encodes 14 digits.
    /// </summary>
    class ITF14Symbology : I2of5Symbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ITF14Symbology"/> class.
        /// </summary>
        public ITF14Symbology()
            : base()
        {
            m_type = TrueSymbologyType.ITF14;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ITF14Symbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public ITF14Symbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.ITF14;
        }

        public override string getValueRestrictions()
        {
            return "ITF-14 symbology expects strings with 13 digits to be encoded. Optionally, user may enter 14th digit. In the latter case the last digit (check digit) will be verified.";
        }

        /// <summary>
        /// Validates the value using ITF-14 symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Checksum is mandatory or not (if applicable).</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (value.Length != 14 && value.Length != 13)
                return false;

			return base.ValueIsValid(value, checksumIsMandatory);
        }

        /// <summary>
        /// Gets the barcode value encoded using ITF-14 symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            if (Value.Length == 13)
            {
                return Value + getChecksumChar(Value);
            }
            return Value;
        }

        protected override SKSize buildBars(SKCanvas canvas, SKFont font)
        {
            int BearerBarsWidth = 4 * NarrowBarWidth;
            int QuietZone = 10 * NarrowBarWidth;
            SKSize drawingSize = new SKSize();
            int x = BearerBarsWidth + QuietZone; // 0;
            int y = BearerBarsWidth; // 0;

            string value = GetEncodedValue(false);
            if (value.Length % 2 != 0)
                throw new BarcodeException("Incorrect value length.");

            x += drawStartCode(x, y);

            int widthNarrow = NarrowBarWidth;
            int widthWide = NarrowBarWidth * WideToNarrowRatio;
            int height = BarHeight;

            for (int i = 0; i < (value.Length - 1); i += 2)
            {
                string barsPattern = getCharPattern(value[i]);
                string gapsPattern = getCharPattern(value[i + 1]);

                for (int patternPos = 0; patternPos < barsPattern.Length; patternPos++)
                {
                    int barWidth = widthNarrow;
                    if (barsPattern[patternPos] == 'w')
                        barWidth = widthWide;

                    int gapWidth = widthNarrow;
                    if (gapsPattern[patternPos] == 'w')
                        gapWidth = widthWide;

                    m_rects.Add(new SKRect(x, y, barWidth+x, height+y));

                    x += barWidth;
                    x += gapWidth;
                }
            }

            x += drawStopCode(x, y);

            m_rects.Add(new SKRect(BearerBarsWidth, 0, x + QuietZone, BearerBarsWidth));
            m_rects.Add(new SKRect(BearerBarsWidth, height + BearerBarsWidth, x + QuietZone, BearerBarsWidth+ height + BearerBarsWidth));
            if (!Options.OnlyHorizontalBearerBar)
            {
                m_rects.Add(new SKRect(0, 0, BearerBarsWidth, height + BearerBarsWidth * 2));
                m_rects.Add(new SKRect(x + QuietZone, 0, BearerBarsWidth, height + BearerBarsWidth * 2));
            }
            drawingSize.Width = x + BearerBarsWidth + QuietZone;
            drawingSize.Height = BarHeight + BearerBarsWidth * 2;
            return drawingSize;
        }


    }
}
