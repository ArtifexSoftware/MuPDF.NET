using SkiaSharp;
using System;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Draws barcodes using Pharma Code symbology rules.
    /// </summary>
    class PharmaCodeSymbology : SymbologyDrawing
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PharmaCodeSymbology"/> class.
        /// </summary>
        public PharmaCodeSymbology()
            : base(TrueSymbologyType.PharmaCode)
        {
            m_type = TrueSymbologyType.PharmaCode;
            Init();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PharmaCodeSymbology"/> class.
        /// </summary>
        /// <param name="prototype">The prototype.</param>
        public PharmaCodeSymbology(SymbologyDrawing prototype)
            : base(prototype, TrueSymbologyType.PharmaCode)
        {
            m_type = TrueSymbologyType.PharmaCode;
            Init();
        }

        private void Init()
        {
            // there is no characters corresponding to PharmaCodeSymbology
            // start and stop symbols.
            Options.ShowStartStop = false;
            AddChecksum = false;
            AddChecksumToCaption = false;
            base.DrawCaption = false;
        }

        /// <summary>
        /// Validates the value using Pharma Code symbology rules.
        /// If value is valid then it will be set as current value.
        /// </summary>
        /// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Parameter is not applicable to this symbology.</param>
        /// <returns>
        /// 	<c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
		public override bool ValueIsValid(string value, bool checksumIsMandatory)
        {
            int val;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
                return false;

            if(Options.PharmaCodeTwoTrack)
                return val >= 1 && val <= 64570080;
            else
                return val >= 1 && val <= 131070;
        }

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>
        /// The value restrictions description string.
        /// </returns>
        public override string getValueRestrictions()
        {
            return "Pharma Code symbology allows only decimal number values to be encoded. Diapason from 1 to 131070 - for OneTrack barcode, from 1 to 64570080 for TwoTrack barcode.";
        }

        /// <summary>
        /// Gets the barcode value encoded using Pharma Code symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>
        /// The barcode value encoded using current symbology rules.
        /// </returns>
        public override string GetEncodedValue(bool forCaption)
        {
            StringBuilder sb = new StringBuilder();

            if (forCaption)
            {
                sb.Append(Value);
            }
            else
            {
                //parse to integer
                int val;
                if (!int.TryParse(Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out val))
                    return string.Empty;

                if (Options.PharmaCodeTwoTrack)
                {
                    //transform to 3
                    sb.Append(To3(val));
                }
                else
                {
                    //add 1
                    val += 1;

                    //transform to binary
                    sb.Append(Convert.ToString(val, 2));

                    //remove first 1
                    sb.Remove(0, 1);
                }

                if (Options.PharmaCodeSupplementaryCode)
                    sb.Append('s');
            }

            return sb.ToString();
        }

        string To3(int v)
        {
            var sb = new StringBuilder();

            while (v > 0)
            {
                var d = v % 3;
                sb.Insert(0, d);
                switch (d)
                {
                    case 0: v = (v - 3) / 3; break;
                    case 1: v = (v - 1) / 3; break;
                    case 2: v = (v - 2) / 3; break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the encoding pattern for given character.
        /// </summary>
        /// <param name="c">The character to retrieve pattern for.</param>
        /// <returns>
        /// The encoding pattern for given character.
        /// </returns>
        protected override string getCharPattern(char c)
        {
            if (Options.PharmaCodeTwoTrack)
            {
                switch (c)
                {
                    case '0':
                        return "30";
                    case '1':
                        return "10";
                    case '2':
                        return "20";
                    case 's':
                        return "s";
                }
            }
            else
            {
                switch (c)
                {
                    case '0':
                        return "n0";
                    case '1':
                        return "w0";
                    case 's':
                        return "s";
                }
            }

            return ":o]";
        }

        protected override Size buildBars(SKCanvas canvas, SKFont font)
        {
            InitSizes();

            Size drawingSize = new Size();
            int x = 0;
            int y = 0;

            string value = GetEncodedValue(false);

            bool drawBar = true;

            foreach (char ch in value)
            {
                string pattern = getCharPattern(ch);
                foreach (char patternChar in pattern)
                {
                    int width = 0;
                    int height = BarHeight;
                    y = 0;
                    switch (patternChar)
                    {
                        case 'w': width = b; break;
                        case 'n': width = a; break;
                        case '0': width = c; break;
                        case '1': width = a; y = BarHeight / 2; height = BarHeight - BarHeight / 2; break;
                        case '2': width = a; y = 0; height = BarHeight / 2; break;
                        case '3': width = a; break;
                        case 's': width = z; x += d - c; break;
                    }

                    if (drawBar)
                        m_rects.Add(new Rectangle(x, y, width, height));

                    x += width;
                    drawBar = !drawBar;
                }
            }

            drawingSize.Width = x;
            drawingSize.Height = BarHeight;
            return drawingSize;
        }

        private int a;
        private int b;
        private int c;
        private int d;
        private int e;
        private int z;

        private void InitSizes()
        {
            var dpi = BarcodeResolution.Width;

            if (Options.PharmaCodeTwoTrack)
            {
                a = Mm2Pixels(1.0f, dpi);
                c = Mm2Pixels(1.0f, dpi);
                e = Mm2Pixels(8.0f, dpi);
            }
            else
            {
                if (Options.PharmaCodeMiniature)
                {
                    a = Mm2Pixels(0.35f, dpi);
                    b = a * 3;
                    c = Mm2Pixels(0.65f, dpi);
                    d = Mm2Pixels(1.00f, dpi);
                    e = Mm2Pixels(6.00f, dpi);
                    z = Mm2Pixels(1.00f, dpi);
                }
                else
                {
                    a = Mm2Pixels(0.5f, dpi);
                    b = a * 3;
                    c = Mm2Pixels(1.00f, dpi);
                    d = Mm2Pixels(1.50f, dpi);
                    e = Mm2Pixels(8.00f, dpi);
                    z = Mm2Pixels(1.50f, dpi);
                }
            }

            BarHeight = e;
        }

        static int Mm2Pixels(double mm, float dpi)
        {
            var inches = mm * 0.03937f;//mm -> inches
            return (int)Math.Ceiling(dpi * inches);//inches -> pixels
        }

        protected override void drawBars(SKCanvas canvas, SKPaint paint, SKPoint position)
        {
            for (int i = 0; i < m_rects.Count; i++)
            {
                var r = (Rectangle)m_rects[i];

                // Change color for last bar if PharmaCodeSupplementaryCode is enabled
                if (i == m_rects.Count - 1 && Options.PharmaCodeSupplementaryCode)
                    paint.Color = Options.PharmaCodeSupplementaryBarColor;
                else
                    paint.Color = paint.Color;

                // Draw filled rectangle
                SKRect rect = new SKRect(
                    r.X + position.X,
                    r.Y + position.Y,
                    r.X + position.X + r.Width,
                    r.Y + position.Y + r.Height
                );

                canvas.DrawRect(rect, paint);
            }
        }
    }
}