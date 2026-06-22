using System;
using System.Collections.Generic;
using System.Linq;
using MuPDF.NET;
using mupdf;

namespace PDF4LLM.Ocr
{
    /// <summary>OCR decision model feature extraction.</summary>
    public static class ComputeOcrFeatures
    {
        public static readonly string[] FeatureNames =
        {
            "num_spans",
            "text_area",
            "text_density",
            "avg_span_height",
            "avg_span_width",
            "num_blocks",
            "num_images",
            "image_area",
            "image_density",
            "num_small_oblique_vectors",
            "log_vector_density",
            "page_sobel_energy",
            "page_sobel_entropy",
            "page_sobel_var",
            "page_white_ratio",
            "img_fft_energy",
            "img_fft_ratio",
            "img_black_ratio",
            "img_white_ratio",
            "img_sobel_energy",
            "img_sobel_orientation_entropy",
            "img_sobel_local_variance",
        };

        private static readonly double[,] SobelX =
        {
            { 1, 0, -1 },
            { 2, 0, -2 },
            { 1, 0, -1 },
        };

        private static readonly double[,] SobelY =
        {
            { 1, 2, 1 },
            { 0, 0, 0 },
            { -1, -2, -1 },
        };

        /// <summary>Return model-relevant features for the given page.</summary>
        /// <param name="blocks">Page blocks from text extraction.</param>
        /// <param name="pageRect">Page media box used for density calculations.</param>
        /// <param name="page">Source page for pixmap and image feature extraction.</param>
        public static Dictionary<string, float> ComputeFeatures(
            List<Block> blocks,
            Rect pageRect,
            Page page)
        {
            int numBlocks = blocks?.Count ?? 0;
            float pageArea = pageRect.Width * pageRect.Height;

            var spanRects = new List<Rect>();
            if (blocks != null)
            {
                foreach (Block b in blocks)
                {
                    if (b?.Type != 0 || b.Lines == null)
                        continue;
                    foreach (Line line in b.Lines)
                    {
                        if (line?.Spans == null)
                            continue;
                        foreach (Span span in line.Spans)
                        {
                            string text = span.Text ?? "";
                            if (string.IsNullOrWhiteSpace(text))
                                continue;
                            spanRects.Add(span.Bbox);
                        }
                    }
                }
            }

            int numSpans = spanRects.Count;
            float textArea = spanRects.Sum(r => r.Width * r.Height);
            float textDensity = pageArea > 0 ? textArea / pageArea : 0f;
            float avgSpanHeight = numSpans > 0 ? spanRects.Average(r => r.Height) : 0f;
            float avgSpanWidth = numSpans > 0 ? spanRects.Average(r => r.Width) : 0f;

            var vectors = blocks?.Where(b => b?.Type == 3).ToList() ?? new List<Block>();
            float vectorArea = vectors.Sum(b => b.Bbox.Width * b.Bbox.Height);
            float logVectorDensity = pageArea > 0
                ? (float)Math.Log(1 + vectorArea / pageArea + 1e-9)
                : 0f;
            int numSmallObliqueVectors = vectors.Count(b =>
                b.Bbox.Width < 15
                && b.Bbox.Height < 15);

            SobelStats pageSobel = SobelFeaturesPage(page);

            var images = blocks?.Where(b => b?.Type == 1).ToList() ?? new List<Block>();
            int numImages = images.Count;
            float imageArea = 0f;
            var relevantImages = new List<(float score, Block img)>();

            foreach (Block img in images)
            {
                Rect visible = Intersect(pageRect, img.Bbox);
                float thisImgArea = visible.Width * visible.Height;
                imageArea += thisImgArea;
                if (pageArea <= 0 || thisImgArea <= 0.01f * pageArea)
                    continue;
                float totalImgArea = img.Bbox.Width * img.Bbox.Height;
                if (totalImgArea <= 0)
                    continue;
                float score = thisImgArea / totalImgArea * thisImgArea;
                relevantImages.Add((score, img));
            }

            float imageDensity = pageArea > 0 ? imageArea / pageArea : 0f;

            var features = new Dictionary<string, float>
            {
                ["num_spans"] = numSpans,
                ["text_area"] = textArea,
                ["text_density"] = textDensity,
                ["avg_span_height"] = avgSpanHeight,
                ["avg_span_width"] = avgSpanWidth,
                ["num_blocks"] = numBlocks,
                ["num_images"] = numImages,
                ["image_area"] = imageArea,
                ["image_density"] = imageDensity,
                ["num_small_oblique_vectors"] = numSmallObliqueVectors,
                ["log_vector_density"] = logVectorDensity,
                ["page_sobel_energy"] = pageSobel.Energy,
                ["page_sobel_entropy"] = pageSobel.Entropy,
                ["page_sobel_var"] = pageSobel.Variance,
                ["page_white_ratio"] = pageSobel.WhiteRatio,
                ["img_fft_energy"] = 0f,
                ["img_fft_ratio"] = 0f,
                ["img_black_ratio"] = 0f,
                ["img_white_ratio"] = 0f,
                ["img_sobel_energy"] = 0f,
                ["img_sobel_orientation_entropy"] = 0f,
                ["img_sobel_local_variance"] = 0f,
            };

            if (relevantImages.Count == 0 || page == null)
                return features;

            Block bestImg = relevantImages.OrderByDescending(x => x.score).First().img;
            byte[] gray = TryGetGray128(bestImg, page);
            if (gray == null)
                return features;

            FftStats fft = FftFeatures(gray);
            SobelStats imgSobel = SobelFeatures(gray, 128, 128);
            features["img_fft_energy"] = fft.Energy;
            features["img_fft_ratio"] = fft.Ratio;
            features["img_black_ratio"] = gray.Count(b => b < 128) / (float)gray.Length;
            features["img_white_ratio"] = gray.Count(b => b > 230) / (float)gray.Length;
            features["img_sobel_energy"] = imgSobel.Energy;
            features["img_sobel_orientation_entropy"] = imgSobel.Entropy;
            features["img_sobel_local_variance"] = imgSobel.Variance;
            return features;
        }

