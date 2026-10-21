using System.Net.Http;
using Microsoft.Extensions.AI;
using MuPDF.NET.PDF4LLM.AI;
using MuPDF.NET.PDF4LLM.AI.Models;
using MuPDF.NET.PDF4LLM.AI.Options;
using OllamaSharp;
using OllamaSharp.Models;

namespace Demo
{
    /// <summary>
    /// Index <c>national-capitals.pdf</c> with a local Ollama
    /// <see cref="IChatClient"/> and <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/>.
    /// Default chat model is <c>mistral</c> (8k context). Override with <c>OLLAMA_MODEL</c>.
    /// Not part of the default sample run (<c>OLLAMA_HOST</c> must be up).
    /// </summary>
    internal partial class Program
    {
        internal static Task TestOllama() =>
            RunOllamaDemoAsync(
                "TestOllama",
                "Llm/national-capitals.pdf",
                OllamaDemoAskQuestions,
                OllamaDemoSearchQueries);

        static async Task RunOllamaDemoAsync(
            string title,
            string relativePdf,
            IReadOnlyList<string> askQuestions,
            IReadOnlyList<string> searchQueries)
        {
            Console.WriteLine($"\n=== {title} (MuPDF.NET.PDF4LLM + Ollama) =======================");

            string host = Environment.GetEnvironmentVariable("OLLAMA_HOST") ?? "http://localhost:11434";
            string chatModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "mistral";
            //string chatModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "llama3.1";
            //string chatModel = Environment.GetEnvironmentVariable("OLLAMA_MODEL") ?? "qwen3";
            string embedModel = Environment.GetEnvironmentVariable("OLLAMA_EMBED_MODEL") ?? "nomic-embed-text";

            if (!Uri.TryCreate(host, UriKind.Absolute, out Uri? uri))
            {
                Console.Error.WriteLine($"Invalid OLLAMA_HOST '{host}'.");
                Environment.ExitCode = 1;
                return;
            }

            string pdfPath = DemoPaths.Input(relativePdf);
            string pdfName = Path.GetFileName(pdfPath);
            if (!File.Exists(pdfPath))
            {
                Console.WriteLine($"Skip: need {pdfName} under TestDocuments/Demo/Llm/.");
                return;
            }

            using HttpClient chatHttp = CreateOllamaHttp(uri);
            using HttpClient embedHttp = CreateOllamaHttp(uri);
            using OllamaApiClient chatOllama = new OllamaApiClient(chatHttp, chatModel);
            using OllamaApiClient embedOllama = new OllamaApiClient(embedHttp, embedModel);

            string? resolvedChat = await TryResolveOllamaModelAsync(chatOllama, uri, chatModel);
            if (resolvedChat == null)
            {
                Environment.ExitCode = 1;
                return;
            }

            chatModel = resolvedChat;
            string? resolvedEmbed = chatModel;
            if (!string.Equals(chatModel, embedModel, StringComparison.OrdinalIgnoreCase))
            {
                resolvedEmbed = await TryResolveOllamaModelAsync(embedOllama, uri, embedModel);
                if (resolvedEmbed == null)
                {
                    Environment.ExitCode = 1;
                    return;
                }
            }

            embedModel = resolvedEmbed;

            IChatClient chat = chatOllama;
            IEmbeddingGenerator<string, Embedding<float>> embed = embedOllama;

            Console.WriteLine($"Ollama {uri}  chat={chatModel}  embed={embedModel}");
            Console.WriteLine($"Indexing {pdfName} …");

            bool priorLayout = MuPDF4LLM.UseLayout;
            AiDocumentCollection aiDocs;
            try
            {
                MuPDF4LLM.SetUseLayout(true);
                var options = new MicrosoftAIConnectorOptions
                {
                    ChatClient = chat,
                    EmbeddingGenerator = embed,
                    UseLayout = true,
                    AskExcerptCount = 2,
                    AskMinScore = 0.35,
                    EmbedBatchSize = 4,
                    EmbedAskQuery = false,
                    MaxOutputTokens = 512,
                    LocalIndexPath = OllamaIndexPathFor(pdfName),
                    Trace = msg => Console.WriteLine("  " + msg),
                };
                aiDocs = await MuPDF4LLM.LoadAiAsync(new[] { pdfPath }, options);
            }
            catch (HttpRequestException ex)
            {
                Console.Error.WriteLine($"Indexing failed: {ex.Message}");
                if (ex.InnerException != null)
                    Console.Error.WriteLine(ex.InnerException.Message);
                if (ex.StatusCode == System.Net.HttpStatusCode.NotImplemented)
                {
                    Console.Error.WriteLine("That model cannot embed. Pull an embedding model:");
                    Console.Error.WriteLine("  ollama pull nomic-embed-text");
                }

                Environment.ExitCode = 1;
                return;
            }
            finally
            {
                MuPDF4LLM.SetUseLayout(priorLayout);
            }

            Console.WriteLine($"Indexed {aiDocs.ChunkCount} chunks from: {string.Join(", ", aiDocs.SourceFiles)}");
            Console.WriteLine();

            Console.WriteLine("--- Ask ---");
            foreach (string question in askQuestions)
                await PrintOllamaAskAsync(aiDocs, question);

            Console.WriteLine("--- Search ---");
            foreach (string query in searchQueries)
                await PrintOllamaSearchAsync(aiDocs, query);

            if (Console.IsInputRedirected)
                return;

            Console.WriteLine($"Ask another question about {pdfName}. Type 'exit' to quit.");
            Console.WriteLine();

            while (true)
            {
                Console.Write("> ");
                string? line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    continue;
                if (line.Equals("exit", StringComparison.OrdinalIgnoreCase))
                    break;

                Console.WriteLine();
                await PrintOllamaAskAsync(aiDocs, line);
            }
        }

