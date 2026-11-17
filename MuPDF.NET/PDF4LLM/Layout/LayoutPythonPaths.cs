using System;
using System.Collections.Generic;
using System.IO;

namespace MuPDF.NET.PDF4LLM.Layout
{
    /// <summary>Shared venv locations for the layout worker.</summary>
    internal static class LayoutPythonPaths
    {
        public const string VenvDirName = ".venv-layout";
        public const string ProjectVenvDirName = ".mupdf4llm-net-venv";

        /// <summary>
        /// Per-user venv created by the layout-worker setup
        /// (%LOCALAPPDATA%\MuPDF.NET\.venv-layout or ~/.local/share/mupdf.net/.venv-layout).
        /// </summary>
        public static string UserLocalVenvRoot()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                string localAppData = Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(localAppData, "MuPDF.NET", VenvDirName);
            }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".local", "share", "mupdf.net", VenvDirName);
        }

        /// <summary>Pre-4.0 venv folder from the deprecated PDF4LLM package (still probed at runtime).</summary>
        public static string LegacyPdf4LlmUserLocalVenvRoot()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                string localAppData = Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(localAppData, "MuPDF.NET.PDF4LLM", VenvDirName);
            }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".local", "share", "mupdf4llm.net", VenvDirName);
        }

        public static string VenvPython(string venvRoot) =>
            Environment.OSVersion.Platform == PlatformID.Win32NT
                ? Path.Combine(venvRoot, "Scripts", "python.exe")
                : Path.Combine(venvRoot, "bin", "python");

        public static string TryResolveVenvPython()
        {
            foreach (string venvRoot in CandidateVenvRoots())
            {
                string py = VenvPython(venvRoot);
                if (File.Exists(py))
                    return py;
            }

            return null;
        }

        public static IEnumerable<string> CandidateVenvRoots()
        {
            yield return UserLocalVenvRoot();
            yield return LegacyPdf4LlmUserLocalVenvRoot();

            foreach (string root in EnumerateSearchRoots())
            {
                yield return Path.Combine(root, ProjectVenvDirName);
                yield return Path.Combine(root, "MuPDF.NET", VenvDirName);
                yield return Path.Combine(root, "MuPDF.NET.PDF4LLM", VenvDirName);
            }
        }

        internal static IEnumerable<string> EnumerateSearchRoots()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string TryDir(string dir)
            {
                if (string.IsNullOrWhiteSpace(dir))
                    return null;
                try
                {
                    return Path.GetFullPath(dir);
                }
                catch
                {
                    return null;
                }
            }

            foreach (string start in new[]
            {
                TryDir(Environment.CurrentDirectory),
                TryDir(AppDomain.CurrentDomain.BaseDirectory),
            })
            {
                string walk = start;
                for (int i = 0; i < 13 && walk != null; i++)
                {
                    if (seen.Add(walk))
                        yield return walk;
                    walk = Directory.GetParent(walk)?.FullName;
                }
            }
        }

        static bool _setupHelpPrinted;
        static bool _versionMismatchWarningPrinted;

        /// <summary>Print setup instructions once when layout is requested but unavailable.</summary>
        internal static void PrintSetupHelp()
        {
            if (_setupHelpPrinted)
                return;
            _setupHelpPrinted = true;

            string venv = UserLocalVenvRoot();
            string venvPy = VenvPython(venv);
            string required = VersionInfo.RequiredPyMuPDFLayout;
            Console.Error.WriteLine(
                $"MuPDF.NET: pymupdf {required} layout is not installed; layout analysis is unavailable.\n" +
                "\n" +
                $"Debian/Ubuntu block pip into system Python (PEP 668 / externally-managed-environment).\n" +
                $"Do not run: python3 -m pip install pymupdf=={required}\n" +
                "Create a venv and pip into that interpreter.\n" +
                "\n" +
                "Preferred (creates the venv and installs):\n" +
                "  dotnet msbuild -t:MuPDFNetSetupLayoutPython\n" +
                "\n" +
                "Manual install:\n" +
                "  sudo apt install python3.11 python3.11-venv python3.11-full   # Ubuntu 22.04 (python3 is 3.10)\n" +
                "  sudo apt install python3-venv python3-pip python3-full        # Ubuntu 24.04+\n" +
                $"  rm -rf {venv}\n" +
                $"  python3 -m venv {venv}\n" +
                $"  {venvPy} -m pip install pymupdf=={required} pymupdf-layout=={required}\n" +
                "\n" +
                "On Ubuntu 22.04 use python3.11 -m venv instead of python3 -m venv.\n" +
                "\n" +
                "Then check:\n" +
                "  dotnet msbuild -t:MuPDFNetVerifyLayoutPython\n" +
                "\n" +
                $"Or set MUPDF_NET_PYTHON to a Python 3.11+ interpreter that already has pymupdf {required}.");
        }

        /// <summary>Warn once when the installed layout is older than expected.</summary>
        internal static void PrintVersionTooLowWarning(string requiredVersion, string installedVersion)
        {
            if (_versionMismatchWarningPrinted)
                return;
            _versionMismatchWarningPrinted = true;

            Console.Error.WriteLine(
                $"MuPDF.NET warning: pymupdf.layout {installedVersion} is installed; " +
                $"MuPDF.NET {VersionInfo.Version} expects pymupdf {requiredVersion} or newer. " +
                "Continuing with the installed version.\n" +
                "To install the expected version:\n" +
                "  dotnet msbuild -t:MuPDFNetSetupLayoutPython");
        }
    }
}