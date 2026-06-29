using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using MuPDF.NET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PDF4LLM.Layout
{
    /// <summary>Python worker that runs layout ONNX inference per page.</summary>
    internal static class PyMuPdfLayoutBridge
    {
        const string WorkerReadyToken = "READY";
        const string WorkerResultPrefix = "RESULT ";

        static readonly object Gate = new object();
        static readonly ConditionalWeakTable<Document, string> TempDocumentPaths =
            new ConditionalWeakTable<Document, string>();

        static readonly string WorkerScript = @"
import contextlib
import json
import sys
import traceback

RESULT_PREFIX = 'RESULT '

try:
    with contextlib.redirect_stdout(sys.stderr):
        import pymupdf
        import pymupdf.layout
        pymupdf.layout.activate()
except Exception as exc:
    print('ERROR ' + json.dumps(str(exc)), flush=True)
    sys.exit(1)

print('READY', flush=True)

for line in sys.stdin:
    line = line.strip()
    if not line:
        continue
    if line == 'QUIT':
        break
    try:
        req = json.loads(line)
        path = req['path']
        page_no = int(req['page'])
        doc = pymupdf.open(path)
        try:
            page = doc[page_no]
            with contextlib.redirect_stdout(sys.stderr):
                page.get_layout()
            result = page.layout_information or []
        finally:
            doc.close()
        print(RESULT_PREFIX + json.dumps(result), flush=True)
    except Exception:
        traceback.print_exc(file=sys.stderr)
        print(RESULT_PREFIX + '[]', flush=True)
";

        static Process _worker;
        static bool? _probeResult;
        static string _version;
        static bool _activated;

        static PyMuPdfLayoutBridge()
        {
            AppDomain.CurrentDomain.ProcessExit += (_, __) => Deactivate();
        }

        public static bool IsAvailable
        {
            get
            {
                if (_probeResult.HasValue)
                    return _probeResult.Value;
                _probeResult = ProbePythonLayout();
                return _probeResult.Value;
            }
        }

        public static string Version
        {
            get
            {
                if (_version != null)
                    return _version;
                if (!IsAvailable)
                    return null;
                _version = RunPythonCapture("import pymupdf.layout; print(pymupdf.layout.version)");
                return _version;
            }
        }

        public static bool IsActivated => _activated;

        public static bool TryActivate()
        {
            if (!IsAvailable)
            {
                _activated = false;
                LayoutPythonPaths.PrintSetupHelp();
                return false;
            }

            lock (Gate)
            {
                EnsureWorker();
                Page.GetLayoutProvider = GetLayoutForPage;
                _activated = true;
            }

            return true;
        }

        public static void Deactivate()
        {
            lock (Gate)
            {
                _activated = false;
                Page.GetLayoutProvider = null;
                StopWorker();
            }
        }

        static bool ProbePythonLayout()
        {
            try
            {
                var psi = CreatePythonStartInfo("-c \"import pymupdf.layout\"");
                using (var proc = Process.Start(psi))
                {
                    if (proc == null)
                        return false;
                    if (!proc.WaitForExit(15000))
                    {
                        try { proc.Kill(); } catch { }
                        return false;
                    }

                    return proc.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        static string RunPythonCapture(string script)
        {
            try
            {
                var psi = CreatePythonStartInfo("-c " + QuoteArgument(script));
                using (var proc = Process.Start(psi))
                {
                    if (proc == null)
                        return null;
                    string stdout = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(15000);
                    if (proc.ExitCode != 0)
                        return null;
                    return stdout.Trim();
                }
            }
            catch
            {
                return null;
            }
        }

        static void EnsureWorker()
        {
            if (_worker != null && !_worker.HasExited)
                return;

            StopWorker();

            string scriptPath = Path.Combine(
                Path.GetTempPath(),
                "pdf4llm_pymupdf_layout_worker.py");
            File.WriteAllText(scriptPath, WorkerScript, Encoding.UTF8);

            var psi = CreatePythonStartInfo(QuoteArgument(scriptPath));
            psi.RedirectStandardInput = true;

            _worker = Process.Start(psi) ?? throw new InvalidOperationException(
                "Failed to start Python layout worker.");

            if (!WaitForWorkerReady(_worker, out string startupDetail))
            {
                StopWorker();
                throw new InvalidOperationException(
                    "pymupdf.layout worker failed to start: " + startupDetail);
            }
        }

        static bool WaitForWorkerReady(Process worker, out string detail)
        {
            var startupLines = new List<string>();
            detail = "";

            for (int i = 0; i < 200; i++)
            {
                string line = worker.StandardOutput.ReadLine();
                if (line == null)
                    break;

                if (line == WorkerReadyToken)
                    return true;

                if (line.StartsWith("ERROR ", StringComparison.Ordinal))
                {
                    detail = line.Substring(6);
                    return false;
                }

                startupLines.Add(line);
            }

            string err = worker.StandardError.ReadToEnd();
            detail = string.Join(Environment.NewLine, startupLines);
            if (!string.IsNullOrEmpty(err))
                detail = string.IsNullOrEmpty(detail) ? err : detail + Environment.NewLine + err;
            if (string.IsNullOrEmpty(detail))
                detail = "worker exited before READY";
            return false;
        }

        static string ReadWorkerJsonPayload(Process worker)
        {
            for (int i = 0; i < 200; i++)
            {
                string line = worker.StandardOutput.ReadLine();
                if (line == null)
                    return null;

                if (line.StartsWith(WorkerResultPrefix, StringComparison.Ordinal))
                    return line.Substring(WorkerResultPrefix.Length);

                // Backward compatibility with older workers that emitted bare JSON.
                if (line.Length > 0 && line[0] == '[')
                    return line;

                Trace.WriteLine("pymupdf.layout worker stdout: " + line);
            }

            return null;
        }

        static void StopWorker()
        {
            if (_worker == null)
                return;

            try
            {
                if (!_worker.HasExited)
                {
                    _worker.StandardInput.WriteLine("QUIT");
                    _worker.StandardInput.Flush();
                    if (!_worker.WaitForExit(5000))
                        _worker.Kill();
                }
            }
            catch
            {
                try { _worker.Kill(); } catch { }
            }
            finally
            {
                _worker.Dispose();
                _worker = null;
            }
        }

        static object GetLayoutForPage(Page page)
        {
            if (page == null)
                return null;

            Document doc = page.Parent;
            if (doc == null)
                return null;

            string path = ResolveDocumentPath(doc);
            if (string.IsNullOrEmpty(path))
                return null;

            lock (Gate)
            {
                EnsureWorker();

                _worker.StandardInput.WriteLine(
                    JsonConvert.SerializeObject(new { path, page = page.Number }));
                _worker.StandardInput.Flush();

                string payload;
                try
                {
                    payload = ReadWorkerJsonPayload(_worker);
                    if (string.IsNullOrEmpty(payload))
                        return null;

                    JToken parsed = JToken.Parse(payload);
                    if (!(parsed is JArray ja))
                        return null;

                    var rows = new List<object[]>(ja.Count);
                    foreach (JToken item in ja)
                    {
                        if (!(item is JArray row) || row.Count < 5)
                            continue;
                        rows.Add(new object[]
                        {
                            row[0].Value<float>(),
                            row[1].Value<float>(),
                            row[2].Value<float>(),
                            row[3].Value<float>(),
                            row[4].Value<string>() ?? "text",
                        });
                    }

                    return rows;
                }
                catch (JsonException ex)
                {
                    Console.Error.WriteLine(
                        "PDF4LLM: pymupdf.layout worker returned invalid JSON: "
                        + ex.Message);
                    return null;
                }
            }
        }

        static string ResolveDocumentPath(Document doc)
        {
            if (doc == null)
                return null;

            if (!string.IsNullOrEmpty(doc.Name)
                && !string.Equals(doc.Name, "<memory>", StringComparison.Ordinal)
                && File.Exists(doc.Name))
            {
                return doc.Name;
            }

            return TempDocumentPaths.GetValue(doc, d =>
            {
                string tmp = Path.Combine(
                    Path.GetTempPath(),
                    "pdf4llm_layout_" + Guid.NewGuid().ToString("N") + ".pdf");
                d.Save(tmp);
                return tmp;
            });
        }

        static string PythonExecutable =>
            Environment.GetEnvironmentVariable("PDF4LLM_PYTHON")
            ?? Environment.GetEnvironmentVariable("PYTHON")
            ?? LayoutPythonPaths.TryResolveVenvPython()
            ?? "python";

        static ProcessStartInfo CreatePythonStartInfo(string arguments)
        {
            return new ProcessStartInfo(PythonExecutable)
            {
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                RedirectStandardInput = false,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
        }

        static string QuoteArgument(string arg) =>
            "\"" + (arg ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }
}
