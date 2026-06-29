using System;
using System.Collections.Generic;
using SkiaSharp;

namespace MuPDF.NET
{
    /// <summary>
    /// Static image-processing helpers for <see cref="SKBitmap"/> preprocessing (OCR, table extraction, etc.).
    /// </summary>
    /// <remarks>
    /// <para>Methods take <c>ref SKBitmap</c> and replace the bitmap in place; many filters dispose the
    /// original and allocate a new buffer. Use <see cref="ImageFilterPipeline"/> to compose ordered filter chains.</para>
    /// </remarks>
    public static class ImageFilter
    {
        /// <summary>
        /// Automatically detects and corrects image skew using projection-based analysis.
        /// </summary>
        /// <param name="image">Image to deskew; replaced with the corrected bitmap.</param>
        /// <param name="minAngle">Minimum tilt in degrees before correction is applied (default 0.4).</param>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        public static void AutoDeskew(ref SKBitmap image, double minAngle = DeskewFilterOptions.DEFAULT_TILT_CORRECTION_ANGLE_THRESHOLD)
        {
            EnsureBitmap(image, nameof(image));
            Deskew.Process(ref image, (float)minAngle);
        }

        /// <summary>
        /// Expands dark pixels to repair broken or fragmented text (morphological dilation).
        /// </summary>
        /// <param name="image">Image to process; replaced in place.</param>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        public static void ApplyDilation(ref SKBitmap image)
        {
            EnsureBitmap(image, nameof(image));
            Dilate.Process(ref image);
        }

        /// <summary>
        /// Removes horizontal or vertical lines while preserving text.
        /// </summary>
        /// <param name="image">Image to process; replaced in place.</param>
        /// <param name="vertical"><c>true</c> for vertical lines, <c>false</c> for horizontal.</param>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        public static void RemoveLines(ref SKBitmap image, bool vertical)
        {
            EnsureBitmap(image, nameof(image));
            LineRemover.Process(ref image, vertical);
        }

        /// <summary>Removes vertical lines (table borders, separators).</summary>
        /// <param name="image">Image to process; replaced in place.</param>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        public static void RemoveVerticalLines(ref SKBitmap image) => RemoveLines(ref image, true);

        /// <summary>Removes horizontal lines (table borders, separators).</summary>
        /// <param name="image">Image to process; replaced in place.</param>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        public static void RemoveHorizontalLines(ref SKBitmap image) => RemoveLines(ref image, false);

        /// <summary>
        /// Detects line segments without modifying the image.
        /// </summary>
        /// <param name="image">Image to analyze.</param>
        /// <returns>Horizontal and vertical segment lists.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        public static (List<LineRemover.Segment> Horizontal, List<LineRemover.Segment> Vertical) CollectLines(ref SKBitmap image)
        {
            EnsureBitmap(image, nameof(image));
            var horizontal = new List<LineRemover.Segment>();
            var vertical = new List<LineRemover.Segment>();
            LineRemover.CollectLines(ref image, ref horizontal, ref vertical);
            return (horizontal, vertical);
        }

        /// <summary>
        /// Detects line segments and fills the supplied lists (created when null).
        /// </summary>
        /// <param name="image">Image to analyze.</param>
        /// <param name="horizontal">Receives horizontal segments.</param>
        /// <param name="vertical">Receives vertical segments.</param>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        public static void CollectLines(ref SKBitmap image, ref List<LineRemover.Segment> horizontal, ref List<LineRemover.Segment> vertical)
        {
            EnsureBitmap(image, nameof(image));
            horizontal ??= new List<LineRemover.Segment>();
            vertical ??= new List<LineRemover.Segment>();
            LineRemover.CollectLines(ref image, ref horizontal, ref vertical);
        }

        /// <summary>
        /// Median filter for noise reduction; even <paramref name="kernelSize"/> is bumped to the next odd value.
        /// </summary>
        /// <param name="image">Image to process; replaced in place.</param>
        /// <param name="kernelSize">Kernel size (positive; default 3).</param>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="kernelSize"/> is not positive.</exception>
        public static void ApplyMedian(ref SKBitmap image, int kernelSize = 3)
        {
            EnsureBitmap(image, nameof(image));
            if (kernelSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(kernelSize), "Kernel size must be positive.");

            if (kernelSize % 2 == 0)
                kernelSize += 1;

            Median.Process(ref image, kernelSize, kernelSize);
        }

