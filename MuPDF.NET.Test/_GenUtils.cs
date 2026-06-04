using System;
using System.Collections.Generic;
using System.IO;
using MuPDF.NET;

namespace MuPDF.NET.Test
{
    internal static class _Constants
    {
        // some colors
        public static float[] red = new float[] { 1, 0, 0 };
        public static float[] blue = new float[] { 0, 0, 1 };
        public static float[] gold = new float[] { 1, 1, 0 };
        public static float[] green = new float[] { 0, 1, 0 };
        public static float[] white = new float[] { 1, 1, 1 };
        public static float[] black = new float[] { 0, 0, 0 };

        // rectangles and points
        public static Rect displ = new Rect(0, 50, 0, 50);
        public static Rect r = new Rect(72, 72, 220, 100);
        public static Rect rect = new Rect(100, 100, 200, 200);

        // string
        public static string t1 = "têxt üsès Lätiñ charß,\nEUR: €, mu: µ, super scripts: ²³!";

        public static Func<string> FILENAME = () =>
        {
            return System.Diagnostics.Process.GetCurrentProcess().MainModule!.FileName;
        };
    }
    internal static class _Path
    {
        public const string SharedFolderName = "Shared";
        public const string OutputFolderName = "_Output";

        public static string ResolveSolutionRoot()
        {
            var dir = Path.GetDirectoryName(typeof(_Path).Assembly.Location)!;
            for (int i = 0; i < 15; i++)
            {
                if (File.Exists(Path.Combine(dir, "MuPDF.NET.sln")))
                    return Path.GetFullPath(dir);
                var parent = Directory.GetParent(dir)?.FullName;
                if (parent == null)
                    break;
                dir = parent;
            }
            throw new InvalidOperationException("Could not find solution root (MuPDF.NET.sln)");
        }

        public static string ResolveTestDocument(string fileName, string? subFolder = null)
        {
            string root = ResolveSolutionRoot();
            return string.IsNullOrWhiteSpace(subFolder)
                ? Path.Combine(root, "TestDocuments", "MuPDF.NET.Test", fileName)
                : Path.Combine(root, "TestDocuments", "MuPDF.NET.Test", subFolder, fileName);
        }

        public static string RequireTestDocument(string fileName, string? subFolder = null)
        {
            string path = ResolveTestDocument(fileName, subFolder);
            if (!string.IsNullOrWhiteSpace(fileName) && !File.Exists(path))
                throw new FileNotFoundException($"Required test document not found: {path}");
            return path;
        }

        public static string Shared(string fileName) =>
            RequireTestDocument(fileName, SharedFolderName);

        public static string ForTestClass(string fileName, string testClassName) =>
            RequireTestDocument(fileName, testClassName);

        public static string ForOutput(string fileName, string testClassName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("fileName must not be empty.", nameof(fileName));
            if (string.IsNullOrWhiteSpace(testClassName))
                throw new ArgumentException("testClassName must not be empty.", nameof(testClassName));

            string path = Path.Combine(
                ResolveSolutionRoot(), "TestDocuments", "MuPDF.NET.Test", OutputFolderName, testClassName, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return path;
        }
    }

    internal static class _Compare
    {
        public static bool GentleCompareWordList(
            IReadOnlyList<(float x0, float y0, float x1, float y1, string word)> w0,
            IReadOnlyList<(float x0, float y0, float x1, float y1, string word)> w1)
        {
            const float tolerance = 1e-3f;
            int wordCount = w0.Count;
            if (wordCount != w1.Count)
                return false;

            for (int i = 0; i < wordCount; i++)
            {
                if (w0[i].word != w1[i].word)
                    return false;

                var r0 = new Rect(w0[i].x0, w0[i].y0, w0[i].x1, w0[i].y1);
                var r1 = new Rect(w1[i].x0, w1[i].y0, w1[i].x1, w1[i].y1);
                float delta = (r1 - r0).Norm();
                if (delta > tolerance)
                    return false;
            }
            return true;
        }

        public static bool GentleCompareWords(
            IReadOnlyList<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> w0,
            IReadOnlyList<(float x0, float y0, float x1, float y1, string word, int blockNo, int lineNo, int wordNo)> w1)
        {
            const float tolerance = 1e-3f;
            if (w0.Count != w1.Count)
                return false;

            for (int i = 0; i < w0.Count; i++)
            {
                if (w0[i].word != w1[i].word)
                    return false;

                var r0 = new Rect(w0[i].x0, w0[i].y0, w0[i].x1, w0[i].y1);
                var r1 = new Rect(w1[i].x0, w1[i].y0, w1[i].x1, w1[i].y1);
                float delta = (r1 - r0).Norm();
                if (delta > tolerance)
                    return false;
            }
            return true;
        }

