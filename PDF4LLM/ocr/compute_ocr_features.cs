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
            // Text
            "num_spans",
            "text_area",
            "text_density",
            "avg_span_height",
            "avg_span_width",
            // Layout
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
            // Pixel features of the most relevant image
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

        private static readonly int BlockText = mupdf.mupdf.FZ_STEXT_BLOCK_TEXT;
        private static readonly int BlockImage = mupdf.mupdf.FZ_STEXT_BLOCK_IMAGE;
        private static readonly int BlockVector = mupdf.mupdf.FZ_STEXT_BLOCK_VECTOR;

        private static double[,] Conv2dFast(double[,] img, double[,] kernel)
        {
            int kh = kernel.GetLength(0);
            int kw = kernel.GetLength(1);
            int ih = img.GetLength(0);
            int iw = img.GetLength(1);

            int padH = kh / 2;
            int padW = kw / 2;

            // padded = np.pad(img, ((pad_h, pad_h), (pad_w, pad_w)), mode="reflect")
            int ph = ih + 2 * padH;
            int pw = iw + 2 * padW;
            var padded = new double[ph, pw];
            for (int y = 0; y < ph; y++)
            {
                for (int x = 0; x < pw; x++)
                {
                    int sy = ReflectPadIndex(y - padH, ih);
                    int sx = ReflectPadIndex(x - padW, iw);
                    padded[y, x] = img[sy, sx];
                }
            }

            // Sliding window view
            // shape = (ih, iw, kh, kw)
            // windows = as_strided(padded, shape=shape, strides=strides)
            var result = new double[ih, iw];
            for (int y = 0; y < ih; y++)
            {
                for (int x = 0; x < iw; x++)
                {
                    double sum = 0;
                    for (int ky = 0; ky < kh; ky++)
                    {
                        for (int kx = 0; kx < kw; kx++)
                            sum += padded[y + ky, x + kx] * kernel[ky, kx];
                    }

                    // Vektorisierte Faltung
                    result[y, x] = sum;
                }
            }

            return result;
        }

        private static (float energy, float entropy, float variance) SobelFeatures(double[,] gray)
        {
            double[,] gx = Conv2dFast(gray, SobelX);
            double[,] gy = Conv2dFast(gray, SobelY);

            int height = gray.GetLength(0);
            int width = gray.GetLength(1);

            var mag = new double[height, width];
            var ang = new double[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double gxv = gx[y, x];
                    double gyv = gy[y, x];
                    mag[y, x] = Math.Sqrt(gxv * gxv + gyv * gyv);
                    ang[y, x] = Math.Atan2(gyv, gxv);
                }
            }

            // sobel_energy = float(np.mean(mag))
            double sobelEnergy = 0;
            int n = height * width;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    sobelEnergy += mag[y, x];
            }
            sobelEnergy /= n;

            // bins = np.linspace(-np.pi, np.pi, 37)
            double[] bins = Linspace(-Math.PI, Math.PI, 37);
            // hist, _ = np.histogram(ang, bins=bins)
            var hist = new double[bins.Length - 1];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    hist[HistogramBin(ang[y, x], bins)]++;
            }

            // p = hist / (hist.sum() + 1e-9)
            double histSum = 0;
            for (int i = 0; i < hist.Length; i++)
                histSum += hist[i];
            histSum += 1e-9;

            // sobel_entropy = float(-np.sum(p * np.log(p + 1e-9)))
            double sobelEntropy = 0;
            for (int i = 0; i < hist.Length; i++)
            {
                double p = hist[i] / histSum;
                sobelEntropy -= p * Math.Log(p + 1e-9);
            }

            // sobel_var = float(np.var(mag))
            double sobelVar = 0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    double d = mag[y, x] - sobelEnergy;
                    sobelVar += d * d;
                }
            }
            sobelVar /= n;

            return ((float)sobelEnergy, (float)sobelEntropy, (float)sobelVar);
        }

        private static (float energy, float entropy, float variance, float whiteRatio) SobelFeaturesPage(Page page)
        {
            // 1. Render page as GRAY pixmap
            using (Pixmap pix = page.GetPixmap(dpi: 72, cs: Colorspace.Gray, alpha: false))
            {
                // 2. Downscale to 128x128 using PyMuPDF
                using (Pixmap pixSmall = new Pixmap(pix, 128, 128))
                {
                    double[,] gray = SamplesToGray(pixSmall.SAMPLES, 128, 128);
                    float pageWhiteRatio = MeanAbove(gray, 128, 128, 230);
                    (float energy, float entropy, float variance) = SobelFeatures(gray);
                    return (energy, entropy, variance, pageWhiteRatio);
                }
            }
        }

        private static (float energy, float entropy, float variance, float whiteRatio) SobelFeaturesPixmap(Pixmap pixmap)
        {
            double[,] gray = SamplesToGray(pixmap.SAMPLES, 128, 128);
            float pageWhiteRatio = MeanAbove(gray, 128, 128, 230);
            (float energy, float entropy, float variance) = SobelFeatures(gray);
            return (energy, entropy, variance, pageWhiteRatio);
        }

        /// <summary>Return model-relevant features for the given page.</summary>
        /// <param name="blocks">
        /// Blocks created by get_text("dict",flags=FLAGS)["blocks].
        /// The FLAGS *MUST* include PRESERVE_IMAGES and COLLECT_VECTORS.
        /// </param>
        /// <param name="pageRect">The rectangle Page.rect.</param>
        /// <param name="page">Source page for pixmap and image feature extraction.</param>
        public static Dictionary<string, float> ComputeFeatures(
            List<Block> blocks,
            Rect pageRect,
            Page page) =>
            ComputeFeatures(blocks, pageRect, page, null);

        /// <summary>
        /// Mirror <c>compute_features(blocks, page_rect, pix)</c> when <c>pix</c> is a 128x128 GRAY pixmap.
        /// </summary>
        public static Dictionary<string, float> ComputeFeatures(
            List<Block> blocks,
            Rect pageRect,
            Pixmap pixmap) =>
            ComputeFeatures(blocks, pageRect, null, pixmap);

        private static Dictionary<string, float> ComputeFeatures(
            List<Block> blocks,
            Rect pageRect,
            Page page,
            Pixmap pixmap)
        {
            int numBlocks = blocks?.Count ?? 0; // total block count
            float pageArea = (pageRect.X1 - pageRect.X0) * (pageRect.Y1 - pageRect.Y0);

            // list of span rectangles
            var spanRects = new List<Rect>();
            if (blocks != null)
            {
                foreach (Block b in blocks)
                {
                    if (b?.Type != BlockText || b.Lines == null)
                        continue;
                    foreach (Line l in b.Lines)
                    {
                        if (l?.Spans == null)
                            continue;
                        foreach (Span s in l.Spans)
                        {
                            if (!SpanIncluded(s.Text))
                                continue;
                            spanRects.Add(s.Bbox);
                        }
                    }
                }
            }

            int numSpans = spanRects.Count; // count of text spans

            // total area covered by text
            float textArea = spanRects.Sum(r => (r.Y1 - r.Y0) * (r.X1 - r.X0));
            // text density
            float textDensity = pageArea > 0 ? textArea / pageArea : 0f;

            float avgSpanHeight;
            float avgSpanWidth;
            if (numSpans > 0)
            {
                avgSpanHeight = spanRects.Average(r => r.Y1 - r.Y0);
                avgSpanWidth = spanRects.Average(r => r.X1 - r.X0);
            }
            else
            {
                avgSpanHeight = 0f;
                avgSpanWidth = 0f;
            }

            // vector block bboxes
            var vectors = blocks?.Where(b => b?.Type == BlockVector).ToList() ?? new List<Block>();
            float vectorArea = vectors.Sum(b => (b.Bbox.Y1 - b.Bbox.Y0) * (b.Bbox.X1 - b.Bbox.X0));
            float logVectorDensity = pageArea > 0
                ? (float)Math.Log(1 + vectorArea / pageArea + 1e-9)
                : 0f;
            int numSmallObliqueVectors = vectors.Count(b =>
                GetIsRect(b)
                && b.Bbox.X1 - b.Bbox.X0 < 15
                && b.Bbox.Y1 - b.Bbox.Y0 < 15);

            // Compute Sobel energy for the entire page as a layout feature
            float pageSobelEnergy;
            float pageSobelEntropy;
            float pageSobelVar;
            float pageWhiteRatio;
            if (pixmap != null)
            {
                (pageSobelEnergy, pageSobelEntropy, pageSobelVar, pageWhiteRatio) = SobelFeaturesPixmap(pixmap);
            }
            else
            {
                (pageSobelEnergy, pageSobelEntropy, pageSobelVar, pageWhiteRatio) = SobelFeaturesPage(page);
            }

            // image block bboxes
            var images = blocks?.Where(b => b?.Type == BlockImage).ToList() ?? new List<Block>();
            // total area covered by images
            int numImages = images.Count; // image block count
            float imageArea = 0f;

            var relevantImages = new List<(float score, Block imageBlock)>();
            foreach (Block imageBlock in images)
            {
                float[] visibleBbox =
                {
                    Math.Max(imageBlock.Bbox.X0, pageRect.X0),
                    Math.Max(imageBlock.Bbox.Y0, pageRect.Y0),
                    Math.Min(imageBlock.Bbox.X1, pageRect.X1),
                    Math.Min(imageBlock.Bbox.Y1, pageRect.Y1),
                };
                float thisImgArea = (visibleBbox[3] - visibleBbox[1]) * (visibleBbox[2] - visibleBbox[0]);
                imageArea += thisImgArea;
                if (thisImgArea <= 0.01f * pageArea)
                    continue;
                float totalImgArea = (imageBlock.Bbox.Y1 - imageBlock.Bbox.Y0) * (imageBlock.Bbox.X1 - imageBlock.Bbox.X0);
                float score = thisImgArea / totalImgArea * thisImgArea;
                relevantImages.Add((score, imageBlock));
            }

            // image density
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
                ["page_sobel_energy"] = pageSobelEnergy,
                ["page_sobel_entropy"] = pageSobelEntropy,
                ["page_sobel_var"] = pageSobelVar,
                ["page_white_ratio"] = pageWhiteRatio,
                ["img_fft_energy"] = 0.0f,
                ["img_fft_ratio"] = 0.0f,
                ["img_black_ratio"] = 0.0f,
                ["img_white_ratio"] = 0.0f,
                ["img_sobel_energy"] = 0.0f,
                ["img_sobel_orientation_entropy"] = 0.0f,
                ["img_sobel_local_variance"] = 0.0f,
            };

            if (relevantImages.Count == 0)
                return features;

            // find most relevant image on page
            Block bestImg = relevantImages.OrderByDescending(x => x.score).First().imageBlock;

            double[,] img = null;
            Pixmap pix = new Pixmap(bestImg.Image);
            try
            {
                if (pix.Alpha != 0)
                {
                    Pixmap noAlpha = new Pixmap(pix, 0); // remove alpha channel
                    pix.Dispose();
                    pix = noAlpha;
                }

                try // apply any mask if available and compatible
                {
                    if (bestImg.Mask != null)
                    {
                        Pixmap maskPix = new Pixmap(bestImg.Mask);
                        try
                        {
                            Pixmap masked = new Pixmap(pix, maskPix); // apply mask to image
                            maskPix.Dispose();
                            maskPix = null;
                            Pixmap maskedNoAlpha = new Pixmap(masked, 0); // remove alpha channel after masking
                            masked.Dispose();
                            pix.Dispose();
                            pix = maskedNoAlpha;
                        }
                        finally
                        {
                            maskPix?.Dispose();
                        }
                    }
                }
                catch (Exception)
                {
                    // pass
                }

                if (pix.N > 1)
                {
                    Pixmap grayPix = new Pixmap(ColorSpace.csGRAY, pix); // convert to grayscale
                    pix.Dispose();
                    pix = grayPix;
                }

                // resize to 128x128 for consistent feature extraction
                Pixmap resized = new Pixmap(pix, 128, 128);
                pix.Dispose();
                pix = resized;

                // FFT / textmap
                img = SamplesToGray(pix.SAMPLES, 128, 128);
            }
            finally
            {
                pix?.Dispose();
            }

            if (img == null)
                return features;

            var re = new double[128, 128];
            var im = new double[128, 128];
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    re[y, x] = img[y, x];
                    im[y, x] = 0;
                }
            }

            Fft2(re, im);
            FftShift(re, im);

            var magnitude = new double[128, 128];
            double mean = 0;
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    magnitude[y, x] = Math.Sqrt(re[y, x] * re[y, x] + im[y, x] * im[y, x]);
                    mean += magnitude[y, x];
                }
            }
            mean /= 128 * 128;

            double aboveSum = 0;
            int aboveCount = 0;
            double belowSum = 0;
            int belowCount = 0;
            for (int y = 0; y < 128; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    if (magnitude[y, x] > mean)
                    {
                        aboveSum += magnitude[y, x];
                        aboveCount++;
                    }
                    else
                    {
                        belowSum += magnitude[y, x];
                        belowCount++;
                    }
                }
            }

            float fftEnergy = (float)mean;
            float fftRatio = (float)((aboveSum / Math.Max(aboveCount, 1))
                / (belowSum / Math.Max(belowCount, 1) + 1e-6));

            float blackRatio = MeanBelow(img, 128, 128, 128);
            float whiteRatio = MeanAbove(img, 128, 128, 230);

            // --- Sobel features ---
            (float sobelEnergy, float sobelEntropy, float sobelVar) = SobelFeatures(img);
            features["img_fft_energy"] = fftEnergy;
            features["img_fft_ratio"] = fftRatio;
            features["img_black_ratio"] = blackRatio;
            features["img_white_ratio"] = whiteRatio;
            features["img_sobel_energy"] = sobelEnergy;
            features["img_sobel_orientation_entropy"] = sobelEntropy;
            features["img_sobel_local_variance"] = sobelVar;

            return features;
        }

        private static bool GetIsRect(Block block)
        {
            // b.get("isrect", False)
            return false;
        }

        private static bool SpanIncluded(string text)
        {
            text = text ?? "";
            // not (s["text"].isspace())
            if (text.Length == 0)
                return true;
            return !text.All(char.IsWhiteSpace);
        }

        private static double[,] SamplesToGray(byte[] samples, int width, int height)
        {
            var gray = new double[height, width];
            if (samples == null)
                return gray;
            int n = Math.Min(samples.Length, width * height);
            for (int i = 0; i < n; i++)
            {
                gray[i / width, i % width] = samples[i];
            }
            return gray;
        }

        private static float MeanAbove(double[,] gray, int width, int height, int threshold)
        {
            int count = 0;
            int total = width * height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (gray[y, x] > threshold)
                        count++;
                }
            }
            return count / (float)total;
        }

        private static float MeanBelow(double[,] gray, int width, int height, int threshold)
        {
            int count = 0;
            int total = width * height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (gray[y, x] < threshold)
                        count++;
                }
            }
            return count / (float)total;
        }

        private static double[] Linspace(double start, double end, int num)
        {
            var result = new double[num];
            if (num == 1)
            {
                result[0] = start;
                return result;
            }

            double step = (end - start) / (num - 1);
            for (int i = 0; i < num; i++)
                result[i] = start + step * i;
            return result;
        }

        private static int HistogramBin(double value, double[] binEdges)
        {
            int nBins = binEdges.Length - 1;
            if (value >= binEdges[nBins - 1])
                return nBins - 1;
            for (int i = 0; i < nBins - 1; i++)
            {
                if (value >= binEdges[i] && value < binEdges[i + 1])
                    return i;
            }
            return 0;
        }

        private static int ReflectPadIndex(int i, int size)
        {
            if (size == 1)
                return 0;
            while (i < 0 || i >= size)
            {
                if (i < 0)
                    i = -i;
                else
                    i = 2 * (size - 1) - i;
            }
            return i;
        }

        private static void FftShift(double[,] re, double[,] im)
        {
            int rows = re.GetLength(0);
            int cols = re.GetLength(1);
            int rowHalf = rows / 2;
            int colHalf = cols / 2;

            for (int y = 0; y < rowHalf; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    int y2 = y + rowHalf;
                    double tmpRe = re[y, x];
                    double tmpIm = im[y, x];
                    re[y, x] = re[y2, x];
                    im[y, x] = im[y2, x];
                    re[y2, x] = tmpRe;
                    im[y2, x] = tmpIm;
                }
            }

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < colHalf; x++)
                {
                    int x2 = x + colHalf;
                    double tmpRe = re[y, x];
                    double tmpIm = im[y, x];
                    re[y, x] = re[y, x2];
                    im[y, x] = im[y, x2];
                    re[y, x2] = tmpRe;
                    im[y, x2] = tmpIm;
                }
            }
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