        /// <summary>
        /// Gamma correction; values below 1.0 darken, above 1.0 brighten.
        /// </summary>
        /// <param name="image">Image to process; replaced in place.</param>
        /// <param name="gamma">Gamma value (must be &gt; 0; 1.0 is unchanged).</param>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="gamma"/> is not positive.</exception>
        public static void AdjustGamma(ref SKBitmap image, double gamma)
        {
            EnsureBitmap(image, nameof(image));
            if (gamma <= 0)
                throw new ArgumentOutOfRangeException(nameof(gamma), "Gamma must be greater than zero.");

            Gamma.Process(ref image, (float)gamma);
        }

        /// <summary>
        /// Adjusts contrast (-100 to +100). Converts to RGB888x when needed.
        /// </summary>
        /// <param name="image">Image to process; replaced in place.</param>
        /// <param name="contrastLevel">Contrast delta (0 = no change).</param>
        /// <returns><c>false</c> if the format is unsupported after conversion.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        public static bool AdjustContrast(ref SKBitmap image, int contrastLevel)
        {
            EnsureBitmap(image, nameof(image));
            EnsureColorType(ref image, SKColorType.Rgb888x, SKAlphaType.Opaque);
            return Contrast.Process(ref image, contrastLevel);
        }

        /// <summary>
        /// Box blur (zone size 1–10). Converts to RGB888x when needed.
        /// </summary>
        /// <param name="image">Image to process; replaced in place.</param>
        /// <param name="blurZoneSize">Blur radius scale (≥ 1).</param>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="blurZoneSize"/> is less than 1.</exception>
        public static void ApplyBlur(ref SKBitmap image, int blurZoneSize)
        {
            EnsureBitmap(image, nameof(image));
            if (blurZoneSize < 1)
                throw new ArgumentOutOfRangeException(nameof(blurZoneSize), "Blur zone size must be at least 1.");

            EnsureColorType(ref image, SKColorType.Rgb888x, SKAlphaType.Opaque);
            Blur.Process(ref image, blurZoneSize);
        }

        /// <summary>
        /// Converts to grayscale (0.299R + 0.587G + 0.114B).
        /// </summary>
        /// <param name="image">Image to process; replaced in place.</param>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        public static void ToGrayscale(ref SKBitmap image)
        {
            EnsureBitmap(image, nameof(image));
            Grayscale.Process(ref image);
        }

        /// <summary>Inverts all pixel colors.</summary>
        /// <param name="image">Image to process; replaced in place.</param>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        public static void ApplyInvert(ref SKBitmap image)
        {
            EnsureBitmap(image, nameof(image));
            Invert.Process(ref image);
        }

        /// <summary>
        /// Scales by <paramref name="scaleFactor"/> preserving aspect ratio.
        /// </summary>
        /// <param name="image">Image to scale; replaced in place.</param>
        /// <param name="scaleFactor">Scale factor (&gt; 0).</param>
        /// <param name="quality">Resampling quality (default High).</param>
        /// <returns>Size after scaling; unchanged size when factor is 1.0.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="scaleFactor"/> is not positive.</exception>
        public static SKSizeI ScaleImage(ref SKBitmap image, double scaleFactor, SKFilterQuality quality = SKFilterQuality.High)
        {
            EnsureBitmap(image, nameof(image));
            if (scaleFactor <= 0)
                throw new ArgumentOutOfRangeException(nameof(scaleFactor), "Scale factor must be greater than zero.");

            if (Math.Abs(scaleFactor - 1d) < double.Epsilon)
                return new SKSizeI(image.Width, image.Height);

            return Scale.Process(ref image, (float)scaleFactor, quality);
        }

        /// <summary>
        /// Scales down to fit within a <paramref name="maxDimension"/> square; no change if already smaller.
        /// </summary>
        /// <param name="image">Image to scale; replaced in place.</param>
        /// <param name="maxDimension">Maximum width and height (&gt; 0).</param>
        /// <param name="quality">Resampling quality (default High).</param>
        /// <returns>Resulting bitmap size.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDimension"/> is not positive.</exception>
        public static SKSizeI Fit(ref SKBitmap image, int maxDimension, SKFilterQuality quality = SKFilterQuality.High)
        {
            EnsureBitmap(image, nameof(image));
            if (maxDimension <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxDimension), "Fit dimension must be greater than zero.");