        static string? OllamaIndexPathFor(string pdfName)
        {
            string? index = Environment.GetEnvironmentVariable("PDF4LLM_INDEX");
            if (string.IsNullOrWhiteSpace(index))
                return null;

            string dir = Path.GetDirectoryName(index) ?? "";
            string stem = Path.GetFileNameWithoutExtension(index);
            string pdfStem = Path.GetFileNameWithoutExtension(pdfName);
            return Path.Combine(dir, stem + "-" + pdfStem + Path.GetExtension(index));
        }

        static async Task PrintOllamaAskAsync(AiDocumentCollection aiDocs, string question)
        {
            Console.WriteLine($"Ask: {question}");
            try
            {
                GroundedAnswer answer = await aiDocs.AskDetailedAsync(question);
                if (answer.InsufficientEvidence)
                    Console.WriteLine("(insufficient evidence)");
                Console.WriteLine(answer.Text);
                if (!string.IsNullOrEmpty(answer.Warning))
                    Console.WriteLine($"  warning: {answer.Warning}");
                foreach (Citation cite in answer.Citations)
                    Console.WriteLine($"  cite: {cite.SourceFileName} p{cite.PageNumber} \"{cite.Quote}\"");
                foreach (RetrievedPassage passage in answer.Evidence)
                    Console.WriteLine($"  evidence: {passage.SourceFileName} p{passage.PageNumber} id={passage.SourceId}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Ask failed: {ex.Message}");
                if (ex.InnerException != null)
                    Console.Error.WriteLine(ex.InnerException.Message);
            }

            Console.WriteLine();
        }

        static async Task PrintOllamaSummarizeAsync(
            AiDocumentCollection aiDocs,
            string source,
            int? pageNumber = null)
        {
            Console.WriteLine(pageNumber is int page
                ? $"Summarize: {source} p{page}"
                : $"Summarize: {source}");
            Console.WriteLine("(local LLM; parts print as they run — not stuck)");
            try
            {
                var progress = new Progress<string>(msg => Console.WriteLine(msg));
                string answer = await aiDocs.SummarizeAsync(source, progress, pageNumber: pageNumber);
                Console.WriteLine(answer);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Summarize failed: {ex.Message}");
                if (ex.InnerException != null)
                    Console.Error.WriteLine(ex.InnerException.Message);
            }

            Console.WriteLine();
        }