        private static Rect Intersect(Rect a, Rect b)
        {
            return new Rect(
                Math.Max(a.X0, b.X0),
                Math.Max(a.Y0, b.Y0),
                Math.Min(a.X1, b.X1),
                Math.Min(a.Y1, b.Y1));
        }

        private static SobelStats SobelFeaturesPage(Page page)
        {
            using (Pixmap pix = page.GetPixmap(dpi: 72, cs: Colorspace.Gray, alpha: false))
            using (Pixmap small = new Pixmap(pix, 128, 128))
            {
                byte[] gray = small.SAMPLES;
                if (gray == null || gray.Length < 128 * 128)
                    return new SobelStats();
                return SobelFeatures(gray, 128, 128);
            }
        }

        private static byte[] TryGetGray128(Block imageBlock, Page page)
        {
            try
            {
                if (imageBlock?.Image == null)
                    return null;
                using (var pix = new Pixmap(imageBlock.Image))
                {
                    Pixmap work = pix;
                    Pixmap noAlpha = null;
                    if (pix.Alpha != 0)
                    {
                        noAlpha = new Pixmap(pix, 0);
                        work = noAlpha;
                    }
                    try
                    {
                        using (Pixmap gray = work.N > 1
                                   ? new Pixmap(ColorSpace.csGRAY, work)
                                   : work)
                        using (var small = new Pixmap(gray, 128, 128))
                        {
                            return small.SAMPLES;
                        }
                    }
                    finally
                    {
                        noAlpha?.Dispose();
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        private readonly struct SobelStats
        {
            public SobelStats(float energy = 0, float entropy = 0, float variance = 0, float whiteRatio = 0)
            {
                Energy = energy;
                Entropy = entropy;
                Variance = variance;
                WhiteRatio = whiteRatio;
            }

            public float Energy { get; }
            public float Entropy { get; }
            public float Variance { get; }
            public float WhiteRatio { get; }
        }

        private static SobelStats SobelFeatures(byte[] gray, int width, int height)
        {
            var mag = new double[height, width];
            var ang = new double[height, width];
            ConvolveSobel(gray, width, height, mag, ang);

            double energy = 0;
            double varAcc = 0;
            int n = width * height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    energy += mag[y, x];
                    varAcc += mag[y, x] * mag[y, x];
                }
            }
            energy /= n;
            double variance = varAcc / n - energy * energy;

            var hist = new double[36];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double a = ang[y, x];
                    int bin = (int)Math.Floor((a + Math.PI) / (2 * Math.PI) * 36);
                    if (bin < 0) bin = 0;
                    if (bin > 35) bin = 35;
                    hist[bin]++;
                }
            }
            double sum = hist.Sum() + 1e-9;
            double entropy = 0;
            for (int i = 0; i < hist.Length; i++)
            {
                double p = hist[i] / sum;
                if (p > 0)
                    entropy -= p * Math.Log(p + 1e-9);
            }

            float whiteRatio = gray.Count(v => v > 230) / (float)gray.Length;
            return new SobelStats((float)energy, (float)entropy, (float)variance, whiteRatio);
        }

