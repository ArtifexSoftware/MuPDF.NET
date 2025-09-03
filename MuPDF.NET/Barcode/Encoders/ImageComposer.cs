using BarcodeWriter.Core.Internal;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Web;

namespace BarcodeWriter.Core
{
    /// <summary>
    /// Represents class that creates a single composite image from several images by placing them
    /// in a fixed position or arranging automatically.
    /// </summary>
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class ImageComposer : IDisposable
    {
        private class ImageData
        {
            public SKImage Image { get; }
            public PointF Position { get; }

            public ImageData(SKImage image, PointF position)
            {
                Image = image;
                Position = position;
            }
        }

        // Full list of input images
        private readonly List<ImageData> _imageDataList = new List<ImageData>();

        // Composition mode
        public CompositionMode CompositionMode { get; set; }

        /// <summary>
        /// Gets or sets the gap between images for automatic composition mode.
        /// </summary>
        public int InnerGap { get; set; }

        /// <summary>
        /// Gets or sets margins for the composite image.
        /// </summary>
        public int Margins { get; set; }

        /// <summary>
        /// Gets or sets the background color of composite image.
        /// </summary>
        [ComVisible(false)]
        public SKColor BackgroundColor { get; set; } = SKColors.White;

        // Result of composition
        private SKImage _outputImage = null;

        /// <summary>
        /// Constructs ImageComposer object.
        /// </summary>
        /// <param name="innerGap">Gap between images in automatic composition mode. Default is 0.</param>
        /// <param name="margins">Margins for output image. Default is 0. </param>
        /// <param name="compositionMode">Composition mode. Default is 'CompositionMode.ArrangeHorizontally'.</param>
        public ImageComposer(int innerGap = 0, int margins = 0, CompositionMode compositionMode = CompositionMode.ArrangeHorizontally)
        {
            InnerGap = innerGap;
            Margins = margins;
            CompositionMode = compositionMode;
        }

        /// <summary>
        /// <inheritdoc />
        /// </summary>
        public void Dispose()
        {
            _outputImage?.Dispose();
            _imageDataList.Clear();
        }

        /// <summary>
        /// Adds an image to the composition.
        /// </summary>
        /// <param name="fileName">Image file path.</param>
        /// <param name="positionX">Horizontal (left) position for fixed positioning mode (see <see cref="CompositionMode"/>). Default is 0.</param>
        /// <param name="positionY">Vertical (top) position for fixed positioning mode (see <see cref="CompositionMode"/>). Default is 0.</param>
        /// <param name="rotationAngle">Rotation angle in degrees. Default is 0.</param>
        public void AddImage(string fileName, int positionX = 0, int positionY = 0, int rotationAngle = 0)
        {
            using (SKData data = SKData.Create(fileName))
            {
                var image = Rotate(SKImage.FromEncodedData(data), rotationAngle);
                _imageDataList.Add(new ImageData(image, new PointF(positionX, positionY)));
            }
        }

        /// <summary>
        /// Adds an image to the composition.
        /// </summary>
        /// <param name="stream">Stream that contains an image file.</param>
        /// <param name="positionX">Horizontal (left) position for fixed positioning mode (see <see cref="CompositionMode"/>). Default is 0.</param>
        /// <param name="positionY">Vertical (top) position for fixed positioning mode (see <see cref="CompositionMode"/>). Default is 0.</param>
        /// <param name="rotationAngle">Rotation angle in degrees. Default is 0.</param>
        [ComVisible(false)]
        public void AddImage(Stream stream, int positionX = 0, int positionY = 0, int rotationAngle = 0)
        {
            using (SKData data = SKData.Create(stream))
            {
                var image = Rotate(SKImage.FromEncodedData(data), rotationAngle);
                _imageDataList.Add(new ImageData(image, new PointF(positionX, positionY)));
            }
        }

        /// <summary>
        /// Adds an image to the composition.
        /// </summary>
        /// <param name="image"><see cref="Image"/> object to add.</param>
        /// <param name="positionX">Horizontal (left) position for fixed positioning mode (see <see cref="CompositionMode"/>). Default is 0.</param>
        /// <param name="positionY">Vertical (top) position for fixed positioning mode (see <see cref="CompositionMode"/>). Default is 0.</param>
        /// <param name="rotationAngle">Rotation angle in degrees. Default is 0.</param>
        [ComVisible(false)]
        public void AddImage(SKImage image, int positionX = 0, int positionY = 0, int rotationAngle = 0)
        {
            var newImage = Rotate(image, rotationAngle);
            _imageDataList.Add(new ImageData(newImage, new PointF(positionX, positionY)));
        }

        /// <summary>
        /// Sets the background color of composite image.
        /// </summary>
        /// <param name="red">Red component of the color. From 0 to 255.</param>
        /// <param name="green">Green component of the color. From 0 to 255.</param>
        /// <param name="blue">Blue component of the color. From 0 to 255.</param>
        /// <param name="alpha">Alpha (transparency) component of the color. From 0 to 255.</param>
        public void SetBackgroundColor(int red, int green, int blue, int alpha = 255)
        {
            BackgroundColor = new SKColor((byte)red, (byte)green, (byte)blue, (byte)alpha);
        }

        /// <summary>
        /// Returns composed image.
        /// </summary>
        /// <returns><see cref="Image"/> object.</returns>
        [ComVisible(false)]
        public SKImage GetComposedImage()
        {
            Run();
            return _outputImage;
        }

        /// <summary>
        /// Saves composed image to a file.
        /// </summary>
        /// <param name="fileName">Output image file path.</param>
        public void SaveComposedImage(string fileName)
        {
            Run();

            using (var stream = File.OpenWrite(fileName))
            {
                SKEncodedImageFormat format = Utils.FormatFromName(fileName);
                using (var data = _outputImage.Encode(format, 100))
                {
                    data.SaveTo(stream);
                }
            }                
        }

        /// <summary>
        /// Saves composed image to a file.
        /// </summary>
        /// <param name="outputStream">Output stream.</param>
        [ComVisible(false)]
        public void SaveComposedImage(Stream outputStream)
        {
            SKEncodedImageFormat format = SKEncodedImageFormat.Png;
            using (var data = _outputImage.Encode(format, 100))
            {
                data.SaveTo(outputStream);
            }
        }

        /// <summary>
        /// Saves composed image to a file.
        /// </summary>
        /// <param name="fileName">Output image file path.</param>
        /// <param name="imageFormat"><see cref="ImageFormat"/> to save.</param>
        [ComVisible(false)]
        public void SaveComposedImage(string fileName, SKEncodedImageFormat imageFormat)
        {
            using (var stream = File.OpenWrite(fileName))
            {
                SKEncodedImageFormat format = SKEncodedImageFormat.Png;
                using (var data = _outputImage.Encode(format, 100))
                {
                    data.SaveTo(stream);
                }
            }                
        }

        /// <summary>
        /// Saves composed image to a file.
        /// </summary>
        /// <param name="outputStream">Output stream.</param>
        /// <param name="imageFormat"><see cref="ImageFormat"/> to save.</param>
        [ComVisible(false)]
        public void SaveComposedImage(Stream outputStream, SKEncodedImageFormat imageFormat)
        {
            // Encode to PNG
            using (var data = _outputImage.Encode(SKEncodedImageFormat.Png, 100))
                { data.SaveTo(outputStream); }
        }

        // Runs the composition process.

        private void Run()
        {
            int width = OutputSizeX();
            int height = OutputSizeY();

            // Create a surface for drawing
            using (var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul)))
            {
                var canvas = surface.Canvas;

                // Clear background
                SKColor skBackground = BackgroundColor;
                canvas.Clear(skBackground);

                // Draw content
                if (CompositionMode == CompositionMode.FixedPosition)
                    DrawFixed(canvas);
                else
                    DrawAuto(canvas);

                // Flush drawing
                canvas.Flush();

                // Create SKImage from the surface
                using (var skImage = surface.Snapshot())
                {
                    // Optionally, encode to PNG or keep as SKImage
                    _outputImage = skImage; // store SKImage
                }
            }
        }

        // Draws images using positions set by AddImage() methods on the provided Graphics object

        private void DrawFixed(SKCanvas canvas)
        {
            foreach (var imageData in _imageDataList)
            {
                using (var skBitmap = SKBitmap.FromImage(imageData.Image))
                {
                    canvas.DrawBitmap(skBitmap, new SKPoint(imageData.Position.X, imageData.Position.Y));
                }
            }
        }

        // Draws images using automatically generated positions on the provided Graphics object

        private void DrawAuto(SKCanvas canvas)
        {
            PointF position = new PointF(Margins, Margins);
            foreach (var imageData in _imageDataList)
            {
                using (var skBitmap = SKBitmap.FromImage(imageData.Image))
                {
                    canvas.DrawBitmap(skBitmap, new SKPoint(imageData.Position.X, imageData.Position.Y));
                    UpdatePositionAuto(ref position, imageData.Image);
                }
            }
        }

        // Updates position according to CompositionMode mode.

        private void UpdatePositionAuto(ref PointF position, SKImage image)
        {
            switch (CompositionMode)
            {
                case CompositionMode.ArrangeHorizontally:
                    position.X += image.Width + InnerGap;
                    break;
                case CompositionMode.ArrangeVertically:
                    position.Y += image.Height + InnerGap;
                    break;
                case CompositionMode.FixedPosition:
                    // no change for parameter as this method used for auto composition only
                    break;
                default:
                    throw new NotImplementedException("Can not handle this CompositionMode");
            }
        }

        // Calculates width of the output image.

        private int OutputSizeX()
        {
            switch (CompositionMode)
            {
                case CompositionMode.ArrangeHorizontally:
                    return HorizontalSizesSum() + 2 * Margins + (_imageDataList.Count - 1) * InnerGap;
                case CompositionMode.ArrangeVertically:
                    return MaxSizeX() + 2 * Margins;
                case CompositionMode.FixedPosition:
                    return (int) Math.Ceiling(MaxRightBottomCorner().X) + 2 * Margins;
                default:
                    throw new NotImplementedException("Can not handle this CompositionMode");
            }
        }

        // Calculates height of the output image.

        private int OutputSizeY()
        {
            switch (CompositionMode)
            {
                case CompositionMode.ArrangeHorizontally:
                    return MaxSizeY() + 2 * Margins;
                case CompositionMode.ArrangeVertically:
                    return VerticalSizesSum() + 2 * Margins + (_imageDataList.Count - 1) * InnerGap;
                case CompositionMode.FixedPosition:
                    return (int) Math.Ceiling(MaxRightBottomCorner().Y) + 2 * Margins;
                default:
                    throw new NotImplementedException("Can not handle this Alignment");
            }
        }

        // Returns the total width of all images.

        private int MaxSizeX()
        {
            var max = 0;

            foreach (var imageData in _imageDataList)
                if (imageData.Image.Width > max)
                    max = imageData.Image.Width;

            return max;
        }

        // Returns the total height of all images.

        private int MaxSizeY()
        {
            var max = 0;

            foreach (var imageData in _imageDataList)
                if (imageData.Image.Height > max)
                    max = imageData.Image.Height;

            return max;
        }

        // Returns max bottom-right point for the output image.

        private PointF MaxRightBottomCorner()
        {
            /**  []
             *              []
             * 
             * 
             *        []     X - this is the result
             */
            var max = new PointF(0, 0);

            foreach (var imageData in _imageDataList)
            {
                var imageRightBottomCornerX = imageData.Position.X + imageData.Image.Width;
                var imageRightBottomCornerY = imageData.Position.Y + imageData.Image.Height;

                if (imageRightBottomCornerX > max.X)
                    max.X = imageRightBottomCornerX;

                if (imageRightBottomCornerY > max.Y)
                    max.Y = imageRightBottomCornerY;
            }

            return max;
        }

        // Returns width of all images arranged horizontally.

        private int HorizontalSizesSum()
        {
            var sizesSum = 0;

            foreach (var imageData in _imageDataList)
                sizesSum += imageData.Image.Width;

            return sizesSum;
        }

        // Returns height of all images arranged vertically.

        private int VerticalSizesSum()
        {
            var sizesSum = 0;

            foreach (var imageData in _imageDataList)
                sizesSum += imageData.Image.Height;

            return sizesSum;
        }

        /// <summary>
        /// Rotates an input image for an angle and produces a resulting image with larger size
        /// </summary>
        /// <param name="image">Input image</param>
        /// <param name="angle">Angle for rotation (degrees)</param>
        /// <returns>New resulting image</returns>
        private SKImage Rotate(SKImage image, float angle)
        {
            // Calculate the size of the rotated image
            var rotatedSize = NewImageRotatedSize(image, angle); // Should return Size or width/height

            var info = new SKImageInfo(rotatedSize.Width, rotatedSize.Height, SKColorType.Rgba8888, SKAlphaType.Premul);

            using (var surface = SKSurface.Create(info))
            {
                var canvas = surface.Canvas;
                canvas.Clear(SKColors.Transparent);

                // Move rotation point to the center of the new image
                canvas.Translate(rotatedSize.Width / 2f, rotatedSize.Height / 2f);

                // Rotate canvas
                canvas.RotateDegrees(angle);

                // Draw the original image centered
                canvas.Translate(-image.Width / 2f, -image.Height / 2f);
                canvas.DrawImage(image, 0, 0);

                // Return the SKImage from surface
                canvas.Flush();
                return surface.Snapshot();
            }
        }

        // Returns new empty bitmap with the size that is large enough to contain an input image rotated for the specified angle.

        private SKImage NewImageRotatedSize(SKImage image, float angle)
        {
            // Convert angle to radians
            double angleRad = angle * Math.PI / 180;

            int width = image.Width;
            int height = image.Height;

            // Calculate new rotated dimensions
            int newWidth = (int)Math.Ceiling(Math.Abs(height * Math.Sin(angleRad)) + Math.Abs(width * Math.Cos(angleRad)));
            int newHeight = (int)Math.Ceiling(Math.Abs(height * Math.Cos(angleRad)) + Math.Abs(width * Math.Sin(angleRad)));

            // Create a new empty SKBitmap with the rotated size
            SKImage rotatedBitmap = SKImage.FromBitmap(new SKBitmap(newWidth, newHeight, image.ColorType, image.AlphaType));

            return rotatedBitmap;
        }
    }

    /// <summary>
    /// Image composition modes for <see cref="ImageComposer"/>.
    /// </summary>
    public enum CompositionMode
    {
        /// <summary>
        /// Automatically
        /// </summary>
        ArrangeHorizontally,

        /// <summary>
        /// 
        /// </summary>
        ArrangeVertically,

        /// <summary>
        /// 
        /// </summary>
        FixedPosition
    }
}