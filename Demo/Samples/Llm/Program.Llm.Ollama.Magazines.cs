namespace Demo
{
    /// <summary>
    /// Index <c>Magazines-PDF.pdf</c> (magazine types, uses, and parts) with local Ollama.
    /// </summary>
    internal partial class Program
    {
        internal static Task TestOllamaMagazines() =>
            RunOllamaDemoAsync(
                "TestOllamaMagazines",
                "Llm/Magazines-PDF.pdf",
                OllamaMagazineAskQuestions,
                OllamaMagazineSearchQueries);

        static readonly string[] OllamaMagazineAskQuestions =
        {
            "What is a magazine, according to the definition in this document?",
            "Name the ten types of magazines listed in the document.",
            "What do fashion magazines focus on?",
            "Which magazine is given as an example of a lifestyle magazine?",
            "What details does a magazine masthead contain?",
            "What is the difference between a magazine and a newspaper?",
            "What is the difference between a magazine, a journal, and a catalog?",
            "What standard magazine sizes are used in the US and UK?",
            "What type of paper is commonly used for magazine printing?",
            "What is the melting point of unobtainium?",
        };

        static readonly string[] OllamaMagazineSearchQueries =
        {
            "lifestyle magazine Rolling Stone",
            "magazine masthead editors writers designers",
            "standard magazine sizes 8.5 by 11",
        };
    }
}
