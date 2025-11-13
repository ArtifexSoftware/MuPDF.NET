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
    /// Draws barcodes using QR Code symbology. QR Code initially was used 
    /// for tracking parts in vehicle manufacturing, but now QR Codes used 
    /// in a much broader context, including both commercial tracking applications 
    /// and convenience-oriented applications aimed at mobile phone users 
    /// (known as mobile tagging).
    /// </summary>
    class QRSymbology : SymbologyDrawing2D
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QRSymbology"/> class.
        /// </summary>
        public QRSymbology()
            : base(TrueSymbologyType.QRCode)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QRSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public QRSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.QRCode)
        {
        }

        /// <summary>
        /// Validates the value using QR Code symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
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
            return "QR Code symbology allows a maximum data size of 7,089 numeric, or 4,296 alphanumeric, or 2,953 bytes, or 1,817 Kanji/Kana characters.\n";
        }

        /// <summary>
        /// Gets the barcode value encoded using QR Code symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using QR Code symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            if (forCaption)
                return Value;

            return "";
        }

        /// <summary>
        /// Gets the encoding pattern for given character.
        /// </summary>
        /// <param name="c">The character to retrieve pattern for.</param>
        /// <returns>The encoding pattern for given character.</returns>
        protected override string getCharPattern(char c)
        {
            return "";
        }

        protected override SKSize buildBars(SKCanvas canvas, SKFont font)
        {
            // a bit weird cycle goes here, but we need it,
            // or we can end up with 2-byte chars which is unsupported
            StringBuilder sb = new StringBuilder();

            // get encoding used in options
            Encoding tmpEncoding = Options.Encoding;

            /*             
            // detect Unicode and auto switch to UTF-8 ?            
            
            bool UnicodeSymbolFound = !Utils.IsLatinISOEncodingOnly(Value);

            // if we are not with encoding UTF8
            // then we are forcing to use UTF8
            if (UnicodeSymbolFound && tmpEncoding != Encoding.UTF8)
            {
                // force encoding to UTF8
                tmpEncoding = Encoding.UTF8;
            }
            */

            byte[] bytes = tmpEncoding.GetBytes(Value);

            for (int i = 0; i < bytes.Length; i++)
                sb.Append((char)bytes[i]);

            SKSize drawingSize = new SKSize();

            QRSymbol symbol = QRSymbol.EncodeString(sb.ToString(), Options.QRVersion,
                Options.QRErrorCorrectionLevel, Options.QREncodeHint, true, false);

            int cellWidth = NarrowBarWidth;
            drawingSize.Width = symbol.Width * cellWidth;
            drawingSize.Height = drawingSize.Width;

            for (int i = 0; i < symbol.Width; i++)
            {
                for (int j = 0; j < symbol.Width; j++)
                {
                    if ((symbol[(i * symbol.Width) + j] & 0x01) != 0)
                        m_rects.Add(new SKRect(j * cellWidth, i * cellWidth, j * cellWidth+cellWidth, i * cellWidth+cellWidth));
                }
            }

            return drawingSize;
        }
    }
}
