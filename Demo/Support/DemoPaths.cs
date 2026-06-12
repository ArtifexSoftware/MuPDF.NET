namespace Demo
{
    /// <summary>
    /// Resolves paths under <c>TestDocuments/Demo/</c> (inputs) and <c>TestDocuments/Demo/_Output/</c> (generated files).
    /// Walks up from <see cref="AppContext.BaseDirectory"/> until <c>MuPDF.NET.sln</c> is found.
    /// </summary>
    internal static class DemoPaths
    {
        public static string RepoRoot()
        {
            var dir = AppContext.BaseDirectory;
            for (int i = 0; i < 12; i++)
            {
                if (string.IsNullOrEmpty(dir))
                    break;
                if (File.Exists(Path.Combine(dir, "MuPDF.NET.sln")))
                    return Path.GetFullPath(dir);
                dir = Directory.GetParent(dir)?.FullName;
            }
            throw new InvalidOperationException("Could not locate solution root (MuPDF.NET.sln). Run Demo from the repository.");
        }

        public static string Input(string relativePath) =>
            Path.GetFullPath(Path.Combine(RepoRoot(), "TestDocuments", "Demo", relativePath));

        public static string Output(string fileName)
        {
            string path = Path.Combine(RepoRoot(), "TestDocuments", "Demo", "_Output", fileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            return path;
        }
    }
}
