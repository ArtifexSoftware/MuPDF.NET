#if NET8_0_OR_GREATER
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MuPDF.NET;
using RapidOcrNet;
using SkiaSharp;

namespace PDF4LLM.Ocr
{
    /// <summary>Lazy RapidOcrNet engine used by <see cref="RapidOcrApi"/> and <see cref="RapidTessApi"/>.</summary>
    internal static class RapidOcrEngine
    {
        const string DetModel = "ch_PP-OCRv5_mobile_det.onnx";
        const string ClsModel = "ch_ppocr_mobile_v2.0_cls_infer.onnx";
        const string RecModel = "latin_PP-OCRv5_rec_mobile_infer.onnx";
        const string KeysModel = "ppocrv5_latin_dict.txt";

        static readonly object Gate = new object();
        static RapidOcr _engine;
        static bool _probeComplete;
        static bool _modelsMissing;
        static string _lastError;
        static bool _warned;

        internal static bool IsAvailable
        {
            get
            {
                // Only short-circuit when the engine is ready. Do not cache false:
                // an earlier probe (e.g. from another test) must not block later use.
                if (_engine != null)
                    return true;
                if (_modelsMissing)
                    return false;

                lock (Gate)
                {
                    if (_engine != null)
                        return true;
                    if (_modelsMissing)
                        return false;
                    if (_probeComplete)
                        return false;

                    _probeComplete = true;
                    if (TryCreateEngine(out _lastError))
                        return true;

                    if (!_warned && !string.IsNullOrEmpty(_lastError))
                    {
                        _warned = true;
                        Console.WriteLine($"Warning: RapidOCR is not available ({_lastError})");
                    }

                    return false;
                }
            }
        }

        internal static string LastError => _lastError;

        /// <summary>Re-run the availability probe (used by tests after models are deployed).</summary>
        internal static void ResetProbe()
        {
            lock (Gate)
            {
                _engine = null;
                _probeComplete = false;
                _modelsMissing = false;
                _lastError = null;
                _warned = false;
            }
        }

        static bool TryCreateEngine(out string error)
        {
            error = null;
            try
            {
                string modelDir = ResolveModelsDirectory();
                if (modelDir == null)
                {
                    _modelsMissing = true;
                    error = "RapidOCR ONNX models were not found. Searched:\n" +
                            string.Join("\n", CandidateModelDirectories());
                    return false;
                }

                var engine = new RapidOcr();
                if (TryInitModels(engine, modelDir, out error))
                {
                    _engine = engine;
                    return true;
                }

                _engine = null;
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                _engine = null;
                return false;
            }
        }

        static bool TryInitModels(RapidOcr engine, string modelDir, out string error)
        {
            error = null;
            string detPath = Path.Combine(modelDir, DetModel);
            string clsPath = Path.Combine(modelDir, ClsModel);
            string recPath = Path.Combine(modelDir, RecModel);
            string keysPath = Path.Combine(modelDir, KeysModel);

            if (IsDefaultModelLayout(modelDir))
            {
                try
                {
                    engine.InitModels();
                    return true;
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }
            }

            try
            {
                engine.InitModels(detPath, clsPath, recPath, keysPath);
                return true;
            }
            catch (Exception ex)
            {
                error = string.IsNullOrEmpty(error)
                    ? ex.Message
                    : error + "; " + ex.Message;
                return false;
            }
        }

        static bool IsDefaultModelLayout(string modelDir)
        {
            if (string.IsNullOrEmpty(AppContext.BaseDirectory))
                return false;

            string defaultDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "models", "v5"));
            return string.Equals(
                Path.GetFullPath(modelDir),
                defaultDir,
                StringComparison.OrdinalIgnoreCase);
        }

        internal static string ResolveModelsDirectory()
        {
            foreach (string dir in CandidateModelDirectories())
            {
                if (Directory.Exists(dir) && File.Exists(Path.Combine(dir, DetModel)))
                    return dir;
            }

            return null;
        }

        static string[] CandidateModelDirectories()
        {
            string subdir = Path.Combine("models", "v5");
            var dirs = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void Add(string dir)
            {
                if (string.IsNullOrWhiteSpace(dir))
                    return;
                string full = Path.GetFullPath(dir);
                if (seen.Add(full))
                    dirs.Add(full);
            }

            if (!string.IsNullOrEmpty(AppContext.BaseDirectory))
                Add(Path.Combine(AppContext.BaseDirectory, subdir));

            string pdf4llmDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (!string.IsNullOrEmpty(pdf4llmDir))
                Add(Path.Combine(pdf4llmDir, subdir));

            string rapidOcrDir = Path.GetDirectoryName(typeof(RapidOcr).Assembly.Location);
            if (!string.IsNullOrEmpty(rapidOcrDir))
                Add(Path.Combine(rapidOcrDir, subdir));

            string envDir = Environment.GetEnvironmentVariable("PDF4LLM_RAPIDOCR_MODELS");
            if (!string.IsNullOrWhiteSpace(envDir))
                Add(envDir);

            AddNuGetRapidOcrModelDirectories(dirs, seen, subdir);

            return dirs.ToArray();
        }

        static void AddNuGetRapidOcrModelDirectories(List<string> dirs, HashSet<string> seen, string subdir)
        {
            string nugetRoot = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
            if (string.IsNullOrWhiteSpace(nugetRoot))
            {
                nugetRoot = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    ".nuget",
                    "packages");
            }

            string rapidOcrRoot = Path.Combine(nugetRoot, "rapidocrnet");
            if (!Directory.Exists(rapidOcrRoot))
                return;

            foreach (string versionDir in Directory.GetDirectories(rapidOcrRoot))
            {
                string candidate = Path.Combine(versionDir, subdir);
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;
                string full = Path.GetFullPath(candidate);
                if (seen.Add(full))
                    dirs.Add(full);
            }
        }

        internal static OcrResult Detect(Pixmap pixmap)
        {
            if (!IsAvailable || pixmap == null)
                return null;

            using SKBitmap bitmap = PixmapToSkBitmap(pixmap);
            if (bitmap == null)
                return null;

            lock (Gate)
                return _engine.Detect(bitmap, RapidOcrOptions.PythonCompat);
        }

        static SKBitmap PixmapToSkBitmap(Pixmap pix)
        {
            byte[] png = pix.ToBytes("png");
            return SKBitmap.Decode(png);
        }
    }
}
#endif
