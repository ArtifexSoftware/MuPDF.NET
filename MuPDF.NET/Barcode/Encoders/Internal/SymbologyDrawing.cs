/**************************************************
 *
 *
 *
 *
**************************************************/

using SkiaSharp;
using System;
using System.Collections;
using System.ComponentModel;
using static System.Net.Mime.MediaTypeNames;

namespace BarcodeWriter.Core.Internal
{
    /// <summary>
    /// Base class for symbology-specific drawings
    /// </summary>
    abstract class SymbologyDrawing : IDisposable
    {
        protected ArrayList m_rects = new ArrayList();
        protected SKSize m_drawingSize = new SKSize();
        protected TrueSymbologyType m_type = TrueSymbologyType.Code39;
        private string m_value = "";
        private SKFont m_captionFont = new SKFont(SKTypeface.FromFamilyName(
            "Arial",
            SKFontStyleWeight.Normal,
            SKFontStyleWidth.Normal,
            SKFontStyleSlant.Upright), 
            12);
        private CaptionPosition m_captionPosition = CaptionPosition.Below;

        /// <summary>
		/// Validates the value using current symbology rules.
		/// If value is valid then it will be set as current value.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Checksum is obligatory or not.</param>
		/// <returns><c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.</returns>
		public abstract bool ValueIsValid(string value, bool checksumIsMandatory);

        /// <summary>
        /// Gets the value restrictions description string.
        /// </summary>
        /// <returns>The value restrictions description string.</returns>
        public abstract string getValueRestrictions();

        /// <summary>
        /// Gets the barcode value encoded using current symbology rules.
        /// </summary>
        /// <param name="forCaption">if set to <c>true</c> then encoded value is for caption. otherwise, for drawing</param>
        /// <returns>The barcode value encoded using current symbology rules.</returns>
        public abstract string GetEncodedValue(bool forCaption);

        /// <summary>
        /// Gets the encoding pattern for given character.
        /// </summary>
        /// <param name="c">The character to retrieve pattern for.</param>
        /// <returns>The encoding pattern for given character.</returns>
        protected abstract string getCharPattern(char c);

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbologyDrawing"/> class.
        /// </summary>
        public SymbologyDrawing()
        {
        }

