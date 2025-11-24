using System;
using System.Collections.Generic;
using SkiaSharp;

namespace MuPDF.NET
{
    /// <summary>
    /// Provides static methods for applying various image processing filters to SKBitmap images.
    /// All filters modify the input image in-place and dispose the original bitmap.
    /// </summary>
    public static class ImageFilter
    {
        static ImageFilter()
        {
            Utils.InitApp();
        }

        /// <summary>
        /// Automatically detects and corrects image skew (rotation) using projection-based analysis.
        /// </summary>
        /// <param name="image">The image to deskew. Will be replaced with the corrected image.</param>
        /// <param name="minAngle">Minimum angle in degrees required to apply correction. Default is 0.4 degrees.</param>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        public static void AutoDeskew(ref SKBitmap image, double minAngle = DeskewFilterOptions.DEFAULT_TILT_CORRECTION_ANGLE_THRESHOLD)
        {
            EnsureBitmap(image, nameof(image));
            Deskew.Process(ref image, minAngle);
        }

        /// <summary>
        /// Applies dilation filter to correct broken letters by expanding dark pixels.
        /// Useful for repairing fragmented text in scanned documents.
        /// </summary>
        /// <param name="image">The image to process. Will be replaced with the processed image.</param>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        public static void ApplyDilation(ref SKBitmap image)
        {
            EnsureBitmap(image, nameof(image));
            Dilate.Process(ref image);
        }

        /// <summary>
        /// Removes lines from the image (either horizontal or vertical).
        /// Uses advanced algorithms to preserve text while removing table borders and separator lines.
        /// </summary>
        /// <param name="image">The image to process. Will be replaced with the processed image.</param>
        /// <param name="vertical">True to remove vertical lines, false to remove horizontal lines.</param>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        public static void RemoveLines(ref SKBitmap image, bool vertical)
        {
            EnsureBitmap(image, nameof(image));
            LineRemover.Process(ref image, vertical);
        }

        /// <summary>
        /// Removes vertical lines from the image (table borders, separators, etc.).
        /// </summary>
        /// <param name="image">The image to process. Will be replaced with the processed image.</param>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        public static void RemoveVerticalLines(ref SKBitmap image) => RemoveLines(ref image, true);

        /// <summary>
        /// Removes horizontal lines from the image (table borders, separators, etc.).
        /// </summary>
        /// <param name="image">The image to process. Will be replaced with the processed image.</param>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        public static void RemoveHorizontalLines(ref SKBitmap image) => RemoveLines(ref image, false);

        /// <summary>
        /// Detects and collects line segments from the image without removing them.
        /// Returns both horizontal and vertical line segments found in the image.
        /// </summary>
        /// <param name="image">The image to analyze.</param>
        /// <returns>A tuple containing lists of horizontal and vertical line segments.</returns>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        public static (List<LineRemover.Segment> Horizontal, List<LineRemover.Segment> Vertical) CollectLines(ref SKBitmap image)
        {
            EnsureBitmap(image, nameof(image));
            var horizontal = new List<LineRemover.Segment>();
            var vertical = new List<LineRemover.Segment>();
            LineRemover.CollectLines(ref image, ref horizontal, ref vertical);
            return (horizontal, vertical);
        }

        /// <summary>
        /// Detects and collects line segments from the image into provided lists.
        /// </summary>
        /// <param name="image">The image to analyze.</param>
        /// <param name="horizontal">List to populate with horizontal line segments. Will be created if null.</param>
        /// <param name="vertical">List to populate with vertical line segments. Will be created if null.</param>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        public static void CollectLines(ref SKBitmap image, ref List<LineRemover.Segment> horizontal, ref List<LineRemover.Segment> vertical)
        {
            EnsureBitmap(image, nameof(image));
            if (horizontal == null)
                horizontal = new List<LineRemover.Segment>();
            if (vertical == null)
                vertical = new List<LineRemover.Segment>();
            LineRemover.CollectLines(ref image, ref horizontal, ref vertical);
        }

