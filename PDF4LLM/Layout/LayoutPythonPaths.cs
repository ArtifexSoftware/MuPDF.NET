using System;
using System.Collections.Generic;
using System.IO;

namespace PDF4LLM.Layout
{
    /// <summary>Shared venv locations for the layout bridge Python bridge.</summary>
    internal static class LayoutPythonPaths
    {
        public const string VenvDirName = ".venv-layout";
        public const string ProjectVenvDirName = ".pdf4llm-venv";

        /// <summary>
        /// Per-user venv created by <c>setup_layout_python.py</c>
        /// (%LOCALAPPDATA%\PDF4LLM\.venv-layout or ~/.local/share/pdf4llm/.venv-layout).
        /// </summary>
        public static string UserLocalVenvRoot()
        {
            if (Environment.OSVersion.Platform == PlatformID.Win32NT)
            {
                string localAppData = Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(localAppData, "PDF4LLM", VenvDirName);
            }

            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".local", "share", "pdf4llm", VenvDirName);
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

            foreach (string root in EnumerateSearchRoots())
            {
                yield return Path.Combine(root, ProjectVenvDirName);
                yield return Path.Combine(root, "PDF4LLM", VenvDirName);
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
            Console.Error.WriteLine(
                "PDF4LLM: pymupdf-layout is not installed; layout analysis is unavailable.\n" +
                "\n" +
                "Install once per machine:\n" +
                "  dotnet msbuild -t:PDF4LLMSetupLayoutPython\n" +
                "\n" +
                "Or run the setup script from the PDF4LLM package / repo:\n" +
                "  python PDF4LLM/scripts/setup_layout_python.py\n" +
                "\n" +
                "On Debian/Ubuntu, install system packages first:\n" +
                "  sudo apt install python3-venv python3-pip\n" +
                "\n" +
                $"The setup script creates a venv at: {venv}\n" +
                "Or set PDF4LLM_PYTHON to a Python 3.10+ interpreter with pymupdf-layout installed.");
        }

        /// <summary>Warn once when the installed pymupdf-layout is older than expected.</summary>
        internal static void PrintVersionTooLowWarning(string requiredVersion, string installedVersion)
        {
            if (_versionMismatchWarningPrinted)
                return;
            _versionMismatchWarningPrinted = true;

            Console.Error.WriteLine(
                $"PDF4LLM warning: pymupdf-layout {installedVersion} is installed; " +
                $"PDF4LLM {VersionInfo.Version} expects pymupdf-layout {requiredVersion} or newer. " +
                "Continuing with the installed version.\n" +
                "To install the expected version:\n" +
                "  dotnet msbuild -t:PDF4LLMSetupLayoutPython");
        }
    }
}