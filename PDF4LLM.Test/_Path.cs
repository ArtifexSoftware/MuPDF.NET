using System;
using System.IO;

namespace PDF4LLM.Test
{
    /// <summary>Test document paths (mirrors <c>MuPDF.NET.Test/_GenUtils.cs</c> <c>_Path</c>).</summary>
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
                ? Path.Combine(root, "TestDocuments", "PDF4LLM.Test", fileName)
                : Path.Combine(root, "TestDocuments", "PDF4LLM.Test", subFolder, fileName);
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
                ResolveSolutionRoot(), "TestDocuments", "PDF4LLM.Test", OutputFolderName, testClassName, fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return path;
        }

        /// <summary>Upstream fixture under <c>layout package-1.28.0/tests/</c> (fallback).</summary>
        public static string Pymupdf4llmTests(string fileName)
        {
            string root = ResolveSolutionRoot();
            foreach (string testsDir in new[]
            {
                Path.Combine(root, "pymupdf4llm-1.28.0", "tests"),
                Path.Combine(root, "pymupdf4llm-1.27.2.3", "tests"),
                Path.Combine(root, "pymupdf4llm", "tests"),
            })
            {
                string path = Path.Combine(testsDir, fileName);
                if (File.Exists(path))
                    return path;
            }

            throw new FileNotFoundException(
                $"Required pymupdf4llm test document not found: {fileName}");
        }

        /// <summary>Test class fixture, or upstream <c>layout package*/tests</c> when absent.</summary>
        public static string ForTestClassOrUpstream(string fileName, string testClassName)
        {
            string local = Path.Combine(
                ResolveSolutionRoot(), "TestDocuments", "PDF4LLM.Test", testClassName, fileName);
            if (File.Exists(local))
                return local;
            return Pymupdf4llmTests(fileName);
        }

        /// <summary>Returns <c>null</c> when the upstream layout package tests fixture is absent.</summary>
        public static string? TryPymupdf4llmTests(string fileName)
        {
            string root = ResolveSolutionRoot();
            foreach (string testsDir in new[]
            {
                Path.Combine(root, "pymupdf4llm-1.28.0", "tests"),
                Path.Combine(root, "pymupdf4llm-1.27.2.3", "tests"),
                Path.Combine(root, "pymupdf4llm", "tests"),
            })
            {
                string path = Path.Combine(testsDir, fileName);
                if (File.Exists(path))
                    return path;
            }

            return null;
        }

        /// <summary>Returns <c>null</c> when the fixture is not present locally or upstream.</summary>
        public static string? TryForTestClass(string fileName, string testClassName)
        {
            string local = Path.Combine(
                ResolveSolutionRoot(), "TestDocuments", "PDF4LLM.Test", testClassName, fileName);
            if (File.Exists(local))
                return local;

            return TryPymupdf4llmTests(fileName);
        }
    }
}