        private static float Rms(byte[] a, byte[] b, int? verbose = null, string out_prefix = "")
        {
            if (a.Length != b.Length)
                throw new ArgumentException("Sequences must have the same length.");
            float e = 0;
            for (int i = 0; i < a.Length; i++)
            {
                if (verbose != null && i % verbose.Value == 0)
                {
                    // Console.WriteLine($"{out_prefix}rms(): i={i} e={e} aa={a[i]} bb={b[i]}.");
                }
                int d = a[i] - b[i];
                e += d * d;
            }
            return (float)Math.Sqrt(e / a.Length);
        }

        public static float PixmapsRms(object a, object b, string out_prefix = "")
        {
            var (aPix, disposeA) = OpenPixmap(a, out_prefix, "a");
            var (bPix, disposeB) = OpenPixmap(b, out_prefix, "b");
            try
            {
                if (aPix.Width != bPix.Width || aPix.Height != bPix.Height)
                    throw new InvalidOperationException(
                        $"Differing rects: a.irect=({aPix.Width}, {aPix.Height}) b.irect=({bPix.Width}, {bPix.Height}).");
                if (aPix.N != bPix.N)
                    throw new InvalidOperationException($"Differing rects: a.N={aPix.N} b.N={bPix.N}.");

                byte[] aMv = aPix.Samples;
                byte[] bMv = bPix.Samples;
                if (aMv.Length != bMv.Length)
                    throw new InvalidOperationException($"Sample length mismatch ({aMv.Length} vs {bMv.Length}).");

                return Rms(aMv, bMv, out_prefix: out_prefix);
            }
            finally
            {
                if (disposeB) bPix.Dispose();
                if (disposeA) aPix.Dispose();
            }
        }

        public static Pixmap PixmapsDiff(object a, object b, string out_prefix = "")
        {
            var (aPix, disposeA) = OpenPixmap(a, out_prefix, "a");
            var (bPix, disposeB) = OpenPixmap(b, out_prefix, "b");
            try
            {
                if (aPix.Width != bPix.Width || aPix.Height != bPix.Height)
                    throw new InvalidOperationException(
                        $"Differing rects: a.irect=({aPix.Width}, {aPix.Height}) b.irect=({bPix.Width}, {bPix.Height}).");
                if (aPix.N != bPix.N)
                    throw new InvalidOperationException($"Differing rects: a.N={aPix.N} b.N={bPix.N}.");

                byte[] aMv = aPix.Samples;
                byte[] bMv = bPix.Samples;
                var c = new Pixmap(aPix.tobytes());
                byte[] cMv = c.Samples;

                if (aMv.Length != bMv.Length || aMv.Length != cMv.Length)
                    throw new InvalidOperationException("Sample buffer length mismatch.");

                for (int i = 0; i < aMv.Length; i++)
                {
                    int aByte = aMv[i];
                    int bByte = bMv[i];
                    int cByte = cMv[i];
                    if (aByte < 0 || aByte >= 256 || bByte < 0 || bByte >= 256 || cByte < 0 || cByte >= 256)
                        throw new InvalidOperationException("Unexpected sample byte value.");
                    cMv[i] = (byte)(128 + (bByte - aByte) / 2);
                }

                c.SetSamples(cMv);
                return c;
            }
            finally
            {
                if (disposeB) bPix.Dispose();
                if (disposeA) aPix.Dispose();
            }
        }

        private static (Pixmap pixmap, bool dispose) OpenPixmap(object arg, string out_prefix, string label)
        {
            if (arg is string path)
                return (new Pixmap(path), true);
            if (arg is Pixmap pixmap)
                return (pixmap, false);
            throw new ArgumentException($"Expected path or Pixmap, got {arg?.GetType().Name ?? "null"}.", nameof(arg));
        }
    }

    internal static class _Version
    {
        public static (int major, int minor, int patch) mupdf_version_tuple()
        {
            string v = Tools.MupdfVersion().Trim();
            var parts = v.Split('.');
            int major = parts.Length > 0 && int.TryParse(parts[0], out int ma) ? ma : 0;
            int minor = parts.Length > 1 && int.TryParse(parts[1], out int mi) ? mi : 0;
            int patch = parts.Length > 2 && int.TryParse(parts[2].Split('-')[0], out int pa) ? pa : 0;
            return (major, minor, patch);
        }

        public static bool mupdf_version_tuple_at_least(int major, int minor, int patch)
        {
            var (ma, mi, pa) = mupdf_version_tuple();
            if (ma != major)
                return ma > major;
            if (mi != minor)
                return mi > minor;
            return pa >= patch;
        }
    }

    internal static class _Tools
    {
        public static bool IsClose(float a, float b, float eps = 1e-5f) =>
            Math.Abs(a - b) < eps;
    }
}
