using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using GS1 QR symbology. GS1 QR is a 2D
    /// (two-dimensional) barcode that holds
    /// large amounts of data in a relatively small space. These barcodes
    /// are used primarily in aerospace, pharmaceuticals, medical device
    /// manufacturing, and by the U.S. Department of Defense to add
    /// visibility to the value chain. GS1 QR can be used for parts
    /// that need to be tracked in the manufacturing process because the
    /// barcode allows users to encode a variety of information related
    /// to the product, such as date or lot number. They are not intended
    /// to be used on items that pass through retail point-of-sale (POS).
    /// </summary>
    class GS1QRSymbology : QRSymbology
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GS1QRSymbology"/> class.
        /// </summary>
        public GS1QRSymbology() : base()
        {
            m_type = TrueSymbologyType.GS1_QRCode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GS1QRSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public GS1QRSymbology(SymbologyDrawing prototype) : base(prototype)
        {
            m_type = TrueSymbologyType.GS1_QRCode;
        }

        /// <summary>
        /// Validates the value using GS1 Data Matrix symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            if (!GS1ValueChecker.Check(value))
                return false;

            //if (value.Length == 0)
            //    return false;

            return true;

        }

        /// <summary>
        /// Gets or sets the barcode value to encode.
        /// The value can be set in 2 forms: with parenthesis for AI and without.
        /// If a value comes in form like 'xxxxxxxxxxx' or like 'xxxxxx|xxxxx' then the SDK automatically sets brackets according to GS1 AI (Application Identifiers).
        /// If a value comes in form like (xx)yyyyyy(zz)nnn then the SDK do NOT verifies the value against AI.
        /// However you may verify if the value is valid according to GS1 rules or not by using Barcode.ValueIsValidGS1() bool function
        /// </summary>
        /// <value>The barcode value to encode.</value>
        public override string Value
        {
            get
            {
                return base.Value;
            }
            set
            {
                bool isBracket = false;
                for (int i = 0; i < value.Length; i++)
                    if (value[i] == '(')
                    {
                        isBracket = true;
                        break;
                    }

                base.Value = value;

                if (isBracket)
                    base.Value = value; // just set the value
                else
                    // else find AI identifiers and convert value from the form like "xxxxxx" into "(yy)xxxx(zz)nn"
                    base.Value = ApplicationIdentifiers.SelectAIs(value);

            }
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            string restriction = "GS1 QR symbology allows to encode a maximum data size of 4,296 alphanumeric characters.\n";
            restriction += "\r\nThe value can be set in 2 forms: with parenthesis for AI and without.";
            restriction += "\r\nIf a value comes in form like 'xxxxxxxxxxx' or like 'xxxxxx|xxxxx' then the SDK automatically sets brackets according to GS1 AI (Application Identifiers).";
            restriction += "\r\nIf a value comes in form like (xx)yyyyyy(zz)nnn then the SDK do NOT verifies the value against AI.";
            restriction += "\r\n\r\nHowever you may verify if the value is valid according to GS1 rules or not by using Barcode.ValueIsValidGS1() bool function";
            return restriction;
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected override string GetIncorrectValueSubstitution()
        {
            return "1112345617ABCDEF";
        }

        private byte[] getDataForEncoding()
        {
            string ss = GS1ValueChecker.GetStripped(Value);

            ss = ss.Replace("%", "%%");//% must be encoded as %%
            ss = ss.Replace("(", "%");//FNC1 inside string must be encoded as %

            byte[] bbytes = new byte[ss.Length];

            int index = 0;
            foreach (char c in ss.ToCharArray())
            {
                bbytes[index++] = (byte)c;
            }
            return bbytes;
        }

        protected override SKSize buildBars(SKCanvas canvas, SKFont font)
        {
            // a bit weird cycle goes here, but we need it,
            // or we can end up with 2-byte chars which is unsupported
            StringBuilder sb = new StringBuilder();

            byte[] bytes = getDataForEncoding();

            for (int i = 0; i < bytes.Length; i++)
                sb.Append((char)bytes[i]);

            SKSize drawingSize = new SKSize();

            QRSymbol symbol = QRSymbol.EncodeString(sb.ToString(), Options.QRVersion,
                Options.QRErrorCorrectionLevel, Options.QREncodeHint, true, true);

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