        /// <summary>
        /// Applies median filter to reduce noise in the image.
        /// Each pixel is replaced with the median value of its neighboring pixels.
        /// </summary>
        /// <param name="image">The image to process. Will be replaced with the processed image.</param>
        /// <param name="kernelSize">Size of the median filter kernel (must be odd). Default is 3.</param>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when kernelSize is not positive.</exception>
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
        /// Adjusts the gamma correction of the image.
        /// Gamma values less than 1.0 darken the image, values greater than 1.0 brighten it.
        /// </summary>
        /// <param name="image">The image to process. Will be replaced with the processed image.</param>
        /// <param name="gamma">Gamma correction value. Must be greater than zero. 1.0 means no change.</param>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when gamma is not greater than zero.</exception>
        public static void AdjustGamma(ref SKBitmap image, double gamma)
        {
            EnsureBitmap(image, nameof(image));
            if (gamma <= 0)
                throw new ArgumentOutOfRangeException(nameof(gamma), "Gamma must be greater than zero.");

            Gamma.Process(ref image, gamma);
        }

        /// <summary>
        /// Adjusts the contrast of the image.
        /// Requires RGB color format. The image will be converted if necessary.
        /// </summary>
        /// <param name="image">The image to process. Will be replaced with the processed image.</param>
        /// <param name="contrastLevel">Contrast adjustment from -100 to +100. 0 means no change.</param>
        /// <returns>True if the operation succeeded, false if the image format is not supported.</returns>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        public static bool AdjustContrast(ref SKBitmap image, int contrastLevel)
        {
            EnsureBitmap(image, nameof(image));
            EnsureColorType(ref image, SKColorType.Rgb888x, SKAlphaType.Opaque);
            return Contrast.Process(ref image, contrastLevel);
        }

        /// <summary>
        /// Applies blur filter to the image using a box blur algorithm.
        /// Requires RGB color format. The image will be converted if necessary.
        /// </summary>
        /// <param name="image">The image to process. Will be replaced with the processed image.</param>
        /// <param name="blurZoneSize">Size of the blur zone (1-10). Larger values create more blur.</param>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when blurZoneSize is less than 1.</exception>
        public static void ApplyBlur(ref SKBitmap image, int blurZoneSize)
        {
            EnsureBitmap(image, nameof(image));
            if (blurZoneSize < 1)
                throw new ArgumentOutOfRangeException(nameof(blurZoneSize), "Blur zone size must be at least 1.");

            EnsureColorType(ref image, SKColorType.Rgb888x, SKAlphaType.Opaque);
            Blur.Process(ref image, blurZoneSize);
        }

        /// <summary>
        /// Converts the image to grayscale using standard RGB weights (0.299R + 0.587G + 0.114B).
        /// </summary>
        /// <param name="image">The image to process. Will be replaced with the grayscale image.</param>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        public static void ToGrayscale(ref SKBitmap image)
        {
            EnsureBitmap(image, nameof(image));
            Grayscale.Process(ref image);
        }

        /// <summary>
        /// Inverts the colors of the image (creates a negative effect).
        /// </summary>
        /// <param name="image">The image to process. Will be replaced with the inverted image.</param>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        public static void ApplyInvert(ref SKBitmap image)
        {
            EnsureBitmap(image, nameof(image));
            Invert.Process(ref image);
        }

        /// <summary>
        /// Scales the image by the specified factor while maintaining aspect ratio.
        /// </summary>
        /// <param name="image">The image to scale. Will be replaced with the scaled image.</param>
        /// <param name="scaleFactor">Scale factor (e.g., 0.5 for half size, 2.0 for double size). Must be greater than zero.</param>
        /// <param name="quality">Filter quality for scaling interpolation. Default is High.</param>
        /// <returns>The new size of the scaled image.</returns>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when scaleFactor is not greater than zero.</exception>
        public static SKSizeI ScaleImage(ref SKBitmap image, double scaleFactor, SKFilterQuality quality = SKFilterQuality.High)
        {
            EnsureBitmap(image, nameof(image));
            if (scaleFactor <= 0)
                throw new ArgumentOutOfRangeException(nameof(scaleFactor), "Scale factor must be greater than zero.");

            if (Math.Abs(scaleFactor - 1d) < double.Epsilon)
                return new SKSizeI(image.Width, image.Height);

            return Scale.Process(ref image, scaleFactor, quality);
        }

