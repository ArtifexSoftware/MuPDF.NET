using PDF4LLM.AI.Models;

namespace PDF4LLM.AI.Abstractions;

/// <summary>Splits extracted pages into RAG-sized chunks (Phase 2 — Kernel Memory).</summary>
public interface IChunkingService
{
    /// <param name="pages">Extracted pages to partition into chunks.</param>
    IReadOnlyList<AiChunk> Chunk(IReadOnlyList<ExtractedPage> pages);
}