        public SymbologyDrawing(TrueSymbologyType type)
        {
            m_type = type;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SymbologyDrawing"/> class.
        /// </summary>
        /// <param name="prototype">The existing SymbologyDrawing object to use as parameter prototype.</param>
        /// <param name="type">The new symbology drawing type.</param>
        public SymbologyDrawing(SymbologyDrawing prototype, TrueSymbologyType type)
        {
            m_type = type;
            AddChecksum = prototype.AddChecksum;
            AddChecksumToCaption = prototype.AddChecksumToCaption;
            BarHeight = prototype.BarHeight;
            CaptionFont = new SKFont(
                prototype.CaptionFont.Typeface,
                prototype.CaptionFont.Size,
                prototype.CaptionFont.ScaleX,
                prototype.CaptionFont.SkewX
            );
            CaptionPosition = prototype.m_captionPosition;
            CustomCaption = (string)prototype.CustomCaption.Clone();
            DrawCaption = prototype.DrawCaption;
            Color = prototype.Color;
            NarrowBarWidth = prototype.NarrowBarWidth;
            Options = (SymbologyOptions)prototype.Options.Clone();
            WideToNarrowRatio = prototype.WideToNarrowRatio;

            if (ValueIsValid(prototype.Value, false))
                m_value = (string)prototype.Value.Clone();
            else
                m_value = GetIncorrectValueSubstitution();
        }

        /// <summary>
        /// Disposes all resources.
        /// </summary>
        public void Dispose()
        {
            m_captionFont.Dispose();
        }

        /// <summary>
        /// Gets the incorrect value substitution.
        /// </summary>
        /// <returns>The incorrect value substitution.</returns>
        protected virtual string GetIncorrectValueSubstitution()
        {
            return "";
        }

        /// <summary>
        /// Gets or sets a value indicating whether checksum should be added to barcode.
        /// </summary>
        /// <value><c>true</c> if checksum should be added to barcode; otherwise, <c>false</c>.</value>
        public bool AddChecksum { get; set; } = true;

        /// <summary>
		/// Gets or sets a value indicating whether checksum should be added to barcode caption.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if checksum should be added to barcode caption; otherwise, <c>false</c>.
		/// </value>
		public bool AddChecksumToCaption { get; set; } = true;

        /// <summary>
		/// Gets the barcode encoded value.
		/// </summary>
		/// <value>The barcode encoded value.</value>
		public string Caption
        {
            get
            {
                if (CustomCaption.Length != 0)
                    return CustomCaption;

                return GetEncodedValue(true);
            }
        }

        /// <summary>
        /// Gets or sets the custom caption text to draw instead of the barcode encoded value.
        /// </summary>
        /// <value>The custom caption text to draw instead of the barcode encoded value.</value>
        public string CustomCaption { get; set; } = "";

        /// <summary>
		/// Gets or sets a value indicating whether to draw the barcode encoded value.
		/// </summary>
		/// <value><c>true</c> if to draw the barcode encoded value; otherwise, <c>false</c>.</value>
		public bool DrawCaption { get; set; } = true;

        /// <summary>
		/// Gets or sets a value indicating whether to draw the barcode encoded
		/// value for 2D barcodes.
		/// </summary>
		/// <value><c>true</c> if to draw the barcode encoded value for 2D 
		/// barcodes; otherwise, <c>false</c>.</value>
		public bool DrawCaption2D { get; set; }

        /// <summary>
		/// Gets the barcode symbology type
		/// </summary>
		/// <value>The barcode symbology.</value>
		public SymbologyType Symbology
        {
            get
            {
                try
                {
                    TypeConverter tc = TypeDescriptor.GetConverter(typeof(SymbologyType));
                    SymbologyType type = (SymbologyType)tc.ConvertFromString(m_type.ToString());
                    return type;
                }
                catch (Exception)
                {
                }

                return SymbologyType.Code39;
            }
        }

        /// <summary>
        /// Gets or sets the barcode symbology specific options.
        /// </summary>
        /// <value>The barcode symbology specific options.</value>
        public SymbologyOptions Options { get; set; } = new SymbologyOptions();

        /// <summary>
		/// Gets or sets the barcode value to encode.
		/// </summary>
		/// <value>The barcode value to encode.</value>
		public virtual string Value
        {
            get
            {
                return m_value;
            }
            set
            {
                if (ValueIsValid(value, false))
                {
                    m_value = value;
                }
                else
                {
                    string generic = "Provided value can't be encoded by current symbology.\n";
                    throw new BarcodeException(generic + getValueRestrictions());
                }
            }
        }

        /// <summary>
        /// Resultion of result image
        /// </summary>
        internal SKSize BarcodeResolution { get; set; }

        /// <summary>
        /// Gets or sets the height of the barcode bars in pixels.
        /// </summary>
        /// <value>The height of the barcode bars in pixels.</value>
        public virtual int BarHeight { get; set; } = 50;

        /// <summary>
		/// Gets or sets the barcode caption font.
		/// </summary>
		/// <value>The barcode caption font.</value>
		public SKFont CaptionFont
        {
            get => m_captionFont;
            set => m_captionFont = value;
        }

        /// <summary>
        /// Gets or sets the width of the narrow bar in pixels.
        /// </summary>
        /// <value>The width of the narrow bar in pixels.</value>
        public virtual int NarrowBarWidth { get; set; } = 3;

        /// <summary>
		/// Gets or sets the width of a wide bar relative to the narrow bar.
		/// </summary>
		/// <value>The width of a wide bar relative to the narrow bar.</value>
		public int WideToNarrowRatio { get; set; } = 3;

        /// <summary>
		/// Gets or sets the color used to draw the barcode bars.
		/// </summary>
		/// <value>The color used to draw the barcode bars.</value>
		public SKColor Color { get; set; } = SKColors.Black;

        /// <summary>
		/// Gets or sets the barcode caption position.
		/// </summary>
		/// <value>The barcode caption position.</value>
		public virtual CaptionPosition CaptionPosition
        {
            get => m_captionPosition;
            set => m_captionPosition = value;
        }

        public virtual CaptionAlignment CaptionAlignment { get; set; }

        /// <summary>
        /// Gets a value indicating whether this symbology can not have
        /// a caption drawn.
        /// </summary>
        public virtual bool PreventCaptionDrawing => false;

        public bool DrawQuietZones { get; set; } = true;

        public int CustomCaptionGap { get; set; } = -1;

        public bool RoundDots { get; set; } = false;

        public int RoundDotsScale { get; set; } = 100;

        /// <summary>
		/// Draws the barcode using current symbology rules.
		/// </summary>
		/// <param name="graphics">The Graphics object to draw the barcode on.</param>
		/// <param name="position">The position in pixels of the top left point of the barcode.</param>
		/// <param name="onlyCalculate">if set to <c>true</c> then barcode does not gets drawn,
		/// only size calculatations performed.</param>
		/// <returns>
		/// The size of the smallest rectangle in pixels that can accommodate the barcode with caption.
		/// </returns>
		public virtual SKSize Draw(SKCanvas canvas, SKPoint position, bool onlyCalculate)
        {
            // There is two main barcode image types: rectangular and EAN-like
            //
            // Rectangular (1D and 2D):
            //
            //                above
            //        | | | || ||| | | | || |
            //        | | | || ||| | | | || |
            //        | | | || ||| | | | || |
            //        | | | || ||| | | | || | 
            //        | | | || ||| | | | || | 
            //                below  
            //
            // EAN-like (EAN-13, EAN-8, ISBN, UPC-A, UPC-E):
            //
            //                above
            //        | | | || ||| | | | || |
            //        | | | || ||| | | | || |
            //        | | | || ||| | | | || |
            //        | | | || ||| | | | || | 
            // before | | left  || right  | | after

            bool wasError = false;

            try
            {
                m_rects.Clear();
                // we shouldn't probide buildBars with graphics of font but, in fact,
                // some symbologies contain parts whose size depends on font size.
                m_drawingSize = buildBars(canvas, CaptionFont);
            }
            catch (BarcodeException e)
            {
                m_drawingSize = calculateOrDrawException(canvas, position, e.Message, onlyCalculate);
                wasError = true;
            }

            // we can't just use CaptionFont because sometimes caption font
            // should be decreased to fit space available in EAN-like barcodes.
            SKFont captionFontToUse = getFontForCaption(canvas, CaptionPosition);

            // calculate size of areas occupied by different caption parts
            // of course, not all areas have non-zero size
            SKSize captionAbove = occupiedByCaptionAbove(canvas, captionFontToUse);
            SKSize captionBelow = occupiedByCaptionBelow(canvas, captionFontToUse);
            int captionBeforeWidth = occupiedByCaptionBefore(canvas, captionFontToUse);
            int captionAfterWidth = occupiedByCaptionAfter(canvas, captionFontToUse);

            // vertical quiet zone for 2D barcode
            int quietZone2D = (this is SymbologyDrawing2D && DrawQuietZones) ?
                NarrowBarWidth * 2 : 0;

            if (!onlyCalculate)
            {
                if (!wasError)
                    using (SKPaint paint = new SKPaint
                    {
                        Color = Color, // map from System.Drawing.Color
                        IsAntialias = true,
                        Style = SKPaintStyle.Fill
                    })
                    {
                        // offset position and draw bars
                        drawBars(canvas, paint, new SKPoint(position.X + captionBeforeWidth, position.Y + captionAbove.Height + quietZone2D));

                        if (CaptionMayBeDrawn)
                        {
                            // draw all caption parts if needed
                            drawCaptionBeforePart(canvas, paint, captionFontToUse, position);

                            // drawCaption should draw left and right caption parts in EAN-like symbologies
                            SKPoint drawingLeft = new SKPoint(position.X + captionBeforeWidth,
                                position.Y + (CaptionPosition == CaptionPosition.Below ? quietZone2D * 2 : 0));
                            drawCaption(canvas, paint, captionFontToUse, drawingLeft);

                            drawCaptionAfterPart(canvas, paint, captionFontToUse, drawingLeft);
                        }
                    }
            }

            // calculate total image width (image is an area occupied by bars and captions)
            float width = captionBeforeWidth + Math.Max(captionAbove.Width, Math.Max(captionBelow.Width, m_drawingSize.Width)) + captionAfterWidth;
            float height = captionAbove.Height + quietZone2D + m_drawingSize.Height + quietZone2D + captionBelow.Height;
            return new SKSize(width, height);
        }

        private SKSize occupiedByCaptionAbove(SKCanvas canvas, SKFont font)
        {
            SKSize captionSize = new SKSize();
            if (CaptionMayBeDrawn && CaptionPosition == CaptionPosition.Above)
                captionSize = calculateCaptionSize(canvas, font);

            return captionSize;
        }

        protected virtual SKSize occupiedByCaptionBelow(SKCanvas canvas, SKFont font)
        {
            SKSize captionSize = new SKSize();
            if (CaptionMayBeDrawn && CaptionPosition == CaptionPosition.Below)
            {
                captionSize = calculateCaptionSize(canvas, font);

                if (CustomCaptionGap != -1)
                {
                    captionSize.Height += CustomCaptionGap;
                }
            }

            return captionSize;
        }

        protected virtual int occupiedByCaptionBefore(SKCanvas canvas, SKFont font)
        {
            return 0;
        }

        protected virtual int occupiedByCaptionAfter(SKCanvas canvas, SKFont font)
        {
            return 0;
        }

        protected virtual SKSize buildBars(SKCanvas canvas, SKFont font)
        {
            SKSize drawingSize = new SKSize();
            int x = 0;
            int y = 0;

            string value = GetEncodedValue(false);

            bool drawBar = true;
            bool firstChar = true;

            foreach (char c in value)
            {
                if (!firstChar)
                {
                    // check if we are drawing Codabar symbology
                    // with this symbology we always should have 
                    if (Symbology == SymbologyType.Codabar)
                    { // always draw intercharacter gap = narrow width for Codabar
                      // otherwise character patterns will merge together as there were no space between them
                      // this will cause unreadable barcodes (see UPN-997837)

                        // by default Options.DrawIntercharacterGap == true, which causes incorrect drawing of Codabar
                        // I don't think this option is needed for Codabar symbology (Plisko)
                        //if (Options.DrawIntercharacterGap) // or if option has this enabled
                        //    x += WideToNarrowRatio * NarrowBarWidth;
                        //else
                        x += NarrowBarWidth;
                    }
                    // other symbologies
                    else if (Options.DrawIntercharacterGap) // or if option has this enabled
                        x += NarrowBarWidth;


                    drawBar = true;
                }
                else
                {
                    firstChar = false;
                }

                string pattern = getCharPattern(c);
                foreach (char patternChar in pattern)
                {
                    int width = NarrowBarWidth;
                    if (patternChar == 'w')
                        width *= WideToNarrowRatio;

                    if (drawBar)
                        m_rects.Add(new SKRect(x, y, x+width, y+BarHeight));

                    x += width;
                    drawBar = !drawBar;
                }
            }

            drawingSize.Width = x;
            drawingSize.Height = BarHeight;
            return drawingSize;
        }

        private SKSize calculateOrDrawException(SKCanvas canvas, SKPoint position, string message, bool onlyCalculate)
        {
            string error = "Failed to encode: " + message;

            using (var paint = new SKPaint
            {
                IsAntialias = true,
                Color = SKColors.Black // Or map from your Color property
            })
            {
                // Measure text bounds
                SKRect bounds = new SKRect();
                this.CaptionFont.MeasureText(error, out bounds);

                if (!onlyCalculate)
                {
                    canvas.DrawText(error, position.X, position.Y - bounds.Top, this.CaptionFont, paint);
                    // Note: subtracting bounds.Top ensures proper baseline alignment
                }

                return new SKSize(bounds.Width + 1, bounds.Height + 1);
            }
        }


        protected virtual void drawBars(SKCanvas canvas, SKPaint paint, SKPoint position)
        {
            if (RoundDots &&
                (Symbology == SymbologyType.QRCode || Symbology == SymbologyType.GS1_QRCode ||
                 Symbology == SymbologyType.DataMatrix || Symbology == SymbologyType.GS1_DataMatrix ||
                 Symbology == SymbologyType.Aztec))
            {
                foreach (SKRect r in m_rects)
                {
                    SKRect dotRect = new SKRect(r.Left + position.X, r.Top + position.Y, r.Width, r.Height);

                    if (RoundDotsScale != 100)
                    {
                        float scaledWidth = r.Width / 100f * RoundDotsScale;
                        float d = (scaledWidth - r.Width) / 2;
                        dotRect.Inflate(d, d);
                    }

                    // Draw ellipse (circle/dot)
                    canvas.DrawOval(dotRect.Left, dotRect.Top, dotRect.Right, dotRect.Bottom, paint);
                }
            }
            else
            {
                foreach (SKRect r in m_rects)
                {
                    SKRect barRect = new SKRect(
                        r.Left + position.X,
                        r.Top + position.Y,
                        r.Right + position.X,
                        r.Bottom + position.Y
                    );

                    canvas.DrawRect(barRect, paint);
                }
            }
        }

        protected virtual void drawCaptionBeforePart(SKCanvas canvas, SKPaint paint, SKFont font, SKPoint position)
        {
        }


        protected virtual void drawCaption(SKCanvas canvas, SKPaint paint, SKFont font, SKPoint position)
        {
            // Measure caption size
            SKSize size = calculateCaptionSize(canvas, font);
            SKSizeI captionSize = new SKSizeI((int)size.Width, (int)size.Height);
            SKRect captionRect = SKRect.Empty;

            // Resolve horizontal alignment
            SKTextAlign hAlign;

            switch (CaptionAlignment)
            {
                case CaptionAlignment.Auto:
                    if (captionSize.Width <= m_drawingSize.Width)
                        hAlign = SKTextAlign.Center;
                    else
                        hAlign = SKTextAlign.Left;
                    break;

                case CaptionAlignment.Left:
                    hAlign = SKTextAlign.Left;
                    break;

                case CaptionAlignment.Center:
                    hAlign = SKTextAlign.Center;
                    break;

                case CaptionAlignment.Right:
                    hAlign = SKTextAlign.Right;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            // Get font metrics
            var metrics = font.Metrics;
            float fontHeight = metrics.Descent - metrics.Ascent + metrics.Leading;

            if (CaptionPosition == CaptionPosition.Above)
            {
                captionRect = new SKRect(
                    position.X,
                    position.Y,
                    position.X + Math.Max(captionSize.Width, m_drawingSize.Width),
                    position.Y + captionSize.Height
                );

                float textWidth = font.MeasureText(Caption);

                float x;

                switch (hAlign)
                {
                    case SKTextAlign.Left:
                        x = captionRect.Left;
                        break;

                    case SKTextAlign.Center:
                        x = captionRect.MidX - (textWidth / 2f);
                        break;

                    case SKTextAlign.Right:
                        x = captionRect.Right - textWidth;
                        break;

                    default:
                        x = captionRect.Left;
                        break;
                }

                float y = captionRect.Top - metrics.Ascent;

                canvas.DrawText(Caption, x, y, font, paint);
            }
            else if (CaptionPosition == CaptionPosition.Below)
            {
                int captionGap = CustomCaptionGap == -1 ? Utils.CalculateCaptionGap(font) : CustomCaptionGap;

                captionRect = new SKRect(
                    position.X,
                    position.Y + m_drawingSize.Height + captionGap,
                    position.X + Math.Max(captionSize.Width, m_drawingSize.Width),
                    position.Y + m_drawingSize.Height + captionGap + captionSize.Height
                );

                if (CustomCaptionGap != -1)
                {
                    // Equivalent of your interlineGap fix
                    float interlineGap = (metrics.Descent - metrics.Ascent + metrics.Leading) - fontHeight;
                    captionRect.Offset(0, -interlineGap);
                }

                float textWidth = font.MeasureText(Caption);

                float x;

                switch (hAlign)
                {
                    case SKTextAlign.Left:
                        x = captionRect.Left;
                        break;

                    case SKTextAlign.Center:
                        x = captionRect.MidX - (textWidth / 2f);
                        break;

                    case SKTextAlign.Right:
                        x = captionRect.Right - textWidth;
                        break;

                    default:
                        x = captionRect.Left;
                        break;
                }

                float y = captionRect.Bottom - metrics.Descent;

                canvas.DrawText(Caption, x, y, font, paint);
            }
        }

        protected virtual void drawCaptionAfterPart(SKCanvas canvas, SKPaint paint, SKFont font, SKPoint position)
        {
        }

        /// <summary>
        /// Gets a value indicating whether a caption may be drawn.
        /// </summary>
        protected virtual bool CaptionMayBeDrawn
        {
            get
            {
                return (DrawCaption && !PreventCaptionDrawing &&
                    (CaptionPosition == CaptionPosition.Above || CaptionPosition == CaptionPosition.Below));
            }
        }

        /// <summary>
        /// Calculates the size of the caption text string.
        /// </summary>
        /// <param name="graphics">The Graphics object used to measure caption text string.</param>
        /// <param name="font">The font.</param>
        /// <returns>The size of the caption text string.</returns>
        protected SKSize calculateCaptionSize(SKCanvas canvas, SKFont font)
        {
            // Measure width directly from SKFont
            float width = font.MeasureText(Caption);

            // Measure height using font metrics
            var metrics = font.Metrics;
            float height = metrics.Descent - metrics.Ascent + metrics.Leading;

            // +1 like in GDI+ version, return integer size
            return new SKSize((width + 1), (height + 1));
        }
        protected virtual SKFont getFontForCaption(SKCanvas canvas, CaptionPosition position)
        {
            return CaptionFont;
        }
    }
}