        /// <summary>
        /// Scales the image to fit within the specified maximum dimension while maintaining aspect ratio.
        /// If the image is already smaller than maxDimension, no scaling is performed.
        /// </summary>
        /// <param name="image">The image to scale. Will be replaced with the scaled image.</param>
        /// <param name="maxDimension">Maximum width or height in pixels. Must be greater than zero.</param>
        /// <param name="quality">Filter quality for scaling interpolation. Default is High.</param>
        /// <returns>The new size of the scaled image.</returns>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when maxDimension is not greater than zero.</exception>
        public static SKSizeI Fit(ref SKBitmap image, int maxDimension, SKFilterQuality quality = SKFilterQuality.High)
        {
            EnsureBitmap(image, nameof(image));
            if (maxDimension <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxDimension), "Fit dimension must be greater than zero.");

            if (image.Width == 0 || image.Height == 0)
                return new SKSizeI(image.Width, image.Height);

            double scale = Math.Min((double)maxDimension / image.Width, (double)maxDimension / image.Height);
            if (scale <= 0)
                scale = 1;

            if (Math.Abs(scale - 1d) < double.Epsilon)
                return new SKSizeI(image.Width, image.Height);

            return ScaleImage(ref image, scale, quality);
        }

        /// <summary>
        /// Applies a sequence of image processing filters to the image in the order specified.
        /// Each filter is applied sequentially, modifying the image in-place.
        /// </summary>
        /// <param name="image">The image to process. Will be replaced with the processed image.</param>
        /// <param name="filters">Collection of preprocessing filters to apply. Null or empty collection is ignored.</param>
        /// <exception cref="ArgumentNullException">Thrown when image is null.</exception>
        /// <exception cref="NotSupportedException">Thrown when an unsupported filter type is encountered.</exception>
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
                            var options = filter.Options as GammaFilterOptions ?? new GammaFilterOptions(1d);
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
                            {
                                ScaleImage(ref image, options.ScaleFactor, options.InterpolationMode);
                            }
                            break;
                        }

                    case ImageProcessingFilterType.Fit:
                        {
                            if (filter.Options is FitFilterOptions options && options.FitToSize > 0)
                            {
                                Fit(ref image, options.FitToSize, options.InterpolationMode);
                            }
                            break;
                        }

                    default:
                        throw new NotSupportedException($"Filter '{filter.Type}' is not supported.");
                }
            }
        }

        /// <summary>
        /// Validates that the bitmap parameter is not null.
        /// </summary>
        /// <param name="bitmap">The bitmap to validate.</param>
        /// <param name="paramName">The name of the parameter for error reporting.</param>
        /// <exception cref="ArgumentNullException">Thrown when bitmap is null.</exception>
        private static void EnsureBitmap(SKBitmap bitmap, string paramName)
        {
            if (bitmap == null)
                throw new ArgumentNullException(paramName);
        }

        /// <summary>
        /// Ensures the bitmap has the specified color type and alpha type.
        /// Converts the bitmap if necessary, disposing the original and replacing it with the converted version.
        /// </summary>
        /// <param name="bitmap">The bitmap to check and convert if needed. Will be replaced if conversion is necessary.</param>
        /// <param name="colorType">The required color type.</param>
        /// <param name="alphaType">The required alpha type. If null, the original alpha type is preserved.</param>
        public static void EnsureColorType(ref SKBitmap bitmap, SKColorType colorType, SKAlphaType? alphaType = null)
        {
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
    }

    /// <summary>
    /// Helper that lets callers compose and reuse preprocessing filter pipelines.
    /// </summary>
    public sealed class ImageFilterPipeline
    {
        private readonly List<PreprocessingFilter> _filters = new List<PreprocessingFilter>();

        /// <summary>
        /// Gets a read-only view of the configured filters, in execution order.
        /// </summary>
        public IReadOnlyList<PreprocessingFilter> Filters => _filters.AsReadOnly();

        /// <summary>
        /// Removes every filter from the pipeline.
        /// </summary>
        public void Clear() => _filters.Clear();

        /// <summary>
        /// Removes all filters matching the specified type.
        /// </summary>
        public void RemoveAll(ImageProcessingFilterType type)
        {
            _filters.RemoveAll(f => f != null && f.Type == type);
        }

        /// <summary>
        /// Adds a filter to the pipeline.
        /// </summary>
        public void AddFilter(ImageProcessingFilterType type, IImageProcessingFilterOptions options = null, bool replaceExisting = false)
        {
            if (replaceExisting)
                RemoveAll(type);

            _filters.Add(new PreprocessingFilter(type, options));
        }

        /// <summary>
        /// Adds an already constructed <see cref="PreprocessingFilter"/>.
        /// </summary>
        public void AddFilter(PreprocessingFilter filter, bool replaceExisting = false)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            if (replaceExisting)
                RemoveAll(filter.Type);

            _filters.Add(filter);
        }

        public void AddDeskew(double minAngle = DeskewFilterOptions.DEFAULT_TILT_CORRECTION_ANGLE_THRESHOLD, bool replaceExisting = true)
        {
            if (minAngle < 0)
                throw new ArgumentOutOfRangeException(nameof(minAngle), "Minimum angle must be non-negative.");

            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Deskew);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.Deskew, new DeskewFilterOptions(minAngle)));
        }

        public void AddDilation(bool replaceExisting = false)
        {
            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Dilate);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.Dilate, null));
        }

        public void AddRemoveVerticalLines(bool replaceExisting = true)
        {
            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.RemoveVerticalLines);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.RemoveVerticalLines, null));
        }

        public void AddRemoveHorizontalLines(bool replaceExisting = true)
        {
            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.RemoveHorizontalLines);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.RemoveHorizontalLines, null));
        }

        public void AddMedian(int blockSize, bool replaceExisting = false)
        {
            if (blockSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(blockSize), "Block size must be positive.");

            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Median);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.Median, new MedianFilterOptions(blockSize)));
        }

        public void AddGamma(double gamma, bool replaceExisting = true)
        {
            if (gamma <= 0)
                throw new ArgumentOutOfRangeException(nameof(gamma), "Gamma must be greater than zero.");

            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Gamma);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.Gamma, new GammaFilterOptions(gamma)));
        }

        public void AddContrast(int contrast, bool replaceExisting = true)
        {
            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Contrast);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.Contrast, new ContrastFilterOptions(contrast)));
        }

        public void AddGrayscale(bool replaceExisting = true)
        {
            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Grayscale);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.Grayscale, null));
        }

        public void AddInvert(bool replaceExisting = true)
        {
            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Invert);

            _filters.Add(new PreprocessingFilter(ImageProcessingFilterType.Invert, null));
        }

        public void AddScale(double scaleFactor, SKFilterQuality quality = SKFilterQuality.High, bool replaceExisting = false)
        {
            if (scaleFactor <= 0)
                throw new ArgumentOutOfRangeException(nameof(scaleFactor), "Scale factor must be greater than zero.");

            if (replaceExisting)
                RemoveAll(ImageProcessingFilterType.Scale);

            _filters.Add(new PreprocessingFilter(
                ImageProcessingFilterType.Scale,
                new ScaleFilterOptions(scaleFactor) { InterpolationMode = quality }));
        }

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
        /// Applies the configured filters to the supplied image.
        /// </summary>
        public void Apply(ref SKBitmap image)
        {
            ImageFilter.ApplyFilters(ref image, _filters);
        }
    }
}
