using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using MuPDF.NET;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MuPDF.NET.PDF4LLM.Layout
{
    /// <summary>worker that runs layout ONNX inference per page.</summary>
    internal static class PyMuPdfLayoutBridge
    {
        const string WorkerReadyToken = "READY";
        const string WorkerResultPrefix = "RESULT ";

        static readonly object Gate = new object();
        static readonly ConditionalWeakTable<Document, string> TempDocumentPaths =
            new ConditionalWeakTable<Document, string>();

        /// <summary>
        /// Optional <c>edge_threshold</c> for the next <c>page.get_layout</c> call in the worker.
        /// Set by <see cref="Helpers.LayoutParseHelpers.ReadPageLayoutRaw"/> around <see cref="Page.GetLayout"/>.
        /// </summary>
        internal static readonly System.Threading.AsyncLocal<float?> CurrentEdgeThreshold =
            new System.Threading.AsyncLocal<float?>();

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
        # get_layout must unset quad corrections here or GNN boxes diverge from to_json.
        pymupdf.TOOLS.unset_quad_corrections(True)
        activate = getattr(pymupdf.layout, 'activate', None)
        if activate:
            activate()
except Exception as exc:
    print('ERROR ' + json.dumps(str(exc)), flush=True)
    sys.exit(1)

print('READY', flush=True)

def serialize_layout_for_json(items):
    out = []
    for entry in items or []:
        if not isinstance(entry, dict):
            out.append(entry)
            continue
        row = {}
        for key, val in entry.items():
            if key == 'table_grid' and val is not None:
                row[key] = {
                    'h_lines': list(getattr(val, 'h_lines', val.get('h_lines', []) if isinstance(val, dict) else [])),
                    'v_lines': list(getattr(val, 'v_lines', val.get('v_lines', []) if isinstance(val, dict) else [])),
                }
            elif key == 'table_cells' and val is not None:
                cells = []
                for cell in val:
                    if isinstance(cell, dict):
                        cells.append(cell)
                    else:
                        cells.append({
                            'text': getattr(cell, 'text', ''),
                            'row': getattr(cell, 'row', 0),
                            'col': getattr(cell, 'col', 0),
                        })
                row[key] = cells
            elif isinstance(val, (list, tuple)):
                row[key] = list(val)
            else:
                row[key] = val
        out.append(row)
    return out

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
        edge_threshold = req.get('edge_threshold', None)
        doc = pymupdf.open(path)
        try:
            page = doc[page_no]
            with contextlib.redirect_stdout(sys.stderr):
                pymupdf.TOOLS.unset_quad_corrections(True)
                try:
                    if edge_threshold is not None:
                        page.get_layout(return_raw=True, edge_threshold=edge_threshold)
                    else:
                        page.get_layout(return_raw=True)
                except TypeError:
                    try:
                        page.get_layout(return_raw=True)
                    except TypeError:
                        page.get_layout()
            result = serialize_layout_for_json(page.layout_information or [])
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
                if (!_probeResult.HasValue)
                    _ = IsAvailable;
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

        /// <summary>
        /// Persist in-memory document changes (derotation, OCR, inserted
        /// contents) for the layout worker and invalidate cached layout.
        /// </summary>
        internal static void RefreshLayoutSnapshot(Document doc)
        {
            if (doc == null)
                return;

            lock (Gate)
            {
                WriteLayoutSnapshotTo(doc, EnsureLayoutSnapshotPath(doc));
                ClearLayoutCache(doc);
            }
        }

        static void ClearLayoutCache(Document doc)
        {
            if (doc == null)
                return;

            for (int i = 0; i < doc.PageCount; i++)
                doc.LoadPage(i).LayoutInformation = null;
        }

        static string EnsureLayoutSnapshotPath(Document doc) =>
            TempDocumentPaths.GetValue(doc, _ => Path.Combine(
                Path.GetTempPath(),
                "pdf4llm_layout_" + Guid.NewGuid().ToString("N") + ".pdf"));

        /// <summary>
        /// Materialize a PDF path the external layout worker can reopen.
        /// Non-PDF Office/HWP inputs are converted because the worker cannot inherit
        /// </summary>
        /// <c>MuPDF.NET.Office</c> <c>MuPDFOffice.Unlock</c> / SmartOffice handlers.

        static void WriteLayoutSnapshotTo(Document doc, string path)
        {
            byte[] pdf = doc.IsPDF ? doc.ToBytes() : doc.ConvertToPdf();
            File.WriteAllBytes(path, pdf);
        }

        static bool ProbePythonLayout()
        {
            string version = TryQueryLayoutVersion();
            if (string.IsNullOrEmpty(version))
                return false;

            string required = NormalizeVersion(VersionInfo.RequiredPyMuPDFLayout);
            string actual = NormalizeVersion(version);
            if (CompareVersions(actual, required) < 0)
                LayoutPythonPaths.PrintVersionTooLowWarning(
                    VersionInfo.RequiredPyMuPDFLayout,
                    version);

            _version = version;
            return true;
        }

        static string TryQueryLayoutVersion()
        {
            try
            {
                return RunPythonCapture("import pymupdf.layout; print(pymupdf.layout.version)");
            }
            catch
            {
                return null;
            }
        }

        static string NormalizeVersion(string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                return "";
            string[] parts = version.Trim().Split('-')[0].Split('.');
            if (parts.Length >= 3)
                return parts[0] + "." + parts[1] + "." + parts[2];
            return string.Join(".", parts);
        }

        /// <summary>Compare dotted numeric versions. Returns &lt;0, 0, or &gt;0.</summary>
        static int CompareVersions(string a, string b)
        {
            string[] pa = a.Split('.');
            string[] pb = b.Split('.');
            int len = Math.Max(pa.Length, pb.Length);
            for (int i = 0; i < len; i++)
            {
                int va = i < pa.Length && int.TryParse(pa[i], out int x) ? x : 0;
                int vb = i < pb.Length && int.TryParse(pb[i], out int y) ? y : 0;
                if (va != vb)
                    return va < vb ? -1 : 1;
            }
            return 0;
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

            lock (Gate)
            {
                EnsureWorker();

                // The worker reopens a PDF file and cannot see in-memory page
                // mutations. FindTables derotates the page (page_rotation_set0)
                // before get_layout so line-based cells and layout boxes share
                // a coordinate system; get_layout is in-process on that
                // page object. Rewrite the snapshot on every call so a stale
                // /Rotate, mediabox, or contents stream cannot gate the table.
                string path = EnsureLayoutSnapshotPath(doc);
                WriteLayoutSnapshotTo(doc, path);
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return null;

                var req = new Dictionary<string, object>
                {
                    ["path"] = path,
                    ["page"] = page.Number,
                };
                if (CurrentEdgeThreshold.Value.HasValue)
                    req["edge_threshold"] = CurrentEdgeThreshold.Value.Value;

                _worker.StandardInput.WriteLine(JsonConvert.SerializeObject(req));
                _worker.StandardInput.Flush();

                string payload;
                try
                {
                    payload = ReadWorkerJsonPayload(_worker);
                    if (string.IsNullOrEmpty(payload))
                        return null;

                    JToken parsed = JToken.Parse(payload);
                    if (parsed is JArray ja)
                        return ja;

                    return null;
                }
                catch (JsonException ex)
                {
                    Console.Error.WriteLine(
                        "MuPDF.NET: pymupdf.layout worker returned invalid JSON: "
                        + ex.Message);
                    return null;
                }
            }
        }

        static string PythonExecutable =>
            Environment.GetEnvironmentVariable("MUPDF_NET_PYTHON")
            ?? Environment.GetEnvironmentVariable("MuPDF4LLM_NET_PYTHON")
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
