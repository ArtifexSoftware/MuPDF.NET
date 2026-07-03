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
        }
    }
}