        private static void ConvolveSobel(
            byte[] gray,
            int width,
            int height,
            double[,] mag,
            double[,] ang)
        {
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double gx = 0;
                    double gy = 0;
                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            int sy = Reflect(y + ky, height);
                            int sx = Reflect(x + kx, width);
                            double v = gray[sy * width + sx];
                            gx += v * SobelX[ky + 1, kx + 1];
                            gy += v * SobelY[ky + 1, kx + 1];
                        }
                    }
                    mag[y, x] = Math.Sqrt(gx * gx + gy * gy);
                    ang[y, x] = Math.Atan2(gy, gx);
                }
            }
        }

        private static int Reflect(int i, int size)
        {
            if (i < 0)
                return -i;
            if (i >= size)
                return 2 * size - i - 1;
            return i;
        }

        private readonly struct FftStats
        {
            public FftStats(float energy, float ratio)
            {
                Energy = energy;
                Ratio = ratio;
            }

            public float Energy { get; }
            public float Ratio { get; }
        }

        private static FftStats FftFeatures(byte[] gray)
        {
            int n = 128;
            var re = new double[n, n];
            var im = new double[n, n];
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                    re[y, x] = gray[y * n + x];
            }
            Fft2(re, im);

            var magnitude = new double[n, n];
            double mean = 0;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    magnitude[y, x] = Math.Sqrt(re[y, x] * re[y, x] + im[y, x] * im[y, x]);
                    mean += magnitude[y, x];
                }
            }
            mean /= n * n;

            double above = 0;
            int aboveCount = 0;
            double below = 0;
            int belowCount = 0;
            for (int y = 0; y < n; y++)
            {
                for (int x = 0; x < n; x++)
                {
                    if (magnitude[y, x] > mean)
                    {
                        above += magnitude[y, x];
                        aboveCount++;
                    }
                    else
                    {
                        below += magnitude[y, x];
                        belowCount++;
                    }
                }
            }
            float ratio = (float)((above / Math.Max(aboveCount, 1)) / (below / Math.Max(belowCount, 1) + 1e-6));
            return new FftStats((float)mean, ratio);
        }

        private static void Fft2(double[,] re, double[,] im)
        {
            int rows = re.GetLength(0);
            int cols = re.GetLength(1);
            for (int i = 0; i < rows; i++)
                Fft1D(re, im, i, cols, row: true);
            for (int j = 0; j < cols; j++)
                Fft1D(re, im, j, rows, row: false);
        }

        private static void Fft1D(double[,] re, double[,] im, int fixedIndex, int len, bool row)
        {
            if (len <= 1)
                return;

            var evenRe = new double[len / 2];
            var evenIm = new double[len / 2];
            var oddRe = new double[len / 2];
            var oddIm = new double[len / 2];

            for (int i = 0; i < len / 2; i++)
            {
                int idxEven = 2 * i;
                int idxOdd = 2 * i + 1;
                if (row)
                {
                    evenRe[i] = re[fixedIndex, idxEven];
                    evenIm[i] = im[fixedIndex, idxEven];
                    oddRe[i] = re[fixedIndex, idxOdd];
                    oddIm[i] = im[fixedIndex, idxOdd];
                }
                else
                {
                    evenRe[i] = re[idxEven, fixedIndex];
                    evenIm[i] = im[idxEven, fixedIndex];
                    oddRe[i] = re[idxOdd, fixedIndex];
                    oddIm[i] = im[idxOdd, fixedIndex];
                }
            }

            Fft1DRecursive(evenRe, evenIm);
            Fft1DRecursive(oddRe, oddIm);

            for (int k = 0; k < len / 2; k++)
            {
                double angle = -2 * Math.PI * k / len;
                double wr = Math.Cos(angle);
                double wi = Math.Sin(angle);
                double tr = wr * oddRe[k] - wi * oddIm[k];
                double ti = wr * oddIm[k] + wi * oddRe[k];
                double outReEven = evenRe[k] + tr;
                double outImEven = evenIm[k] + ti;
                double outReOdd = evenRe[k] - tr;
                double outImOdd = evenIm[k] - ti;
                if (row)
                {
                    re[fixedIndex, k] = outReEven;
                    im[fixedIndex, k] = outImEven;
                    re[fixedIndex, k + len / 2] = outReOdd;
                    im[fixedIndex, k + len / 2] = outImOdd;
                }
                else
                {
                    re[k, fixedIndex] = outReEven;
                    im[k, fixedIndex] = outImEven;
                    re[k + len / 2, fixedIndex] = outReOdd;
                    im[k + len / 2, fixedIndex] = outImOdd;
                }
            }
        }

        private static void Fft1DRecursive(double[] re, double[] im)
        {
            int n = re.Length;
            if (n <= 1)
                return;

            var evenRe = new double[n / 2];
            var evenIm = new double[n / 2];
            var oddRe = new double[n / 2];
            var oddIm = new double[n / 2];
            for (int i = 0; i < n / 2; i++)
            {
                evenRe[i] = re[2 * i];
                evenIm[i] = im[2 * i];
                oddRe[i] = re[2 * i + 1];
                oddIm[i] = im[2 * i + 1];
            }
            Fft1DRecursive(evenRe, evenIm);
            Fft1DRecursive(oddRe, oddIm);
            for (int k = 0; k < n / 2; k++)
            {
                double angle = -2 * Math.PI * k / n;
                double wr = Math.Cos(angle);
                double wi = Math.Sin(angle);
                double tr = wr * oddRe[k] - wi * oddIm[k];
                double ti = wr * oddIm[k] + wi * oddRe[k];
                re[k] = evenRe[k] + tr;
                im[k] = evenIm[k] + ti;
                re[k + n / 2] = evenRe[k] - tr;
                im[k + n / 2] = evenIm[k] - ti;
            }
        }
    }
}
