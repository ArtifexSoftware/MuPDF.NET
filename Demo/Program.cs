namespace Demo
{
    /// <summary>
    /// Demo entry point. With no arguments, every sample in <see cref="SampleMenu"/> runs (including <c>[diag]</c>).
    /// </summary>
    internal partial class Program
    {
        private static void Main(string[] args)
        {
            SampleMenu.Run(args);
            //TestTable2_();
        }

        private static void TestTable2_()
        {
            string testFilePath = Path.GetFullPath(@"D:\national-capitals.pdf");
            if (!File.Exists(testFilePath))
            {
                Console.WriteLine($"Test file not found: {testFilePath}");
                return;
            }

            const int iterations = 100;
            const int degreeOfParallelism = 10;
            int failures = 0;
            long totalTables = 0;

            Console.WriteLine("Multi-thread Utils.GetTables test");
            Console.WriteLine($"PDF: {testFilePath}");
            Console.WriteLine($"Iterations: {iterations}");
            Console.WriteLine($"Degree of parallelism: {degreeOfParallelism}");
            Console.WriteLine();

            Parallel.ForEach(
                Enumerable.Range(0, iterations),
                new ParallelOptions { MaxDegreeOfParallelism = degreeOfParallelism },
                iteration =>
                {
                    try
                    {
                        using var doc = new Document(testFilePath);
                        using var page = doc[0];

                        List<Table> tables = Utils.GetTables(
                            page,
                            clip: page.Rect,
                            vertical_strategy: iteration % 2 == 0 ? "lines" : "text",
                            horizontal_strategy: iteration % 2 == 0 ? "lines" : "text");

                        Interlocked.Add(ref totalTables, tables.Count);
                        Console.WriteLine($"Iteration {iteration + 1} (thread {Environment.CurrentManagedThreadId}): {totalTables} table(s)");
                    }
                    catch (Exception ex)
                    {
                        Interlocked.Increment(ref failures);
                        Console.WriteLine($"Iteration {iteration + 1} FAILED: {ex.Message}");
                    }
                });

            Console.WriteLine();
            Console.WriteLine(
                $"Completed: {iterations - failures}/{iterations} OK, tables found: {totalTables}, failures: {failures}");

            if (failures > 0)
                throw new InvalidOperationException($"{failures} iteration(s) failed.");
        }
    }
}
