using System.IO;
using MuPDF.NET;

namespace PDF4LLM.Test
{
    /// <summary>Base class for PDF4LLM tests (fixture lookup mirrors pymupdf4llm/tests paths).</summary>
    public class LLMTestBase
    {
        protected static string TestProjectDir =>
            Path.GetFullPath(Path.Combine(
                Path.GetDirectoryName(typeof(LLMTestBase).Assembly.Location)!,
                "..", "..", ".."));

        protected string GetResourcePath(string relativePath) =>
            Path.Combine(TestProjectDir, "resources", relativePath);

        /// <summary>pymupdf4llm/tests/&lt;name&gt;</summary>
        protected static string Pymupdf4llmTestsPath(string name) =>
            Path.GetFullPath(Path.Combine(TestProjectDir, "..", "pymupdf4llm", "tests", name));

        /// <summary>Repo tests/ (pymupdf4llm/tests/*.py <c>../../tests/</c>).</summary>
        protected static string RepoTestsPath(string name) =>
            Path.GetFullPath(Path.Combine(TestProjectDir, "..", "tests", name));

        protected static string DemoLlmPath(string name) =>
            Path.GetFullPath(Path.Combine(TestProjectDir, "..", "TestDocuments", "Demo", "Llm", name));

        protected string FixturePath(string name)
        {
            string fromResources = GetResourcePath(name);
            if (File.Exists(fromResources))
                return fromResources;
            string fromPymupdf4llm = Pymupdf4llmTestsPath(name);
            if (File.Exists(fromPymupdf4llm))
                return fromPymupdf4llm;
            string fromRepo = RepoTestsPath(name);
            if (File.Exists(fromRepo))
                return fromRepo;
            string fromDemo = DemoLlmPath(name);
            if (File.Exists(fromDemo))
                return fromDemo;
            return fromResources;
        }

        protected Document OpenTestDocument(string relativePath)
        {
            string fullPath = FixturePath(relativePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Test resource not found: {fullPath}");
            return new Document(fullPath);
        }

        protected const string SharedFolderName = "Shared";
        protected const string OutputFolderName = "_Output";

        protected static string ResolveSolutionRoot()
        {
            var dir = Path.GetDirectoryName(typeof(LLMTestBase).Assembly.Location)!;
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

        protected static string ResolveTestDocument(string fileName, string? subFolder = null)
        {
            string root = ResolveSolutionRoot();
            return string.IsNullOrWhiteSpace(subFolder)
                ? Path.Combine(root, "TestDocuments", "PDF4LLM.Test", fileName)
                : Path.Combine(root, "TestDocuments", "PDF4LLM.Test", subFolder, fileName);
        }

        protected static string RequireTestDocument(string fileName, string? subFolder = null)
        {
            string path = ResolveTestDocument(fileName, subFolder);
            if (!string.IsNullOrWhiteSpace(fileName) && !File.Exists(path))
                throw new FileNotFoundException($"Required test document not found: {path}");
            return path;
        }

        protected static string Shared(string fileName) =>
            RequireTestDocument(fileName, SharedFolderName);

        protected static string ForTestClass(string fileName, string testClassName) =>
            RequireTestDocument(fileName, testClassName);

        protected static string ForOutput(string fileName, string testClassName)
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
    }
}
