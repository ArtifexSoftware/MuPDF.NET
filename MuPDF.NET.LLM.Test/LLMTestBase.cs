using System.IO;
using MuPDF.NET;

namespace MuPDF.NET.LLM.Test
{
    /// <summary>
    /// Base class for MuPDF.NET.LLM tests
    /// </summary>
    public class LLMTestBase
    {
        protected string GetResourcePath(string relativePath)
        {
            // Get the test project directory
            string testDir = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            string projectDir = Path.GetFullPath(Path.Combine(testDir, "..", "..", ".."));
            return Path.Combine(projectDir, "resources", relativePath);
        }

        protected Document OpenTestDocument(string relativePath)
        {
            string fullPath = GetResourcePath(relativePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException($"Test resource not found: {fullPath}");
            return new Document(fullPath);
        }
    }
}
