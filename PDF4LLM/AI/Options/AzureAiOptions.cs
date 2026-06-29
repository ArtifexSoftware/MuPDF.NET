using Azure.AI.OpenAI;
using Microsoft.Extensions.AI;
using OpenAI.Chat;

namespace PDF4LLM.AI.Options;

/// <summary>Azure OpenAI + Azure AI Search settings from the PDF4LLM AI development plan.</summary>
public sealed class AzureAiOptions
{
    public required string OpenAiEndpoint { get; init; }
    public required string OpenAiApiKey { get; init; }
    public required string ChatDeploymentName { get; init; }
    public required string EmbeddingDeploymentName { get; init; }
    public string? SearchEndpoint { get; init; }
    public string? SearchApiKey { get; init; }
    public string? SearchIndexName { get; init; }
    public int EmbeddingDimensions { get; init; } = 1536;
    public bool UseLayout { get; init; }

    /// <summary>Build <see cref="MicrosoftAIConnectorOptions"/> for production Azure pipelines.</summary>
    public MicrosoftAIConnectorOptions ToConnectorOptions()
    {
        var azureClient = new AzureOpenAIClient(new Uri(OpenAiEndpoint), new Azure.AzureKeyCredential(OpenAiApiKey));

        IChatClient chatClient = azureClient
            .GetChatClient(ChatDeploymentName)
            .AsIChatClient();

        IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator = azureClient
            .GetEmbeddingClient(EmbeddingDeploymentName)
            .AsIEmbeddingGenerator();

        return new MicrosoftAIConnectorOptions
        {
            UseLayout = UseLayout,
            ChatClient = chatClient,
            EmbeddingGenerator = embeddingGenerator,
            EmbeddingDimensions = EmbeddingDimensions,
            AzureSearchEndpoint = SearchEndpoint,
            AzureSearchApiKey = SearchApiKey,
            AzureSearchIndexName = SearchIndexName,
        };
    }
}
