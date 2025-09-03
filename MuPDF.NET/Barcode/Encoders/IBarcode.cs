using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;
using System.Drawing.Text;
using System.Drawing.Drawing2D;
using System.IO;
using System.Drawing.Imaging;
using SkiaSharp;

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Delegate for the callback method called from <see cref="Barcode.DrawImagesToPDF(string, string, DrawImagesToPDFCallback)"/>.
    /// </summary>
    /// <param name="pageIndex">Index of page being processed.</param>
    /// <param name="images">Array of Image objects to draw on PDF page.</param>
    /// <param name="points">Array of upper-left points of images (in document units).</param>
    /// <param name="documentLayerName">Name for new PDF layer to place images on. If null or empty, no new layer will be created.</param>
    public delegate void DrawImagesToPDFCallback(int pageIndex, out SKImage[] images, out Point[] points, out string documentLayerName);
        
    /// <summary>
    /// Base interface for all barcode classes.
    /// </summary>
    //[Guid(Barcode.InterfaceID)]
    public interface IBarcodeEncoder
    {
        /// <summary>
        /// Gets or sets the additional barcode caption text to draw.
        /// </summary>
        /// <value>The additional barcode caption text to draw.</value>
        string AdditionalCaption { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether checksum should be added to barcode.
        /// </summary>
        /// <value><c>true</c> if checksum should be added to barcode; otherwise, <c>false</c>.</value>
        bool AddChecksum { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether checksum should be added to barcode caption.
        /// </summary>
        /// <value>
        /// <c>True</c> if checksum should be added to barcode caption; otherwise, <c>false</c>.
        /// </value>
        bool AddChecksumToCaption { get; set; }

        /// <summary>
        /// Gets or sets the barcode caption (by default this is encoded value).
        /// </summary>
        /// <value>The barcode caption.</value>
        string Caption { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to draw the barcode encoded value.
        /// </summary>
        /// <value><c>true</c> if to draw the barcode encoded value; otherwise, <c>false</c>.</value>
        bool DrawCaption { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to draw the barcode encoded value for 2D barcodes.
        /// </summary>
        /// <value>
        /// <c>True</c> if to draw the barcode encoded value for 2D barcodes; otherwise, <c>false</c>.
        /// </value>
        bool DrawCaptionFor2DBarcodes { get; set; }

	    /// <summary>
	    /// Gets or sets a value indicating whether to draw quite zones when
	    /// barcode type supposes such zones.
	    /// </summary>
	    bool DrawQuietZones { get; set; }

		/// <summary>
		/// Gets or sets the barcode symbology type.
		/// </summary>
		/// <value>The barcode symbology type.</value>
		SymbologyType Symbology { get; set; }

        /// <summary>
        /// Gets or sets the barcode symbology specific options.
        /// </summary>
        /// <value>The barcode symbology specific options.</value>
        SymbologyOptions Options { get; set; }

        /// <summary>
        /// Gets or sets the barcode value to encode.
        /// </summary>
        /// <value>The barcode value to encode.</value>
        string Value { get; set; }

        /// <summary>
        /// Gets or sets the supplementary barcode value to encode (used 
        /// with EAN-13, ISBN and UPC-A barcodes).
        /// </summary>
        /// <value>The supplementary barcode value to encode.</value>
        string SupplementValue { get; set; }

        /// <summary>
        /// Gets or sets the position of the additional caption.
        /// </summary>
        /// <value>The additional caption position.</value>
        CaptionPosition AdditionalCaptionPosition { get; set; }
        
        /// <summary>
        /// Gets or sets text alignment of the additional barcode caption.
        /// </summary>
        /// <value>Caption alignment.</value>
        CaptionAlignment AdditionalCaptionAlignment { get; set; }

        /// <summary>
        /// Gets or sets the barcode rotation angle in degrees.
        /// </summary>
        /// <value>The barcode rotation angle in degrees.</value>
        RotationAngle Angle { get; set; }

        /// <summary>
        /// Gets or sets the horizontal resolution of barcode.
        /// </summary>
        /// <value>The horizontal resolution of barcode.</value>
        float ResolutionX { get; set; }

        /// <summary>
        /// Gets or sets the vertical resolution of barcode.
        /// </summary>
        /// <value>The vertical resolution of barcode.</value>
        float ResolutionY { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the component should produce monochrome (1-bit, black and white) images.
		/// </summary>
		/// <remarks>Note: 1-bit monochrome images can be created only in BMP, PNG and TIFF image formats.</remarks>
		/// <value><c>true</c> if images should be saved in monochrome format; otherwise, <c>false</c>.</value>
		bool ProduceMonochromeImages { get; set; }

        /// <summary>
        /// Gets or sets the color used to draw the barcode background.
        /// </summary>
        /// <value>The color used to draw the barcode background.</value>
        SKColor BackColor { get; set; }

        /// <summary>
        /// Gets or sets the position of the barcode caption.
        /// </summary>
        /// <value>The barcode caption position.</value>
        CaptionPosition CaptionPosition { get; set; }
        
        /// <summary>
        /// Gets or sets the alignment of barcode caption text.
        /// </summary>
        /// <value>Caption alignment.</value>
        CaptionAlignment CaptionAlignment { get; set; }

        /// <summary>
        /// Gets or sets the color used to draw the barcode bars and caption(s).
        /// </summary>
        /// <value>The color used to draw the barcode bars and caption(s).</value>
        SKColor ForeColor { get; set; }

        /// <summary>
        /// Gets or sets the barcode margins in pixels.
        /// </summary>
        /// <value>The barcode margins in pixels.</value>
        Margins Margins { get; set; }

        /// <summary>
        /// Gets or sets the font of the additional barcode caption.
        /// </summary>
        /// <value>The additional barcode caption font.</value>
        SKFont AdditionalCaptionFont { get; set; }

        /// <summary>
        /// Gets or sets the height of the barcode bars in pixels.
        /// </summary>
        /// <value>The height of the barcode bars in pixels.</value>
        int BarHeight { get; set; }

        /// <summary>
        /// Gets or sets the font of the barcode caption.
        /// </summary>
        /// <value>The barcode caption font.</value>
        SKFont CaptionFont { get; set; }        

        /// <summary>
        /// Gets or sets the width of the narrow bar in pixels.
        /// </summary>
        /// <value>The width of the narrow bar in pixels.</value>
        int NarrowBarWidth { get; set; }

        /// <summary>
        /// Gets or sets the width of the wide bar relative to the narrow bar.
        /// </summary>
        /// <value>The width of the wide bar relative to the narrow bar.</value>
        int WideToNarrowRatio { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether unused space should be cut when 
        /// drawing or saving barcode images. Unused space is usually a result
        /// of calling one of FitInto methods with size greater then needed
        /// to draw barcode.
        /// </summary>
        /// <value><c>true</c> if unused space should be cut; otherwise, <c>false</c>.</value>
        bool CutUnusedSpace { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether to check output size so it's
        /// not less than barcode size. 
        /// Use FitInto() method to fit barcode into a given physical size.
        /// </summary>
        /// <value>A value indicating whether to check output size so it's
        /// not less than barcode size.</value>
        bool PreserveMinReadableSize { get; set; }
        
        /// <summary>
        /// Sets whether to generate barcodes with round dots. Works only for QR Code, DataMatrix,
        /// and Aztec barcode types.
        /// </summary>
        bool RoundDots { get; set; }
        
        /// <summary>
        /// Scale factor for <see cref="RoundDots"/> in percents.
        /// </summary>
        int RoundDotsScale { get; set; }

		///<summary>
		/// Gets the component version number.
		///</summary>
		string Version { get; }

       
        /// <summary>
        /// Comma-separated list of profiles to apply to the <see cref="Barcode"/>.
        /// Profiles are sets of properties and methods represented as JSON string.
        /// Check the source code examples installed with the SDK.
        /// </summary>
        string Profiles { get; set; }
        
        /// <summary>
        /// Loads profiles from file.
        /// </summary>
        /// <param name="fileName">JSON file containing profiles.</param>
        void LoadProfiles(string fileName);
        
        /// <summary>
        /// Loads profiles from JSON string.
        /// </summary>
        /// <param name="jsonString">JSON string containing profiles.</param>
        void LoadProfilesFromString(string jsonString);

        /// <summary>
        /// Loads profiles from JSON string and automatically applies them. Note that profiles containing
        /// detection keywords will be deferred until the extraction.
        /// </summary>
        /// <remarks>Note, all existing profiles are discarded before loading profiles from the provided string.</remarks>
        /// <param name="jsonString">JSON string containing profiles.</param>
        void LoadAndApplyProfiles(string jsonString);

        /// <summary>
        /// Creates JSON profile will all Barcode properties with current values.
        /// </summary>
        /// <param name="profileName">Name of profile (without spaces).</param>
        /// <param name="outputFileName">Output file name.</param>
        void CreateProfile(string profileName, string outputFileName);

        /// <summary>
        /// Creates JSON profile will all Barcode properties with current values.
        /// </summary>
        /// <param name="profileName">Name of profile (without spaces).</param>
        /// <returns>JSON string.</returns>
        string CreateProfile(string profileName);

        /// <summary>
        /// Validates the value using current symbology rules.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.</returns>
        bool ValueIsValid(string value);

		/// <summary>
		/// Validates the value using current symbology rules.
		/// </summary>
		/// <param name="value">The value.</param>
		/// <param name="checksumIsMandatory">Expect the checksum if it's applicable for selected symbology.</param>
		/// <returns><c>true</c> if value is valid (can be encoded); otherwise, <c>false</c>.</returns>
		bool ValueIsValid(string value, bool checksumIsMandatory);

        /// <summary>
        /// Validates the GS1 value using GS1 rules.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        /// <c>True</c> if value is valid (can be encoded); otherwise, <c>false</c>.
        /// </returns>
        bool ValueIsValidGS1(string value);

        /// <summary>
        /// Gets the value restrictions for the specified symbology.
        /// </summary>
        /// <param name="symbology">The symbology to get value restrictions for.</param>
        /// <returns>The value restrictions for the specified symbology.</returns>
        string GetValueRestrictions(SymbologyType symbology);

        /// <summary>
        /// Gets the supplementary value restrictions.
        /// </summary>
        /// <returns>The supplementary value restrictions.</returns>
        string GetSupplementaryValueRestrictions();

        /// <summary>
        /// Sets the barcode margins in specified units.
        /// </summary>
        /// <param name="left">The left margin value in specified units.</param>
        /// <param name="top">The top margin value in specified units.</param>
        /// <param name="right">The right margin value in specified units.</param>
        /// <param name="bottom">The bottom margin value in specified units.</param>
        /// <param name="unit">The unit of measure for margin values.</param>
        void SetMargins(float left, float top, float right, float bottom, UnitOfMeasure unit);

        /// <summary>
        /// Returns the barcode margins in specified units.
        /// </summary>
        /// <param name="left">Receives the left margin value in specified units.</param>
        /// <param name="top">Receives the top margin value in specified units.</param>
        /// <param name="right">Receives the right margin value in specified units.</param>
        /// <param name="bottom">Receives the bottom margin value in specified units.</param>
        /// <param name="unit">The unit of measure for retrieved margin values.</param>
        void GetMargins(out float left, out float top, out float right, out float bottom, UnitOfMeasure unit);

        /// <summary>
        /// Returns the left barcode margin value in specified units.
        /// </summary>
        /// <param name="unit">The unit of measure for retrieved margin value.</param>
        /// <returns>
        /// The left barcode margin value in specified units.
        /// </returns>
        float GetMarginLeft(UnitOfMeasure unit);

        /// <summary>
        /// Returns the top barcode margin value in specified units.
        /// </summary>
        /// <param name="unit">The unit of measure for retrieved margin value.</param>
        /// <returns>
        /// The top barcode margin value in specified units.
        /// </returns>
        float GetMarginTop(UnitOfMeasure unit);

        /// <summary>
        /// Returns the right barcode margin value in specified units.
        /// </summary>
        /// <param name="unit">The unit of measure for retrieved margin value.</param>
        /// <returns>
        /// The right barcode margin value in specified units.
        /// </returns>
        float GetMarginRight(UnitOfMeasure unit);

        /// <summary>
        /// Retrieves the bottom barcode margin value in specified units.
        /// </summary>
        /// <param name="unit">The unit of measure for retrieved margin value.</param>
        /// <returns>
        /// The bottom barcode margin value in specified units.
        /// </returns>
        float GetMarginBottom(UnitOfMeasure unit);        

        /// <summary>
        /// Sets the height of the barcode bars in specified units.
        /// </summary>
        /// <param name="height">The height of the barcode bars in specified units.</param>
        /// <param name="unit">The unit of measure for bar height value.</param>
        void SetBarHeight(float height, UnitOfMeasure unit);

        /// <summary>
        /// Returns the height of the barcode bars in specified units.
        /// </summary>
        /// <param name="unit">The unit of measure for retrieved bar height value.</param>
        /// <returns>The height of the barcode bars in specified units.</returns>
        float GetBarHeight(UnitOfMeasure unit);

        /// <summary>
        /// Sets the width of the narrow bars in specified units.
        /// </summary>
        /// <param name="width">The width of the narrow bars in specified units.</param>
        /// <param name="unit">The unit of measure for narrow bar width value.</param>
        void SetNarrowBarWidth(float width, UnitOfMeasure unit);

        /// <summary>
        /// Retrieves the width of the narrow bar in specified units.
        /// </summary>
        /// <param name="unit">The unit of measure for retrieved narrow bar width.</param>
        /// <returns>The width of the narrow bar in specified units.</returns>
        float GetNarrowBarWidth(UnitOfMeasure unit);

        /// <summary>
        /// Returns the size in specified units of the smallest rectangle that
        /// can accommodate the barcode.
        /// </summary>
        /// <param name="unit">The unit of measure for retrieved size.</param>
        /// <returns>The size in specified units of the smallest rectangle that
        /// can accommodate the barcode.</returns>
        SizeF GetMinimalSize(UnitOfMeasure unit);

        /// <summary>
        /// Returns the width in specified units of the smallest rectangle that
        /// can accommodate the barcode.
        /// </summary>
        /// <param name="unit">The unit of measure for retrieved width.</param>
        /// <returns>
        /// The width in specified units of the smallest rectangle that
        /// can accommodate the barcode.
        /// </returns>
        float GetMinimalWidth(UnitOfMeasure unit);

        /// <summary>
        /// Returns the height in specified units of the smallest rectangle that
        /// can accommodate the barcode.
        /// </summary>
        /// <param name="unit">The unit of measure for retrieved height.</param>
        /// <returns>
        /// The height in specified units of the smallest rectangle that
        /// can accommodate the barcode.
        /// </returns>
        float GetMinimalHeight(UnitOfMeasure unit);

        /// <summary>
	    /// IMPORTANT: Call this method AFTER setting the barcode value.
        /// Fits the barcode into the area of size specified in pixels.
        /// Calling this method will change output size of the barcode.
        /// Barcode size will be increased in order to occupy all
        /// of the area.
        /// </summary>
        /// <param name="size">The size of the area to fit the barcode into.</param>        
        void FitInto(Size size);

        /// <summary>
	    /// IMPORTANT: Call this method AFTER setting the barcode value.
        /// Fits the barcode into the area of size specified in pixels.
        /// Calling this method will change output size of the barcode.
        /// Barcode size will be increased in order to occupy all
        /// of the area.
        /// </summary>
        /// <param name="width">The width of the area.</param>
        /// <param name="height">The height of the area.</param>        
        void FitInto(int width, int height);

        /// <summary>
	    /// IMPORTANT: Call this method AFTER setting the barcode value.
        /// Fits the barcode into the area of size specified in units.
        /// Calling this method will change output size of the barcode.
        /// Barcode size will be increased in order to occupy all
        /// of the area.
        /// </summary>
        /// <param name="size">The size of the area to fit the barcode into.</param>
        /// <param name="unit">The unit of the size.</param>
        [ComVisible(false)]
        void FitInto(SizeF size, UnitOfMeasure unit);

        /// <summary>
	    /// IMPORTANT: Call this method AFTER setting the barcode value.
        /// Fits the barcode into the area of size specified in units.
        /// Calling this method will change output size of the barcode.
        /// Barcode size will be increased in order to occupy all
        /// of the area.
        /// </summary>
        /// <param name="width">The width of the area in specified units.</param>
        /// <param name="height">The height of the area in specified units.</param>
        /// <param name="unit">The unit of measure for area size.</param>
        void FitInto(float width, float height, UnitOfMeasure unit);

        /// <summary>
        /// Reverts any changes to barcode size caused by a call to any of FitInto methods.
        /// </summary>
        void RevertToNormalSize();

        /// <summary>
        /// Gets or sets the size of the smallest rectangle in pixels that
        /// can accommodate the barcode.
        /// </summary>
        /// <returns>The size of the smallest rectangle in pixels that can accommodate the barcode.</returns>
        Size GetMinimalSize();

        /// <summary>
        /// Draws the barcode on <see cref="System.Drawing.Graphics"/> canvas object.
        /// </summary>
        /// <param name="graphics">The <see cref="System.Drawing.Graphics"/> object to draw the barcode on.</param>
        /// <param name="position">The position in pixels of the top left point of the barcode.</param>
        [ComVisible(false)]
        void Draw(SKCanvas canvas, Point position);
        
        /// <summary>
        /// Draws the barcode.
        /// </summary>
        /// <param name="graphics">The Graphics object to draw the barcode on.</param>
        /// <param name="left">The coordinate of leftmost barcode point in specified units.</param>
        /// <param name="top">The coordinate of topmost barcode point in specified units.</param>
        /// <param name="unit">The unit of measure for coordinates.</param>
        [ComVisible(false)]
        void Draw(SKCanvas canvase, float left, float top, UnitOfMeasure unit);

        /// <summary>
        /// Gets the <see cref="System.Drawing.Image"/> object with the barcode.
        /// </summary>
        /// <returns>The <see cref="System.Drawing.Image"/> object with the barcode.</returns>
        SKBitmap GetImage();

        /// <summary>
		/// Saves the barcode image to file.
        /// </summary>
        /// <param name="fileName">Name of the file to save barcode to.</param>
        void SaveImage(string fileName);

		/// <summary>
		/// Saves the barcode image to file.
		/// </summary>
		/// <param name="fileName">Name of the file to save barcode to.</param>
		/// <param name="imageFormat">Format of the barcode image.</param>
		[ComVisible(false)]
		void SaveImage(string fileName, SKEncodedImageFormat imageFormat);

        /// <summary>
        /// Saves the barcode image to file.
        /// </summary>
        /// <param name="fileName">Name of the file to save barcode to.</param>
        /// <param name="format">The format of the image (may be different then implied by the file name extension).</param>
        /// <param name="areaSize">Size of the area containing image.</param>
        /// <param name="imageLeft">The image leftmost position within area.</param>
        /// <param name="imageTop">The image topmost position within area.</param>
        [ComVisible(false)]
        void SaveImage(string fileName, SKEncodedImageFormat format, Size areaSize, int imageLeft, int imageTop);

        /// <summary>
        /// Saves the barcode image to specified stream.
        /// </summary>
        /// <param name="stream">The stream to save barcode to.</param>
        [ComVisible(false)]
        void SaveImage(Stream stream);

        /// <summary>
        /// Saves the barcode image to specified stream.
        /// </summary>
        /// <param name="stream">The stream to save barcode to.</param>
		/// <param name="imageFormat">Format of the barcode image.</param>
        [ComVisible(false)]
		void SaveImage(Stream stream, SKEncodedImageFormat imageFormat);

        /// <summary>
        /// Saves the image with barcode into given stream.
        /// </summary>
        /// <param name="stream">The stream to save barcode to.</param>
        /// <param name="format">The image format of the barcode.</param>
        /// <param name="areaSize">Size of the area containing image.</param>
        /// <param name="imageLeft">The image leftmost position within area.</param>
        /// <param name="imageTop">The image topmost position within area.</param>
        [ComVisible(false)]
        void SaveImage(Stream stream, SKEncodedImageFormat format, Size areaSize, int imageLeft, int imageTop);

        /// <summary>
        /// Returns the barcode image as byte array.
        /// </summary>
        /// <returns>The byte array containing barcode image.</returns>
        byte[] GetImageBytes();

        /// <summary>
        /// Returns the barcode image in TIFF format as byte array.
        /// </summary>
        /// <returns>The byte array containing barcode image.</returns>
        byte[] GetImageBytesTIFF();

        /// <summary>
        /// Returns the barcode image in PNG format as byte array.
        /// </summary>
        /// <returns>The byte array containing barcode image.</returns>
        byte[] GetImageBytesPNG();

        /// <summary>
        /// Returns the barcode image in GIF format as byte array.
        /// </summary>
        /// <returns>The byte array containing barcode image.</returns>
        byte[] GetImageBytesGIF();

        /// <summary>
        /// Returns the barcode image in JPEG format as byte array.
        /// </summary>
        /// <returns>The byte array containing barcode image.</returns>
        byte[] GetImageBytesJPG();

        /// <summary>
        /// Sets the font of the barcode caption.
        /// </summary>
        /// <param name="familyName">The font family.</param>
        /// <param name="size">The size of the font in pixels.</param>
        void SetCaptionFont(string familyName, int size);

        /// <summary>
        /// Sets the font of the barcode caption font.
        /// </summary>
        /// <param name="familyName">The font family.</param>
        /// <param name="size">The size of the font in pixels.</param>
        /// <param name="bold">If <c>true</c>, then the text drawn with the font will be bold.</param>
        /// <param name="italic">If <c>true</c>, then the text drawn with the font will be italic.</param>
        /// <param name="underline">If <c>true</c>, then the text drawn with the font will be underlined.</param>
        /// <param name="strikeout">If <c>true</c>, then the text drawn with the font will be contain line through the middle.</param>
        /// <param name="gdiCharSet">A GDI character set to use for this font.</param>
        void SetCaptionFont(string familyName, int size, bool bold, bool italic, bool underline, bool strikeout, byte gdiCharSet);

        /// <summary>
        /// Sets the font of the additional barcode caption.
        /// </summary>
        /// <param name="familyName">The font family.</param>
        /// <param name="size">The size of the font in pixels.</param>
        void SetAdditionalCaptionFont(string familyName, int size);

        /// <summary>
        /// Sets the font of the barcode additional caption.
        /// </summary>
        /// <param name="familyName">The font family.</param>
        /// <param name="size">The size of the font in pixels.</param>
        /// <param name="bold">If <c>true</c>, then the text drawn with the font will be bold.</param>
        /// <param name="italic">If <c>true</c>, then the text drawn with the font will be italic.</param>
        /// <param name="underline">If <c>true</c>, then the text drawn with the font will be underlined.</param>
        /// <param name="strikeout">If <c>true</c>, then the text drawn with the font will be contain line through the middle.</param>
        /// <param name="gdiCharSet">A GDI character set to use for this font.</param>
        void SetAdditionalCaptionFont(string familyName, int size, bool bold, bool italic, bool underline, bool strikeout, byte gdiCharSet);

	    /// <summary>
		/// Draws barcode to image at specified coordinates.
		/// </summary>
		/// <param name="inputFile">Input image file path.</param>
		/// <param name="pageIndex">Index of the page for multi-page TIFF images. -1 means all pages.</param>
		/// <param name="x">X coordinate.</param>
		/// <param name="y">Y coordinate.</param>
		/// <param name="outputFile">Output image file path.</param>
	    void DrawToImage(string inputFile, int pageIndex, int x, int y, string outputFile);

	    /// <summary>
	    /// Draws barcode to image at specified coordinates.
	    /// </summary>
	    /// <param name="inputStream">Input stream containing the image to draw the barcode on.</param>
	    /// <param name="pageIndex">Index of the page for multi-page TIFF images. -1 means all pages.</param>
	    /// <param name="x">X coordinate.</param>
	    /// <param name="y">Y coordinate.</param>
	    /// <param name="outputStream">Output stream.</param>
	    void DrawToImage(Stream inputStream, int pageIndex, int x, int y, Stream outputStream);

		/// <summary>
        /// Sets the fore color in RGB format.
        /// </summary>
        /// <param name="r">Red color component.</param>
        /// <param name="g">Green color component.</param>
        /// <param name="b">Blue color component.</param>
        void SetForeColorRGB(byte r, byte g, byte b);

        /// <summary>
        /// Sets the background color in RGB format.
        /// </summary>
        /// <param name="r">Red color component.</param>
        /// <param name="g">Green color component.</param>
        /// <param name="b">Blue color component.</param>
        void SetBackColorRGB(byte R, byte G, byte B);

	    /// <summary>
		/// Sets the gap size between the barcode and caption. To reset the gap to default (1/10 of caption font height) set <paramref name="gap"/> to float.NaN.
	    /// </summary>
	    /// <param name="gap">Gap size in specified units.</param>
	    /// <param name="unit">The unit of measure for margin values.</param>
        /// <remarks>You should use this method after setting the ResolutionY property.</remarks>
	    void SetCustomCaptionGap(float gap, UnitOfMeasure unit);

        /// <summary>
        /// Replaces macro codes with corresponding ASCII control characters.
        /// </summary>
        /// <remarks>
        /// Use following macros to insert ASCII control characters: {NUL}, {SOH}, {STX}, {ETX}, {EOT}, {ENQ}, {ACK}, {BEL}, {BS}, {TAB}, {LF}, {VT}, {FF}, {CR}, 
        /// {SO}, {SI}, {DLE}, {DC1}, {DC2}, {DC3}, {DC4}, {NAK}, {SYN}, {ETB}, {CAN}, {EM}, {SUB}, {ESC}, {FS}, {GS}, {RS}, {US}.
        /// </remarks>
        /// <param name="value">Input value.</param>
        /// <returns>Value with processed macros.</returns>
        string ProcessMacros(string value);

        /// <summary>
        /// Add decorative image to draw in the center of the barcode. 
        /// (!) Supported with QR Code only.
        /// </summary>
        /// <remarks>Note, the embedded image damages the barcode, but the QR Code's error correction algorithm makes it possible 
        /// to decode the damaged barcode if the damage doesn't exceed 7-30% of barcode square (depending on the error correction level).
        /// See <see cref="Options"/> and <see cref="QRErrorCorrectionLevel"/>.
        /// It's recommended to generate the QR Code with highest error correction level and check the barcode is still decodable after image applying. 
        /// To read the barcode you can use ByteScout Barcode Reader SDK: https://bytescout.com/products/developer/barcodereadersdk/bytescoutbarcodereadersdk.html
        /// </remarks>
        /// <param name="imageFileName">Image to add to barcode.</param>
        /// <param name="scale">Scale of the image square relatively to the barcode square, in percents. 
        /// Recommended is 15 percents with the highest error correction level. Set -1 to disable the scaling.</param>
        void AddDecorationImage(string imageFileName, int scale);

        /// <summary>
        /// Add decorative image to draw in the center of the barcode. 
        /// (!) Supported with QR Code only.
        /// </summary>
        /// <remarks>Note, the embedded image damages the barcode, but the QR Code's error correction algorithm makes it possible 
        /// to decode the damaged barcode if the damage doesn't exceed 7-30% of barcode square (depending on the error correction level).
        /// See <see cref="Options"/> and <see cref="QRErrorCorrectionLevel"/>.
        /// It's recommended to generate the QR Code with highest error correction level and check the barcode is still decodable after image applying. 
        /// To read the barcode you can use ByteScout Barcode Reader SDK: https://bytescout.com/products/developer/barcodereadersdk/bytescoutbarcodereadersdk.html
        /// </remarks>
        /// <param name="image">Image to add to barcode.</param>
        /// <param name="scale">Scale of the image square relatively to the barcode square, in percents. 
        /// Recommended is 15 percents with the highest error correction level. Set -1 to disable the scaling.</param>
        [ComVisible(false)]
        void AddDecorationImage(SKImage image, int scale);
    }
}
