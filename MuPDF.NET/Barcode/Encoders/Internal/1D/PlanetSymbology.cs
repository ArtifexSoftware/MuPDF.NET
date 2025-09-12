/**************************************************
 *
 *
 *
 *
**************************************************/

using System;
using System.Text;
using System.Drawing;
using SkiaSharp;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using PLANET symbology. PLANET barcode is used by 
    /// the United States Postal Service to identify and track pieces of 
    /// mail during delivery - the Post Office's "CONFIRM" services.
    /// </summary>
    class PlanetSymbology : PostnetSymbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetSymbology"/> class.
        /// </summary>
        public PlanetSymbology()
            : base()
        {
            m_type = TrueSymbologyType.Planet;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlanetSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public PlanetSymbology(SymbologyDrawing prototype)
            : base(prototype)
        {
            m_type = TrueSymbologyType.Planet;
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "000000000000";
        }

        /// <summary>
        /// Validates the value using PLANET symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (value.Length != 12 && value.Length != 14)
                return false;

            foreach (char c in value)
            {
                if (m_alphabet.IndexOf(c) == -1)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "The PLANET symbology allows only strings with 12 or 14 digits to be encoded.";
        }

        protected override Size buildBars(SKCanvas canvas, SKFont font)
        {
            Size drawingSize = new Size();
            int x = 0;
            int y = 0;

            int tallHeight = BarHeight;
            int shortHeight = BarHeight / WideToNarrowRatio;
            int gapWidth = Math.Max(NarrowBarWidth / 2, 1);
            int width = NarrowBarWidth;

            m_rects.Add(new Rectangle(x, y, width, tallHeight));

            x += width;
            x += gapWidth;

            string value = GetEncodedValue(false);
            foreach (char c in value)
            {
                string pattern = getCharPattern(c);
                foreach (char patternChar in pattern)
                {
                    if (patternChar == '1')
                        m_rects.Add(new Rectangle(x, y + tallHeight - shortHeight, width, shortHeight));
                    else
                        m_rects.Add(new Rectangle(x, y, width, tallHeight));

                    x += width;
                    x += gapWidth;
                }
            }

            m_rects.Add(new Rectangle(x, y, width, tallHeight));
            x += width;

            drawingSize.Width = x;
            drawingSize.Height = BarHeight;
            return drawingSize;
        }
    }
}