            if (image.Width == 0 || image.Height == 0)
                return new SKSizeI(image.Width, image.Height);

            if (image.Width <= maxDimension && image.Height <= maxDimension)
                return new SKSizeI(image.Width, image.Height);

            double scale = Math.Min((double)maxDimension / image.Width, (double)maxDimension / image.Height);
            if (scale <= 0 || scale >= 1d)
                return new SKSizeI(image.Width, image.Height);

            return ScaleImage(ref image, scale, quality);
        }

        /// <summary>
        /// Applies <see cref="PreprocessingFilter"/> entries in order (null entries skipped).
        /// </summary>
        /// <param name="image">Image to process; replaced in place.</param>
        /// <param name="filters">Filter sequence; null is ignored.</param>
        /// <exception cref="ArgumentNullException"><paramref name="image"/> is null.</exception>
        /// <exception cref="NotSupportedException">An unknown <see cref="ImageProcessingFilterType"/> is encountered.</exception>
        /// <remarks>
        /// Supported types: Deskew, Dilate, RemoveVerticalLines, RemoveHorizontalLines, Median, Gamma,
        /// Contrast, Grayscale, Invert, Scale, Fit. Use <see cref="ApplyBlur"/> directly; blur is not a pipeline type.
        /// </remarks>
        public static void ApplyFilters(ref SKBitmap image, IEnumerable<PreprocessingFilter> filters)
        {
            EnsureBitmap(image, nameof(image));
            if (filters == null)
                return;

            foreach (var filter in filters)
            {
                if (filter == null)
                    continue;

                switch (filter.Type)
                {
                    case ImageProcessingFilterType.Deskew:
                    {
                        var options = filter.Options as DeskewFilterOptions ?? new DeskewFilterOptions();
                        AutoDeskew(ref image, options.MinAngle);
                        break;
                    }
                    case ImageProcessingFilterType.Dilate:
                        ApplyDilation(ref image);
                        break;
                    case ImageProcessingFilterType.RemoveVerticalLines:
                        RemoveVerticalLines(ref image);
                        break;
                    case ImageProcessingFilterType.RemoveHorizontalLines:
                        RemoveHorizontalLines(ref image);
                        break;
                    case ImageProcessingFilterType.Median:
                    {
                        var options = filter.Options as MedianFilterOptions ?? new MedianFilterOptions();
                        ApplyMedian(ref image, options.BlockSize);
                        break;
                    }
                    case ImageProcessingFilterType.Gamma:
                    {
                        var options = filter.Options as GammaFilterOptions ?? new GammaFilterOptions(1f);
                        AdjustGamma(ref image, options.Gamma);
                        break;
                    }
                    case ImageProcessingFilterType.Contrast:
                    {
                        var options = filter.Options as ContrastFilterOptions ?? new ContrastFilterOptions(0);
                        AdjustContrast(ref image, options.Contrast);
                        break;
                    }
                    case ImageProcessingFilterType.Grayscale:
                        ToGrayscale(ref image);
                        break;
                    case ImageProcessingFilterType.Invert:
                        ApplyInvert(ref image);
                        break;
                    case ImageProcessingFilterType.Scale:
                    {
                        if (filter.Options is ScaleFilterOptions options && options.ScaleFactor > 0)
                            ScaleImage(ref image, options.ScaleFactor, options.InterpolationMode);
                        break;
                    }
                    case ImageProcessingFilterType.Fit:
                    {
                        if (filter.Options is FitFilterOptions options && options.FitToSize > 0)
                            Fit(ref image, options.FitToSize, options.InterpolationMode);
                        break;
                    }
                    default:
                        throw new NotSupportedException($"Filter '{filter.Type}' is not supported.");
                }
            }
        }