        static async Task PrintOllamaSearchAsync(AiDocumentCollection aiDocs, string query)
        {
            Console.WriteLine($"Search: {query}");
            try
            {
                IReadOnlyList<SearchResult> hits = await aiDocs.SearchAsync(query, topK: 10);
                Console.WriteLine($"  Ranked all {aiDocs.ChunkCount} chunks; showing top {hits.Count}.");
                for (int i = 0; i < hits.Count; i++)
                {
                    SearchResult hit = hits[i];
                    if (i > 0)
                        Console.WriteLine();
                    Console.WriteLine($"  {i + 1}. [{hit.Score:F3}] {hit.Chunk.SourceFileName} p{hit.Chunk.PageNumber}");
                    foreach (string line in SplitSearchPreviewLines(hit.Chunk.Text))
                        Console.WriteLine($"      {line}");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Search failed: {ex.Message}");
                if (ex.InnerException != null)
                    Console.Error.WriteLine(ex.InnerException.Message);
            }

            Console.WriteLine();
        }

        static IEnumerable<string> SplitSearchPreviewLines(string text)
        {
            string[] lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Trim().Split('\n');
            for (int i = 0; i < lines.Length; i++)
                lines[i] = lines[i].TrimEnd();
            return lines;
        }

        static readonly string[] OllamaDemoAskQuestions =
        {
            "What is the capital of South Sudan?",
            "What was the population of Juba in 2017?",
            "What is the capital of Canada?",
            "What percentage of Gibraltar's population lives in the capital?",
            "What is the capital of Tanzania?",
            "What is the listed population of Vatican City?",
            "What is the capital of the United States?",
            "What percentage of the United States population lives in Washington D.C.?",
            "What is the melting point of unobtainium?",
        };

        static readonly string[] OllamaDemoSearchQueries =
        {
            "capital of South Sudan",
            "Vatican City population",
            "Ottawa Canada capital",
        };

        static HttpClient CreateOllamaHttp(Uri uri)
        {
            return new HttpClient
            {
                BaseAddress = uri,
                Timeout = TimeSpan.FromMinutes(10),
            };
        }

        static async Task<string?> TryResolveOllamaModelAsync(OllamaApiClient ollama, Uri uri, string model)
        {
            try
            {
                if (!await ollama.IsRunningAsync())
                {
                    WriteOllamaUnreachable(uri);
                    return null;
                }

                IEnumerable<Model> local = await ollama.ListLocalModelsAsync();
                string? installed = ResolveInstalledModelName(local, model);
                if (installed == null)
                {
                    Console.Error.WriteLine($"Model '{model}' is not installed.");
                    Console.Error.WriteLine($"Install it with: ollama pull {model}");
                    return null;
                }

                if (!string.Equals(installed, model, StringComparison.OrdinalIgnoreCase))
                    Console.WriteLine($"Using installed model '{installed}' for '{model}'.");

                ollama.SelectedModel = installed;
                return installed;
            }
            catch (HttpRequestException ex)
            {
                WriteOllamaUnreachable(uri);
                Console.Error.WriteLine(ex.Message);
                return null;
            }
        }

        static string? ResolveInstalledModelName(IEnumerable<Model> models, string requested)
        {
            var names = new List<string>();
            foreach (Model model in models)
            {
                AddInstalledName(names, model.Name);
                AddInstalledName(names, model.ModelName);
            }

            foreach (string name in names)
            {
                if (string.Equals(name, requested, StringComparison.OrdinalIgnoreCase))
                    return name;
            }

            string latest = requested + ":latest";
            foreach (string name in names)
            {
                if (string.Equals(name, latest, StringComparison.OrdinalIgnoreCase))
                    return name;
            }

            foreach (string name in names)
            {
                if (OllamaNameMatches(name, requested))
                    return name;
            }

            return null;
        }

        static void AddInstalledName(List<string> names, string candidate)
        {
            if (string.IsNullOrEmpty(candidate))
                return;
            if (!names.Exists(n => string.Equals(n, candidate, StringComparison.OrdinalIgnoreCase)))
                names.Add(candidate);
        }

        static bool OllamaNameMatches(string installed, string requested)
        {
            if (string.IsNullOrEmpty(installed))
                return false;
            if (string.Equals(installed, requested, StringComparison.OrdinalIgnoreCase))
                return true;

            int colon = installed.IndexOf(':');
            string baseName = colon < 0 ? installed : installed.Substring(0, colon);
            return string.Equals(baseName, requested, StringComparison.OrdinalIgnoreCase);
        }

        static void WriteOllamaUnreachable(Uri uri)
        {
            Console.Error.WriteLine($"Cannot reach Ollama at {uri}.");
            Console.Error.WriteLine("Start the Ollama app or `ollama serve`, then retry.");
        }
    }
}
