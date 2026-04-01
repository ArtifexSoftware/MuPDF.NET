namespace Demo
{
    internal partial class Program
    {
        internal static void TestImageFilter()
        {
            const string inputPath = @"../../../TestDocuments/Image/table.jpg";
            const string outputPath = @"output.png";

            // Load the image file into SKBitmap
            using (var bitmap = SKBitmap.Decode(inputPath))
            {
                if (bitmap == null)
                {
                    Console.WriteLine("Failed to load image.");
                    return;
                }

                SKBitmap inputBitmap = bitmap.Copy();

                // build the pipeline
                var pipeline = new ImageFilterPipeline();

                // clear any defaults if youΓÇÖre reusing the instance
                pipeline.Clear();

                // add filters one-by-one
                pipeline.AddDeskew(minAngle: 0.5);              // replaces any existing deskew step
                pipeline.AddRemoveHorizontalLines();            // also replaces existing horizontal-removal step
                pipeline.AddRemoveVerticalLines();
                pipeline.AddGrayscale();
                //pipeline.AddMedian(blockSize: 2, replaceExisting: true);
                //pipeline.AddGamma(gamma: 1.2);                  // brighten slightly
                //pipeline.AddContrast(contrast: 100);
                //pipeline.AddFit(100);
                //pipeline.AddDilation();
                //pipeline.AddScale(scaleFactor: 1.75, quality: SKFilterQuality.Medium);
                pipeline.AddInvert();

                // apply the pipeline (bitmap is modified in place)
                pipeline.Apply(ref inputBitmap);

                using (var data = inputBitmap.Encode(SKEncodedImageFormat.Png, 100)) // 100 = quality
                {
                    using (var stream = File.OpenWrite(outputPath))
                    {
                        data.SaveTo(stream);
                    }
                }

                Console.WriteLine($"Loaded image: {bitmap.Width}x{bitmap.Height} pixels");
            }
        }

        internal static void TestImageFilterOcr()
        {
            const string inputPath = @"../../../TestDocuments/Image/boxedpage.jpg";

            using (Pixmap pxmp = new Pixmap(inputPath))
            {
                // build the pipeline
                var pipeline = new ImageFilterPipeline();

                // clear any defaults if youΓÇÖre reusing the instance
                pipeline.Clear();

                // add filters one-by-one
                //pipeline.AddDeskew(minAngle: 0.5);              // replaces any existing deskew step
                //pipeline.AddRemoveHorizontalLines();            // also replaces existing horizontal-removal step
                //pipeline.AddRemoveVerticalLines();
                //pipeline.AddGrayscale();
                //pipeline.AddMedian(blockSize: 2, replaceExisting: true);
                pipeline.AddGamma(gamma: 1.2);                  // brighten slightly
                //pipeline.AddContrast(contrast: 100);
                //pipeline.AddScaleFit(100);
                //pipeline.AddDilation();
                pipeline.AddScale(scaleFactor: 1.75, quality: SKFilterQuality.High);
                //pipeline.AddInvert();

                string txt = pxmp.GetTextFromOcr(pipeline);
                Console.WriteLine(txt);
            }
        }

    }
}