        /// <summary>
        /// Converts <paramref name="bitmap"/> to the required color/alpha type when needed (disposes original).
        /// </summary>
        /// <param name="bitmap">Bitmap to check; replaced when conversion runs.</param>
        /// <param name="colorType">Target color type.</param>
        /// <param name="alphaType">Target alpha type, or null to keep the current alpha type.</param>
        public static void EnsureColorType(ref SKBitmap bitmap, SKColorType colorType, SKAlphaType? alphaType = null)
        {
            if (bitmap == null)
                return;

            if (bitmap.ColorType == colorType && (!alphaType.HasValue || bitmap.AlphaType == alphaType.Value))
                return;

            var info = new SKImageInfo(bitmap.Width, bitmap.Height, colorType, alphaType ?? bitmap.AlphaType);
            var converted = new SKBitmap(info);

            using (var surface = SKSurface.Create(info, converted.GetPixels(), converted.RowBytes))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.White);
                canvas.DrawBitmap(bitmap, new SKRect(0, 0, bitmap.Width, bitmap.Height));
                canvas.Flush();
            }

            bitmap.Dispose();
            bitmap = converted;
        }

        private static void EnsureBitmap(SKBitmap bitmap, string paramName)
        {
            if (bitmap == null)
                throw new ArgumentNullException(paramName);
        }
    }

    /// <summary>
    /// Ordered chain of preprocessing filters for reuse across OCR and pixmap pipelines.
    /// </summary>
    /// <remarks>
    /// <para>Add filters with <c>Add*</c> helpers or <c>AddFilter</c>, then call <see cref="Apply"/>
    /// on an <see cref="SKBitmap"/>. Execution order matches insertion order.</para>
    /// </remarks>
    public sealed class ImageFilterPipeline
    {
        private readonly List<PreprocessingFilter> _filters = new List<PreprocessingFilter>();

        /// <summary>Configured filters in execution order (read-only).</summary>
        public IReadOnlyList<PreprocessingFilter> Filters => _filters.AsReadOnly();

        /// <summary>Removes all filters from the pipeline.</summary>
        public void Clear() => _filters.Clear();

        /// <summary>Removes every filter of <paramref name="type"/> (null entries excluded).</summary>
        /// <param name="type">Filter type to remove.</param>
        public void RemoveAll(ImageProcessingFilterType type)
        {
            _filters.RemoveAll(f => f != null && f.Type == type);
        }

        /// <summary>Adds a filter with optional options.</summary>
        /// <param name="type">Filter type.</param>
        /// <param name="options">Type-specific options, or null.</param>
        /// <param name="replaceExisting">When true, removes existing filters of the same type first.</param>
        public void AddFilter(ImageProcessingFilterType type, IImageProcessingFilterOptions options = null, bool replaceExisting = false)
        {
            if (replaceExisting)
                RemoveAll(type);

            _filters.Add(new PreprocessingFilter(type, options));
        }

        /// <summary>Adds a pre-built filter instance.</summary>
        /// <param name="filter">Filter to append.</param>
        /// <param name="replaceExisting">When true, removes existing filters of the same type first.</param>
        /// <exception cref="ArgumentNullException"><paramref name="filter"/> is null.</exception>
        public void AddFilter(PreprocessingFilter filter, bool replaceExisting = false)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            if (replaceExisting)
                RemoveAll(filter.Type);

            _filters.Add(filter);
        }

        /// <summary>Adds a deskew filter.</summary>
        /// <param name="minAngle">Minimum angle in degrees (non-negative).</param>
        /// <param name="replaceExisting">Default true: replace prior deskew filters.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="minAngle"/> is negative.</exception>
        public void AddDeskew(double minAngle = DeskewFilterOptions.DEFAULT_TILT_CORRECTION_ANGLE_THRESHOLD, bool replaceExisting = true)
        {
            if (minAngle < 0)
                throw new ArgumentOutOfRangeException(nameof(minAngle), "Minimum angle must be non-negative.");

            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Deskew);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.Deskew, new DeskewFilterOptions((float)minAngle)));
        }

        /// <summary>Adds a dilation filter.</summary>
        /// <param name="replaceExisting">When true, removes existing dilation filters first.</param>
        public void AddDilation(bool replaceExisting = false)
        {
            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Dilate);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.Dilate, null));
        }

        /// <summary>Adds a remove-vertical-lines filter.</summary>
        /// <param name="replaceExisting">Default true: replace prior filters of this type.</param>
        public void AddRemoveVerticalLines(bool replaceExisting = true)
        {
            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.RemoveVerticalLines);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.RemoveVerticalLines, null));
        }

        /// <summary>Adds a remove-horizontal-lines filter.</summary>
        /// <param name="replaceExisting">Default true: replace prior filters of this type.</param>
        public void AddRemoveHorizontalLines(bool replaceExisting = true)
        {
            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.RemoveHorizontalLines);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.RemoveHorizontalLines, null));
        }

        /// <summary>Legacy spelling of <see cref="AddRemoveHorizontalLines"/>.</summary>
        public void AddRemoveHoriziontalLines(bool replaceExisting = true) =>
            AddRemoveHorizontalLines(replaceExisting);

        /// <summary>Adds a median noise-reduction filter.</summary>
        /// <param name="blockSize">Kernel size (positive).</param>
        /// <param name="replaceExisting">When true, removes existing median filters first.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="blockSize"/> is not positive.</exception>
        public void AddMedian(int blockSize, bool replaceExisting = false)
        {
            if (blockSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(blockSize), "Block size must be positive.");

            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Median);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.Median, new MedianFilterOptions(blockSize)));
        }

        /// <summary>Adds a gamma correction filter.</summary>
        /// <param name="gamma">Gamma value (&gt; 0).</param>
        /// <param name="replaceExisting">Default true: replace prior gamma filters.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="gamma"/> is not positive.</exception>
        public void AddGamma(double gamma, bool replaceExisting = true)
        {
            if (gamma <= 0)
                throw new ArgumentOutOfRangeException(nameof(gamma), "Gamma must be greater than zero.");

            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Gamma);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.Gamma, new GammaFilterOptions((float)gamma)));
        }

        /// <summary>Adds a contrast adjustment filter (-100 to +100).</summary>
        /// <param name="contrast">Contrast level (0 = unchanged).</param>
        /// <param name="replaceExisting">Default true: replace prior contrast filters.</param>
        public void AddContrast(int contrast, bool replaceExisting = true)
        {
            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Contrast);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.Contrast, new ContrastFilterOptions(contrast)));
        }

        /// <summary>Adds a grayscale conversion filter.</summary>
        /// <param name="replaceExisting">Default true: replace prior grayscale filters.</param>
        public void AddGrayscale(bool replaceExisting = true)
        {
            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Grayscale);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.Grayscale, null));
        }

        /// <summary>Adds a color inversion filter.</summary>
        /// <param name="replaceExisting">Default true: replace prior invert filters.</param>
        public void AddInvert(bool replaceExisting = true)
        {
            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Invert);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.Invert, null));
        }

        /// <summary>Adds a uniform scale filter.</summary>
        /// <param name="scaleFactor">Scale factor (&gt; 0).</param>
        /// <param name="quality">Resampling quality (default High).</param>
        /// <param name="replaceExisting">When true, removes existing scale filters first.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="scaleFactor"/> is not positive.</exception>
        public void AddScale(double scaleFactor, SKFilterQuality quality = SKFilterQuality.High, bool replaceExisting = false)
        {
            if (scaleFactor <= 0)
                throw new ArgumentOutOfRangeException(nameof(scaleFactor), "Scale factor must be greater than zero.");

            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Scale);

            _filters.Add(new PreprocessingFilter(
                ImageProcessingFilterType.Scale,
                new ScaleFilterOptions((float)scaleFactor) { InterpolationMode = quality }));
        }

        /// <summary>
        /// Adds a scale-to-fit filter (fits inside a <paramref name="maxDimension"/> square, aspect preserved).
        /// </summary>
        /// <param name="maxDimension">Maximum width/height (&gt; 0).</param>
        /// <param name="quality">Resampling quality (default High).</param>
        /// <param name="replaceExisting">Default true: replace prior fit filters.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxDimension"/> is not positive.</exception>
        public void AddScaleFit(int maxDimension, SKFilterQuality quality = SKFilterQuality.High, bool replaceExisting = true)
        {
            if (maxDimension <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxDimension), "Fit dimension must be greater than zero.");

            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Fit);

            _filters.Add(new PreprocessingFilter(
                ImageProcessingFilterType.Fit,
                new FitFilterOptions(maxDimension) { InterpolationMode = quality }));
        }

        /// <summary>
        /// Runs all configured filters on <paramref name="image"/> via <see cref="ImageFilter.ApplyFilters"/>.
        /// </summary>
        /// <param name="image">Image to process; replaced in place.</param>
        public void Apply(ref SKBitmap image) => ImageFilter.ApplyFilters(ref image, _filters);
    }
}
