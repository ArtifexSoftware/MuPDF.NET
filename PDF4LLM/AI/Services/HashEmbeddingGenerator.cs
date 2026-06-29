using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.AI;

namespace PDF4LLM.AI.Services;

/// <summary>Deterministic embeddings for development/tests (no cloud credentials).</summary>
public sealed class HashEmbeddingGenerator : IEmbeddingGenerator<string, Embedding<float>>
{
    private readonly int _dimensions;

    /// <param name="dimensions">Output embedding vector size (minimum 8).</param>
    public HashEmbeddingGenerator(int dimensions = 128)
    {
        if (dimensions < 8)
            throw new ArgumentOutOfRangeException(nameof(dimensions));
        _dimensions = dimensions;
    }

    public EmbeddingGeneratorMetadata Metadata { get; } =
        new("hash-embedding", new Uri("https://artifex.com/pdf4llm-ai"));

    /// <param name="values">Texts to embed.</param>
    /// <param name="options">Optional embedding generation options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
        IEnumerable<string> values,
        EmbeddingGenerationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var embeddings = new List<Embedding<float>>();
        foreach (string value in values)
        {
            cancellationToken.ThrowIfCancellationRequested();
            embeddings.Add(new Embedding<float>(HashToVector(value)));
        }

        return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(embeddings));
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose() { }

    private float[] HashToVector(string text)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? ""));
        var vector = new float[_dimensions];
        for (int i = 0; i < _dimensions; i++)
            vector[i] = (hash[i % hash.Length] / 255f) * 2f - 1f;
        return vector;
    }
}
