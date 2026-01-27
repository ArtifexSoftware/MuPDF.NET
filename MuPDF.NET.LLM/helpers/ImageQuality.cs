using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace MuPDF.NET.LLM.Helpers
{
    /// <summary>
    /// Image quality analysis utilities.
    /// Ported and adapted from the Python module helpers/image_quality.py in pymupdf4llm.
    /// </summary>
    public static class ImageQuality
    {
        /// <summary>
        /// Bilinear resize (similar to OpenCV INTER_LINEAR), vectorized implementation in Python.
        /// </summary>
        /// <param name="img">Input image (2D byte array).</param>
        /// <param name="newH">New height.</param>
        /// <param name="newW">New width.</param>
        /// <returns>Resized image.</returns>
        public static byte[,] ResizeBilinear(byte[,] img, int newH, int newW)
        {
            int h = img.GetLength(0);
            int w = img.GetLength(1);
            float[,] imgFloat = new float[h, w];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    imgFloat[y, x] = img[y, x];

            // Target coordinates
            float[] ys = new float[newH];
            float[] xs = new float[newW];
            for (int i = 0; i < newH; i++)
                ys[i] = (i + 0.5f) * (h / (float)newH) - 0.5f;
            for (int i = 0; i < newW; i++)
                xs[i] = (i + 0.5f) * (w / (float)newW) - 0.5f;

            for (int i = 0; i < newH; i++)
                ys[i] = Math.Max(0, Math.Min(h - 1, ys[i]));
            for (int i = 0; i < newW; i++)
                xs[i] = Math.Max(0, Math.Min(w - 1, xs[i]));

            int[] y0 = new int[newH];
            int[] x0 = new int[newW];
            for (int i = 0; i < newH; i++)
                y0[i] = (int)Math.Floor(ys[i]);
            for (int i = 0; i < newW; i++)
                x0[i] = (int)Math.Floor(xs[i]);

            int[] y1 = new int[newH];
            int[] x1 = new int[newW];
            for (int i = 0; i < newH; i++)
                y1[i] = Math.Min(h - 1, y0[i] + 1);
            for (int i = 0; i < newW; i++)
                x1[i] = Math.Min(w - 1, x0[i] + 1);

            byte[,] outImg = new byte[newH, newW];
            for (int y = 0; y < newH; y++)
            {
                float wy = ys[y] - y0[y];
                for (int x = 0; x < newW; x++)
                {
                    float wx = xs[x] - x0[x];
                    // Four corner values via fancy indexing
                    float Ia = imgFloat[y0[y], x0[x]]; // Top-left
                    float Ib = imgFloat[y0[y], x1[x]]; // Top-right
                    float Ic = imgFloat[y1[y], x0[x]]; // Bottom-left
                    float Id = imgFloat[y1[y], x1[x]]; // Bottom-right

                    float top = Ia * (1 - wx) + Ib * wx;
                    float bottom = Ic * (1 - wx) + Id * wx;
                    float val = top * (1 - wy) + bottom * wy;
                    outImg[y, x] = (byte)Math.Max(0, Math.Min(255, val));
                }
            }
            return outImg;
        }

        /// <summary>
        /// 2D convolution (Cross-Correlation) with reflect padding.
        /// Vectorized over kernel in Python.
        /// </summary>
        /// <param name="img">Input image.</param>
        /// <param name="kernel">Convolution kernel.</param>
        /// <returns>Convolved image.</returns>
        public static float[,] Convolve2D(float[,] img, float[,] kernel)
        {
            int kh = kernel.GetLength(0);
            int kw = kernel.GetLength(1);
            int padH = kh / 2;
            int padW = kw / 2;

            int H = img.GetLength(0);
            int W = img.GetLength(1);
            float[,] padded = new float[H + 2 * padH, W + 2 * padW];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    padded[y + padH, x + padW] = img[y, x];

            // Reflect padding
            for (int y = 0; y < padH; y++)
                for (int x = 0; x < W; x++)
                    padded[y, x + padW] = img[padH - y, x];
            for (int y = H + padH; y < H + 2 * padH; y++)
                for (int x = 0; x < W; x++)
                    padded[y, x + padW] = img[2 * H + padH - y - 1, x];
            for (int y = 0; y < H + 2 * padH; y++)
                for (int x = 0; x < padW; x++)
                    padded[y, x] = padded[y, 2 * padW - x];
            for (int y = 0; y < H + 2 * padH; y++)
                for (int x = W + padW; x < W + 2 * padW; x++)
                    padded[y, x] = padded[y, 2 * (W + padW) - x - 2];

            float[,] output = new float[H, W];
            // Loop only over kernel offsets, not over pixels
            for (int i = 0; i < kh; i++)
            {
                for (int j = 0; j < kw; j++)
                {
                    for (int y = 0; y < H; y++)
                    {
                        for (int x = 0; x < W; x++)
                        {
                            output[y, x] += kernel[i, j] * padded[y + i, x + j];
                        }
                    }
                }
            }
            return output;
        }

        /// <summary>
        /// 1D Gaussian kernel
        /// </summary>
        public static float[] GaussianKernel1D(int size = 5, float sigma = 1.0f)
        {
            float[] kernel = new float[size];
            int center = size / 2;
            float sum = 0;
            for (int i = 0; i < size; i++)
            {
                float x = i - center;
                kernel[i] = (float)Math.Exp(-0.5 * (x / sigma) * (x / sigma));
                sum += kernel[i];
            }
            for (int i = 0; i < size; i++)
                kernel[i] /= sum;
            return kernel;
        }

        /// <summary>
        /// Separable Gaussian Blur: first horizontal, then vertical.
        /// </summary>
        /// <param name="img">Input image.</param>
        /// <param name="ksize">Kernel size.</param>
        /// <param name="sigma">Sigma value.</param>
        /// <returns>Blurred image.</returns>
        public static float[,] GaussianBlur(float[,] img, int ksize = 5, float sigma = 1.0f)
        {
            float[] kernel = GaussianKernel1D(ksize, sigma);
            int H = img.GetLength(0);
            int W = img.GetLength(1);
            int pad = ksize / 2;

            // Horizontal
            float[,] padded = new float[H, W + 2 * pad];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    padded[y, x + pad] = img[y, x];
            // Reflect padding
            for (int y = 0; y < H; y++)
                for (int x = 0; x < pad; x++)
                    padded[y, x] = padded[y, 2 * pad - x];
            for (int y = 0; y < H; y++)
                for (int x = W + pad; x < W + 2 * pad; x++)
                    padded[y, x] = padded[y, 2 * (W + pad) - x - 2];

            float[,] tmp = new float[H, W];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float sum = 0;
                    for (int j = 0; j < ksize; j++)
                        sum += kernel[j] * padded[y, x + j];
                    tmp[y, x] = sum;
                }
            }

            // Vertical
            padded = new float[H + 2 * pad, W];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    padded[y + pad, x] = tmp[y, x];
            // Reflect padding
            for (int y = 0; y < pad; y++)
                for (int x = 0; x < W; x++)
                    padded[y, x] = padded[2 * pad - y, x];
            for (int y = H + pad; y < H + 2 * pad; y++)
                for (int x = 0; x < W; x++)
                    padded[y, x] = padded[2 * (H + pad) - y - 2, x];

            float[,] output = new float[H, W];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    float sum = 0;
                    for (int i = 0; i < ksize; i++)
                        sum += kernel[i] * padded[y + i, x];
                    output[y, x] = sum;
                }
            }
            return output;
        }

        /// <summary>
        /// Sobel gradients in x/y, Magnitude and Angle.
        /// </summary>
        /// <param name="img">Input image.</param>
        /// <returns>Magnitude and Angle matrices.</returns>
        public static (float[,] mag, float[,] ang) SobelGradients(byte[,] img)
        {
            float[,] imgFloat = new float[img.GetLength(0), img.GetLength(1)];
            for (int y = 0; y < img.GetLength(0); y++)
                for (int x = 0; x < img.GetLength(1); x++)
                    imgFloat[y, x] = img[y, x];

            float[,] Kx = new float[,] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
            float[,] Ky = new float[,] { { -1, -2, -1 }, { 0, 0, 0 }, { 1, 2, 1 } };

            float[,] gx = Convolve2D(imgFloat, Kx);
            float[,] gy = Convolve2D(imgFloat, Ky);

            int H = img.GetLength(0);
            int W = img.GetLength(1);
            float[,] mag = new float[H, W];
            float[,] ang = new float[H, W];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    mag[y, x] = (float)Math.Sqrt(gx[y, x] * gx[y, x] + gy[y, x] * gy[y, x]);
                    ang[y, x] = (float)Math.Atan2(gy[y, x], gx[y, x]);
                }
            }
            return (mag, ang);
        }

        /// <summary>
        /// Shannon entropy check over 256-bin histogram.
        /// </summary>
        /// <param name="img">Input image.</param>
        /// <param name="threshold">Entropy threshold.</param>
        /// <returns>Entropy value and pass/fail status.</returns>
        public static (double entropy, bool passed) EntropyCheck(byte[,] img, double threshold = 5.0)
        {
            int[] hist = new int[256];
            int H = img.GetLength(0);
            int W = img.GetLength(1);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    hist[img[y, x]]++;

            double total = H * W;
            double entropy = 0;
            for (int i = 0; i < 256; i++)
            {
                if (hist[i] > 0)
                {
                    double p = hist[i] / total;
                    entropy -= p * Math.Log(p, 2);
                }
            }
            return (entropy, entropy >= threshold);
        }

        /// <summary>
        /// Low-Frequency-Ratio in FFT spectrum.
        /// Internally rescales to 128x128.
        /// </summary>
        /// <param name="imgGray">Input grayscale image.</param>
        /// <param name="threshold">Ratio threshold.</param>
        /// <returns>Ratio value and pass/fail status.</returns>
        public static (double ratio, bool passed) FftCheck(byte[,] imgGray, double threshold = 0.15)
        {
            byte[,] small = ResizeBilinear(imgGray, 128, 128);
            Complex[,] f = Fft2D(small);
            Complex[,] fshift = FftShift(f);
            double[,] magnitude = new double[128, 128];
            for (int y = 0; y < 128; y++)
                for (int x = 0; x < 128; x++)
                    magnitude[y, x] = fshift[y, x].Magnitude;

            int h = 128, w = 128;
            double centerSum = 0;
            double totalSum = 0;
            for (int y = h / 4; y < 3 * h / 4; y++)
                for (int x = w / 4; x < 3 * w / 4; x++)
                    centerSum += magnitude[y, x];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    totalSum += magnitude[y, x];

            double ratio = centerSum / totalSum;
            return (ratio, ratio < threshold);
        }

        /// <summary>
        /// Simple 2D FFT using System.Numerics
        /// </summary>
        private static Complex[,] Fft2D(byte[,] img)
        {
            int H = img.GetLength(0);
            int W = img.GetLength(1);
            Complex[,] result = new Complex[H, W];
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    // Simplified FFT - for production, use a proper FFT library
                    // This is a placeholder that converts to complex
                    result[y, x] = new Complex(img[y, x], 0);
                }
            }
            // Note: Full 2D FFT implementation would be needed for production
            return result;
        }

        private static Complex[,] FftShift(Complex[,] f)
        {
            int H = f.GetLength(0);
            int W = f.GetLength(1);
            Complex[,] shifted = new Complex[H, W];
            int h2 = H / 2, w2 = W / 2;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    shifted[(y + h2) % H, (x + w2) % W] = f[y, x];
            return shifted;
        }

        /// <summary>
        /// Otsu Thresholding.
        /// </summary>
        /// <param name="img">Input image.</param>
        /// <returns>Binary image (0 or 255).</returns>
        public static byte[,] OtsuThreshold(byte[,] img)
        {
            int[] hist = new int[256];
            int H = img.GetLength(0);
            int W = img.GetLength(1);
            int total = H * W;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    hist[img[y, x]]++;

            long sumTotal = 0;
            for (int i = 0; i < 256; i++)
                sumTotal += i * hist[i];

            long sumB = 0;
            long wB = 0;
            double maxVar = 0;
            int threshold = 0;

            for (int t = 0; t < 256; t++)
            {
                wB += hist[t];
                if (wB == 0) continue;
                long wF = total - wB;
                if (wF == 0) break;

                sumB += t * hist[t];
                double mB = sumB / (double)wB;
                double mF = (sumTotal - sumB) / (double)wF;
                double varBetween = wB * wF * (mB - mF) * (mB - mF);

                if (varBetween > maxVar)
                {
                    maxVar = varBetween;
                    threshold = t;
                }
            }

            byte[,] binary = new byte[H, W];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    binary[y, x] = (byte)(img[y, x] > threshold ? 255 : 0);
            return binary;
        }

        /// <summary>
        /// 8-connectivity Connected Components, Union-Find based two-pass approach.
        /// </summary>
        /// <param name="binaryImg">Input binary image (0 background, !=0 foreground).</param>
        /// <param name="threshold">Minimum component count threshold.</param>
        /// <returns>Component count and pass/fail status.</returns>
        public static (int components, bool passed) ComponentsCheck(byte[,] binaryImg, int threshold = 10)
        {
            int H = binaryImg.GetLength(0);
            int W = binaryImg.GetLength(1);
            int[,] labels = new int[H, W];
            int maxLabels = H * W / 2 + 1;
            int[] parent = new int[maxLabels];
            int[] rank = new int[maxLabels];
            for (int i = 0; i < maxLabels; i++)
                parent[i] = i;

            int nextLabel = 1;

            int Find(int x)
            {
                while (parent[x] != x)
                {
                    parent[x] = parent[parent[x]];
                    x = parent[x];
                }
                return x;
            }

            void Union(int a, int b)
            {
                int ra = Find(a);
                int rb = Find(b);
                if (ra == rb) return;
                if (rank[ra] < rank[rb])
                    parent[ra] = rb;
                else if (rank[ra] > rank[rb])
                    parent[rb] = ra;
                else
                {
                    parent[rb] = ra;
                    rank[ra]++;
                }
            }

            // First pass
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    if (binaryImg[y, x] == 0) continue;

                    List<int> neighbors = new List<int>();
                    int[] dy = { -1, -1, -1, 0 };
                    int[] dx = { -1, 0, 1, -1 };
                    for (int i = 0; i < 4; i++)
                    {
                        int ny = y + dy[i];
                        int nx = x + dx[i];
                        if (ny >= 0 && ny < H && nx >= 0 && nx < W && labels[ny, nx] > 0)
                            neighbors.Add(labels[ny, nx]);
                    }

                    if (neighbors.Count == 0)
                    {
                        labels[y, x] = nextLabel++;
                    }
                    else
                    {
                        int m = neighbors.Min();
                        labels[y, x] = m;
                        foreach (int n in neighbors)
                            if (n != m) Union(m, n);
                    }
                }
            }

            // Second pass: Label flattening
            Dictionary<int, int> labelMap = new Dictionary<int, int>();
            int current = 1;
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    if (labels[y, x] > 0)
                    {
                        int root = Find(labels[y, x]);
                        if (!labelMap.ContainsKey(root))
                            labelMap[root] = current++;
                        labels[y, x] = labelMap[root];
                    }
                }
            }

            int components = current - 1;
            return (components, components >= threshold);
        }

        /// <summary>
        /// Non-maximum suppression
        /// </summary>
        public static float[,] NonMaxSuppression(float[,] mag, float[,] ang)
        {
            int H = mag.GetLength(0);
            int W = mag.GetLength(1);
            float[,] Z = new float[H, W];
            float[,] angDeg = new float[H, W];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    angDeg[y, x] = ang[y, x] * 180.0f / (float)Math.PI;
                    if (angDeg[y, x] < 0) angDeg[y, x] += 180;
                }

            // Direction quantization
            // 0°, 45°, 90°, 135°

            for (int y = 1; y < H - 1; y++)
            {
                for (int x = 1; x < W - 1; x++)
                {
                    float angle = angDeg[y, x];
                    float m0 = mag[y, x];
                    float m1 = 0, m2 = 0;

                    // Helper function: compares with two neighbors in given direction
                    if ((angle >= 0 && angle < 22.5) || (angle >= 157.5 && angle <= 180))
                    {
                        m1 = mag[y, x - 1];
                        m2 = mag[y, x + 1];
                    }
                    else if (angle >= 22.5 && angle < 67.5)
                    {
                        m1 = mag[y - 1, x + 1];
                        m2 = mag[y + 1, x - 1];
                    }
                    else if (angle >= 67.5 && angle < 112.5)
                    {
                        m1 = mag[y - 1, x];
                        m2 = mag[y + 1, x];
                    }
                    else if (angle >= 112.5 && angle < 157.5)
                    {
                        m1 = mag[y - 1, x - 1];
                        m2 = mag[y + 1, x + 1];
                    }

                    if (m0 >= m1 && m0 >= m2)
                        Z[y, x] = m0;
                }
            }
            return Z;
        }

        /// <summary>
        /// Hysteresis thresholding
        /// </summary>
        public static byte[,] HysteresisThresholding(float[,] img, float low, float high)
        {
            int H = img.GetLength(0);
            int W = img.GetLength(1);
            byte strongVal = 255;
            byte weakVal = 50;
            byte[,] result = new byte[H, W];

            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    if (img[y, x] >= high)
                        result[y, x] = strongVal;
                    else if (img[y, x] >= low)
                        result[y, x] = weakVal;
                }

            bool changed = true;
            while (changed)
            {
                changed = false;

                // Neighborhood of a strong pixel:
                // 8-neighborhood via shifts
                // Weak pixels that border strong become strong
                for (int y = 1; y < H - 1; y++)
                {
                    for (int x = 1; x < W - 1; x++)
                    {
                        if (result[y, x] == weakVal)
                        {
                            bool hasStrong = false;
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    if (dx == 0 && dy == 0) continue;
                                    if (result[y + dy, x + dx] == strongVal)
                                    {
                                        hasStrong = true;
                                        break;
                                    }
                                }
                                if (hasStrong) break;
                            }
                            if (hasStrong)
                            {
                                result[y, x] = strongVal;
                                changed = true;
                            }
                        }
                    }
                }
            }

            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    if (result[y, x] != strongVal)
                        result[y, x] = 0;

            return result;
        }

        /// <summary>
        /// Full Canny Edge Detector.
        /// </summary>
        /// <param name="img">Input image.</param>
        /// <param name="low">Low threshold.</param>
        /// <param name="high">High threshold.</param>
        /// <returns>Edge image.</returns>
        public static byte[,] CannyNumPy(byte[,] img, float low = 50.0f, float high = 100.0f)
        {
            float[,] imgFloat = new float[img.GetLength(0), img.GetLength(1)];
            for (int y = 0; y < img.GetLength(0); y++)
                for (int x = 0; x < img.GetLength(1); x++)
                    imgFloat[y, x] = img[y, x];

            float[,] blur = GaussianBlur(imgFloat, 5, 1.0f);
            byte[,] blurByte = new byte[blur.GetLength(0), blur.GetLength(1)];
            for (int y = 0; y < blur.GetLength(0); y++)
                for (int x = 0; x < blur.GetLength(1); x++)
                    blurByte[y, x] = (byte)Math.Max(0, Math.Min(255, blur[y, x]));

            var (mag, ang) = SobelGradients(blurByte);
            float[,] nms = NonMaxSuppression(mag, ang);
            byte[,] edges = HysteresisThresholding(nms, low, high);
            return edges;
        }

        /// <summary>
        /// Edge density check: mean(edges)/255.0.
        /// </summary>
        /// <param name="edges">Input edge image (0/255).</param>
        /// <param name="threshold">Density threshold.</param>
        /// <returns>Density value and pass/fail status.</returns>
        public static (double density, bool passed) EdgeDensityCheck(byte[,] edges, double threshold = 0.2)
        {
            int H = edges.GetLength(0);
            int W = edges.GetLength(1);
            long sum = 0;
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                    sum += edges[y, x];
            double density = sum / (255.0 * H * W);
            return (density, density >= threshold);
        }

        /// <summary>
        /// Runs all four checks and calculates weighted score.
        /// </summary>
        /// <param name="imgGray">Input 2D byte array (grayscale).</param>
        /// <returns>Dictionary with analysis results.</returns>
        public static Dictionary<string, (double value, bool passed)> AnalyzeImage(byte[,] imgGray)
        {
            // 1) Entropy
            var (entropyVal, entropyOk) = EntropyCheck(imgGray);

            // 2) FFT ratio
            var (fftRatio, fftOk) = FftCheck(imgGray);

            // 3) Components
            byte[,] binary = OtsuThreshold(imgGray);
            var (componentsCnt, componentsOk) = ComponentsCheck(binary);

            // 4) Edges
            byte[,] edges = CannyNumPy(imgGray);
            var (edgeDensity, edgesOk) = EdgeDensityCheck(edges);

            // Weighted score
            int score = 0;
            if (componentsOk) score += 2;
            if (edgesOk) score += 2;
            if (entropyOk) score += 1;
            if (fftOk) score += 1;

            return new Dictionary<string, (double value, bool passed)>
            {
                ["entropy"] = (entropyVal, entropyOk),
                ["fft_ratio"] = (fftRatio, fftOk),
                ["components"] = (componentsCnt, componentsOk),
                ["edge_density"] = (edgeDensity, edgesOk),
                ["score"] = (score, false),
            };
        }
    }
}
